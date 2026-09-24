using BookCatalog.Api.Contracts;
using BookCatalog.Api.Models;
using BookCatalog.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookCatalog.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController(IBookService bookService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Book>>> GetAll()
    {
        var books = await bookService.GetAllAsync();
        return Ok(books);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Book>> GetById(Guid id)
    {
        var book = await bookService.GetByIdAsync(id);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPost]
    public async Task<ActionResult<Book>> Create(CreateBookRequest request)
    {
        var book = await bookService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Book>> Update(Guid id, UpdateBookRequest request)
    {
        var book = await bookService.UpdateAsync(id, request);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await bookService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}