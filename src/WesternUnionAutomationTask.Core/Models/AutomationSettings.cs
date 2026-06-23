namespace WesternUnionAutomationTask.Core.Models;

public sealed class AutomationSettings
{
    public string ParaBankUrl { get; set; } = "https://parabank.parasoft.com/parabank/index.htm";
    public decimal LoanAmountUsd { get; set; } = 10000m;
    public decimal DownPaymentRate { get; set; } = 0.20m;
    public decimal UsdToEurRate { get; set; } = 0.92m;
    public bool RunBrowserHeadless { get; set; } = false;
    public int BrowserWaitSeconds { get; set; } = 8;
    public bool UseFastApiFallback { get; set; } = true;
    public bool FastTransactionMode { get; set; } = true;
}
