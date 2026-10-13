namespace BookCatalog.Api.Contracts;

public record BookCopyResponse(
    Guid Id,
    Guid BookId,
    string InventoryNumber,
    bool IsAvailable,
    DateTime CreatedAt);