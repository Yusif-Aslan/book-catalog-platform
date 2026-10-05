using BookCatalog.Application.Books;
using BookCatalog.Domain.Books;

namespace BookCatalog.Api.Services;

public interface IBookService
{
    Task<IReadOnlyList<Book>> GetAllAsync();
    Task<Book?> GetByIdAsync(Guid id);
    Task<Book> CreateAsync(BookDetails details);
    Task<Book?> UpdateAsync(Guid id, BookDetails details);
    Task<bool> DeleteAsync(Guid id);
}