using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Net;
using System.Text.Json;

namespace MyPage.Functions;

public class SendEmailFunction(ILogger<SendEmailFunction> logger, IConfiguration config) : ISendEmailFunction
{
    [Function("SendEmail")]
    public async Task<HttpResponseData> SendEmail([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        logger.LogInformation("SendEmail function processing request.");
        var response = req.CreateResponse();
        AddCorsHeaders(response);
        if (req.Method == "OPTIONS")
        {
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var emailRequest = JsonSerializer.Deserialize<EmailRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (emailRequest == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Invalid request data");
                return response;
            }

            bool success = await SendEmailAsync(emailRequest.FromName, emailRequest.FromEmail, emailRequest.Message);

            response.StatusCode = success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
            var result = new { success = success, message = success ? "Email sent successfully" : "Failed to send email" };
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing SendEmail request");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Internal server error: {ex.Message}");
            return errorResponse;
        }
    }
    private async Task<bool> SendEmailAsync(string fromName, string fromEmail, string message)
    {
        var smtpHost = config["Smtp:Host"];
        var smtpPort = int.Parse(config["Smtp:Port"] ?? "587");
        var smtpUser = config["Smtp:User"];
        var smtpPass = config["Smtp:Pass"];
        var smtpFrom = config["Smtp:From"];
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(fromName, smtpFrom));
        email.To.Add(MailboxAddress.Parse(smtpFrom));
        email.Subject = $"Wiadomość od {fromName}";
        email.Body = new TextPart("plain") { Text = $"Od: {fromName} ({fromEmail})\n\n{message}" };

        using var smtp = new SmtpClient();

        try
        {
            await smtp.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(smtpUser, smtpPass);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            logger.LogInformation("Email sent successfully to {FromEmail}", fromEmail);
            return true;
        }
        catch (SmtpCommandException ex)
        {
            logger.LogError("SMTP COMMAND ERROR: {Message} | Code: {StatusCode}", ex.Message, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MailKit general SMTP error");
            return false;
        }
    }
    private void AddCorsHeaders(HttpResponseData response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "https://mikolaj-silinski.no");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        response.Headers.Add("Access-Control-Allow-Credentials", "true");
    }
}
public class EmailRequest
{
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}