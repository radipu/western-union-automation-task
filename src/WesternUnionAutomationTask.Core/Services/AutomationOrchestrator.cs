using WesternUnionAutomationTask.Core.Models;
using WesternUnionAutomationTask.Core.Reporting;
using WesternUnionAutomationTask.Core.Validators;

namespace WesternUnionAutomationTask.Core.Services;

public interface IParaBankAutomation
{
    Task<OperationReportRow> ProcessCustomerAsync(CustomerProfile customer, AutomationSettings settings, CancellationToken cancellationToken);
}

public sealed class AutomationOrchestrator
{
    private readonly CsvCustomerReader _csvReader;
    private readonly CustomerValidator _validator;
    private readonly CurrencyCalculator _calculator;
    private readonly ExcelReportWriter _reportWriter;
    private readonly IParaBankAutomation _paraBankAutomation;

    public AutomationOrchestrator(
        CsvCustomerReader csvReader,
        CustomerValidator validator,
        CurrencyCalculator calculator,
        ExcelReportWriter reportWriter,
        IParaBankAutomation paraBankAutomation)
    {
        _csvReader = csvReader;
        _validator = validator;
        _calculator = calculator;
        _reportWriter = reportWriter;
        _paraBankAutomation = paraBankAutomation;
    }

    public async Task<string> RunAsync(
        string csvFilePath,
        string outputDirectory,
        AutomationSettings settings,
        IProgress<AutomationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var customers = _csvReader.ReadCustomers(csvFilePath);
        var reportRows = new List<OperationReportRow>();

        try
        {
            for (var i = 0; i < customers.Count; i++)
            {
            cancellationToken.ThrowIfCancellationRequested();
            var customer = customers[i];
            var fullName = $"{customer.FirstName} {customer.LastName}".Trim();
            progress?.Report(new AutomationProgress($"Processing {fullName}", i, customers.Count));

            var validation = _validator.Validate(customer);
            if (!validation.CanProcess)
            {
                reportRows.Add(CreateReportRow(customer, settings, "Validation Failed", "Not requested", string.Join(" ", validation.Errors.Concat(validation.Warnings))));
                progress?.Report(new AutomationProgress($"Skipped {fullName}: input validation failed", i + 1, customers.Count));
                continue;
            }

            try
            {
                var row = await _paraBankAutomation.ProcessCustomerAsync(customer, settings, cancellationToken);
                if (validation.Warnings.Count > 0)
                {
                    var warningText = string.Join(" ", validation.Warnings);
                    row.Notes = string.IsNullOrWhiteSpace(row.Notes) ? warningText : $"{row.Notes} {warningText}";
                }

                reportRows.Add(row);
            }
            catch (Exception ex)
            {
                reportRows.Add(CreateReportRow(customer, settings, "Automation Failed", "Not requested", ex.Message));
            }

                progress?.Report(new AutomationProgress($"Finished {fullName}", i + 1, customers.Count));
            }
        }
        finally
        {
            if (_paraBankAutomation is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return _reportWriter.WriteReport(reportRows, outputDirectory);
    }

    private OperationReportRow CreateReportRow(CustomerProfile customer, AutomationSettings settings, string automationStatus, string loanStatus, string notes)
    {
        var downPayment = _calculator.CalculateDownPayment(customer.InitialDepositUsd, settings.DownPaymentRate);

        return new OperationReportRow
        {
            RowNumber = customer.RowNumber,
            CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
            Username = customer.Username,
            AccountType = customer.AccountType,
            InitialDepositUsd = customer.InitialDepositUsd,
            InitialDepositEur = _calculator.ConvertUsdToEur(customer.InitialDepositUsd, settings.UsdToEurRate),
            LoanAmountUsd = settings.LoanAmountUsd,
            LoanAmountEur = _calculator.ConvertUsdToEur(settings.LoanAmountUsd, settings.UsdToEurRate),
            DownPaymentUsd = downPayment,
            DownPaymentEur = _calculator.ConvertUsdToEur(downPayment, settings.UsdToEurRate),
            OpenedAccountNumber = "Not opened",
            DateOfBirth = customer.DateOfBirth?.ToString("yyyy-MM-dd") ?? customer.DobRaw,
            DebitCardNumber = customer.DebitCardNumber,
            Cvv = customer.Cvv,
            AutomationStatus = automationStatus,
            LoanRequested = string.Equals(loanStatus, "Not requested", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes",
            LoanStatus = loanStatus,
            Notes = notes
        };
    }
}
