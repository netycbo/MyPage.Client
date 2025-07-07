using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Monitor.Query;

namespace MyPage.Functions;

public class GetTotalVisitFromBegining(ILogger<GetTotalVisitFromBegining> logger)
{
    private readonly string _applicationInsightsResourceId;
    [Function("GetTotalVisit")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");
        if (req.Method == "OPTIONS")
        {
            var response = new OkResult();
            AddCorsHeaders(req.HttpContext.Response);
            return response;
        }
        try
        {
            var credential = new DefaultAzureCredential();
            var client = new LogsQueryClient(credential);
            var queryTimeRange = new QueryTimeRange(DateTimeOffset.Parse("2025-07-02T00:00:00Z"), DateTimeOffset.UtcNow);
            string kustoQuery = "customEvents | where name == 'PageVisit' | summarize totalVisits = count()";

            var response = await client.QueryWorkspaceAsync(
                _applicationInsightsResourceId.Split('/')[4],
                kustoQuery,
                queryTimeRange);

            if (response.Value.Table.Rows.Count > 0)
            {
                long totalVisitsLong = (long)response.Value.Table.Rows[0][0];
                int totalVisitsInt = (int)totalVisitsLong;

                logger.LogInformation($"Total visits from KQL: {totalVisitsInt}");
                return new OkObjectResult(new { totalVisits = totalVisitsInt });
            }
            else
            {
                logger.LogWarning("No data found for total visits query, returning 0.");
                return new OkObjectResult(new { totalVisits = 0 });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting total visits from Application Insights.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
    private void AddCorsHeaders(HttpResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "https://mikolaj-silinski.no");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        response.Headers.Add("Access-Control-Allow-Credentials", "true");
    }
}