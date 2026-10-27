# Book Catalog Platform

A REST API for a library catalog, built with ASP.NET Core (.NET 10), EF Core and PostgreSQL.

- Manage books: create, list with filters and paging, get, update, delete.
- Manage physical copies of a book.
- Borrow and return a copy, and see the loan history of a copy.

Design decisions, trade-offs and known limitations are in [design-note.md](design-note.md).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/), running

Check both:

```bash
dotnet --version
docker version
```

`docker version` must show both a `Client` and a `Server` section. If `Server` is missing, start Docker Desktop and wait until it says "Engine running".

## Run with Docker

From the repository root:

```bash
docker compose up --build -d
```

This starts PostgreSQL and the API. The API applies database migrations on startup.

| What | URL |
| --- | --- |
| Swagger UI | http://localhost:8080/swagger |
| Liveness | http://localhost:8080/health/live |
| Readiness | http://localhost:8080/health/ready |
| PostgreSQL | `localhost:5433`, database, user and password `bookcatalog` |

`docker compose ps` shows `(healthy)` next to `api` once it is up.

Logs:

```bash
docker compose logs -f api
```

Stop:

```bash
docker compose down
```

Stop and delete all data:

```bash
docker compose down -v
```

## Run from source

Use this to run or debug the API from Rider or the command line.

1. Start only the database:

   ```bash
   docker compose up -d db
   ```

2. Give the API a connection string. It is kept in user secrets, not in the repository:

   ```bash
   dotnet user-secrets set "ConnectionStrings:BookCatalog" "Host=localhost;Port=5433;Database=bookcatalog;Username=bookcatalog;Password=bookcatalog" --project src/BookCatalog.Api
   dotnet user-secrets set "Database:MigrateOnStartup" "true" --project src/BookCatalog.Api
   ```

   In Rider: right-click `BookCatalog.Api` → **Tools** → **.NET User Secrets**.

3. Run the API:

   ```bash
   dotnet run --project src/BookCatalog.Api --launch-profile http
   ```

   In Rider: select the **BookCatalog.Api: http** run configuration.

4. Open http://localhost:5095/swagger.

If the connection string is missing or invalid, the API stops at startup with a message that names the setting.

## Run tests

Docker must be running.

```bash
dotnet test
```

This runs:

- **Unit tests** (`tests/BookCatalog.UnitTests`): services, validation and configuration rules, without a database.
- **Integration tests** (`tests/BookCatalog.IntegrationTests`): real HTTP requests against the API and a real PostgreSQL started in a container by Testcontainers. The schema is created from migrations, the data is cleared before each test, and the container is removed after the run.

The first run downloads the `postgres:17-alpine` image, so it takes longer.

## Configuration

| Setting | Default | Meaning |
| --- | --- | --- |
| `ConnectionStrings:BookCatalog` | none, required | PostgreSQL connection string. Must contain `Host` and `Database`. |
| `Database:MigrateOnStartup` | `false` | Apply EF Core migrations when the API starts. |
| `Database:CommandTimeoutSeconds` | `30` | Timeout for one SQL command. Allowed: 1–300. |
| `Database:MaxRetryCount` | `3` | Retries after a transient database error. Allowed: 0–10. |
| `Database:MaxRetryDelaySeconds` | `5` | Longest wait between two retries. Allowed: 1–60. |

Settings can also be given as environment variables, with `__` instead of `:`, for example `ConnectionStrings__BookCatalog`.

All settings are checked at startup. An invalid value stops the API with an error such as:

```
Database:MaxRetryCount must be between 0 and 10, but was 50.
```

## API

| Method | Path | Description |
| --- | --- | --- |
| `GET` | `/api/books` | List books. Filters: `title`, `author`, `genre`, `publishedFrom`, `publishedTo`. Paging: `page`, `pageSize` (max 100). |
| `GET` | `/api/books/{id}` | Get a book. |
| `POST` | `/api/books` | Create a book. |
| `PUT` | `/api/books/{id}` | Update a book. |
| `DELETE` | `/api/books/{id}` | Delete a book and its copies. 409 if a copy has loan history. |
| `GET` | `/api/books/{bookId}/copies` | List copies of a book and whether each is available. |
| `POST` | `/api/books/{bookId}/copies` | Add a copy. The inventory number must be unique. |
| `POST` | `/api/copies/{copyId}/borrow` | Borrow a copy. 409 if it is already borrowed. |
| `POST` | `/api/copies/{copyId}/return` | Return a copy. 409 if it is not borrowed. |
| `GET` | `/api/copies/{copyId}/loans` | Loan history of a copy, newest first. |
| `GET` | `/health/live` | 200 while the process is running. |
| `GET` | `/health/ready` | 200 when the database is reachable, otherwise 503. |

Errors are returned as `application/problem+json` (RFC 9457) with a `traceId`. If the database is unavailable, the API returns 503 with a `Retry-After` header.

Example request bodies:

```json
POST /api/books
{
  "title": "Clean Code",
  "author": "Robert C. Martin",
  "isbn": "978-0-13-235088-4",
  "publishedYear": 2008,
  "genre": "Programming"
}
```

```json
POST /api/books/{bookId}/copies
{ "inventoryNumber": "INV-001" }
```

```json
POST /api/copies/{copyId}/borrow
{ "borrowerName": "Alice" }
```

`src/BookCatalog.Api/BookCatalog.Api.http` contains a full scenario with checks. In Rider, open it and click **Run All Requests in File**.

## Logs

Logs are written to the console as JSON, one object per line. Each entry has a timestamp, level, category, message, the values used in the message and the request scope, including `TraceId`.

Every HTTP request produces one entry with method, path, status code and duration. Health check requests are not logged.

To find the logs of a failed request, take the `traceId` from the error response and search for it.

## Project structure

```
src/
  BookCatalog.Domain           entities: Book, BookCopy, Loan
  BookCatalog.Application      services, repository interfaces, business exceptions
  BookCatalog.Infrastructure   EF Core, PostgreSQL, migrations, configuration, health checks
  BookCatalog.Api              controllers, contracts, validation, error handling, Program.cs
tests/
  BookCatalog.UnitTests
  BookCatalog.IntegrationTests
```

## Database migrations

Add a migration after changing the model:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/BookCatalog.Infrastructure --startup-project src/BookCatalog.Api --output-dir Persistence/Migrations
```

Apply migrations to the local database:

```bash
dotnet ef database update --project src/BookCatalog.Infrastructure --startup-project src/BookCatalog.Api
```
