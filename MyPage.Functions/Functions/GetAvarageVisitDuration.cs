using MailKit;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MyPage.Functions.Functions;

public class GetAvarageVisitDuration(ILogger<GetAvarageVisitDuration> logger, ITelemetryApiCall apiCall, TelemetryClient telemetry)
{
   
    [Function("GetAvarageVisitDuration")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            logger.LogInformation("Fetching avarage visit duration data");
            var query = @" 
            customMetrics
                    | where timestamp >= datetime(2025 - 07 - 07T07: 26:03.932Z) and timestamp<datetime(2025 - 07 - 08T07: 26:03.932Z)
                    | where name == 'TrackPageVisit AvgDurationMs'
                    | extend
                        customMetric_valueSum = iif(itemType == 'customMetric', valueSum, todouble('')),
                        customMetric_valueCount = iif(itemType == 'customMetric', valueCount, toint(''))
                    | summarize OverallAvgDurationMinutes = round((sum(customMetric_valueSum) / sum(customMetric_valueCount)) / 60000, 2)
            ";
            var averageDuration = await apiCall.GetTelemetryData(query);
            if (averageDuration.HasValue)
            {
                logger.LogInformation("Avarage visit duration fetched successfully.");
                return new OkObjectResult(new { averageDuration = averageDuration.Value });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching average visit duration");
            telemetry.TrackException(ex, new Dictionary<string, string>
            {
                { "Function", "GetAvarageVisitDuration" },
                { "Path", req.Path },
                { "QueryString", req.QueryString.ToString() }
            });
            
        }
        return new BadRequestObjectResult(new { error = "Unable to fetch total AvarageVisitDurations." });
    }
}