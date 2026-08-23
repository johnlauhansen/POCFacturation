using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using POC_Facturation.Domain;
using POC_Facturation.Domain.Repositories;
using POC_Facturation.Domain.Services;

namespace POC_Facturation.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;

    public InvoiceService(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task ValidateAndSignInvoiceAsync(Invoice invoice)
    {
        if (invoice == null)
            throw new ArgumentNullException(nameof(invoice));

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Seules les factures au statut Brouillon (Draft) peuvent être validées.");

        if (!invoice.LineItems.Any())
            throw new InvalidOperationException("La facture doit contenir au moins une ligne d'article.");

        if (string.IsNullOrWhiteSpace(invoice.SellerSiret))
            throw new InvalidOperationException("Le numéro de SIRET du vendeur est obligatoire en France.");

        if (string.IsNullOrWhiteSpace(invoice.CustomerName))
            throw new InvalidOperationException("Le nom du client est obligatoire.");

        RecalculateTotals(invoice);

        var lastInvoice = await _invoiceRepository.GetLastInvoiceAsync();
        string previousSignature = lastInvoice?.Signature ?? "INITIAL_ROOT_SIGNATURE_KEY";

        bool isCreditNote = invoice.LineItems.Any(item => item.Quantity < 0 || item.UnitPriceHT < 0);
        invoice.InvoiceNumber = isCreditNote 
            ? GenerateCreditNoteNumber(invoice.IssueDate, lastInvoice)
            : GenerateInvoiceNumber(invoice.IssueDate, lastInvoice);

        invoice.PreviousInvoiceSignature = previousSignature;
        invoice.Signature = CalculateSignature(invoice, previousSignature);

        invoice.Status = InvoiceStatus.Validated;

        if (invoice.Id == 0)
        {
            await _invoiceRepository.AddAsync(invoice);
        }
        else
        {
            await _invoiceRepository.UpdateAsync(invoice);
        }
    }

    public async Task<Invoice> CreateCreditNoteAsync(int originalInvoiceId)
    {
        Invoice? originalInvoice = await _invoiceRepository.GetByIdAsync(originalInvoiceId);
        if (originalInvoice == null)
            throw new InvalidOperationException($"La facture d'origine avec l'ID {originalInvoiceId} n'existe pas.");

        if (originalInvoice.Status != InvoiceStatus.Validated)
            throw new InvalidOperationException("Un avoir ne peut être créé que pour une facture déjà validée officiellement.");

        // Vérifier si un avoir n'a pas déjà été créé (pour éviter les doublons d'annulation)
        Invoice? creditNote = new Invoice
        {
            Status = InvoiceStatus.Draft,
            IssueDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            
            // Recopie des informations du vendeur
            SellerName = originalInvoice.SellerName,
            SellerSiret = originalInvoice.SellerSiret,
            SellerTvaNumber = originalInvoice.SellerTvaNumber,
            SellerAddress = originalInvoice.SellerAddress,

            // Recopie des informations du client
            CustomerName = originalInvoice.CustomerName,
            CustomerAddress = originalInvoice.CustomerAddress,
            IsTvaApplicable = originalInvoice.IsTvaApplicable
        };

        foreach (var item in originalInvoice.LineItems)
        {
            creditNote.LineItems.Add(new InvoiceLineItem
            {
                Description = $"Avoir sur facture {originalInvoice.InvoiceNumber} - {item.Description}",
                Quantity = -item.Quantity,
                UnitPriceHT = item.UnitPriceHT,
                TvaRate = item.TvaRate,
                DogDetailId = item.DogDetailId
            });
        }

        RecalculateTotals(creditNote);

        return creditNote;
    }

    /// <summary>
    /// Recalcul des coùts totaux de la facture
    /// </summary>
    /// <param name="invoice">Facture à recalculer</param>
    private static void RecalculateTotals(Invoice invoice)
    {
        invoice.TotalHT = invoice.LineItems.Sum(item => item.TotalHT);
        
        if (invoice.IsTvaApplicable)
        {
            invoice.TotalTVA = invoice.LineItems.Sum(item => item.TotalTVA);
            invoice.TotalTTC = invoice.LineItems.Sum(item => item.TotalTTC);
        }
        else
        {
            invoice.TotalTVA = 0;
            invoice.TotalTTC = invoice.TotalHT;
        }
    }

    /// <summary>
    /// Génère un numéro de facture unique
    /// Format attendu : F-AAAA-NNNN (ex: F-2026-0001)
    /// </summary>
    /// <param name="issueDate">Date de facturation</param>
    /// <param name="lastInvoiceNumber">Numero de Facture précédente</param>
    /// <returns>Renvoie le nouveau numerio de facture</returns>
    private static string GenerateInvoiceNumber(DateTime issueDate, Invoice? lastInvoice)
    {
        int currentYear = issueDate.Year;
        int nextSequence = 1;

        if (lastInvoice != null && !string.IsNullOrEmpty(lastInvoice.InvoiceNumber))
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 3 && parts[0] == "F"
                && int.TryParse(parts[1], out int lastYear) && int.TryParse(parts[2], out int lastSeq)
                && lastYear == currentYear)
            {
                nextSequence = lastSeq + 1;
            }
        }

        return $"F-{currentYear}-{nextSequence:D4}";
    }

    /// <summary>
    /// Génère le numéro d'avenant
    /// Format attendu : AV-AAAA-NNNN (ex: AV-2026-0001)
    /// </summary>
    /// <param name="issueDate"></param>
    /// <param name="lastInvoice"></param>
    /// <returns></returns>
    private static string GenerateCreditNoteNumber(DateTime issueDate, Invoice? lastInvoice)
    {
        int currentYear = issueDate.Year;
        int nextSequence = 1;

        if (lastInvoice != null && !string.IsNullOrEmpty(lastInvoice.InvoiceNumber))
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 3 && parts[0] == "AV" 
                && int.TryParse(parts[1], out int lastYear) && int.TryParse(parts[2], out int lastSeq) 
                && lastYear == currentYear)
            {
                nextSequence = lastSeq + 1;
            }
        }

        return $"AV-{currentYear}-{nextSequence:D4}";
    }

    /// <summary>
    /// Génération de la signature Cryptographique SHA-256
    /// </summary>
    /// <param name="invoice">Facture</param>
    /// <param name="previousSignature">signature précédente</param>
    /// <returns></returns>
    private static string CalculateSignature(Invoice invoice, string previousSignature)
    {
        string dataToSign = $"{invoice.InvoiceNumber}|" +
                            $"{invoice.IssueDate:yyyy-MM-ddTHH:mm:ss}|" +
                            $"{invoice.TotalTTC.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}|" +
                            $"{previousSignature}";

        using var sha256 = SHA256.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(dataToSign);
        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
