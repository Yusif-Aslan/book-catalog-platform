namespace BookCatalog.Application.Exceptions;

public sealed class BookHasLoanHistoryException(Guid bookId)
    : ConflictException("Book has loan history", $"Book '{bookId}' cannot be deleted because its copies have loan history.")
{
    public Guid BookId { get; } = bookId;
}