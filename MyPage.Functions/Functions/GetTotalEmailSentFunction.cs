using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MyPage.Functions.Functions;

public class GetTotalEmailsSentFunction(ILogger<GetTotalEmailsSentFunction> logger, ITelemetryApiCall apiCall)
{
    [Function("GetTotalEmailsSent")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        logger.LogInformation("Fetching total emails sent...");
        var query = $@"
            customEvents
            | where timestamp > ago(30d)
            | where name == 'EmailSendAttempt'
            | summarize LiczbaWyslanychEmaili = count()";
        var totalCount = await apiCall.GetTelemetryData(query);
        if (totalCount.HasValue)
        {
            logger.LogInformation("Total emails sent fetched successfully.");
            return new OkObjectResult(new { total = totalCount.Value });
        }
        else
        {
            logger.LogError("Failed to fetch total emails sent.");
            return new StatusCodeResult(500);
        }
    }
}
