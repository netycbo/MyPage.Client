using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyPage.Functions;

public class TrackPageVisit(ILogger<TrackPageVisit> logger, TelemetryClient telemetryClient)
{
    [Function("TrackPageVisit")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        if (req.Method == "OPTIONS")
        {
            var response = new OkResult();
            AddCorsHeaders(req.HttpContext.Response);
            return response;
        }
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            logger.LogInformation($"Request body: {requestBody}");

            var pageVisitData = JsonSerializer.Deserialize<PageVisitDataDto>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (pageVisitData == null)
            {
                logger.LogWarning("Failed to deserialize page visit data");
                return new BadRequestObjectResult("Invalid request data");
            }

            var properties = new Dictionary<string, string>
            {
                { "Page", pageVisitData.Page ?? "Unknown" },
                { "VisitTime", pageVisitData.VisitTime.ToString("o") },
            };

            logger.LogInformation($"Tracking event with properties: {string.Join(", ", properties.Select(p => $"{p.Key}={p.Value}"))}");

            telemetryClient.TrackEvent("PageVisit", properties);
            telemetryClient.Flush();

            logger.LogInformation("PageVisit event tracked successfully");

            return new OkResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to track page visit event");
            telemetryClient.TrackException(ex);
            telemetryClient.Flush();

            var errorResponse = new StatusCodeResult(500);
            AddCorsHeaders(req.HttpContext.Response);
            return errorResponse;
        }
    }
    private class PageVisitDataDto
    {
        [JsonPropertyName("page")]
        
        public string? Page { get; set; }
        [JsonPropertyName("visitTime")]
        public DateTime VisitTime { get; set; }
    }
    private void AddCorsHeaders(HttpResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "https://mikolaj-silinski.no");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        response.Headers.Add("Access-Control-Allow-Credentials", "true");
    }
}