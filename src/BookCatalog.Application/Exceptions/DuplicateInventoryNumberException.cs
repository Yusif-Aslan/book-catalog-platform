namespace BookCatalog.Application.Exceptions;

public sealed class DuplicateInventoryNumberException(string inventoryNumber)
    : ConflictException("Duplicate inventory number", $"A copy with inventory number '{inventoryNumber}' already exists.")
{
    public string InventoryNumber { get; } = inventoryNumber;
}