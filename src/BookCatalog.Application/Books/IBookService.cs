using BookCatalog.Application.Common;
using BookCatalog.Domain.Books;

namespace BookCatalog.Application.Books;

public interface IBookService
{
    Task<PagedResult<Book>> GetPageAsync(BookQuery query);
    Task<Book?> GetByIdAsync(Guid id);
    Task<Book> CreateAsync(BookDetails details);
    Task<Book?> UpdateAsync(Guid id, BookDetails details);
    Task<bool> DeleteAsync(Guid id);
}