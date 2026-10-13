namespace BookCatalog.Domain.Books;

public class BookCopy
{
    public Guid Id { get; init; }
    public Guid BookId { get; init; }
    public required string InventoryNumber { get; init; }
    public DateTime CreatedAt { get; init; }

    public Book Book { get; init; } = null!;
    public List<Loan> Loans { get; init; } = [];
}