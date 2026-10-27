# Design Note — Book Catalog Platform

## 1. What the platform does

A REST API for a library catalog.

- **Books**: create, list with filters and paging, get, update, delete.
- **Copies**: a book can have several physical copies. Each copy has a unique inventory number.
- **Lending**: a copy can be borrowed and returned. Every copy keeps its loan history.
- **Operations**: health checks, JSON logs, configuration checked at startup, retries for short database failures, graceful shutdown.

It runs with one command (`docker compose up --build`), and all tests run with one command (`dotnet test`).

## 2. Architecture now

Four projects with a strict dependency direction:

```
Api ──► Application ──► Domain
 │           ▲
 └──► Infrastructure
```

| Project | Contains | Depends on |
| --- | --- | --- |
| `Domain` | `Book`, `BookCopy`, `Loan` | nothing |
| `Application` | `BookService`, `LendingService`, repository interfaces, business exceptions | `Domain` |
| `Infrastructure` | EF Core `DbContext`, configurations, migrations, repositories, `DatabaseOptions`, health check registration | `Application`, `Domain` |
| `Api` | controllers, contracts, validation, `GlobalExceptionHandler`, `Program.cs` | `Application`, `Infrastructure` |

A request goes: controller → service → repository interface → EF Core repository → PostgreSQL.

- Business rules live in the services. They do not know about HTTP or EF Core.
- The database is the final guard for rules that must hold under concurrency: unique indexes, foreign keys, a partial unique index for active loans, a check constraint for dates.
- `Api` uses `Infrastructure` only in `Program.cs` (registration) and in the error handler (to recognise a database outage).

## 3. How it evolved

### Week 1 — one project, data in memory

Controllers, request validation with attributes (ISBN-10/13 checksum, publication year), ProblemDetails for every error, 409 for a duplicate ISBN, Swagger. Books were kept in a Singleton service with a `lock`, because the service did both business logic and storage.

### Week 2 — layers and tests

The single project was split into `Domain`, `Application`, `Infrastructure` and `Api`. Storage moved behind `IBookRepository`. The service stopped using HTTP request types (`BookDetails` instead of `CreateBookRequest`) and `DateTime.UtcNow` (`TimeProvider` instead). Errors moved into one `GlobalExceptionHandler`. Paging and filtering were added. Unit tests with xUnit and NSubstitute were added.

Splitting was harder than expected: the `lock` that protected ISBN uniqueness only worked because everything was in one class.

### Week 3 — PostgreSQL, lending, Docker

EF Core with PostgreSQL replaced the in-memory repository. The `lock` was replaced by a unique index. `BookCopy` and `Loan` were added, with borrowing protected by `SELECT ... FOR UPDATE` and a partial unique index. A Dockerfile and `docker-compose.yml` start the API and the database together.

### Week 4 — production readiness

- Integration tests through real HTTP and a real PostgreSQL.
- Liveness and readiness health checks.
- JSON logs with a trace id on every entry, one entry per request.
- Configuration validated at startup.
- Graceful shutdown with a bounded timeout.
- Retries for transient database errors, and 503 instead of 500 when the database is down.
- Cleanup: removed dead code (`ITransaction`, `EfTransaction`, unused middleware), replaced a prerelease package, split a long method, rewrote the README.

## 4. Key decisions

### Layers as separate projects

A wrong reference (for example `Application` using EF Core) does not compile. With folders it would only be a convention.

*Against it:* more projects and more ceremony for a small API. For one developer a single project with folders would also work.

### A copy is borrowed, not a book

A library has several copies of the same book. Lending a `Book` would allow only one loan at a time per title, or would need a counter that is easy to get wrong.

### Concurrency is enforced by the database

Two requests that borrow the same copy at the same time must not both succeed. Checking in C# is not enough, because both requests can pass the check before either saves.

- The copy row is locked with `SELECT ... FOR UPDATE` inside a transaction. The second request waits, then sees the active loan and gets 409.
- A partial unique index (`BookCopyId` where `ReturnedAt IS NULL`) makes a second active loan impossible even if the code has a bug.
- The same idea is used for ISBN and inventory number: the service checks first to give a clear message, the unique index is the real guarantee.

The integration test `Borrow_SameCopyConcurrently_OnlyOneRequestSucceeds` sends five borrow requests at once and checks that exactly one gets 201.

### ProblemDetails for every error

Every error has the same shape and a `traceId`. Business conflicts are exceptions that inherit `ConflictException`, so the handler maps all of them to 409 in one place.

The integration tests found a bug here: `[Produces("application/json")]` on the controllers changed the content type of 400 and 404 errors to `application/json`, while 409 and 500 were `application/problem+json`. The attribute was removed.

### Integration tests with Testcontainers and Respawn

