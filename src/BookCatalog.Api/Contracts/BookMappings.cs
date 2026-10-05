using BookCatalog.Application.Books;

namespace BookCatalog.Api.Contracts;

public static class BookMappings
{
    public static BookDetails ToDetails(this CreateBookRequest request) =>
        new(request.Title, request.Author, request.Isbn, request.PublishedYear, request.Genre);

    public static BookDetails ToDetails(this UpdateBookRequest request) =>
        new(request.Title, request.Author, request.Isbn, request.PublishedYear, request.Genre);
}