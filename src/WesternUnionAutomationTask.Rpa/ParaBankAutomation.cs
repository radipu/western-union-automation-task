using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WesternUnionAutomationTask.Core.Models;
using WesternUnionAutomationTask.Core.Services;
using WesternUnionAutomationTask.Rpa.Flows;

namespace WesternUnionAutomationTask.Rpa;

[DebuggerStepThrough]
public sealed class ParaBankAutomation : IParaBankAutomation, IDisposable
{
    private IWebDriver? _driver;
    private bool _disposed;

    public async Task<OperationReportRow> ProcessCustomerAsync(CustomerProfile customer, AutomationSettings settings, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var calculator = new CurrencyCalculator();
            var downPaymentUsd = calculator.CalculateDownPayment(customer.InitialDepositUsd, settings.DownPaymentRate);
            var driver = GetDriver(settings.RunBrowserHeadless);
            var apiClient = settings.UseFastApiFallback ? new ParaBankApiClient(settings.ParaBankUrl) : null;
            int? customerId = null;
            string accountNumberForReport = string.Empty;
            string loanStatusForReport = "Not requested";

            var stage = "Starting browser session";
            try
            {
                var waitSeconds = Math.Clamp(settings.BrowserWaitSeconds, 5, 30);
                var registerFlow = new RegisterCustomerFlow(driver, settings.ParaBankUrl, waitSeconds);
                var loginFlow = new LoginCustomerFlow(driver, settings.ParaBankUrl, waitSeconds);
                var accountFlow = new OpenAccountFlow(driver, settings.ParaBankUrl, waitSeconds, apiClient, settings.UseFastApiFallback);
                var loanFlow = new LoanRequestFlow(driver, settings.ParaBankUrl, waitSeconds, apiClient, settings.UseFastApiFallback);

                stage = "Opening ParaBank home page";
                driver.Navigate().GoToUrl(settings.ParaBankUrl);

                stage = "Registering customer";
                var registrationStatus = registerFlow.Register(customer);
                var notes = string.Empty;

                if (string.Equals(registrationStatus, "AlreadyExists", StringComparison.OrdinalIgnoreCase))
                {
                    notes = "Customer username already existed in ParaBank, so the automation logged in with the provided credentials and continued with account opening and loan request.";
                    stage = "Logging in existing customer";
                    if (!loginFlow.Login(customer))
                    {
                        return CreateRow(customer, settings, calculator, downPaymentUsd, "Not opened", "No", "Not requested", "Automation Failed", "Username already existed and login with the provided credentials failed.");
                    }
                }
                else if (!string.Equals(registrationStatus, "Registered", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateRow(customer, settings, calculator, downPaymentUsd, "Not opened", "No", "Not requested", "Automation Failed", registrationStatus);
                }

                if (apiClient is not null)
                {
                    stage = "Resolving ParaBank customer id";
                    customerId = apiClient.LoginForCustomerId(customer);
                    if (customerId is null)
                    {
                        notes = AppendNote(notes, "Customer id could not be resolved from the ParaBank service, so only browser UI steps were available.");
                    }
                }

                if (settings.FastTransactionMode && apiClient is not null && customerId is not null)
                {
                    stage = "Opening new bank account through fast ParaBank service mode";
                    accountNumberForReport = OpenAccountFast(apiClient, customer, customerId.Value);
                    if (string.IsNullOrWhiteSpace(accountNumberForReport))
                    {
                        return CreateRow(customer, settings, calculator, downPaymentUsd, "Not opened", "No", "Not requested", "Automation Failed", "New account could not be created through the ParaBank service.");
                    }

                    stage = "Requesting loan through fast ParaBank service mode";
                    loanStatusForReport = RequestLoanFast(apiClient, settings.LoanAmountUsd, downPaymentUsd, accountNumberForReport, customerId.Value);
                    var fastLoanRequested = string.Equals(loanStatusForReport, "Not requested", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes";

                    stage = "Refreshing browser demonstration pages";
                    NavigateForVisibleDemo(driver, settings.ParaBankUrl, accountNumberForReport);

                    stage = "Logging out customer";
                    LogOut(driver);

                    notes = AppendNote(notes, "Fast transaction mode used ParaBank service calls for account opening and loan request after browser registration/login, then refreshed the browser pages for demonstration and verification.");
                    return CreateRow(customer, settings, calculator, downPaymentUsd, accountNumberForReport, fastLoanRequested, loanStatusForReport, "Completed", notes);
                }

                stage = "Opening new bank account";
                accountNumberForReport = accountFlow.OpenNewAccount(customer.AccountType, customerId);
                if (string.IsNullOrWhiteSpace(accountNumberForReport))
                {
                    return CreateRow(customer, settings, calculator, downPaymentUsd, "Not opened", "No", "Not requested", "Automation Failed", "New account could not be created or captured from ParaBank.");
                }

                stage = "Requesting loan";
                loanStatusForReport = loanFlow.RequestLoan(settings.LoanAmountUsd, downPaymentUsd, accountNumberForReport, customerId);
                var loanRequested = string.Equals(loanStatusForReport, "Not requested", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes";

                stage = "Logging out customer";
                LogOut(driver);

                return CreateRow(customer, settings, calculator, downPaymentUsd, accountNumberForReport, loanRequested, loanStatusForReport, "Completed", notes);
            }
            catch (WebDriverException ex)
            {
                TryReturnToHome(driver, settings.ParaBankUrl);
                var openedAccount = string.IsNullOrWhiteSpace(accountNumberForReport) ? "Not opened" : accountNumberForReport;
                var loanRequested = string.Equals(loanStatusForReport, "Not requested", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes";
                return CreateRow(customer, settings, calculator, downPaymentUsd, openedAccount, loanRequested, loanStatusForReport, "Automation Failed", $"{stage}: {CleanErrorMessage(ex.Message)}");
            }
            catch (InvalidOperationException ex)
            {
                TryReturnToHome(driver, settings.ParaBankUrl);
                var openedAccount = string.IsNullOrWhiteSpace(accountNumberForReport) ? "Not opened" : accountNumberForReport;
                var loanRequested = string.Equals(loanStatusForReport, "Not requested", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes";
                return CreateRow(customer, settings, calculator, downPaymentUsd, openedAccount, loanRequested, loanStatusForReport, "Automation Failed", $"{stage}: {ex.Message}");
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _driver?.Quit();
        }
        finally
        {
            _driver?.Dispose();
            _driver = null;
        }
    }

    private IWebDriver GetDriver(bool headless)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _driver ??= CreateDriver(headless);
        return _driver;
    }

    private static string OpenAccountFast(ParaBankApiClient apiClient, CustomerProfile customer, int customerId)
    {
        var existingAccounts = apiClient.GetAccountIds(customerId);
        var fundingAccountId = existingAccounts.FirstOrDefault();
        if (fundingAccountId <= 0)
        {
            return string.Empty;
        }

        var createdAccountId = apiClient.CreateAccount(customerId, customer.AccountType, fundingAccountId);
        return createdAccountId?.ToString() ?? string.Empty;
    }

    private static string RequestLoanFast(ParaBankApiClient apiClient, decimal loanAmountUsd, decimal downPaymentUsd, string accountNumber, int customerId)
    {
        if (!int.TryParse(accountNumber.Trim(), out var fromAccountId))
        {
            return "Not requested";
        }

        var result = apiClient.RequestLoan(customerId, loanAmountUsd, downPaymentUsd, fromAccountId);
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

    private static void NavigateForVisibleDemo(IWebDriver driver, string paraBankUrl, string accountNumber)
    {
        try
        {
            if (driver is IJavaScriptExecutor js)
            {
                var overviewUrl = paraBankUrl.Replace("index.htm", "overview.htm", StringComparison.OrdinalIgnoreCase);
                js.ExecuteScript("window.location.href = arguments[0];", overviewUrl);
                Thread.Sleep(250);

                if (!string.IsNullOrWhiteSpace(accountNumber))
                {
                    var accountUrl = paraBankUrl.Replace("index.htm", $"activity.htm?id={accountNumber}", StringComparison.OrdinalIgnoreCase);
                    js.ExecuteScript("window.location.href = arguments[0];", accountUrl);
                    Thread.Sleep(250);
                }

                var loanUrl = paraBankUrl.Replace("index.htm", "requestloan.htm", StringComparison.OrdinalIgnoreCase);
                js.ExecuteScript("window.location.href = arguments[0];", loanUrl);
                Thread.Sleep(250);
            }
            else
            {
                driver.Navigate().GoToUrl(paraBankUrl.Replace("index.htm", "overview.htm", StringComparison.OrdinalIgnoreCase));
            }
        }
        catch (WebDriverException)
        {
            
        }
    }

    private static OperationReportRow CreateRow(
        CustomerProfile customer,
        AutomationSettings settings,
        CurrencyCalculator calculator,
        decimal downPaymentUsd,
        string accountNumber,
        string loanRequested,
        string loanStatus,
        string automationStatus,
        string notes)
    {
        return new OperationReportRow
        {
            RowNumber = customer.RowNumber,
            CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
            Username = customer.Username,
            AccountType = customer.AccountType,
            InitialDepositUsd = customer.InitialDepositUsd,
            InitialDepositEur = calculator.ConvertUsdToEur(customer.InitialDepositUsd, settings.UsdToEurRate),
            LoanAmountUsd = settings.LoanAmountUsd,
            LoanAmountEur = calculator.ConvertUsdToEur(settings.LoanAmountUsd, settings.UsdToEurRate),
            DownPaymentUsd = downPaymentUsd,
            DownPaymentEur = calculator.ConvertUsdToEur(downPaymentUsd, settings.UsdToEurRate),
            OpenedAccountNumber = string.IsNullOrWhiteSpace(accountNumber) ? "Not opened" : accountNumber,
            LoanRequested = string.IsNullOrWhiteSpace(loanRequested) ? "No" : loanRequested,
            LoanStatus = string.IsNullOrWhiteSpace(loanStatus) ? "Not requested" : loanStatus,
            DateOfBirth = customer.DateOfBirth?.ToString("yyyy-MM-dd") ?? customer.DobRaw,
            DebitCardNumber = customer.DebitCardNumber,
            Cvv = customer.Cvv,
            AutomationStatus = automationStatus,
            Notes = notes
        };
    }

    private static IWebDriver CreateDriver(bool headless)
    {
        var options = new ChromeOptions
        {
            PageLoadStrategy = PageLoadStrategy.Eager
        };
        options.AddArgument("--disable-notifications");
        options.AddArgument("--disable-popup-blocking");
        options.AddArgument("--disable-search-engine-choice-screen");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--blink-settings=imagesEnabled=false");
        options.AddUserProfilePreference("profile.managed_default_content_settings.images", 2);

        if (headless)
        {
            options.AddArgument("--headless=new");
            options.AddArgument("--window-size=1400,1000");
        }
        else
        {
            options.AddArgument("--start-maximized");
        }

        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(12);
        driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(5);
        return driver;
    }

    private static void LogOut(IWebDriver driver)
    {
        var logoutLinks = driver.FindElements(By.LinkText("Log Out"));
        if (logoutLinks.Count > 0)
        {
            logoutLinks[0].Click();
        }
    }

    private static void TryReturnToHome(IWebDriver driver, string paraBankUrl)
    {
        try
        {
            LogOut(driver);
            driver.Navigate().GoToUrl(paraBankUrl);
        }
        catch (WebDriverException)
        {
            
        }
    }

    private static string CleanErrorMessage(string message)
    {
        var firstLine = message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstLine) ? "Browser automation failed." : firstLine.Trim();
    }

    private static string AppendNote(string current, string additional)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return additional;
        }

        return $"{current} {additional}";
    }
}