- **Real PostgreSQL in Docker**, not SQLite or the EF in-memory provider. The features that matter most (`FOR UPDATE`, partial unique index, `ILIKE`, constraint error codes) only exist in PostgreSQL.
- **One container for the whole run**, schema created by the real migrations. Starting a container per test would be too slow.
- **Respawn clears all tables before each test**, except the migrations history. Each test creates its own data, so tests do not depend on each other or on order.
- **Tests run one after another** inside the shared collection, because they share one database.
- The container is removed after the run, so nothing is left behind.

*Trade-off:* the tests need Docker. Without it, 22 tests fail with a connection error from Testcontainers.

### Health checks: live and ready

- `/health/live` runs no checks. It answers "is the process running and able to serve HTTP?".
- `/health/ready` checks that the database accepts a connection. It answers "can the service do its job?".

If the database is down, `live` stays 200 and `ready` returns 503. Restarting the API would not fix the database, so the container health check in Docker uses `live`. A load balancer or orchestrator should use `ready` to decide whether to send traffic.

The responses are plain text (`Healthy` / `Unhealthy`) without details, so the endpoints reveal nothing about the internals and can stay public.

### Logging

- **Format:** the built-in JSON console formatter, one JSON object per line. No extra library was needed.
- **Tracing a request:** scopes are included, so every entry written during a request carries `TraceId`, `SpanId`, `RequestId` and `RequestPath`. The same trace id is in every error response.
- **One entry per request:** HTTP logging writes method, path, status code and duration. Headers, query strings and bodies are not logged. Health check requests are not logged, because a probe every few seconds would drown everything else.
- **Never logged:** borrower names, passwords, connection strings, request bodies. Configuration errors name the setting but never print its value.
- **Noise reduction:** EF Core SQL logs are at Warning outside Development.

Rule for levels:

| Level | Used for | Example |
| --- | --- | --- |
| Debug | reads that found nothing | book not found on `GET` |
| Information | business events and rejected client requests (4xx) | book created, copy borrowed, duplicate ISBN |
| Warning | something unusual the system handled by itself | — (not used by own code yet) |
| Error | the request failed because of us or a dependency (5xx) | unhandled exception, database unavailable |

A rejected request is not a Warning: it is the client's mistake, and nothing needs to be done about it at 3 a.m. Before week 4, "already borrowed" and "duplicate ISBN" were Warnings. They were moved to Information.

### Configuration validated at startup

`DatabaseOptions` holds the connection string, `MigrateOnStartup`, the command timeout and the retry settings. `DatabaseOptionsValidator` checks them, and `ValidateOnStart` plus reading the options right after `Build()` make the API stop before it accepts any request.

The errors name the exact setting, for example `ConnectionStrings:BookCatalog must specify Host.`, and never include the connection string itself, because it contains a password.

Local secrets are in user secrets. Docker uses environment variables. The only credentials in the repository are the ones for the local Docker database.

### Graceful shutdown

On `SIGTERM` (`docker compose stop`), Kestrel stops accepting new connections and waits for running requests to finish, up to `ShutdownTimeout` (20 s). Docker waits `stop_grace_period` (30 s) before it kills the process, so the application always gets its full 20 s.

Nothing is left half-done:

- Borrowing runs in one transaction. If a request is aborted, the transaction is rolled back.
- Every other write is a single `SaveChanges` or a single `DELETE`, which is atomic.

How it is verified is described in section 9.

### Retries for transient failures

EF Core's retrying execution strategy (`EnableRetryOnFailure`) retries a database operation after a transient error: network error, timeout, too many connections, deadlock or serialization failure. Retries are bounded: at most 3, with exponential backoff and random jitter, at most 5 s between attempts. All values are configurable and validated.

Borrowing uses its own transaction, which the retrying strategy does not allow by default. So the repository runs the whole unit (lock, check, insert, commit) through `ExecuteInTransactionAsync`. If it fails with a transient error, the whole unit is repeated, not only the last statement.

When all retries fail, the client gets **503 Service Unavailable** with `Retry-After: 10`, not a 500.

## 5. What is safe to retry

| Operation | Safe to retry? | Why |
| --- | --- | --- |
| `GET` anything | yes | reads change nothing |
| `PUT /api/books/{id}` | yes | idempotent: the same body gives the same state |
| `DELETE /api/books/{id}` | mostly | second call returns 404 instead of 204, but the state is the same |
| `POST /api/books` | no | a second call creates a second book (if it has no ISBN) |
| `POST .../copies` | no, but protected | a second call fails with 409 on the inventory number |
| `POST .../borrow` | no, but protected | a second call fails with 409, the copy is already borrowed |
| `POST .../return` | no, but protected | a second call fails with 409, the copy is not borrowed |

The weak spot is a lost commit acknowledgement. If PostgreSQL commits a borrow but the connection drops before the API receives the answer, EF retries the whole unit. The retry sees the active loan and returns 409, although the borrow succeeded. The data is still correct, but the client gets the wrong answer. The fix would be an idempotency key sent by the client.

## 6. When the database is down for ten minutes

