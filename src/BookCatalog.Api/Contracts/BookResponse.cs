namespace BookCatalog.Api.Contracts;

public sealed record BookResponse(
    Guid Id,
    string Title,
    string Author,
    string? Isbn,
    int PublishedYear,
    string? Genre,
    DateTime CreatedAt,
    DateTime UpdatedAt);