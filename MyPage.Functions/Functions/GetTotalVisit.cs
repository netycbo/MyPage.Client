using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MyPage.Functions.Functions;


public class GetTotalVisit(ILogger<GetTotalVisit> logger, ITelemetryApiCall apiCall)
{
    [Function("GetTotalVisit")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        logger.LogInformation("fetching total visits data");
        var query = $@"customEvents
            | where name == 'PageVisit'
            | where timestamp >= ago(30d)
            | summarize VisitCount = count() 
            | order by VisitCount desc
            ";
        var totalCount = await apiCall.GetTelemetryData(query);
        if (totalCount.HasValue)
        {
            logger.LogInformation("Total visits fetched successfully.");
            return new OkObjectResult(new { total = totalCount.Value });
        }
        else
        {
            logger.LogError("Failed to fetch total visits.");
            return new StatusCodeResult(500);
        }
    }
}