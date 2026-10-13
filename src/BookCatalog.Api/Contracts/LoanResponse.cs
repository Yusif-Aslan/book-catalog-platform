namespace BookCatalog.Api.Contracts;

public record LoanResponse(
    Guid Id,
    Guid BookCopyId,
    string BorrowerName,
    DateTime BorrowedAt,
    DateTime? ReturnedAt);