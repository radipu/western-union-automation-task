using WesternUnionAutomationTask.Core.Models;

namespace WesternUnionAutomationTask.Core.Validators;

public sealed class CustomerValidator
{
    public CustomerValidationResult Validate(CustomerProfile customer)
    {
        var warnings = new List<string>();
        var errors = new List<string>();

        Require(customer.FirstName, "First name", errors);
        Require(customer.LastName, "Last name", errors);
        Require(customer.Address, "Address", errors);
        Require(customer.City, "City", errors);
        Require(customer.State, "State", errors);
        Require(customer.ZipCode, "Zip code", errors);
        Require(customer.Ssn, "SSN", errors);
        Require(customer.Username, "Username", errors);
        Require(customer.Password, "Password", errors);

        if (customer.InitialDepositUsd <= 100)
        {
            errors.Add("Initial deposit must be greater than 100.");
        }

        if (string.IsNullOrWhiteSpace(customer.DobRaw))
        {
            warnings.Add("DOB is missing.");
        }
        else if (customer.DateOfBirth is null)
        {
            warnings.Add($"DOB '{customer.DobRaw}' could not be converted into a valid date.");
        }

        if (string.IsNullOrWhiteSpace(customer.DebitCardNumber))
        {
            warnings.Add("Debit card number is missing from the operator report fields.");
        }

        if (string.IsNullOrWhiteSpace(customer.Cvv))
        {
            warnings.Add("CVV is missing from the operator report fields.");
        }

        return new CustomerValidationResult(customer, warnings, errors);
    }

    private static void Require(string value, string fieldName, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} is required for ParaBank registration.");
        }
    }
}
