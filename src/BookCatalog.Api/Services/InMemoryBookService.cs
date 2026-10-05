using System.Collections.Concurrent;
using BookCatalog.Api.Contracts;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;

namespace BookCatalog.Api.Services;

public class InMemoryBookService(ILogger<InMemoryBookService> logger) : IBookService
{
    private readonly ConcurrentDictionary<Guid, Book> _books = new();
    private readonly Lock _writeLock = new();

    public Task<IReadOnlyList<Book>> GetAllAsync()
    {
        IReadOnlyList<Book> books = _books.Values
            .OrderBy(b => b.CreatedAt)
            .ToList();

        logger.LogDebug("Retrieved {BookCount} books", books.Count);
        return Task.FromResult(books);
    }

    public Task<Book?> GetByIdAsync(Guid id)
    {
        if (!_books.TryGetValue(id, out var book))
            logger.LogDebug("Book {BookId} not found", id);

        return Task.FromResult(book);
    }

    public Task<Book> CreateAsync(CreateBookRequest request)
    {
        var isbn = NormalizeIsbn(request.Isbn);

        lock (_writeLock)
        {
            EnsureIsbnIsUnique(isbn, excludeId: null);

            var now = DateTime.UtcNow;
            var book = new Book
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Author = request.Author,
                Isbn = isbn,
                PublishedYear = request.PublishedYear,
                Genre = request.Genre,
                CreatedAt = now,
                UpdatedAt = now
            };

            _books[book.Id] = book;

            logger.LogInformation(
                "Created book {BookId} with title {Title} by {Author}",
                book.Id, book.Title, book.Author);

            return Task.FromResult(book);
        }
    }

    public Task<Book?> UpdateAsync(Guid id, UpdateBookRequest request)
    {
        var isbn = NormalizeIsbn(request.Isbn);

        lock (_writeLock)
        {
            if (!_books.TryGetValue(id, out var existing))
            {
                logger.LogInformation("Update skipped: book {BookId} not found", id);
                return Task.FromResult<Book?>(null);
            }

            EnsureIsbnIsUnique(isbn, excludeId: id);

            var updated = new Book
            {
                Id = existing.Id,
                Title = request.Title,
                Author = request.Author,
                Isbn = isbn,
                PublishedYear = request.PublishedYear,
                Genre = request.Genre,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
            
            if (!_books.TryUpdate(id, updated, existing))
            {
                logger.LogInformation("Update skipped: book {BookId} was removed concurrently", id);
                return Task.FromResult<Book?>(null);
            }

            logger.LogInformation("Updated book {BookId}", id);
            return Task.FromResult<Book?>(updated);
        }
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var deleted = _books.TryRemove(id, out _);

        if (deleted)
            logger.LogInformation("Deleted book {BookId}", id);
        else
            logger.LogInformation("Delete skipped: book {BookId} not found", id);

        return Task.FromResult(deleted);
    }

    private void EnsureIsbnIsUnique(string? isbn, Guid? excludeId)
    {
        if (isbn is null)
            return;

        if (_books.Values.Any(b => b.Id != excludeId && b.Isbn == isbn))
        {
            logger.LogWarning("Rejected book with duplicate ISBN {Isbn}", isbn);
            throw new DuplicateIsbnException(isbn);
        }
    }

    private static string? NormalizeIsbn(string? isbn) =>
        isbn?.Replace("-", "").Replace(" ", "").ToUpperInvariant();
}