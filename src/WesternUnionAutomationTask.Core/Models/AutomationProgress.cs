namespace WesternUnionAutomationTask.Core.Models;

public sealed class AutomationProgress
{
    public AutomationProgress(string message, int completed, int total)
    {
        Message = message;
        Completed = completed;
        Total = total;
    }

    public string Message { get; }
    public int Completed { get; }
    public int Total { get; }
}
