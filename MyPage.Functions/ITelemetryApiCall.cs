using Microsoft.AspNetCore.Mvc;

namespace MyPage.Functions
{
    public interface ITelemetryApiCall
    {
        Task<int?> GetTelemetryData(string query);
    }
}