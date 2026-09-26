using System;
using System.IO;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using POC_Facturation.Domain;
using POC_Facturation.Domain.Services;

namespace POC_Facturation.Services.Pdf;

/// <summary>
/// Implémentation du service de génération PDF basée sur QuestPDF (Licence Community).
/// </summary>
public class InvoicePdfService : IInvoicePdfService
{
    static InvoicePdfService()
    {
        // Configuration de la licence QuestPDF Community pour usage non commercial / PME
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public byte[] GenerateInvoicePdf(Invoice invoice)
    {
        if (invoice == null)
            throw new ArgumentNullException(nameof(invoice));

        var document = new InvoicePdfDocument(invoice);
        return document.GeneratePdf();
    }

    public async Task GenerateInvoicePdfToFileAsync(Invoice invoice, string filePath)
    {
        if (invoice == null)
            throw new ArgumentNullException(nameof(invoice));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Le chemin du fichier de destination ne peut pas être vide.", nameof(filePath));

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = new InvoicePdfDocument(invoice);
        await Task.Run(() => document.GeneratePdf(filePath));
    }
}
