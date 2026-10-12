namespace BookCatalog.Domain.Books;

public class Loan
{
    public Guid Id { get; init; }
    public Guid BookCopyId { get; init; }
    public required string BorrowerName { get; init; }
    public DateTime BorrowedAt { get; init; }
    public DateTime? ReturnedAt { get; set; }

    public bool IsActive => ReturnedAt is null;

    public BookCopy BookCopy { get; init; } = null!;
}