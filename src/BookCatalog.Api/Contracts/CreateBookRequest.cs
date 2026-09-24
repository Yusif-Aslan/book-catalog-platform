namespace BookCatalog.Api.Contracts;

public record CreateBookRequest(
    string Title,
    string Author,
    string? Isbn,
    int PublishedYear,
    string? Genre);