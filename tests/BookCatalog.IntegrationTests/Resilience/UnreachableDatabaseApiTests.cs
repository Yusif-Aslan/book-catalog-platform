using System.Net;
using BookCatalog.IntegrationTests.Infrastructure;

namespace BookCatalog.IntegrationTests.Resilience;

public class UnreachableDatabaseApiTests(UnreachableDatabaseApiFactory factory)
    : IClassFixture<UnreachableDatabaseApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBooks_WhenDatabaseIsUnreachable_Returns503ProblemWithRetryAfter()
    {
        var response = await _client.GetAsync("/api/books");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Headers.RetryAfter);
    }
}
