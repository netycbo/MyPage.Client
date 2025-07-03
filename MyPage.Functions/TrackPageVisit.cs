using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text.Json;

namespace MyPage.Functions;

public class TrackPageVisit(ILogger<TrackPageVisit> logger, TelemetryClient telemetryClient)
{
    [Function("TrackPageVisit")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        logger.LogInformation("HTTP trigger function processed a request.");
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var pageVisitData = JsonSerializer.Deserialize<PageVisitDataDto>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        try
        {
            telemetryClient.TrackEvent("PageVisit", new Dictionary<string, string>
        {
            { "Page", pageVisitData?.Page ?? "Unknown" },
            { "VisitTime", pageVisitData?.VisitTime.ToString("o") ?? DateTime.UtcNow.ToString("o") }
        });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to track event");
        }
        return new OkResult();
    }
    private class PageVisitDataDto
    {
        public string? Page { get; set; }
        public DateTime VisitTime { get; set; }
    }
}