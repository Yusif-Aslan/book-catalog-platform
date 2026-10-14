using System.Net;
using System.Net.Http.Json;
using BookCatalog.Api.Contracts;
using BookCatalog.IntegrationTests.Infrastructure;

namespace BookCatalog.IntegrationTests.Lending;

[Collection(IntegrationTestCollection.Name)]
public class LendingApiTests(BookCatalogApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Guid MissingId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task AddCopy_ToExistingBook_Returns201AndCopyIsAvailable()
    {
        var book = await CreateBookAsync();

        var response = await Client.PostAsJsonAsync($"/api/books/{book.Id}/copies", new { inventoryNumber = "INV-001" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var copy = await GetSingleCopyAsync(book.Id);
        Assert.Equal("INV-001", copy.InventoryNumber);
        Assert.True(copy.IsAvailable);
    }

    [Fact]
    public async Task AddCopy_WithInventoryNumberOfExistingCopy_Returns409()
    {
        var book = await CreateBookAsync();
        await AddCopyAsync(book.Id, "INV-001");

        var response = await Client.PostAsJsonAsync($"/api/books/{book.Id}/copies", new { inventoryNumber = "INV-001" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await ReadProblemAsync(response);
    }

    [Fact]
    public async Task AddCopy_ToMissingBook_Returns404()
    {
        var response = await Client.PostAsJsonAsync($"/api/books/{MissingId}/copies", new { inventoryNumber = "INV-001" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Borrow_AvailableCopy_Returns201AndCopyBecomesUnavailable()
    {
        var copy = await CreateAvailableCopyAsync();

        var response = await BorrowAsync(copy.Id, "Alice");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var loan = await ReadAsync<LoanResponse>(response);
        Assert.Equal(copy.Id, loan.BookCopyId);
        Assert.Equal("Alice", loan.BorrowerName);
        Assert.Null(loan.ReturnedAt);

        var storedCopy = await GetSingleCopyAsync(copy.BookId);
        Assert.False(storedCopy.IsAvailable);
    }

    [Fact]
    public async Task Borrow_CopyThatIsAlreadyBorrowed_Returns409()
    {
        var copy = await CreateAvailableCopyAsync();
        (await BorrowAsync(copy.Id, "Alice")).EnsureSuccessStatusCode();

        var response = await BorrowAsync(copy.Id, "Bob");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await ReadProblemAsync(response);
    }

    [Fact]
    public async Task Borrow_SameCopyConcurrently_OnlyOneRequestSucceeds()
    {
        var copy = await CreateAvailableCopyAsync();

        var responses = await Task.WhenAll(
            Enumerable.Range(1, 5).Select(i => BorrowAsync(copy.Id, $"Borrower {i}")));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(4, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Borrow_WithoutBorrowerName_Returns400()
    {
        var copy = await CreateAvailableCopyAsync();

        var response = await BorrowAsync(copy.Id, "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadValidationProblemAsync(response);
        Assert.True(problem.HasErrorFor("BorrowerName"));
    }

    [Fact]
    public async Task Borrow_MissingCopy_Returns404()
    {
        var response = await BorrowAsync(MissingId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Return_BorrowedCopy_Returns200AndCopyIsAvailableAgain()
    {
        var copy = await CreateAvailableCopyAsync();
        (await BorrowAsync(copy.Id)).EnsureSuccessStatusCode();

        var response = await ReturnAsync(copy.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loan = await ReadAsync<LoanResponse>(response);
        Assert.NotNull(loan.ReturnedAt);

        var storedCopy = await GetSingleCopyAsync(copy.BookId);
        Assert.True(storedCopy.IsAvailable);
    }

    [Fact]
    public async Task Return_CopyThatIsNotBorrowed_Returns409()
    {
        var copy = await CreateAvailableCopyAsync();

        var response = await ReturnAsync(copy.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await ReadProblemAsync(response);
    }

    [Fact]
    public async Task GetLoanHistory_AfterTwoLoans_ReturnsBothNewestFirst()
    {
        var copy = await CreateAvailableCopyAsync();
        (await BorrowAsync(copy.Id, "Alice")).EnsureSuccessStatusCode();
        (await ReturnAsync(copy.Id)).EnsureSuccessStatusCode();
        (await BorrowAsync(copy.Id, "Bob")).EnsureSuccessStatusCode();

        var history = await Client.GetFromJsonAsync<List<LoanResponse>>($"/api/copies/{copy.Id}/loans");

        Assert.NotNull(history);
        Assert.Equal(["Bob", "Alice"], history.Select(l => l.BorrowerName));
        Assert.Null(history[0].ReturnedAt);
        Assert.NotNull(history[1].ReturnedAt);
    }

    [Fact]
    public async Task DeleteBook_WithLoanHistory_Returns409AndKeepsBook()
    {
        var copy = await CreateAvailableCopyAsync();
        (await BorrowAsync(copy.Id)).EnsureSuccessStatusCode();
        (await ReturnAsync(copy.Id)).EnsureSuccessStatusCode();

        var response = await Client.DeleteAsync($"/api/books/{copy.BookId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/books/{copy.BookId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    private async Task<BookCopyResponse> CreateAvailableCopyAsync()
    {
        var book = await CreateBookAsync();
        return await AddCopyAsync(book.Id);
    }

    private async Task<BookCopyResponse> GetSingleCopyAsync(Guid bookId)
    {
        var copies = await Client.GetFromJsonAsync<List<BookCopyResponse>>($"/api/books/{bookId}/copies");
        Assert.NotNull(copies);
        return Assert.Single(copies);
    }
}