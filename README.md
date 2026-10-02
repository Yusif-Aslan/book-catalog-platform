# Book Catalog Platform

A REST API for managing books, built with ASP.NET Core (.NET 10).

## How to run

Install the .NET 10 SDK, then run from the repository root:

```bash
dotnet run --project src/BookCatalog.Api --launch-profile https
```

## How to test

Open Swagger in the browser: https://localhost:7134/swagger

From there you can try every endpoint: create, list, get, update and delete books.

## Test inputs

Copy these into Swagger.

**Create a book** (`POST /api/books`) → 201

```json
{
  "title": "Clean Code",
  "author": "Robert C. Martin",
  "isbn": "978-0-13-235088-4",
  "publishedYear": 2008,
  "genre": "Programming"
}
```

**Another book with an ISBN-10** (`POST /api/books`) → 201

```json
{
  "title": "Design Patterns",
  "author": "Erich Gamma",
  "isbn": "0-201-63361-2",
  "publishedYear": 1994,
  "genre": "Programming"
}
```

**Duplicate ISBN** (`POST /api/books`) → 409

Same ISBN as the first book, written without hyphens:

```json
{
  "title": "Clean Code (copy)",
  "author": "Robert C. Martin",
  "isbn": "9780132350884",
  "publishedYear": 2008,
  "genre": "Programming"
}
```

**Invalid book** (`POST /api/books`) → 400

Empty title and author, wrong ISBN checksum, year in the future:

```json
{
  "title": "",
  "author": "",
  "isbn": "978-0-13-235088-5",
  "publishedYear": 3000,
  "genre": null
}
```

**Update a book** (`PUT /api/books/{id}`) → 200

Use an `id` returned when you created a book:

```json
{
  "title": "The Pragmatic Programmer",
  "author": "David Thomas, Andrew Hunt",
  "isbn": "978-0-13-595705-9",
  "publishedYear": 2019,
  "genre": "Programming"
}
```

**Not found** (`GET`, `PUT` or `DELETE /api/books/{id}`) → 404

Use an id that does not exist:

```
00000000-0000-0000-0000-000000000001
```
