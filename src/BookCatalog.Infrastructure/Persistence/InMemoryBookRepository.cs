using System.Collections.Concurrent;
using BookCatalog.Application.Abstractions;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;

namespace BookCatalog.Infrastructure.Persistence;

public class InMemoryBookRepository : IBookRepository
{
    private readonly ConcurrentDictionary<Guid, Book> _books = new();
    private readonly Lock _writeLock = new();

    public Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Book> books = _books.Values
            .OrderBy(b => b.CreatedAt)
            .ToList();

        return Task.FromResult(books);
    }

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _books.TryGetValue(id, out var book);
        return Task.FromResult(book);
    }

    public Task<bool> IsbnExistsAsync(string isbn, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _books.Values.Any(b => b.Id != excludeId && b.Isbn == isbn);
        return Task.FromResult(exists);
    }

    public Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        lock (_writeLock)
        {
            EnsureIsbnIsUnique(book);

            if (!_books.TryAdd(book.Id, book))
                throw new InvalidOperationException($"A book with id '{book.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        lock (_writeLock)
        {
            if (!_books.ContainsKey(book.Id))
                return Task.FromResult(false);

            EnsureIsbnIsUnique(book);

            _books[book.Id] = book;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_writeLock)
        {
            var deleted = _books.TryRemove(id, out _);
            return Task.FromResult(deleted);
        }
    }

    private void EnsureIsbnIsUnique(Book book)
    {
        if (book.Isbn is null)
            return;

        if (_books.Values.Any(b => b.Id != book.Id && b.Isbn == book.Isbn))
            throw new DuplicateIsbnException(book.Isbn);
    }
}