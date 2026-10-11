using BookCatalog.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookCatalog.Infrastructure.Persistence.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public const string IsbnUniqueIndex = "IX_Books_Isbn";

    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Author)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Genre)
            .HasMaxLength(50);

        builder.Property(b => b.Isbn)
            .HasMaxLength(13);

        builder.HasIndex(b => b.Isbn)
            .IsUnique()
            .HasDatabaseName(IsbnUniqueIndex);

        builder.HasIndex(b => new { b.CreatedAt, b.Id });
    }
}