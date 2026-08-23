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

    /// <summary>
    /// Initialise la base de données SQLite locale en s'assurant de la création du schéma.
    /// Cette méthode permet d'encapsuler totalement l'usage du DbContext interne.
    /// </summary>
    public static IServiceProvider InitializeDatabase(this IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            dbContext.Database.EnsureCreated();
        }
        return serviceProvider;
    }
}
