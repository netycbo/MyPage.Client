using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MyPage.Functions;

public class GetTotalEmailsSent(ILogger<GetTotalEmailsSent> logger, IConfiguration config)
{
    private readonly HttpClient _httpClient = new();

    [Function("GetTotalEmailsSent")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        logger.LogInformation("Fetching total emails sent from customMetrics...");

        var appId = config["APPINSIGHTS_APPID"];
        var apiKey = config["APPINSIGHTS_APIKEY"];
        var to = DateTime.UtcNow;
        var from = to.AddDays(-1);

        var query = $@"
            customMetrics
            | where timestamp >= datetime({from:yyyy-MM-ddTHH:mm:ssZ}) and timestamp < datetime({to:yyyy-MM-ddTHH:mm:ssZ})
            | where name == 'SendEmail Count'
            | extend customMetric_valueSum = iif(itemType == 'customMetric', valueSum, todouble(''))
            | summarize totalCount = sum(customMetric_valueSum) by bin(timestamp, 5m)
            | summarize totalSum = sum(totalCount)
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

            return new OkObjectResult(new { totalEmailsSent = totalSum });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching telemetry from Application Insights");
            return new StatusCodeResult(500);
        }
    }
}
