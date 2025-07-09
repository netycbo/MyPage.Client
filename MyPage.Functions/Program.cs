using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyPage.Functions;

var builder = FunctionsApplication.CreateBuilder(args);
builder.Services.AddScoped<ITelemetryApiCall, TelemetryApiCall>();
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowBlazorClient", policy =>
//    {
//        policy.WithOrigins(
//            "https://mikolaj-silinski.no",
//            "https://proud-stone-0db686703.2.azurestaticapps.net"

//        )
//        .AllowAnyHeader()
//        .AllowAnyMethod()
//        .AllowCredentials();
//    });
//});
builder.ConfigureFunctionsWebApplication();
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
builder.Services.AddSingleton<TelemetryClient>();

var host = builder.Build();

host.Run();