using BookCatalog.Application.Books;
using BookCatalog.Application.Common;
using BookCatalog.Domain.Books;

namespace BookCatalog.Application.Abstractions;

public interface IBookRepository
{
    Task<PagedResult<Book>> GetPageAsync(BookQuery query, CancellationToken cancellationToken = default);

    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsbnExistsAsync(string isbn, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Book book, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Book book, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}