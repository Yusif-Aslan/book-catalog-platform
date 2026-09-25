using System.ComponentModel.DataAnnotations;
using BookCatalog.Api.Validation;

namespace BookCatalog.Api.Contracts;

public record CreateBookRequest(
    [Required, StringLength(200)] string Title,
    [Required, StringLength(100)] string Author,
    [Isbn] string? Isbn,
    [PublicationYear] int PublishedYear,
    [StringLength(50)] string? Genre);