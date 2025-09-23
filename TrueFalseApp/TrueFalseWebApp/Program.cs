using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor.Services;
using TrueFalseWebApp.Shared;

using TrueFalseWebApp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<TelegramInitData>();
var config = builder.Configuration;
string apiBaseUrl = config["TrueFalseApi:RestApi"] ?? throw new InvalidOperationException("API base URL is not configured.");
string hubUrl = config["TrueFalseApi:HubUrl"] ?? throw new InvalidOperationException("Hub URL is not configured.");

builder.Services.AddScoped(sp =>
{
    var tgInitData = sp.GetRequiredService<TelegramInitData>();
    var initDataString = tgInitData.InitDataString;
    HttpClient client = new HttpClient
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
    if (!string.IsNullOrEmpty(initDataString))
    {
        client.DefaultRequestHeaders.Add("X-Telegram-InitData", $"tma {initDataString}");
    }
    else
    {
        client.DefaultRequestHeaders.Add("X-Telegram-InitData", $"tma");
    }
    return client;
});

builder.Services.AddTransient(sp =>
{
    var tgInitData = sp.GetRequiredService<TelegramInitData>();
    var initDataString = tgInitData.InitDataString;
    HubConnectionBuilder connectionBuilder = new HubConnectionBuilder();
    if (!string.IsNullOrEmpty(hubUrl))
    {
        connectionBuilder.WithUrl(hubUrl, options =>
        {
            options.Headers.Add("X-Telegram-InitData", $"tma {initDataString}");
        });
    }
    else
    {
        // TODO: just validate the init data before any of this stuff happens
        // and if the data is missing, redirect to some page that tells the user
        // to open the web app through Telegram
        connectionBuilder.WithUrl(hubUrl, options =>
        {
            options.Headers.Add("X-Telegram-InitData", $"tma");
        });
    }
    return connectionBuilder
        .WithAutomaticReconnect()
        .Build();
});
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
});
await builder.Build().RunAsync();
