using BookCatalog.Api.ErrorHandling;
using BookCatalog.Application.Books;
using BookCatalog.Application.Lending;
using BookCatalog.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Book Catalog API";
        document.Info.Description =
            "REST API for managing a book catalog. " +
            "Errors are returned as ProblemDetails (RFC 9457).";
        return Task.CompletedTask;
    });
});
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod
                            | HttpLoggingFields.RequestPath
                            | HttpLoggingFields.ResponseStatusCode
                            | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .Configure<IConfiguration>((options, configuration) =>
        options.ConnectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName) ?? string.Empty)
    .ValidateOnStart();

builder.Services.AddInfrastructure();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ILendingService, LendingService>();

var app = builder.Build();

var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

if (databaseOptions.MigrateOnStartup)
    await app.Services.ApplyMigrationsAsync();

app.UseHttpLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BookCatalog API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).WithHttpLogging(HttpLoggingFields.None);

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready)
}).WithHttpLogging(HttpLoggingFields.None);

app.MapControllers();

await app.RunAsync();