using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POC_Facturation.Data.Repositories;
using POC_Facturation.Domain.Repositories;

namespace POC_Facturation.Data;

public static class DataServiceRegistration
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, string connectionString)
    {
        // Enregistrement du DbContext SQLite interne
        services.AddDbContext<InvoiceDbContext>(options =>
            options.UseSqlite(connectionString));

        // Enregistrement des dépôts sous leurs abstractions publiques
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IDogRepository, DogRepository>();

        return services;
    }
}
