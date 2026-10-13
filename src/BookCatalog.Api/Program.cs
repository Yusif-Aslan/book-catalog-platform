using BookCatalog.Application.Books;
using BookCatalog.Application.Lending;
using BookCatalog.Infrastructure;
using BookCatalog.Api.ErrorHandling;

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
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var connectionString = builder.Configuration.GetConnectionString("BookCatalog")
                       ?? throw new InvalidOperationException("Connection string 'BookCatalog' is not configured.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ILendingService, LendingService>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.ApplyMigrationsAsync();

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
app.MapControllers();

await app.RunAsync(); 