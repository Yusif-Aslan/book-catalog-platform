using BookCatalog.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookCatalog.Infrastructure.Persistence.Configurations;

internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public const string ActiveLoanUniqueIndex = "IX_Loans_BookCopyId_Active";
    public const string ReturnedAfterBorrowedCheck = "CK_Loans_ReturnedAt_After_BorrowedAt";

    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans", table => table.HasCheckConstraint(
            ReturnedAfterBorrowedCheck,
            "\"ReturnedAt\" IS NULL OR \"ReturnedAt\" >= \"BorrowedAt\""));

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.BorrowerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Ignore(l => l.IsActive);

        builder.HasOne(l => l.BookCopy)
            .WithMany(c => c.Loans)
            .HasForeignKey(l => l.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.BookCopyId, ActiveLoanUniqueIndex)
            .IsUnique()
            .HasFilter("\"ReturnedAt\" IS NULL");

        builder.HasIndex(l => new { l.BookCopyId, l.BorrowedAt });
    }
}