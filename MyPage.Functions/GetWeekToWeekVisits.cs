using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MyPage.Functions;

public class GetWeekToWeekVisits(ILogger<GetWeekToWeekVisits> logger, IConfiguration config)
{
    private readonly HttpClient _httpClient = new();
    [Function("GetWeekToWeekVisits")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");
        var appId = config["APPINSIGHTS_APPID"];
        var apiKey = config["APPINSIGHTS_APIKEY"];
        var query = $@"
            let currentWeek =
                customEvents
                | where name == 'PageVisit'
                | where timestamp >= ago(7d)
                | where tostring(customDimensions.Page) == 'Home'
                | summarize CurrentWeekVisits = count();
            let previousWeek =
                customEvents
                | where name == 'PageVisit'
                | where timestamp between (ago(14d) .. ago(7d))
                | where tostring(customDimensions.Page) == 'Home'
                | summarize PreviousWeekVisits = count();
            currentWeek
            | extend Page = 'Home'
            | join kind=fullouter (previousWeek | extend Page = 'Home') on Page
            | extend
                CurrentWeekVisits = iff(isnull(CurrentWeekVisits), 0, CurrentWeekVisits),
                PreviousWeekVisits = iff(isnull(PreviousWeekVisits), 0, PreviousWeekVisits)
            | extend
                ChangePercent = iff(PreviousWeekVisits == 0 and CurrentWeekVisits == 0, 0.0, 
                               iff(PreviousWeekVisits == 0, 100.0, 
                                   round((CurrentWeekVisits - PreviousWeekVisits) * 100.0 / PreviousWeekVisits, 1)))
            | project ChangePercent
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

            return new OkObjectResult(new { pageVisitChangeInPercent = totalSum });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching telemetry from Application Insights");
            return new StatusCodeResult(500);
        }
    }
}