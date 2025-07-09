using MailKit;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MyPage.Functions.Functions;

public class GetWeekToWeekVisits(ILogger<GetWeekToWeekVisits> logger, ITelemetryApiCall apiCall, TelemetryClient telemetry)
{
   
    [Function("GetWeekToWeekVisits")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            logger.LogInformation("fetching week to week ratio data ");
            var query = @"
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
                PreviousWeekVisits = iff(isnull(PreviousWeekVisits), 0, PreviousWeekVisits),
                Change = CurrentWeekVisits - PreviousWeekVisits,
                ChangePercent = iff(
    PreviousWeekVisits == 0 and CurrentWeekVisits == 0, 0.0,
    iff(PreviousWeekVisits == 0, 100.0,
        round((CurrentWeekVisits - PreviousWeekVisits) * 100.0 / PreviousWeekVisits, 1)
    )
)
            | project ChangePercent        
         ";
            var weekToWeekData = await apiCall.GetTelemetryData(query);
            if (weekToWeekData != null)
            {
                logger.LogInformation("Week to week visits fetched successfully.");
                return new OkObjectResult(new { total = weekToWeekData.Value });
            }
        }
        catch (Exception ex)
        {
            telemetry.TrackException(ex, new Dictionary<string, string>
            {
                { "Function", "GetWeekToWeekVisits" },
                { "Path", req.Path },
                { "QueryString", req.QueryString.ToString() }
            });
        }
        return new BadRequestObjectResult(new { error = "Unable to fetch total GetWeekToWeekVisits." });
    }
}