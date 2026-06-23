namespace WesternUnionAutomationTask.Core.Models;

public sealed class OperationReportRow
{
    public int RowNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal InitialDepositUsd { get; set; }
    public decimal InitialDepositEur { get; set; }
    public decimal LoanAmountUsd { get; set; }
    public decimal LoanAmountEur { get; set; }
    public decimal DownPaymentUsd { get; set; }
    public decimal DownPaymentEur { get; set; }
    public string OpenedAccountNumber { get; set; } = string.Empty;
    public string LoanRequested { get; set; } = string.Empty;
    public string LoanStatus { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string DebitCardNumber { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public string AutomationStatus { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.Now;
}
