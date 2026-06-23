using OpenQA.Selenium;
using WesternUnionAutomationTask.Core.Models;
using WesternUnionAutomationTask.Rpa.Pages;

namespace WesternUnionAutomationTask.Rpa.Flows;

internal sealed class RegisterCustomerFlow : BrowserPage
{
    private readonly string _homeUrl;
    private readonly string _registerUrl;

    public RegisterCustomerFlow(IWebDriver driver, string homeUrl, int waitSeconds) : base(driver, waitSeconds)
    {
        _homeUrl = homeUrl;
        _registerUrl = homeUrl.Replace("index.htm", "register.htm", StringComparison.OrdinalIgnoreCase);
    }

    public string Register(CustomerProfile customer)
    {
        EnsureLoggedOutHomePage();
        NavigateTo(_registerUrl);

        Type(By.Id("customer.firstName"), customer.FirstName);
        Type(By.Id("customer.lastName"), customer.LastName);
        Type(By.Id("customer.address.street"), customer.Address);
        Type(By.Id("customer.address.city"), customer.City);
        Type(By.Id("customer.address.state"), customer.State);
        Type(By.Id("customer.address.zipCode"), customer.ZipCode);
        Type(By.Id("customer.phoneNumber"), customer.PhoneNumber);
        Type(By.Id("customer.ssn"), customer.Ssn);
        Type(By.Id("customer.username"), customer.Username);
        Type(By.Id("customer.password"), customer.Password);
        Type(By.Id("repeatedPassword"), customer.Password);

        SubmitForm(By.XPath("//input[@type='submit' and @value='Register']"));

        var bodyText = BodyText();
        if (bodyText.Contains("Your account was created successfully", StringComparison.OrdinalIgnoreCase))
        {
            return "Registered";
        }

        if (bodyText.Contains("This username already exists", StringComparison.OrdinalIgnoreCase))
        {
            return "AlreadyExists";
        }

        if (bodyText.Contains("required", StringComparison.OrdinalIgnoreCase) || bodyText.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            return "Registration failed: ParaBank returned validation errors.";
        }

        return "Registration could not be confirmed from the ParaBank confirmation page.";
    }

    private void EnsureLoggedOutHomePage()
    {
        if (IsPresent(By.LinkText("Log Out")))
        {
            Click(By.LinkText("Log Out"));
        }

        NavigateTo(_homeUrl);
    }
}
