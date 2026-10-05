namespace BookCatalog.Application.Books;

public sealed record BookDetails(
    string Title,
    string Author,
    string? Isbn,
    int PublishedYear,
    string? Genre);