namespace WesternUnionAutomationTask.Core.Models;

public sealed class CustomerValidationResult
{
    public CustomerValidationResult(CustomerProfile customer, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
    {
        Customer = customer;
        Warnings = warnings;
        Errors = errors;
    }

    public CustomerProfile Customer { get; }
    public IReadOnlyList<string> Warnings { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool CanProcess => Errors.Count == 0;
    public string Status => CanProcess ? (Warnings.Count == 0 ? "Ready" : "Ready with warnings") : "Skipped";
}
