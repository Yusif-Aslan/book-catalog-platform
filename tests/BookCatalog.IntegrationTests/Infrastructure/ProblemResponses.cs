namespace BookCatalog.IntegrationTests.Infrastructure;

public sealed record ProblemResponse(int? Status, string? Title, string? Detail, string? TraceId);

public sealed record ValidationProblemResponse(int? Status, Dictionary<string, string[]> Errors)
{
    public bool HasErrorFor(string field) =>
        Errors.Keys.Any(key => string.Equals(key, field, StringComparison.OrdinalIgnoreCase));
}