using BookCatalog.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Infrastructure.Persistence;

public sealed class BookCatalogDbContext(DbContextOptions<BookCatalogDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookCatalogDbContext).Assembly);
}