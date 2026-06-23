using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using WesternUnionAutomationTask.Core.Models;

namespace WesternUnionAutomationTask.Rpa;

internal sealed class ParaBankApiClient
{
    private readonly string _serviceBaseUrl;
    private readonly HttpClient _httpClient;

    public ParaBankApiClient(string paraBankHomeUrl)
    {
        var root = paraBankHomeUrl.Replace("/index.htm", string.Empty, StringComparison.OrdinalIgnoreCase).TrimEnd('/');
        _serviceBaseUrl = $"{root}/services/bank";
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(4)
        };
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json, application/xml;q=0.8");
    }

    public int? LoginForCustomerId(CustomerProfile customer)
    {
        var username = Uri.EscapeDataString(customer.Username);
        var password = Uri.EscapeDataString(customer.Password);
        var response = SendGet($"login/{username}/{password}");
        return ReadFirstInt(response, "id", "customerId");
    }

    public IReadOnlyList<int> GetAccountIds(int customerId)
    {
        var response = SendGet($"customers/{customerId}/accounts");
        return ReadAllInts(response, "id").Distinct().ToList();
    }

    public int? CreateAccount(int customerId, string accountType, int fromAccountId)
    {
        var typeCode = string.Equals(accountType, "SAVINGS", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var response = SendPost($"createAccount?customerId={customerId}&newAccountType={typeCode}&fromAccountId={fromAccountId}");
        return ReadFirstInt(response, "id", "accountId");
    }

    public LoanApiResult RequestLoan(int customerId, decimal amount, decimal downPayment, int fromAccountId)
    {
        var amountText = amount.ToString("0.00", CultureInfo.InvariantCulture);
        var downPaymentText = downPayment.ToString("0.00", CultureInfo.InvariantCulture);
        var response = SendPost($"requestLoan?customerId={customerId}&amount={amountText}&downPayment={downPaymentText}&fromAccountId={fromAccountId}");

        var approved = ReadFirstBool(response, "approved");
        var accountId = ReadFirstInt(response, "accountId", "newAccountId", "loanAccountId");
        var message = ReadFirstString(response, "message", "response", "status") ?? string.Empty;

        if (approved == true)
        {
            return new LoanApiResult(true, accountId, string.IsNullOrWhiteSpace(message) ? "Approved" : message);
        }

        if (approved == false)
        {
            return new LoanApiResult(false, accountId, string.IsNullOrWhiteSpace(message) ? "Denied" : message);
        }

        return new LoanApiResult(null, accountId, string.IsNullOrWhiteSpace(message) ? "Submitted" : message);
    }

    private string SendGet(string relativeUrl)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_serviceBaseUrl}/{relativeUrl}");
        using var response = _httpClient.Send(request);
        return ReadSuccessfulResponse(response);
    }

    private string SendPost(string relativeUrl)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_serviceBaseUrl}/{relativeUrl}");
        using var response = _httpClient.Send(request);
        return ReadSuccessfulResponse(response);
    }

    private static string ReadSuccessfulResponse(HttpResponseMessage response)
    {
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"ParaBank service returned {(int)response.StatusCode}: {body}");
        }

        return body;
    }

    private static int? ReadFirstInt(string response, params string[] names)
    {
        foreach (var value in ReadAllInts(response, names))
        {
            return value;
        }

        return null;
    }

    private static IEnumerable<int> ReadAllInts(string response, params string[] names)
    {
        foreach (var value in ReadJsonInts(response, names))
        {
            yield return value;
        }

        foreach (var name in names)
        {
            foreach (Match match in Regex.Matches(response, $"<{Regex.Escape(name)}>(\\d+)</{Regex.Escape(name)}>", RegexOptions.IgnoreCase))
            {
                if (int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    yield return parsed;
                }
            }
        }
    }

    private static IEnumerable<int> ReadJsonInts(string response, params string[] names)
    {
        List<int> values;
        try
        {
            using var document = JsonDocument.Parse(response);
            values = ReadJsonInts(document.RootElement, names).ToList();
        }
        catch (JsonException)
        {
            values = new List<int>();
        }

        return values;
    }

    private static IEnumerable<int> ReadJsonInts(JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var parsed))
                    {
                        yield return parsed;
                    }
                    else if (property.Value.ValueKind == JsonValueKind.String && int.TryParse(property.Value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedString))
                    {
                        yield return parsedString;
                    }
                }

                foreach (var nested in ReadJsonInts(property.Value, names))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in ReadJsonInts(item, names))
                {
                    yield return nested;
                }
            }
        }
    }

    private static bool? ReadFirstBool(string response, string name)
    {
        try
        {
            using var document = JsonDocument.Parse(response);
            return ReadJsonBool(document.RootElement, name);
        }
        catch (JsonException)
        {
            var match = Regex.Match(response, $"<{Regex.Escape(name)}>(true|false)</{Regex.Escape(name)}>", RegexOptions.IgnoreCase);
            return match.Success ? bool.Parse(match.Groups[1].Value) : null;
        }
    }

    private static bool? ReadJsonBool(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    return property.Value.GetBoolean();
                }

                var nested = ReadJsonBool(property.Value, name);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = ReadJsonBool(item, name);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static string? ReadFirstString(string response, params string[] names)
    {
        try
        {
            using var document = JsonDocument.Parse(response);
            var jsonValue = ReadJsonString(document.RootElement, names);
            if (!string.IsNullOrWhiteSpace(jsonValue))
            {
                return jsonValue;
            }
        }
        catch (JsonException)
        {
            // XML fallback below.
        }

        foreach (var name in names)
        {
            var match = Regex.Match(response, $"<{Regex.Escape(name)}>(.*?)</{Regex.Escape(name)}>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return null;
    }

    private static string? ReadJsonString(JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)) && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }

                var nested = ReadJsonString(property.Value, names);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = ReadJsonString(item, names);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }
}

internal sealed record LoanApiResult(bool? Approved, int? AccountId, string Message);
