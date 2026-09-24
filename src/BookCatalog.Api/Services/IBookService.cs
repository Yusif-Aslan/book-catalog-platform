using BookCatalog.Api.Contracts;
using BookCatalog.Api.Models;

namespace BookCatalog.Api.Services;

public interface IBookService
{
    Task<IReadOnlyList<Book>> GetAllAsync();
    Task<Book?> GetByIdAsync(Guid id);
    Task<Book> CreateAsync(CreateBookRequest request);
    Task<Book?> UpdateAsync(Guid id, UpdateBookRequest request);
    Task<bool> DeleteAsync(Guid id);
}