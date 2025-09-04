using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;
using StackExchange.Redis;
using TrueFalseBackend.Services;
using TrueFalseBackend.Models;

public class RedisStateUpdater : IHostedService
{
    private readonly IHubContext<MultiplayerHub> _hubContext;
    private readonly GameService _gameService;
    private readonly IConnectionMultiplexer _conn;
    // private readonly IDatabase _db;
    private readonly ISubscriber _subscriber;
    private readonly Channel<(string, string)> _stateChan;
    private readonly Channel<(string, string)> _playersChan;
    private readonly Channel<(string, string)> _answersChan;

    public RedisStateUpdater(IHubContext<MultiplayerHub> hubContext, IConnectionMultiplexer connection, GameService gameService)
    {
        _hubContext = hubContext;
        _conn = connection;
        // _db = _conn.GetDatabase();
        _subscriber = _conn.GetSubscriber();
        _gameService = gameService;
        _stateChan = Channel.CreateUnbounded<(string, string)>();
        _playersChan = Channel.CreateUnbounded<(string, string)>();
        _answersChan = Channel.CreateUnbounded<(string, string)>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Updater start async");
        _ = Task.Run(async () =>
        {
            await foreach (var (channel, message) in _stateChan.Reader.ReadAllAsync(cancellationToken))
            {
                _ = Task.Run(async () =>
                {
                    string[] channelParts = channel.ToString().Split(':');
                    string roomId = channelParts[1];
                    await _hubContext.Clients.Group(roomId).SendAsync("state", message);
                    // await SaveString(roomId, message);
                });
            }
        });

        _ = Task.Run(async () =>
        {
            await foreach (var (channel, message) in _playersChan.Reader.ReadAllAsync(cancellationToken))
            {
                _ = Task.Run(async () =>
                {
                    string[] channelParts = channel.ToString().Split(':');
                    string roomId = channelParts[1];
                    Console.WriteLine($"roomId: {roomId} broadcasting");
                    await _hubContext.Clients.Group(roomId).SendAsync("players", message);
                    // await SaveString(channel, message);
                });
            }
        });

        _ = Task.Run(async () =>
        {
            await foreach (var (channel, message) in _answersChan.Reader.ReadAllAsync(cancellationToken))
            {
                _ = Task.Run(async () =>
                {
                    string[] channelParts = channel.ToString().Split(':');
                    string roomId = channelParts[1];
                    int roundId = int.Parse(channelParts[2]);
                    // await SaveString(channel, message);
                    await _gameService.OnAnswersUpdated(roomId, roundId, JsonSerializer.Deserialize<RoundAnswers>(message));
                });
            }
        });

        RedisChannel redisStateChan = new RedisChannel($"states:*", RedisChannel.PatternMode.Pattern);
        _ = _subscriber.SubscribeAsync(redisStateChan, async (channel, message) =>
        {
            Console.WriteLine($"OnSubscribe -- channel: {channel}, message: {message}");
            await _stateChan.Writer.WriteAsync((channel, message.ToString()));
        });
        RedisChannel redisPlayersChan = new RedisChannel($"players:*", RedisChannel.PatternMode.Pattern);
        _ = _subscriber.SubscribeAsync(redisPlayersChan, (channel, message) =>
        {
            Console.WriteLine($"OnSubscribe -- channel: {channel}, message: {message}");
            _ = _playersChan.Writer.WriteAsync((channel, message.ToString()));
        });
        RedisChannel redisAnswersChan = new RedisChannel($"answers:*", RedisChannel.PatternMode.Pattern);
        _ = _subscriber.SubscribeAsync(redisAnswersChan, (channel, message) =>
        {
            Console.WriteLine($"OnSubscribe -- channel: {channel}, message: {message}");
            _ = _answersChan.Writer.WriteAsync((channel, message.ToString()));
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken token) => Task.CompletedTask;

    // private async Task SaveString(string key, string? roomStateJson)
    // {
    //     Console.WriteLine("Save string " + roomStateJson);
    //     if (roomStateJson == null) return;
    //     await _db.StringSetAsync(key, roomStateJson);
    // }
}
