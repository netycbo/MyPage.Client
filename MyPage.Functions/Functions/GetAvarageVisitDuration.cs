using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MyPage.Functions.Functions;

public class GetAvarageVisitDuration(ILogger<GetAvarageVisitDuration> logger, ITelemetryApiCall apiCall)
{
   
    [Function("GetAvarageVisitDuration")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        logger.LogInformation("Fetching avarage visit duration data");
        var query =@" 
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
        else
        {
            logger.LogError("Failed to fetch avarage visit duration.");
            return new StatusCodeResult(500);
        }
       
    }
}