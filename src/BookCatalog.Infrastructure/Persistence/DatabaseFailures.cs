using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace BookCatalog.Infrastructure.Persistence;

public static class DatabaseFailures
{
    public static bool IsUnavailable(Exception exception) =>
        exception is RetryLimitExceededException
        || FindNpgsqlException(exception) is { IsTransient: true };

    private static NpgsqlException? FindNpgsqlException(Exception exception) =>
        exception as NpgsqlException ?? exception.InnerException as NpgsqlException;
}
