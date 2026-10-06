using BookCatalog.Application.Books;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;
using BookCatalog.Infrastructure.Persistence;

namespace BookCatalog.UnitTests.Infrastructure.Persistence;

public class InMemoryBookRepositoryTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string Isbn = "9780132350884";

    private readonly InMemoryBookRepository _repository = new();

    [Fact]
    public async Task GetPageAsync_ReturnsBooksOrderedByCreationTime()
    {
        await _repository.AddAsync(CreateBook(title: "Third", order: 2));
        await _repository.AddAsync(CreateBook(title: "First", order: 0));
        await _repository.AddAsync(CreateBook(title: "Second", order: 1));

        var page = await _repository.GetPageAsync(Query());

        Assert.Equal(["First", "Second", "Third"], page.Items.Select(b => b.Title));
    }

    [Fact]
    public async Task GetPageAsync_ReturnsRequestedPageWithTotalCountAndTotalPages()
    {
        await AddNumberedBooksAsync(5);

        var page = await _repository.GetPageAsync(Query(page: 2, pageSize: 2));

        Assert.Equal(["Book 3", "Book 4"], page.Items.Select(b => b.Title));
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task GetPageAsync_OnLastPartialPage_ReturnsRemainingBooks()
    {
        await AddNumberedBooksAsync(5);

        var page = await _repository.GetPageAsync(Query(page: 3, pageSize: 2));

        Assert.Equal(["Book 5"], page.Items.Select(b => b.Title));
    }

    [Fact]
    public async Task GetPageAsync_WhenPageIsBeyondLastPage_ReturnsNoItemsButKeepsTotalCount()
    {
        await AddNumberedBooksAsync(3);

        var page = await _repository.GetPageAsync(Query(page: 5000, pageSize: 10));

        Assert.Empty(page.Items);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
    }

    [Fact]
    public async Task GetPageAsync_WhenRepositoryIsEmpty_ReturnsZeroTotalPages()
    {
        var page = await _repository.GetPageAsync(Query());

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public async Task GetPageAsync_WithTitleFilter_MatchesPartiallyAndIgnoresCase()
    {
        await _repository.AddAsync(CreateBook(title: "Clean Code"));
        await _repository.AddAsync(CreateBook(title: "Clean Architecture"));
        await _repository.AddAsync(CreateBook(title: "Refactoring"));

        var page = await _repository.GetPageAsync(Query() with { Title = "CLEAN" });

        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, b => Assert.Contains("Clean", b.Title));
    }

    [Fact]
    public async Task GetPageAsync_WithAuthorFilter_MatchesPartiallyAndIgnoresCase()
    {
        await _repository.AddAsync(CreateBook(author: "Martin Fowler"));
        await _repository.AddAsync(CreateBook(author: "Robert C. Martin"));
        await _repository.AddAsync(CreateBook(author: "Kent Beck"));

        var page = await _repository.GetPageAsync(Query() with { Author = "martin" });

        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetPageAsync_WithGenreFilter_MatchesExactlyAndIgnoresCase()
    {
        await _repository.AddAsync(CreateBook(genre: "Fiction"));
        await _repository.AddAsync(CreateBook(genre: "Science Fiction"));
        await _repository.AddAsync(CreateBook(genre: null));

        var page = await _repository.GetPageAsync(Query() with { Genre = "FICTION" });

        var book = Assert.Single(page.Items);
        Assert.Equal("Fiction", book.Genre);
    }

    [Fact]
    public async Task GetPageAsync_WithYearRange_IncludesBothBoundaries()
    {
        await _repository.AddAsync(CreateBook(year: 1999));
        await _repository.AddAsync(CreateBook(year: 2000));
        await _repository.AddAsync(CreateBook(year: 2010));
        await _repository.AddAsync(CreateBook(year: 2011));

        var page = await _repository.GetPageAsync(Query() with { PublishedFrom = 2000, PublishedTo = 2010 });

        Assert.Equal([2000, 2010], page.Items.Select(b => b.PublishedYear).Order());
    }

    [Fact]
    public async Task GetPageAsync_WithMultipleFilters_ReturnsOnlyBooksMatchingAll()
    {
        await _repository.AddAsync(CreateBook(author: "Martin Fowler", genre: "Software", year: 2018));
        await _repository.AddAsync(CreateBook(author: "Martin Fowler", genre: "Software", year: 1999));
        await _repository.AddAsync(CreateBook(author: "Kent Beck", genre: "Software", year: 2018));

        var page = await _repository.GetPageAsync(
            Query() with { Author = "Fowler", Genre = "Software", PublishedFrom = 2010 });

        var book = Assert.Single(page.Items);
        Assert.Equal(2018, book.PublishedYear);
        Assert.Equal("Martin Fowler", book.Author);
    }

    [Fact]
    public async Task GetPageAsync_WithFilterAndPaging_CountsOnlyMatchingBooks()
    {
        await _repository.AddAsync(CreateBook(genre: "Software", order: 0));
        await _repository.AddAsync(CreateBook(genre: "Fiction", order: 1));
        await _repository.AddAsync(CreateBook(genre: "Software", order: 2));
        await _repository.AddAsync(CreateBook(genre: "Fiction", order: 3));
        await _repository.AddAsync(CreateBook(genre: "Software", order: 4));

        var page = await _repository.GetPageAsync(Query(page: 1, pageSize: 1) with { Genre = "Software" });

        Assert.Single(page.Items);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookExists_ReturnsIt()
    {
        var book = CreateBook();
        await _repository.AddAsync(book);

        var result = await _repository.GetByIdAsync(book.Id);

        Assert.Same(book, result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookDoesNotExist_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task IsbnExistsAsync_WhenAnotherBookHasIsbn_ReturnsTrue()
    {
        await _repository.AddAsync(CreateBook(isbn: Isbn));

        var exists = await _repository.IsbnExistsAsync(Isbn);

        Assert.True(exists);
    }

    [Fact]
    public async Task IsbnExistsAsync_WhenOnlyExcludedBookHasIsbn_ReturnsFalse()
    {
        var book = CreateBook(isbn: Isbn);
        await _repository.AddAsync(book);

        var exists = await _repository.IsbnExistsAsync(Isbn, excludeId: book.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task AddAsync_WhenIsbnAlreadyExists_ThrowsDuplicateIsbnException()
    {
        await _repository.AddAsync(CreateBook(isbn: Isbn));

        await Assert.ThrowsAsync<DuplicateIsbnException>(
            () => _repository.AddAsync(CreateBook(isbn: Isbn)));
    }

    [Fact]
    public async Task AddAsync_WithoutIsbn_AllowsMultipleBooks()
    {
        await _repository.AddAsync(CreateBook(isbn: null));
        await _repository.AddAsync(CreateBook(isbn: null));

        var page = await _repository.GetPageAsync(Query());

        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task UpdateAsync_WhenBookDoesNotExist_ReturnsFalse()
    {
        var updated = await _repository.UpdateAsync(CreateBook());

        Assert.False(updated);
    }

    [Fact]
    public async Task UpdateAsync_WithItsOwnIsbn_ReturnsTrueAndStoresChanges()
    {
        var book = CreateBook(title: "Old", isbn: Isbn);
        await _repository.AddAsync(book);
        var changed = CopyWith(book, title: "New");

        var updated = await _repository.UpdateAsync(changed);

        Assert.True(updated);
        var stored = await _repository.GetByIdAsync(book.Id);
        Assert.Equal("New", stored?.Title);
    }

    [Fact]
    public async Task UpdateAsync_WithIsbnOfAnotherBook_ThrowsDuplicateIsbnException()
    {
        await _repository.AddAsync(CreateBook(isbn: Isbn));
        var other = CreateBook(isbn: null);
        await _repository.AddAsync(other);

        await Assert.ThrowsAsync<DuplicateIsbnException>(
            () => _repository.UpdateAsync(CopyWith(other, isbn: Isbn)));
    }

    [Fact]
    public async Task DeleteAsync_WhenBookExists_ReturnsTrueAndRemovesIt()
    {
        var book = CreateBook();
        await _repository.AddAsync(book);

        var deleted = await _repository.DeleteAsync(book.Id);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(book.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenBookDoesNotExist_ReturnsFalse()
    {
        var deleted = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    private static BookQuery Query(int page = 1, int pageSize = 10) => new(page, pageSize);

    private async Task AddNumberedBooksAsync(int count)
    {
        for (var i = 1; i <= count; i++)
            await _repository.AddAsync(CreateBook(title: $"Book {i}", order: i));
    }

    private static Book CreateBook(
        string title = "Title",
        string author = "Author",
        string? genre = null,
        int year = 2000,
        string? isbn = null,
        int order = 0) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Author = author,
        Isbn = isbn,
        PublishedYear = year,
        Genre = genre,
        CreatedAt = BaseTime.AddMinutes(order),
        UpdatedAt = BaseTime.AddMinutes(order)
    };

    private static Book CopyWith(Book book, string? title = null, string? isbn = null) => new()
    {
        Id = book.Id,
        Title = title ?? book.Title,
        Author = book.Author,
        Isbn = isbn ?? book.Isbn,
        PublishedYear = book.PublishedYear,
        Genre = book.Genre,
        CreatedAt = book.CreatedAt,
        UpdatedAt = book.UpdatedAt
    };
}