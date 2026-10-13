using BookCatalog.Application.Abstractions;
using BookCatalog.Application.Books;
using BookCatalog.Application.Common;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;
using BookCatalog.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Infrastructure.Persistence;

public sealed class EfBookRepository(BookCatalogDbContext db) : IBookRepository
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<PagedResult<Book>> GetPageAsync(BookQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Book> books = db.Books.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            var pattern = $"%{EscapeLikePattern(query.Title)}%";
            books = books.Where(b => EF.Functions.ILike(b.Title, pattern, LikeEscapeCharacter));
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            var pattern = $"%{EscapeLikePattern(query.Author)}%";
            books = books.Where(b => EF.Functions.ILike(b.Author, pattern, LikeEscapeCharacter));
        }

        if (!string.IsNullOrWhiteSpace(query.Genre))
        {
            var pattern = EscapeLikePattern(query.Genre);
            books = books.Where(b => b.Genre != null && EF.Functions.ILike(b.Genre, pattern, LikeEscapeCharacter));
        }

        if (query.PublishedFrom is { } publishedFrom)
            books = books.Where(b => b.PublishedYear >= publishedFrom);

        if (query.PublishedTo is { } publishedTo)
            books = books.Where(b => b.PublishedYear <= publishedTo);

        var totalCount = await books.CountAsync(cancellationToken);

        var items = await books
            .OrderBy(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Book>(items, query.Page, query.PageSize, totalCount);
    }

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> IsbnExistsAsync(string isbn, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var books = db.Books.Where(b => b.Isbn == isbn);

        if (excludeId is { } id)
            books = books.Where(b => b.Id != id);

        return books.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        db.Books.Add(book);
        await SaveChangesAsync(book, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        db.Books.Update(book);

        try
        {
            await SaveChangesAsync(book, cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedRows = await db.Books
                .Where(b => b.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            return deletedRows > 0;
        }
        catch (Exception ex) when (PostgresErrors.IsForeignKeyViolation(ex))     
        {
            throw new BookHasLoanHistoryException(id);                           
        }
    }

    private async Task SaveChangesAsync(Book book, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex, BookConfiguration.IsbnUniqueIndex))
        {
            throw new DuplicateIsbnException(book.Isbn!);
        }
    }

    private static string EscapeLikePattern(string value) =>
        value
            .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter)
            .Replace("%", LikeEscapeCharacter + "%")
            .Replace("_", LikeEscapeCharacter + "_");
}