using BookCatalog.Application.Abstractions;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;
using Microsoft.Extensions.Logging;
using BookCatalog.Application.Common;

namespace BookCatalog.Application.Books;

public class BookService(
    IBookRepository repository,
    TimeProvider timeProvider,
    ILogger<BookService> logger) : IBookService
{
    public async Task<PagedResult<Book>> GetPageAsync(BookQuery query, CancellationToken cancellationToken = default)
    {
        var page = await repository.GetPageAsync(query, cancellationToken);

        logger.LogDebug(
            "Retrieved page {Page} with {ItemCount} of {TotalCount} books",
            page.Page, page.Items.Count, page.TotalCount);

        return page;
    }

    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await repository.GetByIdAsync(id, cancellationToken);

        if (book is null)
            logger.LogDebug("Book {BookId} not found", id);

        return book;
    }

    public async Task<Book> CreateAsync(BookDetails details, CancellationToken cancellationToken = default) 
    {
        var isbn = NormalizeIsbn(details.Isbn);

        await EnsureIsbnIsUniqueAsync(isbn, excludeId: null, cancellationToken); 

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = details.Title,
            Author = details.Author,
            Isbn = isbn,
            PublishedYear = details.PublishedYear,
            Genre = details.Genre,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(book, cancellationToken);

        logger.LogInformation(
            "Created book {BookId} with title {Title} by {Author}",
            book.Id, book.Title, book.Author);

        return book;
    }

    public async Task<Book?> UpdateAsync(Guid id, BookDetails details, CancellationToken cancellationToken = default)
    {
        var isbn = NormalizeIsbn(details.Isbn);

        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            logger.LogInformation("Update skipped: book {BookId} not found", id);
            return null;
        }

        await EnsureIsbnIsUniqueAsync(isbn, excludeId: id, cancellationToken);

        var updated = new Book
        {
            Id = existing.Id,
            Title = details.Title,
            Author = details.Author,
            Isbn = isbn,
            PublishedYear = details.PublishedYear,
            Genre = details.Genre,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        if (!await repository.UpdateAsync(updated, cancellationToken))
        {
            logger.LogInformation("Update skipped: book {BookId} was removed concurrently", id);
            return null;
        }

        logger.LogInformation("Updated book {BookId}", id);
        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);

        if (deleted)
            logger.LogInformation("Deleted book {BookId}", id);
        else
            logger.LogInformation("Delete skipped: book {BookId} not found", id);

        return deleted;
    }

    private async Task EnsureIsbnIsUniqueAsync(string? isbn, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (isbn is null)
            return;

        if (await repository.IsbnExistsAsync(isbn, excludeId, cancellationToken))
        {
            logger.LogWarning("Rejected book with duplicate ISBN {Isbn}", isbn);
            throw new DuplicateIsbnException(isbn);
        }
    }

    private static string? NormalizeIsbn(string? isbn) =>
        isbn?.Replace("-", "").Replace(" ", "").ToUpperInvariant();
}