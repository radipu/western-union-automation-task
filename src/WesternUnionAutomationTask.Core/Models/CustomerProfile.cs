namespace WesternUnionAutomationTask.Core.Models;

public sealed class CustomerProfile
{
    public int RowNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Ssn { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal InitialDepositUsd { get; set; }
    public string DobRaw { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string DebitCardNumber { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
}
