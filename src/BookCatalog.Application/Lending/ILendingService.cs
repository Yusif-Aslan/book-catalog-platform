using BookCatalog.Domain.Books;

namespace BookCatalog.Application.Lending;

public interface ILendingService
{
    Task<IReadOnlyList<BookCopy>?> GetCopiesAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task<BookCopy?> AddCopyAsync(Guid bookId, string inventoryNumber, CancellationToken cancellationToken = default);
    Task<Loan?> BorrowAsync(Guid copyId, string borrowerName, CancellationToken cancellationToken = default);
    Task<Loan?> ReturnAsync(Guid copyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Loan>?> GetLoanHistoryAsync(Guid copyId, CancellationToken cancellationToken = default);
}