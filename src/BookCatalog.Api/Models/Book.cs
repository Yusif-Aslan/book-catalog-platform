namespace BookCatalog.Api.Models;

public class Book
{
    public Guid Id { get; init; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public string? Isbn { get; set; }
    public int PublishedYear { get; set; }
    public string? Genre { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
}