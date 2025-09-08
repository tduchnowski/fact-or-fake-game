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
    private readonly ILogger<RedisStateUpdater> _logger;
    private readonly IConnectionMultiplexer _conn;
    private readonly ISubscriber _subscriber;
    private readonly Channel<(string, string)> _stateChan;
    private readonly Channel<(string, string)> _playersChan;
    private readonly Channel<(string, string)> _answersChan;

    public RedisStateUpdater(IHubContext<MultiplayerHub> hubContext, IConnectionMultiplexer connection, GameService gameService, ILogger<RedisStateUpdater> logger)
    {
        _hubContext = hubContext;
        _conn = connection;
        _subscriber = _conn.GetSubscriber();
        _gameService = gameService;
        _logger = logger;
        _stateChan = Channel.CreateUnbounded<(string, string)>();
        _playersChan = Channel.CreateUnbounded<(string, string)>();
        _answersChan = Channel.CreateUnbounded<(string, string)>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RedisStateUpdater starting...");
        _ = Task.Run(async () =>
        {
            await foreach (var (channel, message) in _stateChan.Reader.ReadAllAsync(cancellationToken))
            {
                _ = Task.Run(async () =>
                {
                    string[] channelParts = channel.ToString().Split(':');
                    string roomId = channelParts[1];
                    await _hubContext.Clients.Group(roomId).SendAsync("state", message);
                    _logger.LogDebug("Broadcasted RoomState: {message}", message);
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
                    await _hubContext.Clients.Group(roomId).SendAsync("players", message);
                    _logger.LogDebug("Broadcasted PlayersInfo: {message}", message);
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
                    await _gameService.OnAnswersUpdated(roomId, roundId, JsonSerializer.Deserialize<RoundAnswers>(message));
                    _logger.LogDebug("Updated answers: {message}", message);
                });
            }
        });

        RedisChannel redisStateChan = new RedisChannel($"states:*", RedisChannel.PatternMode.Pattern);
        _ = _subscriber.SubscribeAsync(redisStateChan, async (channel, message) =>
        {
            await _stateChan.Writer.WriteAsync((channel, message.ToString()));
            _logger.LogDebug("RoomState was published: {message}", message);
        });
        RedisChannel redisPlayersChan = new RedisChannel($"players:*", RedisChannel.PatternMode.Pattern);
        _ = _subscriber.SubscribeAsync(redisPlayersChan, (channel, message) =>
        {
            _ = _playersChan.Writer.WriteAsync((channel, message.ToString()));
            _logger.LogDebug("PlayersInfo was published: {message}", message);
        });
        RedisChannel redisAnswersChan = new RedisChannel($"answers:*", RedisChannel.PatternMode.Pattern);
        _ = _subscriber.SubscribeAsync(redisAnswersChan, (channel, message) =>
        {
            _ = _answersChan.Writer.WriteAsync((channel, message.ToString()));
            _logger.LogDebug("Answers were published: {message}", message);
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken token) => Task.CompletedTask;
}
