using System.Threading.Tasks;

namespace POC_Facturation.Domain.Services;

/// <summary>
/// Service de génération d'édition PDF pour les factures et avoirs de l'élevage canin.
/// </summary>
public interface IInvoicePdfService
{
    /// <summary>
    /// Génère le document PDF sous forme de tableau d'octets.
    /// </summary>
    /// <param name="invoice">Facture ou avoir à éditer.</param>
    /// <returns>Contenu binaire du fichier PDF.</returns>
    byte[] GenerateInvoicePdf(Invoice invoice);

    /// <summary>
    /// Génère et écrit directement le document PDF dans un fichier cible.
    /// </summary>
    /// <param name="invoice">Facture ou avoir à éditer.</param>
    /// <param name="filePath">Chemin d'accès absolu ou relatif du fichier PDF.</param>
    Task GenerateInvoicePdfToFileAsync(Invoice invoice, string filePath);
}
