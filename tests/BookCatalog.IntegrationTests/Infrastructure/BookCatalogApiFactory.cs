using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace BookCatalog.IntegrationTests.Infrastructure;

public sealed class BookCatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    private NpgsqlConnection? _connection;
    private Respawner? _respawner;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        _ = Server;

        _connection = new NpgsqlConnection(_database.GetConnectionString());
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    public Task ResetDatabaseAsync()
    {
        if (_respawner is null || _connection is null)
            throw new InvalidOperationException("The test database has not been initialized.");

        return _respawner.ResetAsync(_connection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:BookCatalog", _database.GetConnectionString());
        builder.UseSetting("Database:MigrateOnStartup", "true");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();

        await _database.DisposeAsync();
    }
}