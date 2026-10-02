# Design Note

## What I built

A REST API for a book catalog. You can create, list, get, update and delete books. Books are stored in memory.

## Structure

- `Controllers` handle HTTP requests.
- `Contracts` are the request models the client sends.
- `Models` contains the `Book` entity.
- `Services` stores books and applies business rules.
- `Validation` contains custom validation for ISBN and year.

## Decisions

- I use `Guid` as the book id, so no counter is needed.
- The service is a Singleton, so the data stays alive while the app runs.
- Requests are validated with attributes. Invalid requests get a 400 with clear errors.
- All errors use the same format (ProblemDetails).
- A book's ISBN must be unique. A duplicate returns 409 Conflict.
- Important operations and errors are logged.
- Swagger documents every endpoint.

## What I would improve

- Separate business logic from storage.
- Return a response model instead of the `Book` entity.
- Add pagination, filtering and unit tests.

## What was hard

The hardest part was the ISBN checks: validating ISBN-10 and ISBN-13 checksums, and making sure two books cannot have the same ISBN.

Uniqueness was tricky because of race conditions. Two requests could check the same ISBN at the same time, both see it is free, and both save it. I solved this with a `lock`, so checking and saving happen as one step.
