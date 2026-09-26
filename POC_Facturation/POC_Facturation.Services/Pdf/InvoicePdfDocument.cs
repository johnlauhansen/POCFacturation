using System;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POC_Facturation.Domain;

namespace POC_Facturation.Services.Pdf;

/// <summary>
/// Définition déclarative QuestPDF pour la génération du document officiel de facture / avoir.
/// </summary>
public class InvoicePdfDocument : IDocument
{
    public Invoice Invoice { get; }

    public InvoicePdfDocument(Invoice invoice)
    {
        Invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Lato").FontColor(Colors.Grey.Darken3));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        var titleColor = Invoice.LineItems.Any(i => i.Quantity < 0 || i.UnitPriceHT < 0) 
            ? Colors.Red.Darken2 
            : Colors.Blue.Darken3;

        string documentTitle = Invoice.LineItems.Any(i => i.Quantity < 0 || i.UnitPriceHT < 0)
            ? "AVOIR FISCAL"
            : (Invoice.Status == InvoiceStatus.Draft ? "FACTURE (BROUILLON)" : "FACTURE OFFICIELLE");

        container.Column(column =>
        {
            // Ligne supérieure : Vendeur (Gauche) et Titre / Numéro de facture (Droite)
            column.Item().Row(row =>
            {
                // Vendeur (Élevage)
                row.RelativeItem(3).Column(col =>
                {
                    col.Item().Text(Invoice.SellerName.ToUpperInvariant()).FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Text(Invoice.SellerAddress).FontSize(9.5f).FontColor(Colors.Grey.Darken2);
                    col.Item().Text($"SIRET : {Invoice.SellerSiret}").FontSize(9f).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrWhiteSpace(Invoice.SellerTvaNumber))
                    {
                        col.Item().Text($"N° TVA : {Invoice.SellerTvaNumber}").FontSize(9f).FontColor(Colors.Grey.Darken1);
                    }
                });

                // Titre et métadonnées facture
                row.RelativeItem(2).AlignRight().Column(col =>
                {
                    col.Item().Text(documentTitle).FontSize(16).Bold().FontColor(titleColor);
                    col.Item().Text($"N° {Invoice.InvoiceNumber}").FontSize(12).Bold().FontColor(Colors.Grey.Darken4);
                    col.Item().PaddingTop(4).Text($"Date d'émission : {Invoice.IssueDate:dd/MM/yyyy}").FontSize(9f);
                    col.Item().Text($"Date d'échéance : {Invoice.DueDate:dd/MM/yyyy}").FontSize(9f);
                });
            });

            column.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Bloc Client (Destinataire de la facture)
            column.Item().PaddingTop(10).PaddingBottom(15).Row(row =>
            {
                row.RelativeItem(3).Text(""); // Espacement gauche

                row.RelativeItem(2).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(10).Column(clientCol =>
                {
                    clientCol.Item().Text("FACTURÉ À :").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken1);
                    clientCol.Item().PaddingTop(2).Text(Invoice.CustomerName).FontSize(11).Bold().FontColor(Colors.Grey.Darken4);
                    clientCol.Item().Text(Invoice.CustomerAddress).FontSize(9.5f).FontColor(Colors.Grey.Darken2);
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");

        container.Column(column =>
        {
            // Tableau des articles / prestations
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(5); // Description et détails du chiot
                    columns.RelativeColumn(2); // P.U. HT
                    columns.RelativeColumn(1); // Qté
                    columns.RelativeColumn(1.5f); // TVA %
                    columns.RelativeColumn(2); // Total TTC
                });

                // En-tête du tableau
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("Description de la prestation / Animal").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignRight().Text("P.U. HT").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignCenter().Text("Qté").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignCenter().Text("TVA").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignRight().Text("Total TTC").Bold().FontColor(Colors.White);
                });

                // Lignes de facture
                for (int i = 0; i < Invoice.LineItems.Count; i++)
                {
                    var item = Invoice.LineItems[i];
                    var bgColor = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Column(itemCol =>
                    {
                        itemCol.Item().Text(item.Description).Bold();

                        // Mentions d'identification de l'animal si vente d'un chiot
                        if (item.DogDetail != null)
                        {
                            var dog = item.DogDetail;
                            string lofText = dog.IsLof ? $"LOF : {dog.LofNumber}" : "Non LOF (Type)";
                            string passportText = !string.IsNullOrWhiteSpace(dog.PassportNumber) ? $" | Pass : {dog.PassportNumber}" : "";
                            itemCol.Item().PaddingTop(2).Text($"🐾 {dog.Breed} ({dog.Color}) | Sexe : {dog.DogSex} | I-CAD : {dog.IcadNumber} | {lofText}{passportText}")
                                .FontSize(8f).Italic().FontColor(Colors.Blue.Darken2);
                        }
                    });

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight()
                        .Text(item.UnitPriceHT.ToString("N2", culture) + " €");

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignCenter()
                        .Text(item.Quantity.ToString());

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignCenter()
                        .Text(item.TvaRate.ToString("0.#") + " %");

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight()
                        .Text(item.TotalTTC.ToString("N2", culture) + " €").Bold();
                }
            });

            // Récapitulatif des totaux financiers (à droite)
            column.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem(3).Text(""); // Espace gauche

                row.RelativeItem(2).Column(totalCol =>
                {
                    totalCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Total HT :").FontSize(10);
                        r.RelativeItem().AlignRight().Text(Invoice.TotalHT.ToString("N2", culture) + " €").SemiBold();
                    });

                    totalCol.Item().PaddingTop(3).Row(r =>
                    {
                        r.RelativeItem().Text("Montant TVA :").FontSize(10);
                        r.RelativeItem().AlignRight().Text(Invoice.TotalTVA.ToString("N2", culture) + " €").SemiBold();
                    });

                    totalCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    totalCol.Item().PaddingTop(5).Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL TTC :").FontSize(12).Bold().FontColor(Colors.Blue.Darken4);
                        r.RelativeItem().AlignRight().Text(Invoice.TotalTTC.ToString("N2", culture) + " €").FontSize(13).Bold().FontColor(Colors.Blue.Darken4);
                    });
                });
            });

            // Mention légale exonération Art. 293 B si non assujetti
            if (!Invoice.IsTvaApplicable)
            {
                column.Item().PaddingTop(15).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(6)
                    .AlignCenter().Text("Mention légale : « TVA non applicable - article 293 B du Code Général des Impôts (CGI) ».")
                    .FontSize(8.5f).Italic().FontColor(Colors.Grey.Darken2);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            // Bloc de Conformité Fiscale NF525 / Anti-Fraude TVA (si validée)
            if (Invoice.Status == InvoiceStatus.Validated)
            {
                column.Item().Border(1).BorderColor(Colors.Green.Lighten2).Background(Colors.Green.Lighten5).Padding(8).Column(sigCol =>
                {
                    sigCol.Item().Text("🔒 CONFORMITÉ FISCALE & SCELLAGE NUMÉRIQUE (Art. 286 du CGI / NF525)").FontSize(8.5f).Bold().FontColor(Colors.Green.Darken3);
                    sigCol.Item().PaddingTop(2).Text($"Empreinte précédente : {Invoice.PreviousInvoiceSignature}").FontSize(7.5f).FontFamily("Consolas").FontColor(Colors.Green.Darken2);
                    sigCol.Item().Text($"Signature SHA-256 : {Invoice.Signature}").FontSize(7.5f).Bold().FontFamily("Consolas").FontColor(Colors.Green.Darken3);
                    sigCol.Item().PaddingTop(2).Text("Ce document est scellé électroniquement. Son intégrité et son inaltérabilité sont garanties en base de données.")
                        .FontSize(7.5f).Italic().FontColor(Colors.Green.Darken3);
                });
            }

            // Bas de page classique avec pagination
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text("Facture émise par le logiciel POC Facturation Élevage Canin").FontSize(8f).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" sur ");
                    x.TotalPages();
                });
            });
        });
    }
}
