using Microsoft.Extensions.Options;
using Npgsql;

namespace BookCatalog.Infrastructure;

public sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    private const string ConnectionStringKey = "ConnectionStrings:" + DatabaseOptions.ConnectionStringName;

    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var errors = new List<string>();

        ValidateConnectionString(options.ConnectionString, errors);
        ValidateRange(nameof(DatabaseOptions.CommandTimeoutSeconds), options.CommandTimeoutSeconds, 1, 300, errors);

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateConnectionString(string connectionString, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add($"{ConnectionStringKey} is required.");
            return;
        }

        NpgsqlConnectionStringBuilder builder;

        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            errors.Add($"{ConnectionStringKey} is not a valid PostgreSQL connection string: {ex.Message}");
            return;
        }

        if (string.IsNullOrWhiteSpace(builder.Host))
            errors.Add($"{ConnectionStringKey} must specify Host.");

        if (string.IsNullOrWhiteSpace(builder.Database))
            errors.Add($"{ConnectionStringKey} must specify Database.");
    }

    private static void ValidateRange(string setting, int value, int min, int max, List<string> errors)
    {
        if (value < min || value > max)
            errors.Add($"{DatabaseOptions.SectionName}:{setting} must be between {min} and {max}, but was {value}.");
    }
}
