using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using WesternUnionAutomationTask.Rpa;
using WesternUnionAutomationTask.Rpa.Pages;

namespace WesternUnionAutomationTask.Rpa.Flows;

internal sealed class LoanRequestFlow : BrowserPage
{
    private readonly string _loanUrl;
    private readonly ParaBankApiClient? _apiClient;
    private readonly bool _useFastApiFallback;

    public LoanRequestFlow(IWebDriver driver, string homeUrl, int waitSeconds, ParaBankApiClient? apiClient, bool useFastApiFallback) : base(driver, waitSeconds)
    {
        _loanUrl = homeUrl.Replace("index.htm", "requestloan.htm", StringComparison.OrdinalIgnoreCase);
        _apiClient = apiClient;
        _useFastApiFallback = useFastApiFallback;
    }

    public string RequestLoan(decimal loanAmountUsd, decimal downPaymentUsd, string accountNumber, int? customerId)
    {
        try
        {
            NavigateTo(_loanUrl);
            if (!WaitForFastPageSignal(By.Id("amount"), 3))
            {
                throw new WebDriverTimeoutException("Loan form did not load quickly; switching to ParaBank service fallback.");
            }

            Type(By.Id("amount"), loanAmountUsd.ToString("0.00", CultureInfo.InvariantCulture));
            Type(By.Id("downPayment"), downPaymentUsd.ToString("0.00", CultureInfo.InvariantCulture));

            Wait.Until(_ => new SelectElement(WaitFor(By.Id("fromAccountId"))).Options.Count > 0);
            var fromAccount = new SelectElement(WaitFor(By.Id("fromAccountId")));
            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                var matchingAccount = fromAccount.Options.FirstOrDefault(option =>
                    string.Equals(option.Text.Trim(), accountNumber.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(option.GetAttribute("value")?.Trim(), accountNumber.Trim(), StringComparison.OrdinalIgnoreCase));

                if (matchingAccount is not null)
                {
                    fromAccount.SelectByText(matchingAccount.Text.Trim());
                }
            }

            SubmitForm(By.XPath("//input[@type='submit' and @value='Apply Now']"));

            Wait.Until(_ =>
            {
                var bodyText = TryText(By.TagName("body"));
                return bodyText.Contains("Loan Request Processed", StringComparison.OrdinalIgnoreCase)
                    || bodyText.Contains("Approved", StringComparison.OrdinalIgnoreCase)
                    || bodyText.Contains("Denied", StringComparison.OrdinalIgnoreCase)
                    || bodyText.Contains("Error", StringComparison.OrdinalIgnoreCase);
            });

            var body = TryText(By.TagName("body"));
            if (body.Contains("Approved", StringComparison.OrdinalIgnoreCase))
            {
                var loanAccount = TryText(By.Id("newAccountId"));
                return string.IsNullOrWhiteSpace(loanAccount)
                    ? "Approved - loan request confirmation displayed"
                    : $"Approved - loan account {loanAccount}";
            }

            if (body.Contains("Denied", StringComparison.OrdinalIgnoreCase))
            {
                return "Denied - loan request confirmation displayed";
            }

            if (body.Contains("Loan Request Processed", StringComparison.OrdinalIgnoreCase))
            {
                return "Submitted - loan request processed page displayed";
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

        if (!_useFastApiFallback || _apiClient is null || customerId is null || !int.TryParse(accountNumber?.Trim(), out var fromAccountId))
        {
            return "Not requested";
        }

        var result = _apiClient.RequestLoan(customerId.Value, loanAmountUsd, downPaymentUsd, fromAccountId);
        if (result.Approved == true)
        {
            return result.AccountId is null
                ? $"Approved - {result.Message}"
                : $"Approved - loan account {result.AccountId} ({result.Message})";
        }

        if (result.Approved == false)
        {
            return $"Denied - {result.Message}";
        }

        return $"Submitted - {result.Message}";
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
