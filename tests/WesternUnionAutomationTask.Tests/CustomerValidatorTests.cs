using WesternUnionAutomationTask.Core.Models;
using WesternUnionAutomationTask.Core.Validators;
using Xunit;

namespace WesternUnionAutomationTask.Tests;

public sealed class CustomerValidatorTests
{
    [Fact]
    public void Validate_ReturnsErrors_WhenRegistrationFieldsAreMissing()
    {
        var customer = new CustomerProfile
        {
            FirstName = "Bob",
            Username = "bob_user",
            Password = "Password1",
            InitialDepositUsd = 500
        };

        var result = new CustomerValidator().Validate(customer);

        Assert.False(result.CanProcess);
        Assert.Contains(result.Errors, error => error.Contains("Last name"));
        Assert.Contains(result.Errors, error => error.Contains("Address"));
    }

    [Fact]
    public void Validate_AllowsProcessing_WhenRequiredFieldsExist()
    {
        var customer = new CustomerProfile
        {
            FirstName = "Alice",
            LastName = "Smith",
            Address = "123 Maple St",
            City = "Vilnius",
            State = "LT",
            ZipCode = "01100",
            Ssn = "123-45-6789",
            Username = "alice_test",
            Password = "Password1",
            InitialDepositUsd = 1000
        };

        var result = new CustomerValidator().Validate(customer);

        Assert.True(result.CanProcess);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Validate_ReturnsError_WhenInitialDepositIsNotGreaterThan100(decimal initialDeposit)
    {
        var customer = new CustomerProfile
        {
            FirstName = "Test",
            LastName = "Customer",
            Address = "123 Test Street",
            City = "Vilnius",
            State = "LT",
            ZipCode = "01100",
            Ssn = "123-45-6789",
            Username = "test_customer",
            Password = "Password1",
            InitialDepositUsd = initialDeposit
        };

        var result = new CustomerValidator().Validate(customer);

        Assert.False(result.CanProcess);
        Assert.Contains(result.Errors, error => error.Contains("greater than 100"));
    }

}
