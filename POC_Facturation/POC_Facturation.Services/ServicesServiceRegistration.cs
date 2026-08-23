using Microsoft.Extensions.DependencyInjection;
using POC_Facturation.Domain.Services;

namespace POC_Facturation.Services;

public static class ServicesServiceRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Enregistrement des services métiers applicatifs
        services.AddScoped<IInvoiceService, InvoiceService>();

        return services;
    }
}
