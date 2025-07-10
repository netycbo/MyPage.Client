using MailKit;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MyPage.Functions.Functions;


public class GetTotalVisit(ILogger<GetTotalVisit> logger, ITelemetryApiCall apiCall, TelemetryClient telemetry)
{
    [Function("GetTotalVisit")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            logger.LogInformation("fetching total visits data");
            var query = $@"customEvents
                | where name == 'PageVisit'
                | where timestamp >= ago(30d)
                | summarize VisitCount = count()
                | project VisitCount
                ";
            var totalCount = await apiCall.GetTelemetryData(query);
            if (totalCount.HasValue)
            {
                logger.LogInformation("Total visits fetched successfully.");
                return new OkObjectResult(new { total = totalCount.Value });
            }
        }
        catch (Exception ex)
        {
            telemetry.TrackException(ex, new Dictionary<string, string>
            {
                { "Function", "GetTotalVisit" },
                { "Path", req.Path },
                { "QueryString", req.QueryString.ToString() }
            });

            
        }
        return new BadRequestObjectResult(new { error = "Unable to fetch total visits." });
    }
}