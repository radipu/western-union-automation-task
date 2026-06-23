# Western Union Automation Task

## Project Overview

Western Union Automation Task is a C# WPF desktop application built for the ParaBank practical automation exercise. The application reads customer data from a CSV or XLSX file, performs the required ParaBank operations for each valid customer, and generates an Excel operator report after completion.

The automation covers the following process:

1. Open ParaBank at `https://parabank.parasoft.com/parabank/index.htm`.
2. Register each customer from the uploaded input file.
3. Log in when a customer username already exists from a previous run.
4. Open a new bank account for the customer.
5. Request a loan of USD 10,000.
6. Use a down payment equal to 20% of the customer's initial deposit.
7. Capture the ParaBank loan result exactly as returned by the application.
8. Generate an Excel report in EUR and USD.
9. Log out of ParaBank and close the browser session.

The application was designed as a small RPA-style solution with a simple user interface, validation, logging, retry/fallback handling, and a structured report for the operator.

---

## Technology Stack

- Language: C#
- UI Framework: WPF
- Runtime: .NET 8
- Browser Automation: Selenium WebDriver with Chrome
- Reporting: ClosedXML for Excel report generation
- Input Files: CSV and XLSX
- Test Framework: xUnit

No database is required for this project. All data is read from the uploaded file and the result is written to an Excel report.

---

## Solution Structure

```text
Western Union Automation Task/
│
├── Western Union Automation Task.sln
├── README.md
│
├── src/
│   ├── Western Union Automation Task/
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   └── appsettings.json
│   │
│   ├── WesternUnionAutomationTask.Core/
│   │   ├── Models/
│   │   ├── Services/
│   │   ├── Validators/
│   │   └── Reporting/
│   │
│   └── WesternUnionAutomationTask.Rpa/
│       ├── Flows/
│       ├── Pages/
│       ├── ParaBankAutomation.cs
│       └── ParaBankApiClient.cs
│
├── tests/
│   └── WesternUnionAutomationTask.Tests/
│
├── Input/
│   └── ParaBank users.csv
│
├── Output/
│
├── build/
│   ├── Publish-Windows-App.ps1
│   └── Publish-Windows-App.bat
│
└── deployment/
    └── DEPLOYMENT_NOTES.txt
```

---

## Prerequisites

Before running the project, install the following:

1. Windows 10 or Windows 11
2. Visual Studio 2022
3. .NET 8 SDK
4. Google Chrome
5. Internet connection, because the automation runs against the public ParaBank demo site

ChromeDriver is handled through the Selenium package setup, so a separate manual ChromeDriver installation should not be required in the normal setup.

---

## How to Run from Visual Studio

1. Extract the ZIP file.
2. Open the solution file:

```text
Western Union Automation Task.sln
```

3. In Visual Studio, restore NuGet packages if prompted.
4. Set the WPF project as the startup project:

```text
src/Western Union Automation Task/Western Union Automation Task.csproj
```

5. Build the solution:

```text
Build > Build Solution
```

6. Run the application:

```text
Debug > Start Without Debugging
```

7. In the application, click **Browse** and select the input file, for example:

```text
Input/ParaBank users.csv
```

8. Click **Start Automation**.
9. Wait until the process finishes.
10. Open the generated report from the `Output` folder.

The report file name follows this pattern:

```text
ParaBank_Operator_Report_yyyyMMdd_HHmmss.xlsx
```

---

## How to Run from Command Line

From the root project folder, run:

```bash
dotnet restore "Western Union Automation Task.sln"
dotnet build "Western Union Automation Task.sln" -c Release
dotnet run --project "src/Western Union Automation Task/Western Union Automation Task.csproj" -c Release
```

---

## How to Publish a Windows Executable

A publish script is included in the `build` folder.

Using PowerShell:

```powershell
cd "Western Union Automation Task"
./build/Publish-Windows-App.ps1
```

Or using the batch file:

```bat
cd "Western Union Automation Task"
build\Publish-Windows-App.bat
```

The published application will be created under the publish output folder defined by the script. The executable name is:

```text
Western Union Automation Task.exe
```

---

## Input File Format

The application accepts CSV and XLSX files. The expected customer fields are:

| Field | Description |
|---|---|
| First Name | Customer first name |
| Last Name | Customer last name |
| Address | Customer street address |
| City | Customer city |
| State | Customer state |
| Zip Code | Customer postal/ZIP code |
| Phone | Customer phone number |
| SSN | Customer SSN/test identifier |
| Username | ParaBank username |
| Password | ParaBank password |
| Initial Deposit | Initial deposit used for down payment calculation |
| DOB | Date of birth, included in the operator report |
| Debit Card | Debit card number, included in the operator report |
| CVV | CVV, included in the operator report |

Rows with missing required data are skipped safely and written to the report with a validation message. This prevents one bad input row from stopping the whole automation run.

---

## Configuration

The main configuration file is:

```text
src/Western Union Automation Task/appsettings.json
```

Current configuration:

```json
{
  "ParaBankUrl": "https://parabank.parasoft.com/parabank/index.htm",
  "LoanAmountUsd": 10000,
  "DownPaymentRate": 0.20,
  "UsdToEurRate": 0.92,
  "RunBrowserHeadless": false,
  "BrowserWaitSeconds": 8,
  "UseFastApiFallback": true
}
```

### Important Settings

