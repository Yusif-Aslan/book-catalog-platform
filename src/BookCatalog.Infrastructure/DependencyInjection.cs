using BookCatalog.Application.Abstractions;
using BookCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookCatalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BookCatalogDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IBookRepository, EfBookRepository>();
        services.AddScoped<ILendingRepository, EfLendingRepository>();
        return services;
    }
}