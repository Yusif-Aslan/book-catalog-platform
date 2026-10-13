using BookCatalog.Domain.Books;

namespace BookCatalog.Api.Contracts;

public static class LendingMappings
{
    public static BookCopyResponse ToResponse(this BookCopy copy) =>
        new(
            copy.Id,
            copy.BookId,
            copy.InventoryNumber,
            copy.Loans.All(l => !l.IsActive),
            copy.CreatedAt);

    public static LoanResponse ToResponse(this Loan loan) =>
        new(
            loan.Id,
            loan.BookCopyId,
            loan.BorrowerName,
            loan.BorrowedAt,
            loan.ReturnedAt);
}