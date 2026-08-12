using Azure.Monitor.OpenTelemetry.Exporter;
using GameBackend.DailyRewards;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(
    Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Services.AddSingleton(new DailyRewardOptions
{
    SupportedQuestId = "daily_login",
    RewardCurrencyId = "currency.soft",
    RewardAmount = 50
});

builder.Services.AddSingleton<ISystemClock, SystemClock>();

builder.Services.AddSingleton<IDailyRewardStateStore,
    InMemoryDailyRewardStateStore>();

builder.Services.AddSingleton<ICurrencyWallet,
    InMemoryCurrencyWallet>();

builder.Services.AddScoped<DailyRewardService>();

builder.Build().Run();