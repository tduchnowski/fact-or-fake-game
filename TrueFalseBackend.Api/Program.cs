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
builder.Services.AddSingleton<IQuestionProvider>(sp =>
{
    return new InMemoryQuestionProvider(new List<Question>
    {
      new Question { Id = 1, Text = "Question 1", Answer = true },
      new Question { Id = 2, Text = "Question 2", Answer = false},
      new Question { Id = 3, Text = "Question 3", Answer = true },
      new Question { Id = 4, Text = "Question 4", Answer = false},
      new Question {Id = 5, Text = "Is Lerusha the most talented painter in the world?", Answer = true},
      new Question {Id = 6, Text = "Is Tommy Boy 1.96m tall?", Answer = true},
      new Question {Id = 7, Text = "Was Lerusha born in 2000?", Answer = true},
      new Question {Id = 8, Text = "Is Lerusha super angry when she's hungry?", Answer = true}
    });
});
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
