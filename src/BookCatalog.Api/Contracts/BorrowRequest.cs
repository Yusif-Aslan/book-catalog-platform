using System.ComponentModel.DataAnnotations;

namespace BookCatalog.Api.Contracts;

public record BorrowRequest(
    [Required, StringLength(100)] string BorrowerName);