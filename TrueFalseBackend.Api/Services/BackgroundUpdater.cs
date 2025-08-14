using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;
using System.Text.Json;
using StackExchange.Redis;
using TrueFalseBackend.Services;
using TrueFalseBackend.Models;

public class RedisStateUpdater : IHostedService
{
    private readonly IHubContext<MultiplayerHub> _hubContext;
    private readonly GameService _gameService;
    private readonly IConnectionMultiplexer _conn;
    private readonly IDatabase _db;
    private readonly ISubscriber _subscriber;
    private readonly Channel<(string, string)> _messageChan;

    public RedisStateUpdater(IHubContext<MultiplayerHub> hubContext, IConnectionMultiplexer connection, GameService gameService)
    {
        _hubContext = hubContext;
        _conn = connection;
        _db = _conn.GetDatabase();
        _subscriber = _conn.GetSubscriber();
        _gameService = gameService;
        _messageChan = Channel.CreateUnbounded<(string, string)>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            Console.WriteLine("Starting listening on the channel");
            await foreach (var (roomId, message) in _messageChan.Reader.ReadAllAsync(cancellationToken))
            {
                _ = Task.Run(async () =>
                {
                    await _hubContext.Clients.Group(roomId).SendAsync("state", message);
                    await _hubContext.Clients.All.SendAsync("state", message);
                    await SaveString(roomId, message);
                    RoomState state = JsonSerializer.Deserialize<RoomState>(message);
                    _gameService.UpdateState(roomId, state);
                });
            }
        });

        RedisChannel chan = new RedisChannel($"states:*", RedisChannel.PatternMode.Pattern);
        _subscriber.SubscribeAsync(chan, (channel, message) =>
        {
            string[] channelParts = channel.ToString().Split(':');
            string roomId = channelParts[1];
            Console.WriteLine(message.ToString());
            _ = _messageChan.Writer.WriteAsync((roomId, message.ToString()));
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken token) => Task.CompletedTask;

    private async Task SaveString(string roomId, string? roomStateJson)
    {
        Console.WriteLine("Save string " + roomStateJson);
        if (roomStateJson == null) return;
        await _db.StringSetAsync($"states:{roomId}", roomStateJson);
    }
}
