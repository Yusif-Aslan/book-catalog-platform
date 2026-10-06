using BookCatalog.Application.Abstractions;
using BookCatalog.Application.Books;
using BookCatalog.Application.Common;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace BookCatalog.UnitTests.Application.Books;

public class BookServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateTime EarlierTime = new(2025, 12, 1, 8, 0, 0, DateTimeKind.Utc);
    private const string NormalizedIsbn = "9780132350884";

    private readonly IBookRepository _repository = Substitute.For<IBookRepository>();
    private readonly BookService _service;

    public BookServiceTests()
    {
        _service = new BookService(_repository, new FakeTimeProvider(Now), NullLogger<BookService>.Instance);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsPageFromRepository()
    {
        var query = new BookQuery(Page: 2, PageSize: 10, Genre: "Fiction");
        var expected = new PagedResult<Book>([], 2, 10, 0);
        _repository.GetPageAsync(Arg.Is(query), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _service.GetPageAsync(query);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookDoesNotExist_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Is(id), Arg.Any<CancellationToken>()).Returns((Book?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithValidDetails_ReturnsBookWithGivenFields()
    {
        var details = Details(NormalizedIsbn);

        var book = await _service.CreateAsync(details);

        Assert.Equal(details.Title, book.Title);
        Assert.Equal(details.Author, book.Author);
        Assert.Equal(details.PublishedYear, book.PublishedYear);
        Assert.Equal(details.Genre, book.Genre);
    }

    [Fact]
    public async Task CreateAsync_WithValidDetails_AssignsNewId()
    {
        var book = await _service.CreateAsync(Details());

        Assert.NotEqual(Guid.Empty, book.Id);
    }

    [Fact]
    public async Task CreateAsync_WithValidDetails_SetsCreatedAtAndUpdatedAtToCurrentTime()
    {
        var book = await _service.CreateAsync(Details());

        Assert.Equal(Now.UtcDateTime, book.CreatedAt);
        Assert.Equal(Now.UtcDateTime, book.UpdatedAt);
    }

    [Theory]
    [InlineData("978-0-13-235088-4", "9780132350884")]
    [InlineData("978 0 13 235088 4", "9780132350884")]
    [InlineData("0-201-61622-x", "020161622X")]
    public async Task CreateAsync_WithFormattedIsbn_StoresNormalizedIsbn(string input, string expected)
    {
        var book = await _service.CreateAsync(Details(input));

        Assert.Equal(expected, book.Isbn);
    }

    [Fact]
    public async Task CreateAsync_WithValidDetails_SavesBookInRepository()
    {
        var book = await _service.CreateAsync(Details(NormalizedIsbn));

        await _repository.Received(1).AddAsync(
            Arg.Is<Book>(b => b.Id == book.Id && b.Isbn == NormalizedIsbn),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenIsbnAlreadyExists_ThrowsDuplicateIsbnException()
    {
        ArrangeIsbnExists(NormalizedIsbn, excludeId: null);

        var exception = await Assert.ThrowsAsync<DuplicateIsbnException>(
            () => _service.CreateAsync(Details(NormalizedIsbn)));

        Assert.Equal(NormalizedIsbn, exception.Isbn);
    }

    [Fact]
    public async Task CreateAsync_WhenIsbnAlreadyExists_DoesNotSaveBook()
    {
        ArrangeIsbnExists(NormalizedIsbn, excludeId: null);

        await Assert.ThrowsAsync<DuplicateIsbnException>(() => _service.CreateAsync(Details(NormalizedIsbn)));

        await _repository.DidNotReceive().AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenIsbnHasHyphens_ChecksUniquenessOfNormalizedIsbn()
    {
        await _service.CreateAsync(Details("978-0-13-235088-4"));

        await _repository.Received(1).IsbnExistsAsync(
            Arg.Is(NormalizedIsbn),
            Arg.Is<Guid?>(id => id == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithoutIsbn_DoesNotCheckIsbnUniqueness()
    {
        await _service.CreateAsync(Details(isbn: null));

        await _repository.DidNotReceive().IsbnExistsAsync(
            Arg.Any<string>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenBookDoesNotExist_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Is(id), Arg.Any<CancellationToken>()).Returns((Book?)null);

        var result = await _service.UpdateAsync(id, Details());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenBookDoesNotExist_DoesNotCallRepositoryUpdate()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Is(id), Arg.Any<CancellationToken>()).Returns((Book?)null);

        await _service.UpdateAsync(id, Details());

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenBookExists_ReturnsBookWithNewFieldsAndSameId()
    {
        var existing = ArrangeExistingBook();
        var details = Details(NormalizedIsbn);

        var result = await _service.UpdateAsync(existing.Id, details);

        Assert.NotNull(result);
        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(details.Title, result.Title);
        Assert.Equal(details.Author, result.Author);
        Assert.Equal(NormalizedIsbn, result.Isbn);
        Assert.Equal(details.PublishedYear, result.PublishedYear);
        Assert.Equal(details.Genre, result.Genre);
    }

    [Fact]
    public async Task UpdateAsync_WhenBookExists_KeepsCreatedAtAndSetsUpdatedAtToCurrentTime()
    {
        var existing = ArrangeExistingBook();

        var result = await _service.UpdateAsync(existing.Id, Details());

        Assert.NotNull(result);
        Assert.Equal(EarlierTime, result.CreatedAt);
        Assert.Equal(Now.UtcDateTime, result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithIsbn_ChecksUniquenessExcludingTheBookItself()
    {
        var existing = ArrangeExistingBook();

        await _service.UpdateAsync(existing.Id, Details(NormalizedIsbn));

        await _repository.Received(1).IsbnExistsAsync(
            Arg.Is(NormalizedIsbn),
            Arg.Is<Guid?>(existing.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenIsbnBelongsToAnotherBook_ThrowsDuplicateIsbnException()
    {
        var existing = ArrangeExistingBook();
        ArrangeIsbnExists(NormalizedIsbn, excludeId: existing.Id);

        await Assert.ThrowsAsync<DuplicateIsbnException>(
            () => _service.UpdateAsync(existing.Id, Details(NormalizedIsbn)));
    }

    [Fact]
    public async Task UpdateAsync_WhenIsbnBelongsToAnotherBook_DoesNotSaveChanges()
    {
        var existing = ArrangeExistingBook();
        ArrangeIsbnExists(NormalizedIsbn, excludeId: existing.Id);

        await Assert.ThrowsAsync<DuplicateIsbnException>(
            () => _service.UpdateAsync(existing.Id, Details(NormalizedIsbn)));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenBookIsRemovedConcurrently_ReturnsNull()
    {
        var existing = ArrangeExistingBook();
        _repository.UpdateAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.UpdateAsync(existing.Id, Details());

        Assert.Null(result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_ReturnsWhetherRepositoryDeletedTheBook(bool deletedInRepository)
    {
        var id = Guid.NewGuid();
        _repository.DeleteAsync(Arg.Is(id), Arg.Any<CancellationToken>()).Returns(deletedInRepository);

        var result = await _service.DeleteAsync(id);

        Assert.Equal(deletedInRepository, result);
    }

    private static BookDetails Details(string? isbn = null) =>
        new("Clean Code", "Robert C. Martin", isbn, 2008, "Software Engineering");

    private Book ArrangeExistingBook()
    {
        var existing = new Book
        {
            Id = Guid.NewGuid(),
            Title = "Old Title",
            Author = "Old Author",
            Isbn = null,
            PublishedYear = 2000,
            Genre = null,
            CreatedAt = EarlierTime,
            UpdatedAt = EarlierTime
        };

        _repository.GetByIdAsync(Arg.Is(existing.Id), Arg.Any<CancellationToken>()).Returns(existing);
        _repository.UpdateAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>()).Returns(true);

        return existing;
    }

    private void ArrangeIsbnExists(string isbn, Guid? excludeId)
    {
        _repository
            .IsbnExistsAsync(Arg.Is(isbn), Arg.Is<Guid?>(excludeId), Arg.Any<CancellationToken>())
            .Returns(true);
    }
}