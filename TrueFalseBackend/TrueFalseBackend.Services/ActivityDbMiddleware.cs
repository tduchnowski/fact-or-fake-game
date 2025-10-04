using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Web;
using System.Text.Json;
using TrueFalseBackend.Models;

namespace TrueFalseBackend.Services;

public class ActivityDbMiddleware : IMiddleware
{
    private readonly ActivityLoggerQueue _activityQueue;
    private readonly ILogger<ActivityDbMiddleware> _logger;

    public ActivityDbMiddleware(ActivityLoggerQueue activityQueue, ILogger<ActivityDbMiddleware> logger)
    {
        _activityQueue = activityQueue;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            string initData = (string)context.Items["InitData"];
            if (string.IsNullOrEmpty(initData))
            {
                var parsed = HttpUtility.ParseQueryString(initData);
                WebAppUser? webAppUser = JsonSerializer.Deserialize<WebAppUser>(HttpUtility.UrlDecode(parsed["user"]));
                if (webAppUser != null)
                {
                    User user = new User
                    {
                        Username = webAppUser.Username,
                        TimeFirstActivity = DateTime.UtcNow,
                        TimeLastActivity = DateTime.UtcNow
                    };
                    await _activityQueue.Enqueue(user);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error:{e}", e);
        }
        await next(context);
    }
}
