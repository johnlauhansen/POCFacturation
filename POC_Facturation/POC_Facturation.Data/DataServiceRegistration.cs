using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POC_Facturation.Data.Repositories;
using POC_Facturation.Domain;
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

            // Seed initial de chiens si la base est vide
            if (!dbContext.DogDetails.Any())
            {
                dbContext.DogDetails.AddRange(new DogDetail[]
                {
                    new() { IcadNumber = "250268712345678", Breed = "Berger Allemand", DogSex = Sex.Male, BirthDate = new DateTime(2025, 05, 10), IsLof = true, LofNumber = "LOF-123456/789", Color = "Noir et Feu", PassportNumber = "FR123456" },
                    new() { IcadNumber = "250268712345679", Breed = "Golden Retriever", DogSex = Sex.Female, BirthDate = new DateTime(2025, 06, 15), IsLof = true, LofNumber = "LOF-987654/321", Color = "Sable", PassportNumber = "FR987654" },
                    new() { IcadNumber = "250268712345680", Breed = "Jack Russell", DogSex = Sex.Male, BirthDate = new DateTime(2025, 12, 01), IsLof = false, Color = "Blanc et Marron", PassportNumber = "" }
                });
                dbContext.SaveChanges();
            }
        }
        return serviceProvider;
    }
}
