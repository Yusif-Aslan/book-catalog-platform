using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookCatalog.IntegrationTests.Infrastructure;

public sealed class UnreachableDatabaseApiFactory : WebApplicationFactory<Program>
{
    private const string UnreachableConnectionString =
        "Host=127.0.0.1;Port=1;Database=bookcatalog;Username=bookcatalog;Password=bookcatalog;Timeout=2";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:BookCatalog", UnreachableConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Database:MaxRetryCount", "1");
        builder.UseSetting("Database:MaxRetryDelaySeconds", "1");
    }
}