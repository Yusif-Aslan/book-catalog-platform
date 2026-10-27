using BookCatalog.Application.Abstractions;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;
using Microsoft.Extensions.Logging;

namespace BookCatalog.Application.Lending;

public class LendingService(
    ILendingRepository repository,
    TimeProvider timeProvider,
    ILogger<LendingService> logger) : ILendingService
{
    public async Task<IReadOnlyList<BookCopy>?> GetCopiesAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        if (!await repository.BookExistsAsync(bookId, cancellationToken))
        {
            logger.LogDebug("Book {BookId} not found", bookId);
            return null;
        }

        return await repository.GetCopiesAsync(bookId, cancellationToken);
    }

    public async Task<BookCopy?> AddCopyAsync(Guid bookId, string inventoryNumber, CancellationToken cancellationToken = default)
    {
        if (!await repository.BookExistsAsync(bookId, cancellationToken))
        {
            logger.LogInformation("Add copy skipped: book {BookId} not found", bookId);
            return null;
        }

        var copy = new BookCopy
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            InventoryNumber = inventoryNumber.Trim(),
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        await repository.AddCopyAsync(copy, cancellationToken);

        logger.LogInformation(
            "Added copy {CopyId} with inventory number {InventoryNumber} to book {BookId}",
            copy.Id, copy.InventoryNumber, bookId);

        return copy;
    }

    public async Task<Loan?> BorrowAsync(Guid copyId, string borrowerName, CancellationToken cancellationToken = default)
    {
        await using var transaction = await repository.BeginTransactionAsync(cancellationToken);

        if (!await repository.LockCopyAsync(copyId, cancellationToken))
        {
            logger.LogInformation("Borrow skipped: copy {CopyId} not found", copyId);
            return null;
        }

        if (await repository.HasActiveLoanAsync(copyId, cancellationToken))
        {
            logger.LogInformation("Rejected borrow: copy {CopyId} is already borrowed", copyId);
            throw new CopyAlreadyBorrowedException(copyId);
        }

        var loan = new Loan
        {
            Id = Guid.NewGuid(),
            BookCopyId = copyId,
            BorrowerName = borrowerName.Trim(),
            BorrowedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        await repository.AddLoanAsync(loan, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Copy {CopyId} borrowed, loan {LoanId}", copyId, loan.Id);
        return loan;
    }

    public async Task<Loan?> ReturnAsync(Guid copyId, CancellationToken cancellationToken = default)
    {
        var loan = await repository.GetActiveLoanAsync(copyId, cancellationToken);

        if (loan is null)
        {
            if (!await repository.CopyExistsAsync(copyId, cancellationToken))
            {
                logger.LogInformation("Return skipped: copy {CopyId} not found", copyId);
                return null;
            }

            logger.LogInformation("Rejected return: copy {CopyId} is not borrowed", copyId);
            throw new CopyNotBorrowedException(copyId);
        }

        loan.ReturnedAt = timeProvider.GetUtcNow().UtcDateTime;
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Copy {CopyId} returned, loan {LoanId}", copyId, loan.Id);
        return loan;
    }

    public async Task<IReadOnlyList<Loan>?> GetLoanHistoryAsync(Guid copyId, CancellationToken cancellationToken = default)
    {
        if (!await repository.CopyExistsAsync(copyId, cancellationToken))
        {
            logger.LogDebug("Copy {CopyId} not found", copyId);
            return null;
        }

        return await repository.GetLoanHistoryAsync(copyId, cancellationToken);
    }
}