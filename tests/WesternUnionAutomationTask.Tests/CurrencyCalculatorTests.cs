using WesternUnionAutomationTask.Core.Services;
using Xunit;

namespace WesternUnionAutomationTask.Tests;

public sealed class CurrencyCalculatorTests
{
    [Theory]
    [InlineData(1000, 0.20, 200)]
    [InlineData(750.55, 0.20, 150.11)]
    public void CalculateDownPayment_ReturnsTwentyPercent(decimal initialDeposit, decimal rate, decimal expected)
    {
        var actual = new CurrencyCalculator().CalculateDownPayment(initialDeposit, rate);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConvertUsdToEur_UsesConfiguredRate()
    {
        var actual = new CurrencyCalculator().ConvertUsdToEur(10000m, 0.92m);

        Assert.Equal(9200m, actual);
    }
}
