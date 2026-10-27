namespace BookCatalog.Infrastructure;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "BookCatalog";

    public string ConnectionString { get; set; } = string.Empty;

    public bool MigrateOnStartup { get; set; }

    public int CommandTimeoutSeconds { get; set; } = 30;

    public int MaxRetryCount { get; set; } = 3;

    public int MaxRetryDelaySeconds { get; set; } = 5;
}
