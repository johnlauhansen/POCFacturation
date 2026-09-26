using Microsoft.Extensions.DependencyInjection;
using POC_Facturation.Domain.Services;
using POC_Facturation.Services.Pdf;

namespace POC_Facturation.Services;

public static class ServicesServiceRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Enregistrement des services métiers applicatifs
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();
        services.AddSingleton<IGlobalExceptionHandler, GlobalExceptionHandler>();

        return services;
    }
}
