
namespace MyPage.Client
{
    public interface IMiddlewareLogging
    {
        Task<T?> GetFromJsonAsync<T>(string reqesturl, string methodName = "");
        Task<int> GetIntValueAsync(string requestUri, string valueKey, int defaultValue, string methodName = "");
    }
}