using System.Net.Sockets;
using BookCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace BookCatalog.UnitTests.Infrastructure;

public class DatabaseFailuresTests
{
    [Fact]
    public void IsUnavailable_WhenRetriesAreExhausted_ReturnsTrue()
    {
        var exception = new RetryLimitExceededException("Retries exhausted");

        Assert.True(DatabaseFailures.IsUnavailable(exception));
    }

    [Fact]
    public void IsUnavailable_WhenConnectionFailsWithNetworkError_ReturnsTrue()
    {
        var exception = new NpgsqlException("Failed to connect", new SocketException());

        Assert.True(DatabaseFailures.IsUnavailable(exception));
    }

    [Fact]
    public void IsUnavailable_WhenTransientErrorIsWrapped_ReturnsTrue()
    {
        var exception = new InvalidOperationException(
            "Transient failure",
            new NpgsqlException("Failed to connect", new SocketException()));

        Assert.True(DatabaseFailures.IsUnavailable(exception));
    }

    [Fact]
    public void IsUnavailable_ForUnrelatedException_ReturnsFalse()
    {
        Assert.False(DatabaseFailures.IsUnavailable(new InvalidOperationException("Bug")));
    }
}
