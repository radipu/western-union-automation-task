using OpenQA.Selenium;
using WesternUnionAutomationTask.Core.Models;
using WesternUnionAutomationTask.Rpa.Pages;

namespace WesternUnionAutomationTask.Rpa.Flows;

internal sealed class LoginCustomerFlow : BrowserPage
{
    private readonly string _homeUrl;

    public LoginCustomerFlow(IWebDriver driver, string homeUrl, int waitSeconds) : base(driver, waitSeconds)
    {
        _homeUrl = homeUrl;
    }

    public bool Login(CustomerProfile customer)
    {
        NavigateTo(_homeUrl);
        Type(By.Name("username"), customer.Username);
        Type(By.Name("password"), customer.Password);
        SubmitForm(By.XPath("//input[@type='submit' and @value='Log In']"));

        var body = BodyText();
        return body.Contains("Accounts Overview", StringComparison.OrdinalIgnoreCase)
            || IsPresent(By.LinkText("Log Out"));
    }
}
