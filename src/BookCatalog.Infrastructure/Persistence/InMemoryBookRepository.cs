using System.Collections.Concurrent;
using BookCatalog.Application.Abstractions;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;
using BookCatalog.Application.Books;
using BookCatalog.Application.Common;

namespace BookCatalog.Infrastructure.Persistence;

public class InMemoryBookRepository : IBookRepository
{
    private readonly ConcurrentDictionary<Guid, Book> _books = new();
    private readonly Lock _writeLock = new();

    public Task<PagedResult<Book>> GetPageAsync(BookQuery query, CancellationToken cancellationToken = default)
    {
        var books = _books.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Title))
            books = books.Where(b => b.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.Author))
            books = books.Where(b => b.Author.Contains(query.Author, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.Genre))
            books = books.Where(b => string.Equals(b.Genre, query.Genre, StringComparison.OrdinalIgnoreCase));

        if (query.PublishedFrom is not null)
            books = books.Where(b => b.PublishedYear >= query.PublishedFrom);

        if (query.PublishedTo is not null)
            books = books.Where(b => b.PublishedYear <= query.PublishedTo);

        var matching = books
            .OrderBy(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .ToList();

        var items = matching
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<Book>(items, query.Page, query.PageSize, matching.Count));
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