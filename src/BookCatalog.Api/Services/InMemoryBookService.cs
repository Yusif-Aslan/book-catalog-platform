using System.Collections.Concurrent;
using BookCatalog.Api.Contracts;
using BookCatalog.Api.Models;
using BookCatalog.Api.Exceptions;
namespace BookCatalog.Api.Services;

public class InMemoryBookService : IBookService
{
    private readonly ConcurrentDictionary<Guid, Book> _books = new();
    private readonly Lock _writeLock = new();

    public Task<IReadOnlyList<Book>> GetAllAsync()
    {
        IReadOnlyList<Book> books = _books.Values
            .OrderBy(b => b.CreatedAt)
            .ToList();
        return Task.FromResult(books);
    }

    public Task<Book?> GetByIdAsync(Guid id)
    {
        _books.TryGetValue(id, out var book);
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
            return Task.FromResult(book);
        }
    }

    public Task<Book?> UpdateAsync(Guid id, UpdateBookRequest request)
    {
        var isbn = NormalizeIsbn(request.Isbn);

        lock (_writeLock)
        {
            if (!_books.TryGetValue(id, out var existing))
                return Task.FromResult<Book?>(null);

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

            // TryUpdate fails if the book was deleted in the meantime
            var success = _books.TryUpdate(id, updated, existing);
            return Task.FromResult(success ? updated : null);
        }
    }

    private void EnsureIsbnIsUnique(string? isbn, Guid? excludeId)
    {
        if (isbn is null)
            return;

        if (_books.Values.Any(b => b.Id != excludeId && b.Isbn == isbn))
            throw new DuplicateIsbnException(isbn);
    }

    private static string? NormalizeIsbn(string? isbn) =>
        isbn?.Replace("-", "").Replace(" ", "").ToUpperInvariant();

    public Task<bool> DeleteAsync(Guid id)
    {
        return Task.FromResult(_books.TryRemove(id, out _));
    }
}