using BookCatalog.Application.Books;
using BookCatalog.Domain.Books;

namespace BookCatalog.Api.Contracts;

public static class BookMappings
{
    public static BookDetails ToDetails(this CreateBookRequest request) =>
        new(request.Title, request.Author, request.Isbn, request.PublishedYear, request.Genre);

    public static BookDetails ToDetails(this UpdateBookRequest request) =>
        new(request.Title, request.Author, request.Isbn, request.PublishedYear, request.Genre);

    public static BookResponse ToResponse(this Book book) =>
        new(
            book.Id,
            book.Title,
            book.Author,
            book.Isbn,
            book.PublishedYear,
            book.Genre,
            book.CreatedAt,
            book.UpdatedAt);
}