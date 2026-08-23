namespace POC_Facturation.Domain;

public enum InvoiceStatus
{
    Draft,       // Brouillon : modifiable et supprimable
    Validated,   // Validée : immuable, numéro de facture attribué, chaînée cryptographiquement
    Cancelled    // Annulée : remplacée par un avoir ou rectifiée
}
