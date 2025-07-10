using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyPage.Functions
{
    public class TelemetryApiCall(IConfiguration config, ILogger<TelemetryApiCall> logger, TelemetryClient telemetry) : ITelemetryApiCall
    {
        private readonly HttpClient _httpClient = new();
        public async Task<int?> GetTelemetryData(string query)
        {
            var appId = config["APPINSIGHTS_APPID"];
            var apiKey = config["APPINSIGHTS_APIKEY"];
            var uri = $"https://api.applicationinsights.io/v1/apps/{appId}/query";
            var request = new HttpRequestMessage(HttpMethod.Get, $"{uri}?query={Uri.EscapeDataString(query)}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("x-api-key", apiKey);
            telemetry.TrackTrace("AppInsights config", SeverityLevel.Information, new Dictionary<string, string>
            {
                { "AppId", appId ?? "null" },
                { "ApiKey", string.IsNullOrWhiteSpace(apiKey) ? "NO" : "YES" }
            });
            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogError($"Błąd HTTP: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {errorContent}");

                    telemetry.TrackException(new HttpRequestException($"AppInsights 404: {uri}"),
                        new Dictionary<string, string>
                        {
                    { "Query", query },
                    { "StatusCode", ((int)response.StatusCode).ToString() },
                    { "ResponseBody", errorContent }
                        });

                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                var totalCount = doc
                    .RootElement
                    .GetProperty("tables")[0]
                    .GetProperty("rows")[0][0]
                    .GetInt32();
                return totalCount;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching telemetry from Application Insights");
                return null;
            }
        }
    }
}
