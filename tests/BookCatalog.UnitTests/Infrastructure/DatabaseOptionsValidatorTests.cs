using BookCatalog.Infrastructure;

namespace BookCatalog.UnitTests.Infrastructure;

public class DatabaseOptionsValidatorTests
{
    private const string ValidConnectionString =
        "Host=localhost;Port=5432;Database=bookcatalog;Username=bookcatalog;Password=secret";

    private readonly DatabaseOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithValidOptions_Succeeds()
    {
        var result = _validator.Validate(null, Options(ValidConnectionString));

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithoutConnectionString_FailsWithKeyName(string connectionString)
    {
        var result = _validator.Validate(null, Options(connectionString));

        Assert.True(result.Failed);
        Assert.Contains("ConnectionStrings:BookCatalog is required.", result.Failures!);
    }

    [Fact]
    public void Validate_WithUnknownKeyword_Fails()
    {
        var result = _validator.Validate(null, Options("Hots=localhost;Database=bookcatalog"));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.StartsWith("ConnectionStrings:BookCatalog is not a valid PostgreSQL connection string"));
    }

    [Fact]
    public void Validate_WithoutHostAndDatabase_ReportsBoth()
    {
        var result = _validator.Validate(null, Options("Username=bookcatalog;Password=secret"));

        Assert.True(result.Failed);
        Assert.Contains("ConnectionStrings:BookCatalog must specify Host.", result.Failures!);
        Assert.Contains("ConnectionStrings:BookCatalog must specify Database.", result.Failures!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(301)]
    public void Validate_WithCommandTimeoutOutOfRange_Fails(int seconds)
    {
        var options = Options(ValidConnectionString);
        options.CommandTimeoutSeconds = seconds;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains($"Database:CommandTimeoutSeconds must be between 1 and 300, but was {seconds}.", result.Failures!);
    }

    [Theory]
    [InlineData(-1, 5, "Database:MaxRetryCount must be between 0 and 10, but was -1.")]
    [InlineData(11, 5, "Database:MaxRetryCount must be between 0 and 10, but was 11.")]
    [InlineData(3, 0, "Database:MaxRetryDelaySeconds must be between 1 and 60, but was 0.")]
    [InlineData(3, 61, "Database:MaxRetryDelaySeconds must be between 1 and 60, but was 61.")]
    public void Validate_WithRetrySettingsOutOfRange_Fails(int maxRetryCount, int maxRetryDelaySeconds, string expectedError)
    {
        var options = Options(ValidConnectionString);
        options.MaxRetryCount = maxRetryCount;
        options.MaxRetryDelaySeconds = maxRetryDelaySeconds;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(expectedError, result.Failures!);
    }

    [Fact]
    public void Validate_WithInvalidConnectionString_DoesNotRevealPassword()
    {
        var result = _validator.Validate(null, Options("Hots=localhost;Password=super-secret"));

        Assert.True(result.Failed);
        Assert.DoesNotContain(result.Failures!, f => f.Contains("super-secret"));
    }

    private static DatabaseOptions Options(string connectionString) => new() { ConnectionString = connectionString };
}
