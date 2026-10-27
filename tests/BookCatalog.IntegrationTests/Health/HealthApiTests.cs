using System.Net;
using BookCatalog.IntegrationTests.Infrastructure;

namespace BookCatalog.IntegrationTests.Health;

[Collection(IntegrationTestCollection.Name)]
public class HealthApiTests(BookCatalogApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Live_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_WhenDatabaseIsReachable_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}