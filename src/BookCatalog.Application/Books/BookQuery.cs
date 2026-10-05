namespace BookCatalog.Application.Books;

public sealed record BookQuery(
    int Page,
    int PageSize,
    string? Title = null,
    string? Author = null,
    string? Genre = null,
    int? PublishedFrom = null,
    int? PublishedTo = null);