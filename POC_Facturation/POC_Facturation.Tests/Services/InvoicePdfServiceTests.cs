using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using POC_Facturation.Domain;
using POC_Facturation.Services.Pdf;

namespace POC_Facturation.Tests.Services;

/// <summary>
/// Tests unitaires pour InvoicePdfService (génération QuestPDF) respectant les principes AAA et Carrefour.
/// </summary>
public class InvoicePdfServiceTests
{
    private static Invoice CreateSampleValidatedInvoice()
    {
        return new Invoice
        {
            Id = 1,
            InvoiceNumber = "F-2026-0001",
            Status = InvoiceStatus.Validated,
            IssueDate = new DateTime(2026, 09, 26),
            DueDate = new DateTime(2026, 10, 26),
            SellerName = "Élevage du Val de la Sensée",
            SellerSiret = "12345678901234",
            SellerTvaNumber = "FR12345678901",
            SellerAddress = "12 Rue de la Ferme, 59000 Lille",
            CustomerName = "Jean Dupont",
            CustomerAddress = "45 Avenue des Vosges, 67000 Strasbourg",
            IsTvaApplicable = true,
            PreviousInvoiceSignature = "INITIAL_ROOT_SIGNATURE_KEY",
            Signature = "KzH4v1f87s...SHA256==",
            LineItems = new ObservableCollection<InvoiceLineItem>
            {
                new()
                {
                    Description = "Vente Chiot Berger Allemand",
                    Quantity = 1,
                    UnitPriceHT = 1200m,
                    TvaRate = 20.0m,
                    DogDetail = new DogDetail
                    {
                        Breed = "Berger Allemand",
                        Color = "Noir et Feu",
                        DogSex = Sex.Male,
                        IcadNumber = "250269600123456",
                        IsLof = true,
                        LofNumber = "LOF-BA-2026-9876",
                        PassportNumber = "FR-2026-PA-001"
                    }
                },
                new()
                {
                    Description = "Sac d'aliment chiot premium (15kg)",
                    Quantity = 2,
                    UnitPriceHT = 60m,
                    TvaRate = 20.0m
                }
            }
        };
    }

    [Fact]
    public void Should_GenerateValidPdfBytes_When_InvoiceIsValid()
    {
        // Arrange
        var service = new InvoicePdfService();
        var invoice = CreateSampleValidatedInvoice();

        // Act
        byte[] pdfBytes = service.GenerateInvoicePdf(invoice);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "Le fichier PDF généré doit contenir plus de 1 Ko de données.");

        // Vérification de la signature binaire d'un fichier PDF (%PDF-)
        string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task Should_GenerateValidPdfFile_When_InvoiceIsValid()
    {
        // Arrange
        var service = new InvoicePdfService();
        var invoice = CreateSampleValidatedInvoice();
        string tempFilePath = Path.Combine(Path.GetTempPath(), $"Facture_Test_{Guid.NewGuid():N}.pdf");

        try
        {
            // Act
            await service.GenerateInvoicePdfToFileAsync(invoice, tempFilePath);

            // Assert
            Assert.True(File.Exists(tempFilePath));
            var fileInfo = new FileInfo(tempFilePath);
            Assert.True(fileInfo.Length > 1000);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public void Should_GenerateValidPdfBytes_When_InvoiceIsCreditNote()
    {
        // Arrange
        var service = new InvoicePdfService();
        var invoice = CreateSampleValidatedInvoice();
        invoice.InvoiceNumber = "AV-2026-0001";
        invoice.LineItems = new ObservableCollection<InvoiceLineItem>
        {
            new()
            {
                Description = "Avoir sur facture F-2026-0001",
                Quantity = -1,
                UnitPriceHT = 1200m,
                TvaRate = 20.0m
            }
        };

        // Act
        byte[] pdfBytes = service.GenerateInvoicePdf(invoice);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 500);
        string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_InvoiceIsNull()
    {
        // Arrange
        var service = new InvoicePdfService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.GenerateInvoicePdf(null!));
    }
}