1. `/health/ready` returns 503 on the next probe. `/health/live` stays 200, so the container is not restarted.
2. Every API request that needs the database is retried up to 3 times, then returns **503 with `Retry-After: 10`**. Users see an error after a few seconds. No request hangs forever.
3. Each failed request is logged at Error with its trace id, so the outage is visible in the logs.
4. No data is corrupted: writes either committed before the outage or were rolled back.
5. When the database comes back, the next request works. Nothing has to be restarted.

The cost: during those ten minutes, every request still tries the database and waits for its retries. That is the problem a **circuit breaker** solves: after several failures it stops calling the database for a while and fails immediately, which protects the database when it comes back and gives users a fast answer. There is no circuit breaker in this project. It would need Polly or a custom one, and with one API instance and one database it was not the most important thing to add.

## 7. What I would do differently

- **Start with the real database and integration tests.** The in-memory repository and the `lock` in weeks 1 and 2 were rewritten in week 3. A PostgreSQL container from day one would have avoided writing concurrency code twice.
- **Write integration tests before adding attributes for documentation.** `[Produces("application/json")]` was added for Swagger and silently changed the error content type. Only a test through real HTTP showed it.
- **Use a separate migration step** instead of migrating on startup. With several instances, all of them would try to migrate at the same time.
- **Store readers as entities**, not as a free-text borrower name. Today the same person can appear under different spellings, and the name is personal data stored without any rule for deleting it.
- **Keep Git branches up to date.** One week-4 branch was created from an outdated local `main`, and the integration test project was missing from it.

## 8. Known limitations and what breaks first under load

Known limitations:

- No authentication or authorization. Anyone who can reach the API can delete books.
- `borrow` is not idempotent (see section 5).
- `POST /api/books` with a lost commit acknowledgement is retried and fails on the primary key or the ISBN index. The client gets 500 or 409, although the book was created.
- Two simultaneous `return` calls on the same copy can both succeed. The second overwrites `ReturnedAt`. The data stays valid, but one call should have been 409.
- ISBN normalisation is done both in `IsbnAttribute` and in `BookService`.
- No circuit breaker (see section 6).
- Readiness does not switch to 503 when shutdown starts. Kestrel stops accepting connections, but a load balancer might still try to send requests for a moment.
- Migrations run on startup.

If ten thousand users arrive at once, the expected order of failure:

1. **Database connections.** Npgsql allows 100 connections per instance by default. Requests beyond that wait for a free connection and then time out. The retries make it worse: a request that timed out is sent again.
2. **Search queries.** `title` and `author` filters use `ILIKE '%text%'`, which cannot use a normal index, plus a `COUNT(*)` on every page. As the catalog grows, every search scans the whole table. A trigram index (`pg_trgm`) and keyset paging would help.
3. **Borrowing a popular copy.** `FOR UPDATE` makes requests for the same copy wait for each other. That is correct, but it is a queue.
4. **One API instance.** It has no state, so it can be scaled to several instances behind a load balancer. That is planned for month 2 in Azure.

## 9. How graceful shutdown is verified

The idea: make a request slow on purpose, stop the API while the request is running, and check that it still finishes.

1. Start everything: `docker compose up --build -d`.
2. In Swagger (http://localhost:8080/swagger), create a book and a copy. Note the `copyId`.
3. Freeze the database: `docker compose pause db`. Every new database call now hangs.
4. In Swagger, send `POST /api/copies/{copyId}/borrow`. It does not answer, because it is waiting for the database.
5. In a terminal, stop the API: `docker compose stop api`. The command does not return immediately. The API is waiting for the running request.
6. Within about 10 s, unfreeze the database: `docker compose unpause db`.
7. Expected:
   - Swagger shows **201 Created** for the borrow request.
   - `docker compose stop api` returns after the request finished, not after 30 s.
   - `docker compose logs api` shows `Application is shutting down...` **before** the log entry for `POST /api/copies/.../borrow` with status 201.
8. Start the API again with `docker compose start api`. `GET /api/books/{bookId}/copies` shows the copy as not available. The loan was saved completely.

Negative case: repeat, but wait longer than 20 s before `unpause`. The API gives up and aborts the request. After restarting, the copy is still available. The transaction was rolled back, and no half-written loan exists.

## 10. Testing summary

| Kind | What | Count |
| --- | --- | --- |
| Unit | `BookService`, `LendingService`, validation attributes, query validation, configuration validator, database failure detection | 82 |
| Integration | books CRUD, filtering and paging, copies, borrow/return, concurrent borrow, delete with loan history, error formats, health checks, startup with bad configuration, 503 when the database is unreachable | 27 |

A test that would still pass if the feature were broken: `GetAll_WithTitleFilterAndPaging` expects the second page to contain the second book created. If the `ORDER BY` were removed, PostgreSQL would most likely still return rows of a small table in insertion order, so the test would still pass. Ordering would need a test where insertion order and expected order differ.
