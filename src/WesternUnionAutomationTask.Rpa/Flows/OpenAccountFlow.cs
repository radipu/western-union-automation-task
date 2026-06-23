using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using WesternUnionAutomationTask.Rpa;
using WesternUnionAutomationTask.Rpa.Pages;

namespace WesternUnionAutomationTask.Rpa.Flows;

internal sealed class OpenAccountFlow : BrowserPage
{
    private readonly string _openAccountUrl;
    private readonly string _overviewUrl;
    private readonly ParaBankApiClient? _apiClient;
    private readonly bool _useFastApiFallback;

    public OpenAccountFlow(IWebDriver driver, string homeUrl, int waitSeconds, ParaBankApiClient? apiClient, bool useFastApiFallback) : base(driver, waitSeconds)
    {
        _openAccountUrl = homeUrl.Replace("index.htm", "openaccount.htm", StringComparison.OrdinalIgnoreCase);
        _overviewUrl = homeUrl.Replace("index.htm", "overview.htm", StringComparison.OrdinalIgnoreCase);
        _apiClient = apiClient;
        _useFastApiFallback = useFastApiFallback;
    }

    public string OpenNewAccount(string accountType, int? customerId)
    {
        try
        {
            NavigateTo(_openAccountUrl);
            var formLoaded = WaitForFastPageSignal(By.Id("type"), 3);

            if (formLoaded && IsPresent(By.Id("type")) && IsPresent(By.Id("fromAccountId")))
            {
                var typeDropdown = new SelectElement(WaitFor(By.Id("type")));
                var normalizedType = string.IsNullOrWhiteSpace(accountType) ? "CHECKING" : accountType.Trim().ToUpperInvariant();
                var matchingType = typeDropdown.Options.FirstOrDefault(option =>
                    string.Equals(option.Text.Trim(), normalizedType, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(option.GetAttribute("value")?.Trim(), normalizedType, StringComparison.OrdinalIgnoreCase));

                if (matchingType is not null)
                {
                    typeDropdown.SelectByText(matchingType.Text.Trim());
                }

                Wait.Until(_ => new SelectElement(WaitFor(By.Id("fromAccountId"))).Options.Count > 0);
                SubmitForm(By.XPath("//input[@type='submit' and @value='Open New Account']"));

                Wait.Until(_ =>
                {
                    var accountId = TryText(By.Id("newAccountId"));
                    var bodyText = TryText(By.TagName("body"));
                    return !string.IsNullOrWhiteSpace(accountId)
                        || bodyText.Contains("Account Opened", StringComparison.OrdinalIgnoreCase)
                        || bodyText.Contains("Congratulations", StringComparison.OrdinalIgnoreCase)
                        || bodyText.Contains("Error", StringComparison.OrdinalIgnoreCase);
                });

                var newAccountId = TryText(By.Id("newAccountId"));
                if (!string.IsNullOrWhiteSpace(newAccountId))
                {
                    return newAccountId;
                }

                var accountLinks = Driver.FindElements(By.CssSelector("a[href*='activity.htm?id=']"));
                var accountFromLink = accountLinks.LastOrDefault()?.Text.Trim();
                if (!string.IsNullOrWhiteSpace(accountFromLink))
                {
                    return accountFromLink;
                }
            }
        }
        catch (WebDriverTimeoutException) when (_useFastApiFallback)
        {
            
        }
        catch (NoSuchElementException) when (_useFastApiFallback)
        {
            
        }
        catch (InvalidOperationException) when (_useFastApiFallback)
        {
            
        }

        if (!_useFastApiFallback || _apiClient is null || customerId is null)
        {
            return string.Empty;
        }

        var accountIds = _apiClient.GetAccountIds(customerId.Value);
        var fromAccountId = accountIds.FirstOrDefault();
        if (fromAccountId <= 0)
        {
            return string.Empty;
        }

        var createdAccountId = _apiClient.CreateAccount(customerId.Value, accountType, fromAccountId);
        if (createdAccountId is null)
        {
            return string.Empty;
        }

        NavigateTo(_overviewUrl);
        return createdAccountId.Value.ToString();
    }
    private bool WaitForFastPageSignal(By locator, int seconds)
    {
        try
        {
            var shortWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds))
            {
                PollingInterval = TimeSpan.FromMilliseconds(120)
            };
            shortWait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
            return shortWait.Until(_ => IsPresent(locator) || BodyText().Contains("Error", StringComparison.OrdinalIgnoreCase));
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

}
