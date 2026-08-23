using System;
using System.Collections.Generic;

namespace POC_Facturation.Domain;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;     // Numéro séquentiel sans trou (ex: F-2026-0001)
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    
    // Coordonnées du vendeur
    public string SellerName { get; set; } = string.Empty;
    public string SellerSiret { get; set; } = string.Empty;       // SIRET obligatoire en France
    public string SellerTvaNumber { get; set; } = string.Empty;   // Numéro de TVA intra-communautaire (facultatif si non applicable)
    public string SellerAddress { get; set; } = string.Empty;

    // Coordonnées du client
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    
    // Lignes de la facture
    public List<InvoiceLineItem> LineItems { get; set; } = new();
    
    // Indicateur si assujetti à la TVA
    public bool IsTvaApplicable { get; set; } = true;             // false si micro-entreprise (Franchise en base de TVA - art. 293 B du CGI)

    // Totaux calculés
    public decimal TotalHT { get; set; }
    public decimal TotalVAT { get; set; }
    public decimal TotalTTC { get; set; }

    // --- Sécurité Anti-Fraude (Loi TVA / NF525) ---
    public string Signature { get; set; } = string.Empty;                 // Hash SHA-256 de cette facture
    public string PreviousInvoiceSignature { get; set; } = string.Empty;  // Signature de la facture précédente (chaînage)
}
