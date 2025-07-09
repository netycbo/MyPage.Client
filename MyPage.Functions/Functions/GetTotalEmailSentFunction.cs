using MailKit;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MyPage.Functions.Functions;

public class GetTotalEmailsSentFunction(ILogger<GetTotalEmailsSentFunction> logger, ITelemetryApiCall apiCall, TelemetryClient telemetry)
{
    [Function("GetTotalEmailsSent")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
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
        }
        catch (Exception ex)
        {
            telemetry.TrackException(ex, new Dictionary<string, string>
            {
                { "Function", "GetAvarageVisitDuration" },
                { "Path", req.Path },
                { "QueryString", req.QueryString.ToString() }
            });
        }
        return new BadRequestObjectResult(new { error = "Unable to fetch total emails sent." });
    }
}
