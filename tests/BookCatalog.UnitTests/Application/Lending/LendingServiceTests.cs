using BookCatalog.Application.Abstractions;
using BookCatalog.Application.Exceptions;
using BookCatalog.Application.Lending;
using BookCatalog.Domain.Books;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace BookCatalog.UnitTests.Application.Lending;

public class LendingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateTime EarlierTime = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly ILendingRepository _repository = Substitute.For<ILendingRepository>();
    private readonly LendingService _service;

    public LendingServiceTests()
    {
        _repository
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Loan?>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<Loan?>>>()(call.Arg<CancellationToken>()));
        _service = new LendingService(_repository, new FakeTimeProvider(Now), NullLogger<LendingService>.Instance);
    }

    [Fact]
    public async Task GetCopiesAsync_WhenBookDoesNotExist_ReturnsNull()
    {
        var bookId = Guid.NewGuid();
        _repository.BookExistsAsync(Arg.Is(bookId), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.GetCopiesAsync(bookId);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddCopyAsync_WhenBookDoesNotExist_ReturnsNullAndDoesNotSave()
    {
        var bookId = Guid.NewGuid();
        _repository.BookExistsAsync(Arg.Is(bookId), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.AddCopyAsync(bookId, "INV-0001");

        Assert.Null(result);
        await _repository.DidNotReceive().AddCopyAsync(Arg.Any<BookCopy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddCopyAsync_WhenBookExists_SavesTrimmedCopyForBook()
    {
        var bookId = Guid.NewGuid();
        _repository.BookExistsAsync(Arg.Is(bookId), Arg.Any<CancellationToken>()).Returns(true);

        var copy = await _service.AddCopyAsync(bookId, "  INV-0001 ");

        Assert.NotNull(copy);
        Assert.NotEqual(Guid.Empty, copy.Id);
        Assert.Equal(bookId, copy.BookId);
        Assert.Equal("INV-0001", copy.InventoryNumber);
        Assert.Equal(Now.UtcDateTime, copy.CreatedAt);
        await _repository.Received(1).AddCopyAsync(Arg.Is(copy), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BorrowAsync_WhenCopyDoesNotExist_ReturnsNullAndDoesNotAddLoan()
    {
        var copyId = Guid.NewGuid();
        _repository.LockCopyAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.BorrowAsync(copyId, "Alice");

        Assert.Null(result);
        await _repository.DidNotReceive().AddLoanAsync(Arg.Any<Loan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BorrowAsync_WhenCopyIsAlreadyBorrowed_ThrowsAndDoesNotAddLoan()
    {
        var copyId = Guid.NewGuid();
        _repository.LockCopyAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(true);
        _repository.HasActiveLoanAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(true);

        var exception = await Assert.ThrowsAsync<CopyAlreadyBorrowedException>(
            () => _service.BorrowAsync(copyId, "Alice"));

        Assert.Equal(copyId, exception.CopyId);
        await _repository.DidNotReceive().AddLoanAsync(Arg.Any<Loan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BorrowAsync_WhenCopyIsAvailable_SavesLoanInsideOneTransaction()
    {
        var copyId = Guid.NewGuid();
        _repository.LockCopyAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(true);
        _repository.HasActiveLoanAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(false);

        var loan = await _service.BorrowAsync(copyId, " Alice ");

        Assert.NotNull(loan);
        Assert.Equal(copyId, loan.BookCopyId);
        Assert.Equal("Alice", loan.BorrowerName);
        Assert.Equal(Now.UtcDateTime, loan.BorrowedAt);
        Assert.Null(loan.ReturnedAt);
        await _repository.Received(1).AddLoanAsync(Arg.Is(loan), Arg.Any<CancellationToken>());
        await _repository.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<Loan?>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnAsync_WhenCopyDoesNotExist_ReturnsNull()
    {
        var copyId = Guid.NewGuid();
        _repository.GetActiveLoanAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns((Loan?)null);
        _repository.CopyExistsAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ReturnAsync(copyId);

        Assert.Null(result);
    }

    [Fact]
    public async Task ReturnAsync_WhenCopyIsNotBorrowed_ThrowsAndDoesNotSave()
    {
        var copyId = Guid.NewGuid();
        _repository.GetActiveLoanAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns((Loan?)null);
        _repository.CopyExistsAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<CopyNotBorrowedException>(() => _service.ReturnAsync(copyId));

        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnAsync_WhenCopyIsBorrowed_SetsReturnedAtAndSaves()
    {
        var copyId = Guid.NewGuid();
        var activeLoan = Loan(copyId);
        _repository.GetActiveLoanAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(activeLoan);

        var result = await _service.ReturnAsync(copyId);

        Assert.Same(activeLoan, result);
        Assert.Equal(Now.UtcDateTime, activeLoan.ReturnedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLoanHistoryAsync_WhenCopyDoesNotExist_ReturnsNull()
    {
        var copyId = Guid.NewGuid();
        _repository.CopyExistsAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.GetLoanHistoryAsync(copyId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLoanHistoryAsync_WhenCopyExists_ReturnsLoansFromRepository()
    {
        var copyId = Guid.NewGuid();
        IReadOnlyList<Loan> history = [Loan(copyId)];
        _repository.CopyExistsAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetLoanHistoryAsync(Arg.Is(copyId), Arg.Any<CancellationToken>()).Returns(history);

        var result = await _service.GetLoanHistoryAsync(copyId);

        Assert.Same(history, result);
    }

    private static Loan Loan(Guid copyId) => new()
    {
        Id = Guid.NewGuid(),
        BookCopyId = copyId,
        BorrowerName = "Alice",
        BorrowedAt = EarlierTime
    };
}