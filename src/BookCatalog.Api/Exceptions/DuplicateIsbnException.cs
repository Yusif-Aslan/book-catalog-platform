namespace BookCatalog.Api.Exceptions;

public class DuplicateIsbnException(string isbn)
    : Exception($"A book with ISBN '{isbn}' already exists.")
{
    public string Isbn { get; } = isbn;
}