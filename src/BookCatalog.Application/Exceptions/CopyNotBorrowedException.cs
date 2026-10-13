namespace BookCatalog.Application.Exceptions;

public sealed class CopyNotBorrowedException(Guid copyId)
    : ConflictException("Copy not borrowed", $"Copy '{copyId}' is not borrowed.")
{
    public Guid CopyId { get; } = copyId;
}