namespace BookCatalog.Api.Contracts;

public record UpdateBookRequest(
    string Title,
    string Author,
    string? Isbn,
    int PublishedYear,
    string? Genre);