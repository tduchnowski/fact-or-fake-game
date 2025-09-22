using Microsoft.Extensions.Logging;
using System.Text.Json;
using StackExchange.Redis;
using TrueFalseBackend.Models;

namespace TrueFalseBackend.Infra.Redis;

public class RedisDb
{
    public IConnectionMultiplexer Connection { get; set; }
    public IDatabase Db { get; set; }
    public ISubscriber Subscriber { get; set; }

    public RedisDb(IConnectionMultiplexer conn)
    {
        Connection = conn;
        Db = Connection.GetDatabase();
        Subscriber = Connection.GetSubscriber();
    }
}

public interface IRoomSynchronizer
{
    Task<RoomState> GetRoomState(string roomId);
    Task PublishRoomState(string roomId, RoomState roomState);
    Task<PlayersInfo> GetPlayersInfo(string roomId);
    Task PublishPlayersInfo(string roomId, PlayersInfo playersInfo);
    Task<RoundAnswers> GetRoundAnswers(string roomId, int roundId);
    Task PublishRoundAnswers(string roomId, int round, RoundAnswers roundAnswers);
    Task RemoveSaved(string roomId);
    Task<string?> GetRoomForUser(string connectionId);
    Task AddConnectionToRoomMapping(string connectionId, string roomId);
    Task RemoveConnectionToRoomMapping(string connectionId);
}

public class RedisDbException : Exception
{
    public RedisDbException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class RedisKeyMissing : Exception
{
    public RedisKeyMissing(string message) : base(message)
    {
    }
}

public class NullResultError : Exception
{
    public NullResultError(string message) : base(message)
    {
    }
}

public class RedisGame : IRoomSynchronizer
{
    private readonly RedisDb _redisDb;
    private readonly ILogger<RedisGame> _logger;

    public RedisGame(RedisDb redisDb, ILogger<RedisGame> logger)
    {
        _redisDb = redisDb;
        _logger = logger;
    }

    public async Task<RoomState> GetRoomState(string roomId)
    {
        return await GetObject<RoomState>($"states:{roomId}");
    }

    public async Task PublishRoomState(string roomId, RoomState roomState)
    {
        await PublishObject($"states:{roomId}", roomState, TimeSpan.FromMinutes(5));
    }

    public async Task<PlayersInfo> GetPlayersInfo(string roomId)
    {
        return await GetObject<PlayersInfo>($"players:{roomId}");
    }

    public async Task PublishPlayersInfo(string roomId, PlayersInfo playersInfo)
    {
        await PublishObject($"players:{roomId}", playersInfo, TimeSpan.FromHours(1));
    }

    public async Task<RoundAnswers> GetRoundAnswers(string roomId, int roundId)
    {
        return await GetObject<RoundAnswers>($"answers:{roomId}:{roundId}");
    }

    public async Task PublishRoundAnswers(string roomId, int roundId, RoundAnswers answers)
    {
        await PublishObject($"answers:{roomId}:{roundId}", answers, TimeSpan.FromMinutes(5));
    }

    public async Task<string?> GetRoomForUser(string connectionId)
    {
        return await _redisDb.Db.StringGetAsync($"users:{connectionId}");
    }

    public async Task AddConnectionToRoomMapping(string connectionId, string roomId)
    {
        await _redisDb.Db.StringSetAsync($"users:{connectionId}", roomId);
    }

    public async Task RemoveConnectionToRoomMapping(string connectionId)
    {
        await _redisDb.Db.KeyDeleteAsync($"users:{connectionId}");
    }

    private async Task<T> GetObject<T>(string key)
    {
        try
        {
            string? quizStateJson = await _redisDb.Db.StringGetAsync(key);
            if (quizStateJson == null) throw new RedisKeyMissing($"key '{key}' not present");
            T obj = JsonSerializer.Deserialize<T>(quizStateJson) ?? throw new NullResultError($"serialization resulted in a null");
            return obj;
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or JsonException or NullResultError)
        {
            _logger.LogError(ex, "failed to get an object for key {key}", key);
            throw new RedisDbException($"GetObject({key}) failed", ex);
        }
    }

    private async Task PublishObject(string channel, JsonStringer obj, TimeSpan expiration)
    {
        try
        {
            RedisChannel chan = new RedisChannel(channel, RedisChannel.PatternMode.Literal);
            string roomStateJson = obj.ToJsonString();
            await _redisDb.Db.StringSetAsync(chan.ToString(), roomStateJson, expiration);
            await _redisDb.Subscriber.PublishAsync(chan, roomStateJson);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or RedisServerException)
        {
            _logger.LogError(ex, "failed to publish object in a channel {channel}", channel);
            throw new RedisDbException($"PublishObject({channel})", ex);
        }
    }

    public async Task RemoveSaved(string roomId)
    {
        try
        {
            await _redisDb.Db.KeyDeleteAsync($"states:{roomId}");
            await _redisDb.Db.KeyDeleteAsync($"players:{roomId}");
            var endpoint = _redisDb.Connection.GetEndPoints().First();
            var server = _redisDb.Connection.GetServer(endpoint);
            await foreach (var key in server.KeysAsync(database: _redisDb.Db.Database, pattern: $"answers:{roomId}:*", pageSize: 100))
            {
                await _redisDb.Db.KeyDeleteAsync(key);
            }
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or RedisServerException)
        {
            _logger.LogError(ex, "failed to remove saved data for room {roomId}", roomId);
            throw new RedisDbException($"RemoveSaved({roomId})", ex);
        }
    }
}
