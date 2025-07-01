using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace MyPage.Functions
{
    public interface ISendEmailFunction
    {
        Task<HttpResponseData> SendEmail([HttpTrigger(AuthorizationLevel.Function, new[] { "post" })] HttpRequestData req);
    }
}