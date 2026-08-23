using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using POC_Facturation.Domain;
using POC_Facturation.Services;
using POC_Facturation.Tests.Mocks;

namespace POC_Facturation.Tests.Services;

public class InvoiceServiceTests
{
    [Fact]
    public async Task ValidateAndSignInvoiceAsync_WithValidInvoice_ShouldValidateAndSign()
    {
        // Arrange
        var mockRepo = new MockInvoiceRepository();
        var service = new InvoiceService(mockRepo);

        var invoice = new Invoice
        {
            Status = InvoiceStatus.Draft,
            SellerSiret = "12345678901234",
            CustomerName = "Jean Dupont",
            IssueDate = new DateTime(2026, 08, 23),
            LineItems = new List<InvoiceLineItem>
            {
                new() { Description = "Vente Chiot Berger Allemand", Quantity = 1, UnitPriceHT = 1200m, TvaRate = 20m }
            }
        };

        // Act
        await service.ValidateAndSignInvoiceAsync(invoice);

        // Assert
        Assert.Equal(InvoiceStatus.Validated, invoice.Status);
        Assert.Equal("F-2026-0001", invoice.InvoiceNumber);
        Assert.Equal("INITIAL_ROOT_SIGNATURE_KEY", invoice.PreviousInvoiceSignature);
        Assert.False(string.IsNullOrEmpty(invoice.Signature));
        Assert.True(mockRepo.AddCalled);
    }

    [Fact]
    public async Task ValidateAndSignInvoiceAsync_WithNoLines_ShouldThrowException()
    {
        // Arrange
        var mockRepo = new MockInvoiceRepository();
        var service = new InvoiceService(mockRepo);

        var invoice = new Invoice
        {
            Status = InvoiceStatus.Draft,
            SellerSiret = "12345678901234",
            CustomerName = "Jean Dupont",
            LineItems = new List<InvoiceLineItem>() // Vide
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ValidateAndSignInvoiceAsync(invoice));
        Assert.Contains("au moins une ligne d'article", ex.Message);
    }

    [Fact]
    public async Task ValidateAndSignInvoiceAsync_WithNoSiret_ShouldThrowException()
    {
        // Arrange
        var mockRepo = new MockInvoiceRepository();
        var service = new InvoiceService(mockRepo);

        var invoice = new Invoice
        {
            Status = InvoiceStatus.Draft,
            SellerSiret = "", // Manquant
            CustomerName = "Jean Dupont",
            LineItems = new List<InvoiceLineItem> { new() { Description = "Test", UnitPriceHT = 10m } }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ValidateAndSignInvoiceAsync(invoice));
        Assert.Contains("numéro de SIRET", ex.Message);
    }

    [Fact]
    public async Task ValidateAndSignInvoiceAsync_WithAlreadyValidatedInvoice_ShouldThrowException()
    {
        // Arrange
        var mockRepo = new MockInvoiceRepository();
        var service = new InvoiceService(mockRepo);

        var invoice = new Invoice
        {
            Status = InvoiceStatus.Validated, // Déjà validée !
            SellerSiret = "12345678901234",
            CustomerName = "Jean Dupont",
            LineItems = new List<InvoiceLineItem> { new() { Description = "Test", UnitPriceHT = 10m } }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ValidateAndSignInvoiceAsync(invoice));
        Assert.Contains("Brouillon (Draft) peuvent être validées", ex.Message);
    }

    [Fact]
    public async Task ValidateAndSignInvoiceAsync_MultipleInvoices_ShouldChainSignaturesCorrectly()
    {
        // Arrange
        var mockRepo = new MockInvoiceRepository();
        var service = new InvoiceService(mockRepo);

        var invoice1 = new Invoice
        {
            Status = InvoiceStatus.Draft,
            SellerSiret = "12345678901234",
            CustomerName = "Client Un",
            IssueDate = new DateTime(2026, 08, 23),
            LineItems = new List<InvoiceLineItem> { new() { Description = "Chiot 1", UnitPriceHT = 1000m, TvaRate = 20m } }
        };

        var invoice2 = new Invoice
        {
            Status = InvoiceStatus.Draft,
            SellerSiret = "12345678901234",
            CustomerName = "Client Deux",
            IssueDate = new DateTime(2026, 08, 23),
            LineItems = new List<InvoiceLineItem> { new() { Description = "Chiot 2", UnitPriceHT = 1500m, TvaRate = 20m } }
        };

        // Act
        await service.ValidateAndSignInvoiceAsync(invoice1);
        await service.ValidateAndSignInvoiceAsync(invoice2); // Va lire invoice1 de la base pour le chaînage

        // Assert
        Assert.Equal("F-2026-0001", invoice1.InvoiceNumber);
        Assert.Equal("F-2026-0002", invoice2.InvoiceNumber); // Incrément de séquence
        Assert.Equal("INITIAL_ROOT_SIGNATURE_KEY", invoice1.PreviousInvoiceSignature);
        Assert.Equal(invoice1.Signature, invoice2.PreviousInvoiceSignature); // Chaînage cryptographique
        Assert.NotEqual(invoice1.Signature, invoice2.Signature);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_WithNonValidatedInvoice_ShouldThrowException()
    {
        // Arrange
        var mockRepo = new MockInvoiceRepository();
        var service = new InvoiceService(mockRepo);

        var draftInvoice = new Invoice { Id = 1, Status = InvoiceStatus.Draft };
        await mockRepo.AddAsync(draftInvoice);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateCreditNoteAsync(draftInvoice.Id));
        Assert.Contains("déjà validée officiellement", ex.Message);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_WithValidatedInvoice_ShouldCreateDraftCreditNoteWithNegativeTotals()
    {
        // Arrange
        var mockRepo = new MockInvoiceRepository();
        var service = new InvoiceService(mockRepo);

        var originalInvoice = new Invoice
        {
            Id = 0,
            Status = InvoiceStatus.Draft,
            SellerSiret = "12345678901234",
            SellerName = "Elevage Elevé",
            CustomerName = "Jean Client",
            IsTvaApplicable = true,
            LineItems = new List<InvoiceLineItem>
            {
                new() { Description = "Chiot Vente", Quantity = 1, UnitPriceHT = 1000m, TvaRate = 20m }
            }
        };

        // Valider l'originale d'abord pour qu'elle passe au statut Validated
        await service.ValidateAndSignInvoiceAsync(originalInvoice);

        // Act
        var creditNote = await service.CreateCreditNoteAsync(originalInvoice.Id);

        // Assert
        Assert.Equal(InvoiceStatus.Draft, creditNote.Status); // L'avoir créé est un brouillon à valider
        Assert.Equal(originalInvoice.SellerSiret, creditNote.SellerSiret);
        Assert.Equal(originalInvoice.CustomerName, creditNote.CustomerName);
        Assert.True(creditNote.IsTvaApplicable);
        
        Assert.Single(creditNote.LineItems);
        var item = creditNote.LineItems.First();
        Assert.Equal(-1, item.Quantity); // Quantité négative
        Assert.Equal(1000m, item.UnitPriceHT);
        Assert.Equal(-1000m, item.TotalHT);
        Assert.Equal(-200m, item.TotalTVA);
        Assert.Equal(-1200m, item.TotalTTC);

        Assert.Equal(-1000m, creditNote.TotalHT);
        Assert.Equal(-200m, creditNote.TotalTVA);
        Assert.Equal(-1200m, creditNote.TotalTTC);
    }
}
