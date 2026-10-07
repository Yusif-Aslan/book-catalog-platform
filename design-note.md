# Design Note

# Week 1

## What was built

A REST API for a book catalog. You can create, list, get, update and delete books. Books are stored in memory.

## Structure

- `Controllers` handle HTTP requests.
- `Contracts` are the request models the client sends.
- `Models` contains the `Book` entity.
- `Services` stores books and applies business rules.
- `Validation` contains custom validation for ISBN and year.

## Decisions

- The book id is a `Guid`, so no counter is needed.
- The service is a Singleton, so the data stays alive while the app runs.
- Requests are validated with attributes. Invalid requests get a 400 with clear errors.
- All errors use the same format (ProblemDetails).
- A book's ISBN must be unique. A duplicate returns 409 Conflict.
- Important operations and errors are logged.
- Swagger documents every endpoint.

## What could be improved

- Separate business logic from storage.
- Return a response model instead of the `Book` entity.
- Add pagination, filtering and unit tests.

## What was hard

The hardest part was the ISBN checks: validating ISBN-10 and ISBN-13 checksums, and making sure two books cannot have the same ISBN.

Uniqueness was tricky because of race conditions. Two requests could check the same ISBN at the same time, both see it is free, and both save it. This is solved with a `lock`, so checking and saving happen as one step.

# Week 2

## What was built

The project is split into layers. The book list has pagination and filtering. Errors are handled in one place. Unit tests are added.

## Structure

Now there are four projects instead of one:

- `Domain` has the `Book` entity.
- `Application` has the business logic (`BookService`) and the `IBookRepository` interface.
- `Infrastructure` has `InMemoryBookRepository`, which stores the books.
- `Api` has controllers, request and response models, validation and error handling.

There is also a `UnitTests` project.

`Application` uses `Domain`. `Infrastructure` uses `Application` and `Domain`. `Api` uses `Application` and `Infrastructure`.

## What changed

- The service had logic and storage together. Now storage is in a separate repository.
- The service took `CreateBookRequest`. Now it takes `BookDetails`, so it does not know about HTTP.
- The controller returned `Book`. Now it returns `BookResponse`.
- Every controller method had `try/catch`. Now errors are handled in one place.
- The list returned all books. Now it returns one page and can be filtered.
- The service used `DateTime.UtcNow`. Now it gets the time from `TimeProvider`.

## Decisions

- `IBookRepository` is in `Application`, not in `Infrastructure`. This way the logic does not depend on storage. To use another storage, only a new repository needs to be added and one line in `Program.cs` changed.
- `Api` uses `Infrastructure` only in `Program.cs`, to register the repository.
- Layers are separate projects, not folders, so a wrong reference does not compile.
- All repository methods are async and have a `CancellationToken`, so a real database can be used without changing the interface.
- The repository is a Singleton, so the books stay in memory. The service is Scoped, because it has no data.
- Models are mapped by hand in `BookMappings`. There are not many fields, so a library is not needed.
- Create and update use the same `BookDetails`, because they have the same fields.
- The service checks if the ISBN is free before saving. The repository checks it again under a `lock`, in case two requests come at the same time.

## Pagination and filtering

- `page` starts from 1. `pageSize` is 20 by default, max 100.
- The response has the books, `totalCount` and `totalPages`.
- If the page is after the end, the list is empty, not an error.
- The list can be filtered by `title`, `author`, `genre`, `publishedFrom` and `publishedTo`.
- Title and author can match part of the text. Genre must match fully. Case does not matter.
- `totalCount` counts only the books that match the filters.
- Filtering and paging are in the repository, not in the service, so with a database they can run in SQL.

## Error handling

- All errors go to `GlobalExceptionHandler`.
- Business errors like duplicate ISBN return 409 with a clear message.
- Unknown errors return 500 with a general message. Details are only in the logs.
- Every error has a `traceId`, so it can be found in the logs.

## Testing

- Tools: xUnit and NSubstitute.
- `BookService` is tested with a fake repository.
- ISBN, year and query validation are tested.
- Filtering and paging in `InMemoryBookRepository` are tested.
- Tests do not use any database, file or network.
- Controllers and the error handler are not tested. That needs real HTTP, so it is integration testing, not unit testing.
- There is no coverage percentage goal. The goal is to cover every rule and every error case.

## What was painful to change

- The service had logic and storage in one class, so it had to be split.
- The service used `CreateBookRequest`, so it could not be moved until that was changed.
- `DateTime.UtcNow` made dates hard to test, so `TimeProvider` was added.
- The `lock` only worked because everything was in one class. After the split, ISBN uniqueness had to be designed again.

The week 1 code worked, but one class did too many things. That is why it was hard to change.

## What could be improved

- Put ISBN normalization and checksum in one place.
- Add integration tests.

## What was hard

Splitting the ISBN check between the service and the repository and still keeping it safe.

Also Git. When Rider moved files, it changed `using` in other files too, but only the moved files were committed. So some early commits do not build. Now the solution is built and all changed files are checked before each commit.