| Setting | Purpose |
|---|---|
| ParaBankUrl | ParaBank start URL |
| LoanAmountUsd | Loan amount requested for each customer |
| DownPaymentRate | 20% down payment calculation rate |
| UsdToEurRate | USD to EUR conversion rate used in the report |
| RunBrowserHeadless | `false` shows the browser for demo; `true` runs faster in background |
| BrowserWaitSeconds | Maximum UI wait time for browser elements |
| UseFastApiFallback | Uses ParaBank service fallback when the UI is slow or unstable |

For a live demonstration, keep:

```json
"RunBrowserHeadless": false
```

For faster execution without showing the browser, change it to:

```json
"RunBrowserHeadless": true
```

---

## Report Output

The Excel report is generated in the `Output` folder. It includes the main operation status and business data needed by the ParaBank operator.

Report columns include:

- Customer name
- Username
- DOB
- Debit Card number
- CVV
- Initial Deposit USD
- Initial Deposit EUR
- Opened Account
- Loan Amount USD
- Loan Amount EUR
- Down Payment USD
- Down Payment EUR
- Loan Requested
- Loan Status
- Status
- Notes

The report captures the real result returned by ParaBank. If ParaBank denies a loan, the application records the denial reason instead of forcing a successful result.

---

## Why Some Loans Can Be Denied

The automation always submits the required loan values:

- Loan amount: USD 10,000
- Down payment: 20% of the customer's initial deposit

The final loan result is decided by ParaBank's own demo-bank backend. A customer with a higher down payment can still be denied because ParaBank may consider its own internal customer/account state, previous runs, existing accounts, or other backend rules.

This is expected behavior. The application records the actual response returned by ParaBank to keep the report accurate.

---

## Demo Recommendation

For the Western Union demo, visible browser mode is recommended because it clearly shows the RPA process:

1. Browser opens ParaBank.
2. Customer is registered or logged in.
3. New account is opened.
4. Loan request is submitted.
5. Result is captured.
6. Customer is logged out.
7. Excel report is generated.

For repeated test runs or faster execution, headless mode can be enabled in `appsettings.json`.

---

## Main Implementation Details

### 1. Input Reading

`CsvCustomerReader` reads customer records from CSV/XLSX input and maps them into customer profile objects.

### 2. Validation

`CustomerValidator` checks required fields before the browser automation starts. Invalid rows are skipped and included in the report with clear notes.

### 3. Automation Orchestration

`AutomationOrchestrator` controls the end-to-end process for each customer. It calls validation, browser automation, currency calculation, and report generation.

### 4. Browser Automation

`ParaBankAutomation` coordinates the ParaBank process. The flow is split into smaller classes:

- `RegisterCustomerFlow`
- `LoginCustomerFlow`
- `OpenAccountFlow`
- `LoanRequestFlow`

This structure keeps the code readable and makes each automation step easier to test or update.

### 5. API Fallback

`ParaBankApiClient` is used as a fallback when ParaBank UI pages are slow or unstable. The browser still opens for the demo, but the fallback improves reliability and speed for account and loan operations.

### 6. Excel Report

`ExcelReportWriter` generates the final operator report using ClosedXML.

---

## Testing

The solution includes unit tests for validation and currency calculation.

Run tests with:

```bash
dotnet test "Western Union Automation Task.sln"
```

Current test coverage focuses on:

- Required customer field validation
- Initial deposit validation
- Currency conversion calculation
- Down payment calculation

---

## Known Notes and Assumptions

- ParaBank is a public demo application, so its availability and response time can vary.
- If the same usernames are used multiple times, ParaBank may keep previous customer/account history. In that case, the application logs in with the existing credentials and continues the process.
- Loan approval/denial is not controlled by this application. The result is captured from ParaBank.
- The USD to EUR rate is configurable and currently set in `appsettings.json`.
- No database is used because the task can be completed with file input and Excel output.

---

## Troubleshooting

### Browser does not open

Check that Google Chrome is installed and updated.

### Automation is slower than expected

Set headless mode to `true` in `appsettings.json`:

```json
"RunBrowserHeadless": true
```

For demo mode, keep it as `false` so the browser is visible.

### Some rows are skipped

Open the generated Excel report and check the `Status` and `Notes` columns. Missing required customer fields are reported there.

### Loan is denied

This is a ParaBank business result. The automation records the response exactly as returned by ParaBank.

### Existing username message appears

This means the customer was already registered in a previous run. The application logs in with the provided credentials and continues.

---

## Delivery Contents

The delivery package contains:

- Full C# source code
- Visual Studio solution file
- WPF desktop application
- Input sample folder
- Output report folder
- Publish scripts
- Unit tests
- README instructions

---

## Summary

This project demonstrates an RPA-style automation approach using C#. It provides a practical end-to-end solution for reading customer input, automating ParaBank operations, handling validation and runtime exceptions, and producing a meaningful Excel report for the operator.

## Performance mode added in v6

The application includes a `FastTransactionMode` setting in `appsettings.json`.

```json
"FastTransactionMode": true
```

When this is enabled, the application still uses the browser for registration/login and visible demonstration, but it performs the account opening and loan request through ParaBank's own service endpoints after resolving the customer ID. This avoids unnecessary waiting on ParaBank demo UI pages that can sometimes load slowly or leave dropdown values empty. The browser is refreshed to the relevant ParaBank pages afterward so the run remains demonstrable.

Set `FastTransactionMode` to `false` when a slower, fully form-driven Selenium-only account and loan flow is required for debugging or comparison.
