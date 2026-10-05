namespace BookCatalog.Application.Exceptions;

public abstract class ConflictException(string title, string message) : Exception(message)
{
    public string Title { get; } = title;
}