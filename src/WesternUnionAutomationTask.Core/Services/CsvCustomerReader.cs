using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using WesternUnionAutomationTask.Core.Models;

namespace WesternUnionAutomationTask.Core.Services;

public sealed class CsvCustomerReader
{
    public IReadOnlyList<CustomerProfile> ReadCustomers(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Customer input file was not found.", filePath);
        }

        var extension = Path.GetExtension(filePath);
        if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) || extension.Equals(".xlsm", StringComparison.OrdinalIgnoreCase))
        {
            return ReadExcelCustomers(filePath);
        }

        return ReadCsvCustomers(filePath);
    }

    private static IReadOnlyList<CustomerProfile> ReadCsvCustomers(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();

        var customers = new List<CustomerProfile>();
        var rowNumber = 2;

        while (csv.Read())
        {
            customers.Add(CreateCustomer(rowNumber, columnName => GetValue(csv, columnName)));
            rowNumber++;
        }

        return customers;
    }

    private static IReadOnlyList<CustomerProfile> ReadExcelCustomers(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed() ?? throw new InvalidOperationException("The customer workbook is empty.");
        var headerMap = headerRow.CellsUsed()
            .Where(cell => !string.IsNullOrWhiteSpace(cell.GetString()))
            .GroupBy(cell => NormalizeHeader(cell.GetString()))
            .ToDictionary(group => group.Key, group => group.First().Address.ColumnNumber);

        var customers = new List<CustomerProfile>();
        var firstDataRow = headerRow.RowNumber() + 1;
        var lastDataRow = worksheet.LastRowUsed()?.RowNumber() ?? firstDataRow - 1;

        for (var rowNumber = firstDataRow; rowNumber <= lastDataRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (row.IsEmpty())
            {
                continue;
            }

            customers.Add(CreateCustomer(rowNumber, columnName => GetValue(row, headerMap, columnName)));
        }

        return customers;
    }

    private static CustomerProfile CreateCustomer(int rowNumber, Func<string, string> getValue)
    {
        var initialDepositText = getValue("Initial Deposit");
        decimal.TryParse(initialDepositText, NumberStyles.Any, CultureInfo.InvariantCulture, out var initialDeposit);

        var dobRaw = getValue("DOB");

        return new CustomerProfile
        {
            RowNumber = rowNumber,
            FirstName = getValue("First Name"),
            LastName = getValue("Last Name"),
            Address = getValue("Address"),
            City = getValue("City"),
            State = getValue("State"),
            ZipCode = getValue("Zip Code"),
            PhoneNumber = getValue("Phone Number"),
            Ssn = getValue("SSN"),
            Username = getValue("Username"),
            Password = getValue("Password"),
            AccountType = getValue("Account Type"),
            InitialDepositUsd = initialDeposit,
            DobRaw = dobRaw,
            DateOfBirth = TryParseDateOfBirth(dobRaw),
            DebitCardNumber = getValue("Debit Card"),
            Cvv = getValue("CVV")
        };
    }

    private static string GetValue(CsvReader csv, string columnName)
    {
        try
        {
            return csv.GetField(columnName)?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetValue(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string columnName)
    {
        return headerMap.TryGetValue(NormalizeHeader(columnName), out var columnNumber)
            ? row.Cell(columnNumber).GetFormattedString().Trim()
            : string.Empty;
    }

    private static string NormalizeHeader(string value)
    {
        return value.Trim().Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
    }

    private static DateTime? TryParseDateOfBirth(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[]
        {
            "MM-dd-yy", "M-d-yy", "dd/MM/yyyy", "d/M/yyyy", "MMMM d, yyyy", "MMM d, yyyy", "yyyy-MM-dd"
        };

        if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate))
        {
            return exactDate;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            ? parsedDate
            : null;
    }
}
