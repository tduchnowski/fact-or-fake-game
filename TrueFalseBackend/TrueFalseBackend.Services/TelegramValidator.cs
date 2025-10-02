using System.Text;
using System.Web;
using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;

public class TelegramValidator : IMiddleware
{
    private readonly string _botToken;

    public TelegramValidator(string botToken) => _botToken = botToken;

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
            data = headerParts[1];
            if (IsValid(data))
            {
                context.Items["InitData"] = data;
                await next(context);
            }
            else
            {
                await UnathorizedResponse(context, "InitData validation failed");
                return;
            }
        }
        else
        {
            await UnathorizedResponse(context, "No Authorization header found");
            return;
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
        using var hmac1 = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
        var secretKey = hmac1.ComputeHash(Encoding.UTF8.GetBytes(_botToken));
        using var hmac2 = new HMACSHA256(secretKey);
        var serverSideHash = hmac2.ComputeHash(Encoding.UTF8.GetBytes(checkString));
        var serverSideHashHex = BitConverter.ToString(serverSideHash).Replace("-", "").ToLower();
        return serverSideHashHex == hash;
    }

    private async Task UnathorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync(message);
    }
}
