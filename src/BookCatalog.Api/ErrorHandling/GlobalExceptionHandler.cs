using BookCatalog.Application.Exceptions;
using BookCatalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookCatalog.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const int ClientClosedRequest = 499;
    private const string RetryAfterSeconds = "10";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation(
                "Request {Method} {Path} was cancelled by the client",
                httpContext.Request.Method, httpContext.Request.Path);

            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var problem = CreateProblem(httpContext, exception);
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private ProblemDetails CreateProblem(HttpContext httpContext, Exception exception)
    {
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path;

        if (exception is ConflictException conflict)
        {
            logger.LogInformation(
                "Request {Method} {Path} rejected with conflict: {Reason}",
                method, path, conflict.Message);

            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = conflict.Title,
                Detail = conflict.Message
            };
        }

        if (DatabaseFailures.IsUnavailable(exception))
        {
            logger.LogError(exception, "Database unavailable while processing {Method} {Path}", method, path);

            httpContext.Response.Headers.RetryAfter = RetryAfterSeconds;

            return new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service temporarily unavailable",
                Detail = "A dependency of the service is not reachable. Retry the request later."
            };
        }

        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", method, path);

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = "The server could not process the request. Include the traceId when reporting this problem."
        };
    }
}
