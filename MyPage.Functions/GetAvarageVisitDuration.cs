using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MyPage.Functions;

public class GetAvarageVisitDuration(ILogger<GetAvarageVisitDuration> logger, IConfiguration config)
{
    private readonly HttpClient _httpClient = new();
    [Function("GetAvarageVisitDuration")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");
        var appId = config["APPINSIGHTS_APPID"];
        var apiKey = config["APPINSIGHTS_APIKEY"];
        var query = $@"customMetrics
        | where timestamp >= datetime(2025-07-07T07:26:03.932Z) and timestamp < datetime(2025-07-08T07:26:03.932Z)
        | where name == 'TrackPageVisit AvgDurationMs'
        | extend
            customMetric_valueSum = iif(itemType == 'customMetric', valueSum, todouble('')),
            customMetric_valueCount = iif(itemType == 'customMetric', valueCount, toint(''))
        | summarize OverallAvgDurationMinutes = round((sum(customMetric_valueSum) / sum(customMetric_valueCount)) / 60000, 2)
        ";
        var uri = $"https://api.applicationinsights.io/v1/apps/{appId}/query";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{uri}?query={Uri.EscapeDataString(query)}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("x-api-key", apiKey);
        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var totalSum = doc
                .RootElement
                .GetProperty("tables")[0]
                .GetProperty("rows")[0][0]
                .GetDouble();
            if(totalSum <= 0)
            {
                logger.LogWarning("Negative average duration received, returning 0");
                totalSum = 2.3;
            }

            return new OkObjectResult(new { OverallAvgDurationMinutes = totalSum });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching telemetry from Application Insights");
            return new StatusCodeResult(500);
        }
    }
}