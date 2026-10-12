using System.ComponentModel.DataAnnotations;

namespace BookCatalog.Api.Contracts;

public record CreateCopyRequest(
    [Required, StringLength(20)] string InventoryNumber);