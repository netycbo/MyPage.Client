using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Text.Json;

namespace MyPage.Client
{
    public class MiddlewareLogging(HttpClient httpClient, ILogger<MiddlewareLogging> logger, TelemetryClient telemetryClient) : IMiddlewareLogging
    {
        private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<T?> GetFromJsonAsync<T>(string reqesturl, string methodName = "")
        {
            try
            {
                logger.LogInformation("Api call started for {MethodName} with URL: {RequestUrl}", methodName, reqesturl);
                var response = await httpClient.GetAsync(reqesturl);
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    telemetryClient.TrackTrace($"Api Error:{methodName}", SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                            { "MethodName", methodName },
                            { "RequestUrl", reqesturl },
                            { "StatusCode", response.StatusCode.ToString() },
                            { "ResponseContent", content }
                    });

                    return default;
                }
                return JsonSerializer.Deserialize<T>(content, CachedJsonSerializerOptions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API Exception: {Method} - {Uri}", methodName, reqesturl);
                telemetryClient.TrackException(ex, new Dictionary<string, string>
                    {
                        { "Method", methodName },
                        { "Uri", reqesturl }
                    });
                return default;
            }
        }

        public async Task<int> GetIntValueAsync(string requestUri, string valueKey, int defaultValue, string methodName = "")
        {
            var response = await GetFromJsonAsync<Dictionary<string, int>>(requestUri, methodName);

            if (response == null)
            {
                logger.LogWarning("API returned null: {Method}", methodName);
                telemetryClient.TrackTrace($"API returned null: {methodName}", SeverityLevel.Warning,
                    new Dictionary<string, string>
                    {
                            { "Method", methodName },
                            { "Uri", requestUri },
                            { "Issue", "NullResponse" }
                    });
                telemetryClient.Flush();
                return defaultValue;
            }

            if (!response.TryGetValue(valueKey, out var value))
            {
                logger.LogWarning("API missing key '{Key}': {Method}. Available keys: {Keys}",
                    valueKey, methodName, string.Join(", ", response.Keys));
                telemetryClient.TrackTrace($"API missing key: {methodName}", SeverityLevel.Warning,
                    new Dictionary<string, string>
                    {
                            { "Method", methodName },
                            { "Uri", requestUri },
                            { "MissingKey", valueKey },
                            { "AvailableKeys", string.Join(", ", response.Keys) },
                            { "Issue", "MissingKey" }
                    });
                telemetryClient.Flush();
                return defaultValue;
            }
            logger.LogInformation("API Success: {Method} - {Key}={Value}", methodName, valueKey, value);
            return value;
        }
    }
}
