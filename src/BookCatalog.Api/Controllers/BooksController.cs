using BookCatalog.Api.Contracts;
using BookCatalog.Application.Books;
using BookCatalog.Application.Exceptions;
using BookCatalog.Domain.Books;
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
    [ProducesResponseType(typeof(IReadOnlyList<Book>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Book>>> GetAll()
    {
        var books = await bookService.GetAllAsync();
        return Ok(books);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a book by id")]
    [EndpointDescription("Returns a single book. Responds with 404 if no book has the given id.")]
    [ProducesResponseType(typeof(Book), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Book>> GetById(Guid id)
    {
        var book = await bookService.GetByIdAsync(id);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPost]
    [EndpointSummary("Create a book")]
    [EndpointDescription(
        "Creates a new book. Title and author are required. " +
        "ISBN is optional but must be a valid ISBN-10 or ISBN-13 and unique across the catalog. " +
        "Published year must be between 1450 and the current year.")]
    [ProducesResponseType(typeof(Book), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Book>> Create(CreateBookRequest request)
    {
        try
        {
            var book = await bookService.CreateAsync(request.ToDetails());
            return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
        }
        catch (DuplicateIsbnException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate ISBN",
                detail: ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a book")]
    [EndpointDescription(
        "Replaces all editable fields of an existing book. " +
        "The same validation rules as for creation apply.")]
    [ProducesResponseType(typeof(Book), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Book>> Update(Guid id, UpdateBookRequest request)
    {
        try
        {
            var book = await bookService.UpdateAsync(id, request.ToDetails());
            return book is null ? NotFound() : Ok(book);
        }
        catch (DuplicateIsbnException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate ISBN",
                detail: ex.Message);
        }
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