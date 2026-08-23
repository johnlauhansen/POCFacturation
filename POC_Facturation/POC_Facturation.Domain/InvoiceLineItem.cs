namespace POC_Facturation.Domain;

public class InvoiceLineItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;      // Description de l'article (ex: Vente de chiot Berger Allemand Mâle)
    public int Quantity { get; set; } = 1;
    public decimal UnitPriceHT { get; set; }                     // Prix unitaire HT
    public decimal TvaRate { get; set; } = 20.0m;                // Taux de TVA (standard à 20% en France pour la vente d'animaux)
    
    // Informations spécifiques du chien associé si c'est une vente de chien
    public int? DogDetailId { get; set; }
    public DogDetail? DogDetail { get; set; }

    // Propriétés calculées
    public decimal TotalHT => UnitPriceHT * Quantity;
    public decimal TotalTVA => TotalHT * (TvaRate / 100m);
    public decimal TotalTTC => TotalHT + TotalTVA;
}
