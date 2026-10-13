using Npgsql;

namespace BookCatalog.Infrastructure.Persistence;

internal static class PostgresErrors
{
    public static bool IsUniqueViolation(Exception exception, string constraintName) =>
        FindPostgresException(exception) is { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && postgres.ConstraintName == constraintName;

    public static bool IsForeignKeyViolation(Exception exception) =>
        FindPostgresException(exception) is { SqlState: PostgresErrorCodes.ForeignKeyViolation };

    private static PostgresException? FindPostgresException(Exception exception) =>
        exception as PostgresException ?? exception.InnerException as PostgresException;
}