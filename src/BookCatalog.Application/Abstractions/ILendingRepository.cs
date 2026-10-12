using BookCatalog.Domain.Books;

namespace BookCatalog.Application.Abstractions;

public interface ILendingRepository
{
    Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<bool> CopyExistsAsync(Guid copyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookCopy>> GetCopiesAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task AddCopyAsync(BookCopy copy, CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<bool> LockCopyAsync(Guid copyId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveLoanAsync(Guid copyId, CancellationToken cancellationToken = default);

    Task AddLoanAsync(Loan loan, CancellationToken cancellationToken = default);

    Task<Loan?> GetActiveLoanAsync(Guid copyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Loan>> GetLoanHistoryAsync(Guid copyId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}