using BookCatalog.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookCatalog.Infrastructure.Persistence.Configurations;

internal sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public const string InventoryNumberUniqueIndex = "IX_BookCopies_InventoryNumber";

    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.InventoryNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.InventoryNumber)
            .IsUnique()
            .HasDatabaseName(InventoryNumberUniqueIndex);

        builder.HasOne(c => c.Book)
            .WithMany()
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}