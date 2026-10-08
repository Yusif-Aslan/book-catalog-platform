using BookCatalog.Application.Common;
using BookCatalog.Domain.Books;

namespace BookCatalog.Application.Books;

public interface IBookService
{
    Task<PagedResult<Book>> GetPageAsync(BookQuery query, CancellationToken cancellationToken = default);
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Book> CreateAsync(BookDetails details, CancellationToken cancellationToken = default);
    Task<Book?> UpdateAsync(Guid id, BookDetails details, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}