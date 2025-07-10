using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyPage.Client
{
    public class MiddlewareLogging(HttpClient httpClient, ILogger<MiddlewareLogging> logger) : IMiddlewareLogging
    {
        private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private async Task SendLogToFunction(string message, bool isError, SeverityLevel severity, Dictionary<string, string> properties)
        {
            try
            {
                var logEntry = new
                {
                    Message = message,
                    IsError = isError,
                    Severity = severity,
                    Properties = properties
                };

                // Wysyłanie do Azure Function
                await httpClient.PostAsJsonAsync("api/logs", logEntry);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send log to Azure Function");
            }
        }

        public async Task<T?> GetFromJsonAsync<T>(string requestUrl, string methodName = "")
        {
            try
            {
                logger.LogInformation("Api call started for {MethodName} with URL: {RequestUrl}", methodName, requestUrl);
                var response = await httpClient.GetAsync(requestUrl);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await SendLogToFunction(
                        $"API Error: {methodName}",
                        isError: true,
                        SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "MethodName", methodName },
                            { "RequestUrl", requestUrl },
                            { "StatusCode", response.StatusCode.ToString() },
                            { "ResponseContent", content }
                        });

                    return default;
                }

                return JsonSerializer.Deserialize<T>(content, CachedJsonSerializerOptions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API Exception: {Method} - {Uri}", methodName, requestUrl);
                await SendLogToFunction(
                    $"API Exception: {ex.Message}",
                    isError: true,
                    SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "Method", methodName },
                        { "Uri", requestUrl },
                        { "StackTrace", ex.StackTrace ?? "" }
                    });

                return default;
            }
        }

        public async Task<int> GetIntValueAsync(string requestUri, string valueKey, int defaultValue, string methodName = "")
        {
            var response = await GetFromJsonAsync<Dictionary<string, int>>(requestUri, methodName);

            if (response == null)
            {
                await SendLogToFunction(
                    $"API returned null: {methodName}",
                    isError: false,
                    SeverityLevel.Warning,
                    new Dictionary<string, string>
                    {
                        { "Method", methodName },
                        { "Uri", requestUri },
                        { "Issue", "NullResponse" }
                    });

                return defaultValue;
            }

            if (!response.TryGetValue(valueKey, out var value))
            {
                await SendLogToFunction(
                    $"API missing key '{valueKey}': {methodName}",
                    isError: false,
                    SeverityLevel.Warning,
                    new Dictionary<string, string>
                    {
                        { "Method", methodName },
                        { "Uri", requestUri },
                        { "MissingKey", valueKey },
                        { "AvailableKeys", string.Join(", ", response.Keys) },
                        { "Issue", "MissingKey" }
                    });

                return defaultValue;
            }

            logger.LogInformation("API Success: {Method} - {Key}={Value}", methodName, valueKey, value);
            return value;
        }
    }
}