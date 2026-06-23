namespace WesternUnionAutomationTask.Core.Services;

public sealed class CurrencyCalculator
{
    public decimal CalculateDownPayment(decimal initialDepositUsd, decimal downPaymentRate)
    {
        return Math.Round(initialDepositUsd * downPaymentRate, 2, MidpointRounding.AwayFromZero);
    }

    public decimal ConvertUsdToEur(decimal amountUsd, decimal usdToEurRate)
    {
        return Math.Round(amountUsd * usdToEurRate, 2, MidpointRounding.AwayFromZero);
    }
}
