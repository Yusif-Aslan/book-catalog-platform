using System.Collections.Concurrent;
using BookCatalog.Api.Contracts;
using BookCatalog.Api.Models;

namespace BookCatalog.Api.Services;

public class InMemoryBookService : IBookService
{
    private readonly ConcurrentDictionary<Guid, Book> _books = new();

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
        var now = DateTime.UtcNow;
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Author = request.Author,
            Isbn = request.Isbn,
            PublishedYear = request.PublishedYear,
            Genre = request.Genre,
            CreatedAt = now,
            UpdatedAt = now
        };

        _books[book.Id] = book;
        return Task.FromResult(book);
    }

    public Task<Book?> UpdateAsync(Guid id, UpdateBookRequest request)
    {
        if (!_books.TryGetValue(id, out var existing))
            return Task.FromResult<Book?>(null);

        var updated = new Book
        {
            Id = existing.Id,
            Title = request.Title,
            Author = request.Author,
            Isbn = request.Isbn,
            PublishedYear = request.PublishedYear,
            Genre = request.Genre,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
        
        var success = _books.TryUpdate(id, updated, existing);
        return Task.FromResult(success ? updated : null);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return Task.FromResult(_books.TryRemove(id, out _));
    }
}