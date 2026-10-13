using BookCatalog.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookCatalog.Infrastructure.Persistence;

internal sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}