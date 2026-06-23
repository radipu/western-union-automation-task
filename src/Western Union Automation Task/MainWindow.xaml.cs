using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using WesternUnionAutomationTask.Core.Models;
using WesternUnionAutomationTask.Core.Reporting;
using WesternUnionAutomationTask.Core.Services;
using WesternUnionAutomationTask.Core.Validators;
using WesternUnionAutomationTask.Rpa;

namespace WesternUnionAutomationTask.App;

public partial class MainWindow : Window
{
    private string? _inputFilePath;
    private string? _lastReportPath;
    private CancellationTokenSource? _runCancellation;
    private readonly AutomationSettings _settings;

    public MainWindow()
    {
        InitializeComponent();
        _settings = LoadSettings();
        ShowSettings(_settings);
    }

    private void SelectCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Customer files (*.csv;*.xlsx;*.xlsm)|*.csv;*.xlsx;*.xlsm|CSV files (*.csv)|*.csv|Excel files (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All files (*.*)|*.*",
            Title = "Select ParaBank users file"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _inputFilePath = dialog.FileName;
            SelectedFileText.Text = _inputFilePath;

            var reader = new CsvCustomerReader();
            var validator = new CustomerValidator();
            var customers = reader.ReadCustomers(_inputFilePath);
            var validations = customers.Select(validator.Validate).ToList();

            var ready = validations.Count(x => x.CanProcess);
            var skipped = validations.Count - ready;
            var warnings = validations.Count(x => x.Warnings.Count > 0);

            TotalRecordsText.Text = customers.Count.ToString();
            ReadyRecordsText.Text = ready.ToString();
            SkippedRecordsText.Text = skipped.ToString();
            SummaryText.Text = $"Loaded customers: {customers.Count} | Ready: {ready} | Skipped: {skipped} | With warnings: {warnings}";
            StartButton.IsEnabled = customers.Count > 0;
            AddLog("Customer file loaded and validated.");
        }
        catch (Exception ex)
        {
            StartButton.IsEnabled = false;
            AddLog($"Input validation error: {ex.Message}");
            MessageBox.Show(ex.Message, "Input file error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void StartAutomation_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_inputFilePath))
        {
            MessageBox.Show("Please select the customer CSV or Excel file first.", "Missing input", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _runCancellation = new CancellationTokenSource();
        StartButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        OpenReportButton.IsEnabled = false;
        RunProgress.Value = 0;
        ProgressPercentText.Text = "0%";
        StatusText.Text = "Automation running";
        AddLog("Automation started.");

        try
        {
            var outputDirectory = Path.Combine(AppContext.BaseDirectory, "Output");
            Directory.CreateDirectory(outputDirectory);

            var orchestrator = new AutomationOrchestrator(
                new CsvCustomerReader(),
                new CustomerValidator(),
                new CurrencyCalculator(),
                new ExcelReportWriter(),
                new ParaBankAutomation());

            var progress = new Progress<AutomationProgress>(item =>
            {
                AddLog(item.Message);
                var percent = item.Total == 0 ? 0 : item.Completed * 100.0 / item.Total;
                RunProgress.Value = percent;
                ProgressPercentText.Text = $"{percent:0}%";
            });

            _lastReportPath = await orchestrator.RunAsync(_inputFilePath, outputDirectory, _settings, progress, _runCancellation.Token);
            RunProgress.Value = 100;
            ProgressPercentText.Text = "100%";
            StatusText.Text = "Completed";
            AddLog($"Report created: {_lastReportPath}");
            OpenReportButton.IsEnabled = true;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Cancelled";
            AddLog("Automation cancelled by operator.");
        }
        catch (Exception ex)
        {
            StatusText.Text = "Stopped with error";
            AddLog($"Error: {ex.Message}");
            MessageBox.Show(ex.Message, "Automation error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _runCancellation?.Dispose();
            _runCancellation = null;
            StartButton.IsEnabled = !string.IsNullOrWhiteSpace(_inputFilePath);
            CancelButton.IsEnabled = false;
        }
    }

    private void CancelAutomation_Click(object sender, RoutedEventArgs e)
    {
        _runCancellation?.Cancel();
        CancelButton.IsEnabled = false;
        AddLog("Cancellation requested. Current browser step will finish or stop safely.");
    }

    private void OpenReportFolder_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_lastReportPath) || !File.Exists(_lastReportPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{_lastReportPath}\"",
            UseShellExecute = true
        });
    }

    private static AutomationSettings LoadSettings()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(settingsPath))
        {
            return new AutomationSettings();
        }

        var json = File.ReadAllText(settingsPath);
        return JsonSerializer.Deserialize<AutomationSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AutomationSettings();
    }

    private void ShowSettings(AutomationSettings settings)
    {
        ParaBankUrlText.Text = $"ParaBank URL: {settings.ParaBankUrl}";
        LoanAmountText.Text = $"Loan amount: {settings.LoanAmountUsd:C}";
        DownPaymentText.Text = $"Down payment rule: {settings.DownPaymentRate:P0} of initial deposit";
        CurrencyRateText.Text = $"Report currency conversion: 1 USD = {settings.UsdToEurRate:0.####} EUR | Browser: {(settings.RunBrowserHeadless ? "Headless fastest mode" : "Visible demo mode")} | Fast fallback: {(settings.UseFastApiFallback ? "On" : "Off")} | Fast transactions: {(settings.FastTransactionMode ? "On" : "Off")} | Browser wait: {settings.BrowserWaitSeconds}s";
    }

    private void AddLog(string message)
    {
        LogList.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
    }
}
