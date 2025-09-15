using Microsoft.JSInterop;
using TrueFalseWebApp.Models;

namespace TrueFalseWebApp.Shared;

public class TelegramInitData
{
    private readonly IJSInProcessRuntime _jsSynchronousRuntime;

    public TelegramInitData(IJSRuntime jsRuntime)
    {
        _jsSynchronousRuntime = (IJSInProcessRuntime)jsRuntime;
    }

    public string GetInitDataString()
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

    public WebAppInitDataUnsafe? GetUnsafeData()
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
}