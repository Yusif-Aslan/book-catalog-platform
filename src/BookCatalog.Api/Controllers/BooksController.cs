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
    [EndpointSummary("Get all books")]
    [EndpointDescription("Returns all books in the catalog, ordered by creation time.")]
    [ProducesResponseType(typeof(IReadOnlyList<BookResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookResponse>>> GetAll()
    {
        var books = await bookService.GetAllAsync();
        return Ok(books.Select(b => b.ToResponse()).ToList());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a book by id")]
    [EndpointDescription("Returns a single book. Responds with 404 if no book has the given id.")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> GetById(Guid id)
    {
        var book = await bookService.GetByIdAsync(id);
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
    public async Task<ActionResult<BookResponse>> Create(CreateBookRequest request)
    {
        var book = await bookService.CreateAsync(request.ToDetails());
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
    public async Task<ActionResult<BookResponse>> Update(Guid id, UpdateBookRequest request)
    {
        var book = await bookService.UpdateAsync(id, request.ToDetails());
        return book is null ? NotFound() : Ok(book.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a book")]
    [EndpointDescription("Deletes a book. Responds with 404 if no book has the given id.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await bookService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}