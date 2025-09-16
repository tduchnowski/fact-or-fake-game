using Microsoft.JSInterop;
using TrueFalseWebApp.Models;

namespace TrueFalseWebApp.Shared;

public class TelegramInitData
{
    private readonly IJSInProcessRuntime _jsSynchronousRuntime;
    public string? InitDataString { get; init; }
    public WebAppInitDataUnsafe? InitDataUnsafe { get; private set; }

    public TelegramInitData(IJSRuntime jsRuntime)
    {
        _jsSynchronousRuntime = (IJSInProcessRuntime)jsRuntime;
        InitDataString = GetInitDataString();
        InitDataUnsafe = GetUnsafeData();
    }

    private string GetInitDataString()
    {
        try
        {
            return _jsSynchronousRuntime.Invoke<string?>("getTelegramInitData") ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting Telegram Web App init data: {ex}");
            return string.Empty;
        }
    }

    private WebAppInitDataUnsafe? GetUnsafeData()
    {
        try
        {
            return _jsSynchronousRuntime.Invoke<WebAppInitDataUnsafe?>("getTelegramInitDataUnsafe");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting Telegram Web App unsafe init data: {ex}");
            return null;
        }
    }

    public void ResetStartParam()
    {
        if (InitDataUnsafe is not null)
        {
            InitDataUnsafe = InitDataUnsafe with { StartParam = null };
        }
    }
}