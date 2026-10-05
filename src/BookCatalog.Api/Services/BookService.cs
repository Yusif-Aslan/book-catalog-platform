using BookCatalog.Application.Abstractions;
using BookCatalog.Application.Books;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;

namespace BookCatalog.Api.Services;

public class BookService(
    IBookRepository repository,
    ILogger<BookService> logger) : IBookService
{
    public async Task<IReadOnlyList<Book>> GetAllAsync()
    {
        var books = await repository.GetAllAsync();

        logger.LogDebug("Retrieved {BookCount} books", books.Count);
        return books;
    }

    public async Task<Book?> GetByIdAsync(Guid id)
    {
        var book = await repository.GetByIdAsync(id);

        if (book is null)
            logger.LogDebug("Book {BookId} not found", id);

        return book;
    }

    public async Task<Book> CreateAsync(BookDetails details)
    {
        var isbn = NormalizeIsbn(details.Isbn);

        await EnsureIsbnIsUniqueAsync(isbn, excludeId: null);

        var now = DateTime.UtcNow;
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

        await repository.AddAsync(book);

        logger.LogInformation(
            "Created book {BookId} with title {Title} by {Author}",
            book.Id, book.Title, book.Author);

        return book;
    }

    public async Task<Book?> UpdateAsync(Guid id, BookDetails details)
    {
        var isbn = NormalizeIsbn(details.Isbn);

        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
        {
            logger.LogInformation("Update skipped: book {BookId} not found", id);
            return null;
        }

        await EnsureIsbnIsUniqueAsync(isbn, excludeId: id);

        var updated = new Book
        {
            Id = existing.Id,
            Title = details.Title,
            Author = details.Author,
            Isbn = isbn,
            PublishedYear = details.PublishedYear,
            Genre = details.Genre,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        if (!await repository.UpdateAsync(updated))
        {
            logger.LogInformation("Update skipped: book {BookId} was removed concurrently", id);
            return null;
        }

        logger.LogInformation("Updated book {BookId}", id);
        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deleted = await repository.DeleteAsync(id);

        if (deleted)
            logger.LogInformation("Deleted book {BookId}", id);
        else
            logger.LogInformation("Delete skipped: book {BookId} not found", id);

        return deleted;
    }

    private async Task EnsureIsbnIsUniqueAsync(string? isbn, Guid? excludeId)
    {
        if (isbn is null)
            return;

        if (await repository.IsbnExistsAsync(isbn, excludeId))
        {
            logger.LogWarning("Rejected book with duplicate ISBN {Isbn}", isbn);
            throw new DuplicateIsbnException(isbn);
        }
    }

    private static string? NormalizeIsbn(string? isbn) =>
        isbn?.Replace("-", "").Replace(" ", "").ToUpperInvariant();
}