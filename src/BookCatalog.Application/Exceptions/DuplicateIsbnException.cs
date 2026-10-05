namespace BookCatalog.Application.Exceptions;

public sealed class DuplicateIsbnException(string isbn)
    : ConflictException("Duplicate ISBN", $"A book with ISBN '{isbn}' already exists.")
{
    public string Isbn { get; } = isbn;
}