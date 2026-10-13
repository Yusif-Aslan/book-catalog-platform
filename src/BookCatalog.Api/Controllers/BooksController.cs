using BookCatalog.Api.Contracts;
using BookCatalog.Application.Books;
using Microsoft.AspNetCore.Mvc;

namespace BookCatalog.Api.Controllers;

[ApiController]
[Route("api/books")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class BooksController(IBookService bookService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Get books")]
    [EndpointDescription(
        "Returns one page of books ordered by creation time, " +
        "together with the total number of matching books and pages. " +
        "Title and author match partially, genre matches exactly; all text filters ignore case. " +
        "Filters can be combined.")]
    [ProducesResponseType(typeof(PagedResponse<BookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<BookResponse>>> GetAll(
        [FromQuery] GetBooksRequest request,
        CancellationToken cancellationToken)
    {
        var page = await bookService.GetPageAsync(request.ToQuery(), cancellationToken);
        return Ok(page.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a book by id")]
    [EndpointDescription("Returns a single book. Responds with 404 if no book has the given id.")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var book = await bookService.GetByIdAsync(id, cancellationToken);  
        return book is null ? NotFound() : Ok(book.ToResponse());
    }

    [HttpPost]
    [EndpointSummary("Create a book")]
    [EndpointDescription(
        "Creates a new book. Title and author are required. " +
        "ISBN is optional but must be a valid ISBN-10 or ISBN-13 and unique across the catalog. " +
        "Published year must be between 1450 and the current year.")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResponse>> Create(
        CreateBookRequest request,
        CancellationToken cancellationToken)
    {
        var book = await bookService.CreateAsync(request.ToDetails(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a book")]
    [EndpointDescription(
        "Replaces all editable fields of an existing book. " +
        "The same validation rules as for creation apply.")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResponse>> Update(
        Guid id,
        UpdateBookRequest request,
        CancellationToken cancellationToken)
    {
        var book = await bookService.UpdateAsync(id, request.ToDetails(), cancellationToken);
        return book is null ? NotFound() : Ok(book.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a book")]
    [EndpointDescription(
        "Deletes a book together with its copies. Responds with 404 if no book has the given id " +   // ←
        "and with 409 if any of its copies has loan history.")]                                       // ←
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]                      // ←
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await bookService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}