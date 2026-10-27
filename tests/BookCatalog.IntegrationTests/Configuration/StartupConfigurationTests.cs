using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookCatalog.IntegrationTests.Configuration;

public class StartupConfigurationTests
{
    [Fact]
    public async Task Startup_WithoutConnectionString_FailsWithMessageNamingTheSetting()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var misconfigured = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:BookCatalog", "");
        });

        var exception = Record.Exception(() => misconfigured.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("ConnectionStrings:BookCatalog is required.", exception.ToString());
    }

    [Fact]
    public async Task Startup_WithInvalidCommandTimeout_FailsWithMessageNamingTheSetting()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var misconfigured = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:BookCatalog", "Host=localhost;Database=bookcatalog");
            builder.UseSetting("Database:CommandTimeoutSeconds", "0");
        });

        var exception = Record.Exception(() => misconfigured.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("Database:CommandTimeoutSeconds must be between 1 and 300, but was 0.", exception.ToString());
    }
}
