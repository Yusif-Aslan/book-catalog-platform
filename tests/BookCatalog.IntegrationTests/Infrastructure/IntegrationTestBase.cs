using System.Net.Http.Json;
using BookCatalog.Api.Contracts;

namespace BookCatalog.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private const string ProblemJsonMediaType = "application/problem+json";

    private readonly BookCatalogApiFactory _factory;

    protected IntegrationTestBase(BookCatalogApiFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();
    }

    protected HttpClient Client { get; }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<BookResponse> CreateBookAsync(string title = "Clean Code", string? isbn = null)
    {
        var response = await Client.PostAsJsonAsync("/api/books", new
        {
            title,
            author = "Robert C. Martin",
            isbn,
            publishedYear = 2008,
            genre = "Programming"
        });

        response.EnsureSuccessStatusCode();
        return await ReadAsync<BookResponse>(response);
    }

    protected async Task<BookCopyResponse> AddCopyAsync(Guid bookId, string inventoryNumber = "INV-001")
    {
        var response = await Client.PostAsJsonAsync($"/api/books/{bookId}/copies", new { inventoryNumber });

        response.EnsureSuccessStatusCode();
        return await ReadAsync<BookCopyResponse>(response);
    }

    protected Task<HttpResponseMessage> BorrowAsync(Guid copyId, string borrowerName = "Alice") =>
        Client.PostAsJsonAsync($"/api/copies/{copyId}/borrow", new { borrowerName });

    protected Task<HttpResponseMessage> ReturnAsync(Guid copyId) =>
        Client.PostAsync($"/api/copies/{copyId}/return", null);

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>()
        ?? throw new InvalidOperationException($"Response body could not be read as {typeof(T).Name}.");

    protected static Task<ProblemResponse> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(ProblemJsonMediaType, response.Content.Headers.ContentType?.MediaType);
        return ReadAsync<ProblemResponse>(response);
    }

    protected static Task<ValidationProblemResponse> ReadValidationProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(ProblemJsonMediaType, response.Content.Headers.ContentType?.MediaType);
        return ReadAsync<ValidationProblemResponse>(response);
    }
}