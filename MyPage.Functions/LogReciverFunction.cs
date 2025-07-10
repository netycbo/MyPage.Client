using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


public static class LogReceiverFunction
{
    [Function("LogReceiver")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "logs")] HttpRequest req,
        ILogger log)
    {
        var logData = await req.ReadFromJsonAsync<LogEntry>();
        var telemetryClient = new TelemetryClient();

        if (logData.IsError)
        {
            telemetryClient.TrackException(new Exception(logData.Message), logData.Properties);
        }
        else
        {
            telemetryClient.TrackTrace(logData.Message, logData.Severity, logData.Properties);
        }

        telemetryClient.Flush();
        return new OkResult();
    }
}

public class LogEntry
{
    public string Message { get; set; }
    public bool IsError { get; set; }
    public SeverityLevel Severity { get; set; }
    public Dictionary<string, string> Properties { get; set; }
}