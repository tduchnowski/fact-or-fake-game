using System.Text.Json;
using StackExchange.Redis;

using TrueFalseBackend.Models;
using TrueFalseBackend.Shared;

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

public interface IRoomStateSynchronizer
{
    Task PublishRoomState(string roomId, RoomState roomState);
    Task<RoomState?> GetRoomState(string roomId);
}

public class RedisGame : IRoomStateSynchronizer
{
    private readonly RedisDb _redisDb;

    public RedisGame(RedisDb redisDb) => _redisDb = redisDb;

    public async Task PublishRoomState(string roomId, RoomState roomState)
    {
        RedisChannel chan = new RedisChannel($"states:{roomId}", RedisChannel.PatternMode.Literal);
        await _redisDb.Subscriber.PublishAsync(chan, roomState.ToJsonString());
    }

    public async Task<RoomState?> GetRoomState(string roomId)
    {
        string? quizStateJson = await _redisDb.Db.StringGetAsync($"states:{roomId}");
        if (quizStateJson == null) return null;
        return JsonSerializer.Deserialize<RoomState>(quizStateJson);
    }
}

// public class RedisGameService
// {
//     private readonly RedisDb _redisDb;
//
//     public RedisGameService(RedisDb redisDb) => _redisDb = redisDb;
//
//     // TODO: refactor these two subscribing functions into on
//     // subscribe function with a handler as a parameter
//     public async Task SubscribeToRoomStates(string roomId)
//     {
//         Console.WriteLine("Subscribing to room " + roomId);
//         RedisChannel chan = new RedisChannel($"states:{roomId}", RedisChannel.PatternMode.Literal);
//         await _redisDb.Subscriber.SubscribeAsync(chan, async (channel, message) =>
//         {
//             Console.WriteLine($"Received a new state for room {roomId}: {message}");
//             // await _hubContext.Clients.Group(roomId).SendAsync("state", message);
//             await SaveString(roomId, message);
//         });
//     }
//
//     public async Task SubscribeToRoomRoundCancellations(string roomId)
//     {
//         Console.WriteLine("Subscribing to room cancellations " + roomId);
//         RedisChannel chan = new RedisChannel($"cancel:{roomId}:*", RedisChannel.PatternMode.Pattern);
//         await _redisDb.Subscriber.SubscribeAsync(chan, (channel, message) =>
//         {
//             try
//             {
//                 string[] channelParts = channel.ToString().Split(':');
//                 Console.WriteLine($"{channel}, {channelParts}");
//                 if (int.TryParse(channelParts[2], out int round))
//                 {
//                     Console.WriteLine($"Received a new cancellation request for {roomId}: {round}");
//                     RoomTokens.CancelRoomTokenSource(roomId, round);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine(e);
//             }
//         });
//     }
//
//     // TODO: these two also could be made into one
//     public async Task PublishState(string roomId, RoomState state)
//     {
//         RedisChannel chan = new RedisChannel($"states:{roomId}", RedisChannel.PatternMode.Literal);
//         await _redisDb.Subscriber.PublishAsync(chan, JsonSerializer.Serialize(state));
//     }
//
//     public async Task PublishCancel(string roomId, int round)
//     {
//         RedisChannel chan = new RedisChannel($"cancel:{roomId}:{round}", RedisChannel.PatternMode.Literal);
//         await _redisDb.Subscriber.PublishAsync(chan, "true");
//     }
//
//     public async Task SaveState(string roomId, RoomState state)
//     {
//         await SaveString(roomId, JsonSerializer.Serialize(state));
//     }
//
//     public async Task SaveString(string roomId, string? roomStateJson)
//     {
//         Console.WriteLine("Save string " + roomStateJson);
//         if (roomStateJson == null) return;
//         await _redisDb.Db.StringSetAsync($"states:{roomId}", roomStateJson);
//     }
//
//     public async Task<RoomState?> GetRoomState(string roomId)
//     {
//         Console.WriteLine("GetState Redis: room " + roomId);
//         string? quizStateJson = await _redisDb.Db.StringGetAsync($"states:{roomId}");
//         if (quizStateJson == null) return null;
//         return JsonSerializer.Deserialize<RoomState>(quizStateJson);
//     }
// }
