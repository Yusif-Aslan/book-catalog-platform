namespace BookCatalog.Application.Exceptions;

public sealed class CopyAlreadyBorrowedException(Guid copyId)
    : ConflictException("Copy already borrowed", $"Copy '{copyId}' is already borrowed.")
{
    public Guid CopyId { get; } = copyId;
}