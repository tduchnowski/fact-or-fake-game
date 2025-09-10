using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TrueFalseBackend.Infra.Data;
using TrueFalseBackend.Infra.Redis;
using TrueFalseBackend.Services;
using TrueFalseBackend.Models;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IQuestionProvider>(sp =>
    {
        List<Question> questions = Enumerable.Range(1, 100).Select(n => new Question { Id = n, Text = $"Question {n}", Answer = n % 2 == 0 }).ToList();
        return new InMemoryQuestionProvider(questions);
    });
}
else
{
    // TODO: replace it with a provider from a real db
    builder.Services.AddSingleton<IQuestionProvider>(sp =>
    {
        List<Question> questions = Enumerable.Range(1, 100).Select(n => new Question { Id = n, Text = $"Question {n}", Answer = n % 2 == 0 }).ToList();
        return new InMemoryQuestionProvider(questions);
    });
}
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => { return ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")); });
builder.Services.AddSingleton<IRoomSynchronizer, RedisGame>();
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<RedisDb>();
builder.Services.AddSingleton(sp =>
{
    var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
    var endpoints = new List<RedLockMultiplexer>
    {
        new RedLockMultiplexer(multiplexer)
    };
    return RedLockFactory.Create(endpoints);
});
builder.Services.AddHostedService<RedisStateUpdater>();
builder.Services.AddSingleton<IRedisLockerHelper, RedisLocker>();

var app = builder.Build();
app.MapControllers();
app.MapHub<MultiplayerHub>("/rooms");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.Run();
