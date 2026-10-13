using BookCatalog.Application.Abstractions;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;
using BookCatalog.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Infrastructure.Persistence;

public sealed class EfLendingRepository(BookCatalogDbContext db) : ILendingRepository
{
    public Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        db.Books.AnyAsync(b => b.Id == bookId, cancellationToken);

    public Task<bool> CopyExistsAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        db.BookCopies.AnyAsync(c => c.Id == copyId, cancellationToken);

    public async Task<IReadOnlyList<BookCopy>> GetCopiesAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        await db.BookCopies
            .AsNoTracking()
            .Include(c => c.Loans.Where(l => l.ReturnedAt == null))
            .Where(c => c.BookId == bookId)
            .OrderBy(c => c.InventoryNumber)
            .ToListAsync(cancellationToken);

    public async Task AddCopyAsync(BookCopy copy, CancellationToken cancellationToken = default)
    {
        db.BookCopies.Add(copy);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex, BookCopyConfiguration.InventoryNumberUniqueIndex))
        {
            throw new DuplicateInventoryNumberException(copy.InventoryNumber);
        }
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await db.Database.BeginTransactionAsync(cancellationToken));

    public async Task<bool> LockCopyAsync(Guid copyId, CancellationToken cancellationToken = default)
    {
        var lockedIds = await db.Database
            .SqlQuery<Guid>($"""SELECT "Id" AS "Value" FROM "BookCopies" WHERE "Id" = {copyId} FOR UPDATE""")
            .ToListAsync(cancellationToken);

        return lockedIds.Count > 0;
    }

    public Task<bool> HasActiveLoanAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        db.Loans.AnyAsync(l => l.BookCopyId == copyId && l.ReturnedAt == null, cancellationToken);

    public async Task AddLoanAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        db.Loans.Add(loan);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex, LoanConfiguration.ActiveLoanUniqueIndex))
        {
            throw new CopyAlreadyBorrowedException(loan.BookCopyId);
        }
    }

    public Task<Loan?> GetActiveLoanAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        db.Loans.FirstOrDefaultAsync(l => l.BookCopyId == copyId && l.ReturnedAt == null, cancellationToken);

    public async Task<IReadOnlyList<Loan>> GetLoanHistoryAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        await db.Loans
            .AsNoTracking()
            .Where(l => l.BookCopyId == copyId)
            .OrderByDescending(l => l.BorrowedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}