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
    Task<RoomState?> GetRoomState(string roomId);
    Task PublishRoomState(string roomId, RoomState roomState);
    Task<PlayersInfo?> GetPlayersInfo(string roomId);
    Task PublishPlayersInfo(string roomId, PlayersInfo playersInfo);
    Task<RoundAnswers?> GetRoundAnswers(string roomId, int roundId);
    Task PublishRoundAnswers(string roomId, int round, RoundAnswers roundAnswers);
    Task RemoveSaved(string roomId);
    Task<string?> GetRoomForUser(string connectionId);
    Task AddConnectionToRoomMapping(string connectionId, string roomId);
    Task RemoveConnectionToRoomMapping(string connectionId);
}

public class RedisGame : IRoomSynchronizer
{
    private readonly RedisDb _redisDb;

    public RedisGame(RedisDb redisDb) => _redisDb = redisDb;

    public async Task<RoomState?> GetRoomState(string roomId)
    {
        string? quizStateJson = await _redisDb.Db.StringGetAsync($"states:{roomId}");
        if (quizStateJson == null) return null;
        return JsonSerializer.Deserialize<RoomState>(quizStateJson);
    }

    public async Task PublishRoomState(string roomId, RoomState roomState)
    {
        RedisChannel chan = new RedisChannel($"states:{roomId}", RedisChannel.PatternMode.Literal);
        string roomStateJson = roomState.ToJsonString();
        await _redisDb.Db.StringSetAsync(chan.ToString(), roomStateJson);
        await _redisDb.Subscriber.PublishAsync(chan, roomStateJson);
    }

    public async Task<PlayersInfo?> GetPlayersInfo(string roomId)
    {
        string? playersInfoJson = await _redisDb.Db.StringGetAsync($"players:{roomId}");
        if (playersInfoJson == null) return null;
        return JsonSerializer.Deserialize<PlayersInfo>(playersInfoJson);
    }

    public async Task PublishPlayersInfo(string roomId, PlayersInfo playersInfo)
    {
        RedisChannel chan = new RedisChannel($"players:{roomId}", RedisChannel.PatternMode.Literal);
        string playersInfoJson = playersInfo.ToJsonString();
        await _redisDb.Db.StringSetAsync(chan.ToString(), playersInfoJson);
        await _redisDb.Subscriber.PublishAsync(chan, playersInfoJson);
    }

    public async Task<RoundAnswers?> GetRoundAnswers(string roomId, int roundId)
    {
        string? roundAnswers = await _redisDb.Db.StringGetAsync($"answers:{roomId}:{roundId}");
        if (roundAnswers == null) return null;
        return JsonSerializer.Deserialize<RoundAnswers>(roundAnswers);
    }

    public async Task PublishRoundAnswers(string roomId, int roundId, RoundAnswers answers)
    {
        RedisChannel chan = new RedisChannel($"answers:{roomId}:{roundId}", RedisChannel.PatternMode.Literal);
        string answersJson = answers.ToJsonString();
        await _redisDb.Db.StringSetAsync(chan.ToString(), answersJson);
        await _redisDb.Subscriber.PublishAsync(chan, answersJson);
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

    public async Task RemoveSaved(string roomId)
    {
        await _redisDb.Db.KeyDeleteAsync($"states:{roomId}");
        await _redisDb.Db.KeyDeleteAsync($"players:{roomId}");
        var endpoint = _redisDb.Connection.GetEndPoints().First();
        var server = _redisDb.Connection.GetServer(endpoint);
        foreach (var key in server.Keys(pattern: $"answers:{roomId}:*", pageSize: 100))
        {
            await _redisDb.Db.KeyDeleteAsync(key);
        }
    }
}
