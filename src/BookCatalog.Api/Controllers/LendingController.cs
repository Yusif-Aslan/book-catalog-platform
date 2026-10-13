using BookCatalog.Api.Contracts;
using BookCatalog.Application.Lending;
using Microsoft.AspNetCore.Mvc;

namespace BookCatalog.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class LendingController(ILendingService lendingService) : ControllerBase
{
    [HttpGet("books/{bookId:guid}/copies")]
    [EndpointSummary("Get copies of a book")]
    [EndpointDescription("Returns all physical copies of a book and whether each copy is available.")]
    [ProducesResponseType(typeof(IReadOnlyList<BookCopyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BookCopyResponse>>> GetCopies(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var copies = await lendingService.GetCopiesAsync(bookId, cancellationToken);
        return copies is null ? NotFound() : Ok(copies.Select(c => c.ToResponse()).ToList());
    }

    [HttpPost("books/{bookId:guid}/copies")]
    [EndpointSummary("Add a copy to a book")]
    [EndpointDescription("Adds a physical copy. The inventory number must be unique across the catalog.")]
    [ProducesResponseType(typeof(BookCopyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookCopyResponse>> AddCopy(
        Guid bookId,
        CreateCopyRequest request,
        CancellationToken cancellationToken)
    {
        var copy = await lendingService.AddCopyAsync(bookId, request.InventoryNumber, cancellationToken);
        return copy is null ? NotFound() : CreatedAtAction(nameof(GetCopies), new { bookId }, copy.ToResponse());
    }

    [HttpPost("copies/{copyId:guid}/borrow")]
    [EndpointSummary("Borrow a copy")]
    [EndpointDescription("Creates a loan for the copy. Responds with 409 if the copy is already borrowed.")]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanResponse>> Borrow(
        Guid copyId,
        BorrowRequest request,
        CancellationToken cancellationToken)
    {
        var loan = await lendingService.BorrowAsync(copyId, request.BorrowerName, cancellationToken);
        return loan is null ? NotFound() : CreatedAtAction(nameof(GetLoanHistory), new { copyId }, loan.ToResponse());
    }

    [HttpPost("copies/{copyId:guid}/return")]
    [EndpointSummary("Return a copy")]
    [EndpointDescription("Closes the active loan of the copy. Responds with 409 if the copy is not borrowed.")]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanResponse>> Return(
        Guid copyId,
        CancellationToken cancellationToken)
    {
        var loan = await lendingService.ReturnAsync(copyId, cancellationToken);
        return loan is null ? NotFound() : Ok(loan.ToResponse());
    }

    [HttpGet("copies/{copyId:guid}/loans")]
    [EndpointSummary("Get loan history of a copy")]
    [EndpointDescription("Returns all loans of the copy, newest first.")]
    [ProducesResponseType(typeof(IReadOnlyList<LoanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LoanResponse>>> GetLoanHistory(
        Guid copyId,
        CancellationToken cancellationToken)
    {
        var loans = await lendingService.GetLoanHistoryAsync(copyId, cancellationToken);
        return loans is null ? NotFound() : Ok(loans.Select(l => l.ToResponse()).ToList());
    }
}