using System.Net;
using System.Net.Http.Json;
using BookCatalog.Api.Contracts;
using BookCatalog.IntegrationTests.Infrastructure;

namespace BookCatalog.IntegrationTests.Books;

[Collection(IntegrationTestCollection.Name)]
public class BooksApiTests(BookCatalogApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Guid MissingId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Create_WithValidRequest_Returns201AndBookCanBeFetchedFromLocation()
    {
        var response = await Client.PostAsJsonAsync("/api/books", new
        {
            title = "Clean Code",
            author = "Robert C. Martin",
            isbn = "978-0-13-235088-4",
            publishedYear = 2008,
            genre = "Programming"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadAsync<BookResponse>(response);
        Assert.Equal("9780132350884", created.Isbn);

        var fetched = await Client.GetFromJsonAsync<BookResponse>(response.Headers.Location);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Clean Code", fetched.Title);
        Assert.Equal("9780132350884", fetched.Isbn);
    }

    [Fact]
    public async Task Create_WithInvalidFields_Returns400WithErrorForEachField()
    {
        var response = await Client.PostAsJsonAsync("/api/books", new
        {
            title = "",
            author = "",
            isbn = "978-0-13-235088-5",
            publishedYear = 3000,
            genre = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadValidationProblemAsync(response);
        Assert.True(problem.HasErrorFor("Title"));
        Assert.True(problem.HasErrorFor("Author"));
        Assert.True(problem.HasErrorFor("Isbn"));
        Assert.True(problem.HasErrorFor("PublishedYear"));
    }

    [Fact]
    public async Task Create_WithIsbnOfExistingBookInDifferentFormat_Returns409()
    {
        await CreateBookAsync(isbn: "978-0-13-235088-4");

        var response = await Client.PostAsJsonAsync("/api/books", new
        {
            title = "Clean Code (copy)",
            author = "Robert C. Martin",
            isbn = "9780132350884",
            publishedYear = 2008,
            genre = "Programming"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("Duplicate ISBN", problem.Title);
        Assert.NotNull(problem.TraceId);
    }

    [Fact]
    public async Task GetById_WhenBookDoesNotExist_Returns404Problem()
    {
        var response = await Client.GetAsync($"/api/books/{MissingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task Update_WithValidRequest_Returns200AndStoresChanges()
    {
        var book = await CreateBookAsync();

        var response = await Client.PutAsJsonAsync($"/api/books/{book.Id}", new
        {
            title = "The Pragmatic Programmer",
            author = "David Thomas, Andrew Hunt",
            isbn = "978-0-13-595705-9",
            publishedYear = 2019,
            genre = "Programming"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await Client.GetFromJsonAsync<BookResponse>($"/api/books/{book.Id}");
        Assert.NotNull(stored);
        Assert.Equal("The Pragmatic Programmer", stored.Title);
        Assert.Equal("9780135957059", stored.Isbn);
        Assert.Equal(2019, stored.PublishedYear);
    }

    [Fact]
    public async Task Update_WhenBookDoesNotExist_Returns404()
    {
        var response = await Client.PutAsJsonAsync($"/api/books/{MissingId}", new
        {
            title = "Missing",
            author = "Nobody",
            isbn = (string?)null,
            publishedYear = 2000,
            genre = (string?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenBookExists_Returns204AndBookIsGone()
    {
        var book = await CreateBookAsync();

        var response = await Client.DeleteAsync($"/api/books/{book.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/books/{book.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithTitleFilterAndPaging_ReturnsRequestedPageOfMatchingBooks()
    {
        await CreateBookAsync("Clean Code");
        await CreateBookAsync("Clean Architecture");
        await CreateBookAsync("Refactoring");

        var page = await Client.GetFromJsonAsync<PagedResponse<BookResponse>>(
            "/api/books?title=clean&page=2&pageSize=1");

        Assert.NotNull(page);
        var book = Assert.Single(page.Items);
        Assert.Equal("Clean Architecture", book.Title);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
    }
}