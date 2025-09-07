using Microsoft.AspNetCore.SignalR;
using TrueFalseBackend.Models;
using TrueFalseBackend.Infra.Redis;
using TrueFalseBackend.Services;

// this Hub is handling actual games of users its
// role is to create RoomStates and publish it on a
// Redis channel, after which it gets processed 
// further
public class MultiplayerHub : Hub
{
    private readonly IRoomSynchronizer _redisGame;
    private readonly GameService _gameService;
    private readonly IRedisLockerHelper _redisLocker;

    public MultiplayerHub(IRoomSynchronizer redisGame, IRedisLockerHelper redisLocker, GameService gameService)
    {
        _redisGame = redisGame;
        _redisLocker = redisLocker;
        _gameService = gameService;
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"User connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"User disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(string roomId)
    {
        // TODO: first there needs to be some checking if roomId is even registered
        Console.WriteLine($"Join Room: {roomId}");
        string resource = $"lock:players:{roomId}";
        try
        {
            await _redisLocker.ExecuteWithLock(resource, async () =>
            {
                PlayersInfo playersInfo = await _redisGame.GetPlayersInfo(roomId) ?? new();
                playersInfo.AddPlayer(Context.ConnectionId);
                await _redisGame.PublishPlayersInfo(roomId, playersInfo);
            });
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }
        catch (TimeoutException)
        {
            Console.WriteLine("JoinRoom timeout exception");
        }
    }

    public async Task GetState(string roomId)
    {
        RoomState? roomState = await _redisGame.GetRoomState(roomId);
        if (roomState == null) return;
        await Clients.Caller.SendAsync("state", roomState.ToJsonString());
    }

    public async Task SetName(string roomId, string name)
    {
        Console.WriteLine($"SetName [Room ID: {roomId}, name: {name}]");
        string resource = $"lock:players:{roomId}";
        try
        {
            await _redisLocker.ExecuteWithLock(resource, async () =>
            {
                PlayersInfo? playersInfo = await _redisGame.GetPlayersInfo(roomId);
                if (playersInfo == null) return;
                Player? p = playersInfo.GetPlayer(Context.ConnectionId);
                if (p == null) return;
                p.PlayerName = name;
                await _redisGame.PublishPlayersInfo(roomId, playersInfo);
            });
        }
        catch (TimeoutException)
        {
            Console.WriteLine("SetName locker timeout");
        }
    }

    // TODO: check if a player requesting the start of the game is a host
    public async Task StartGame(string roomId)
    {
        await _gameService.StartGame(roomId);
    }

    public async Task SendAnswer(string roomId, int round, string answer)
    {
        Console.WriteLine($"Send answer [RoomId: {roomId}, Round: {round}, Answer: {answer}]");
        string resource = $"lock:answers:{roomId}:{round}";
        try
        {
            await _redisLocker.ExecuteWithLock(resource, async () =>
            {
                RoundAnswers roundAnswers = await _redisGame.GetRoundAnswers(roomId, round) ?? new();
                roundAnswers.AddAnswer(Context.ConnectionId, answer);
                await _redisGame.PublishRoundAnswers(roomId, round, roundAnswers);
            });
        }
        catch (TimeoutException)
        {
            Console.WriteLine("SendAnswer locker timeout");
        }
    }
}
