using ClosedXML.Excel;
using WesternUnionAutomationTask.Core.Models;

namespace WesternUnionAutomationTask.Core.Reporting;

public sealed class ExcelReportWriter
{
    public string WriteReport(IEnumerable<OperationReportRow> rows, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"ParaBank_Operator_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        var filePath = Path.Combine(outputDirectory, fileName);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Operations");

        var headers = new[]
        {
            "CSV Row", "Customer Name", "Username", "Account Type", "Initial Deposit USD", "Initial Deposit EUR",
            "Loan Amount USD", "Loan Amount EUR", "Down Payment USD", "Down Payment EUR", "Opened Account",
            "Loan Requested", "Loan Status", "DOB", "Debit Card", "CVV", "Automation Status", "Notes", "Processed At"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var currentRow = 2;
        foreach (var row in rows)
        {
            sheet.Cell(currentRow, 1).Value = row.RowNumber;
            sheet.Cell(currentRow, 2).Value = row.CustomerName;
            sheet.Cell(currentRow, 3).Value = row.Username;
            sheet.Cell(currentRow, 4).Value = row.AccountType;
            sheet.Cell(currentRow, 5).Value = row.InitialDepositUsd;
            sheet.Cell(currentRow, 6).Value = row.InitialDepositEur;
            sheet.Cell(currentRow, 7).Value = row.LoanAmountUsd;
            sheet.Cell(currentRow, 8).Value = row.LoanAmountEur;
            sheet.Cell(currentRow, 9).Value = row.DownPaymentUsd;
            sheet.Cell(currentRow, 10).Value = row.DownPaymentEur;
            sheet.Cell(currentRow, 11).Value = row.OpenedAccountNumber;
            sheet.Cell(currentRow, 12).Value = row.LoanRequested;
            sheet.Cell(currentRow, 13).Value = row.LoanStatus;
            sheet.Cell(currentRow, 14).Value = row.DateOfBirth;
            sheet.Cell(currentRow, 15).Value = row.DebitCardNumber;
            sheet.Cell(currentRow, 16).Value = row.Cvv;
            sheet.Cell(currentRow, 17).Value = row.AutomationStatus;
            sheet.Cell(currentRow, 18).Value = row.Notes;
            sheet.Cell(currentRow, 19).Value = row.ProcessedAt;
            currentRow++;
        }

        var usedRange = sheet.RangeUsed();
        if (usedRange is not null)
        {
            usedRange.CreateTable();
            sheet.Row(1).Style.Font.Bold = true;
            sheet.Columns().AdjustToContents();
            sheet.SheetView.FreezeRows(1);
            sheet.Column(5).Style.NumberFormat.Format = "$#,##0.00";
            sheet.Column(6).Style.NumberFormat.Format = "€#,##0.00";
            sheet.Column(7).Style.NumberFormat.Format = "$#,##0.00";
            sheet.Column(8).Style.NumberFormat.Format = "€#,##0.00";
            sheet.Column(9).Style.NumberFormat.Format = "$#,##0.00";
            sheet.Column(10).Style.NumberFormat.Format = "€#,##0.00";
            sheet.Column(19).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        }

        var summary = workbook.Worksheets.Add("Summary");
        var list = rows.ToList();
        summary.Cell("A1").Value = "ParaBank Automation Summary";
        summary.Cell("A1").Style.Font.Bold = true;
        summary.Cell("A3").Value = "Total records";
        summary.Cell("B3").Value = list.Count;
        summary.Cell("A4").Value = "Completed";
        summary.Cell("B4").Value = list.Count(r => r.AutomationStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase));
        summary.Cell("A5").Value = "Validation / Automation Failed";
        summary.Cell("B5").Value = list.Count(r => r.AutomationStatus.Contains("Failed", StringComparison.OrdinalIgnoreCase));
        summary.Cell("A6").Value = "Loan requests attempted";
        summary.Cell("B6").Value = list.Count(r => r.LoanRequested.Equals("Yes", StringComparison.OrdinalIgnoreCase));
        summary.Cell("A7").Value = "Accounts opened";
        summary.Cell("B7").Value = list.Count(r => !string.IsNullOrWhiteSpace(r.OpenedAccountNumber) && !r.OpenedAccountNumber.Equals("Not opened", StringComparison.OrdinalIgnoreCase));
        summary.Cell("A8").Value = "Generated at";
        summary.Cell("B8").Value = DateTime.Now;
        summary.Columns().AdjustToContents();

        workbook.SaveAs(filePath);
        return filePath;
    }
}
