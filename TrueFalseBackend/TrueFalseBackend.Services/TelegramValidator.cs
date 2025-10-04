using System.Text;
using System.Web;
using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;

public class TelegramValidator : IMiddleware
{
    private readonly string _botToken;
    private readonly string _apiKey;

    public TelegramValidator(string botToken, string apiKey)
    {
        _botToken = botToken;
        _apiKey = apiKey;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        StringValues initData;
        if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 204;
            return;
        }
        else if (context.Request.Path.StartsWithSegments("/api/hub") && context.Request.Query.TryGetValue("X-Telegram-Initdata", out initData) && StringValues.IsNullOrEmpty(initData))
        {
            string data = initData.ToString();
            var headerParts = data.Split(" ");
            if (headerParts.Length < 2)
            {
                await UnathorizedResponse(context, "InitData validation failed");
                return;
            }
            data = headerParts[1];
            if (IsValid(data))
            {
                await next(context);
            }
            else
            {
                await UnathorizedResponse(context, "InitData validation failed");
                return;
            }
        }
        else if (context.Request.Headers.TryGetValue("X-Telegram-Initdata", out initData))
        {
            string data = initData.ToString();
            var headerParts = data.Split(" ");
            if (headerParts.Length < 2)
            {
                await UnathorizedResponse(context, "InitData validation failed");
                return;
            }
            string mode = headerParts[0];
            data = headerParts[1];
            switch (mode)
            {
                case "tma":
                    if (IsValid(data))
                    {
                        context.Items["InitData"] = data;
                        await next(context);
                    }
                    else
                    {
                        await UnathorizedResponse(context, "InitData validation failed");
                    }
                    break;
                case "bot":
                    if (IsValidBot(data))
                    {
                        context.Items["InitData"] = headerParts[2];
                        await next(context);
                    }
                    else
                    {
                        await UnathorizedResponse(context, "InitData validation failed");
                    }
                    break;
            }
        }
        else
        {
            await UnathorizedResponse(context, "No Authorization header found");
        }
    }

    private bool IsValid(string data)
    {
        var parsed = HttpUtility.ParseQueryString(data);
        string hash = parsed["hash"];
        var withoutHashPairs = parsed.AllKeys
          .Where(k => k != "hash")
          .OrderBy(k => k)
          .Select(k => $"{k}={parsed[k]}");
        var checkString = string.Join("\n", withoutHashPairs);
        Console.WriteLine(checkString);
        using var hmac1 = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
        var secretKey = hmac1.ComputeHash(Encoding.UTF8.GetBytes(_botToken));
        using var hmac2 = new HMACSHA256(secretKey);
        var serverSideHash = hmac2.ComputeHash(Encoding.UTF8.GetBytes(checkString));
        var serverSideHashHex = BitConverter.ToString(serverSideHash).Replace("-", "").ToLower();
        return serverSideHashHex == hash;
    }

    private bool IsValidBot(string key)
    {
        return key == _apiKey;
    }

    private async Task UnathorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync(message);
    }
}
