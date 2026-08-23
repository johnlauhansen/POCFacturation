using Xunit;
using POC_Facturation.Domain;

namespace POC_Facturation.Tests.Domain;

public class InvoiceLineItemTests
{
    [Fact]
    public void TotalHT_ShouldBeProductOfUnitPriceAndQuantity()
    {
        // Arrange
        var item = new InvoiceLineItem
        {
            UnitPriceHT = 1000m,
            Quantity = 2
        };

        // Act & Assert
        Assert.Equal(2000m, item.TotalHT);
    }

    [Theory]
    [InlineData(1000, 2, 20.0, 400)]
    [InlineData(500, 1, 5.5, 27.5)]
    [InlineData(1500, 3, 0.0, 0)]
    public void TotalTVA_ShouldBeCalculatedBasedOnTvaRate(decimal unitPrice, int quantity, decimal tvaRate, decimal expectedTva)
    {
        // Arrange
        var item = new InvoiceLineItem
        {
            UnitPriceHT = unitPrice,
            Quantity = quantity,
            TvaRate = tvaRate
        };

        // Act & Assert
        Assert.Equal(expectedTva, item.TotalTVA);
    }

    [Theory]
    [InlineData(1000, 2, 20.0, 2400)]
    [InlineData(500, 1, 5.5, 527.5)]
    [InlineData(1500, 3, 0.0, 4500)]
    public void TotalTTC_ShouldBeSumOfTotalHTAndTotalTVA(decimal unitPrice, int quantity, decimal tvaRate, decimal expectedTtc)
    {
        // Arrange
        var item = new InvoiceLineItem
        {
            UnitPriceHT = unitPrice,
            Quantity = quantity,
            TvaRate = tvaRate
        };

        // Act & Assert
        Assert.Equal(expectedTtc, item.TotalTTC);
    }
}
