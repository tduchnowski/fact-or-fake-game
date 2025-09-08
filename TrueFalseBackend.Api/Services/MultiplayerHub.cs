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

    public async Task<OperationResult> JoinRoom(string roomId)
    {
        // TODO: first there needs to be some checking if roomId is even registered
        Console.WriteLine($"Join Room: {roomId}");
        try
        {
            if (await _redisGame.GetRoomState(roomId) == null) return OperationResult.Fail("There's no room registered for this room id");
            bool ok = await _redisLocker.ExecuteWithLock($"lock:players:{roomId}", async () =>
            {
                PlayersInfo playersInfo = await _redisGame.GetPlayersInfo(roomId) ?? new();
                playersInfo.AddPlayer(Context.ConnectionId);
                await _redisGame.PublishPlayersInfo(roomId, playersInfo);
                return true;
            });
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            return OperationResult.Success();
        }
        catch (TimeoutException)
        {
            Console.WriteLine("JoinRoom timeout exception");
            return OperationResult.Fail("Internal server error");
        }
        catch (Exception e)
        {
            return OperationResult.Fail("Internal server error");
        }
    }

    public async Task<RoomState?> GetState(string roomId)
    {
        return await _redisGame.GetRoomState(roomId);
    }

    public async Task<OperationResult> SetName(string roomId, string name)
    {
        Console.WriteLine($"SetName [Room ID: {roomId}, name: {name}]");
        try
        {
            bool ok = await _redisLocker.ExecuteWithLock($"lock:players:{roomId}", async () =>
            {
                PlayersInfo? playersInfo = await _redisGame.GetPlayersInfo(roomId);
                if (playersInfo == null) return false;
                Player? p = playersInfo.GetPlayer(Context.ConnectionId);
                if (p == null) return false;
                p.PlayerName = name;
                await _redisGame.PublishPlayersInfo(roomId, playersInfo);
                return true;
            });
            if (!ok) return OperationResult.Fail("Internal server error");
            return OperationResult.Success();
        }
        catch (TimeoutException)
        {
            Console.WriteLine("SetName locker timeout");
            return OperationResult.Fail("Internal server error");
        }
    }

    // TODO: check if a player requesting the start of the game is a host
    public async Task<OperationResult> StartGame(string roomId)
    {
        if (await _gameService.StartGame(roomId)) return OperationResult.Success();
        return OperationResult.Fail("Couldn't start the game");
    }

    public async Task<OperationResult> SendAnswer(string roomId, int round, string answer)
    {
        Console.WriteLine($"Send answer [RoomId: {roomId}, Round: {round}, Answer: {answer}]");
        try
        {
            bool ok = await _redisLocker.ExecuteWithLock($"lock:answers:{roomId}:{round}", async () =>
            {
                RoomState? roomState = await _redisGame.GetRoomState(roomId);
                if (roomState == null || roomState.CurrentRound.Id != round) return false;
                RoundAnswers roundAnswers = await _redisGame.GetRoundAnswers(roomId, round) ?? new();
                roundAnswers.AddAnswer(Context.ConnectionId, answer);
                await _redisGame.PublishRoundAnswers(roomId, round, roundAnswers);
                return true;
            });
            return OperationResult.Success();
        }
        catch (TimeoutException)
        {
            Console.WriteLine("SendAnswer locker timeout");
            return OperationResult.Fail("Internal server error");
        }
        catch (Exception e)
        {
            return OperationResult.Fail("Internal server error");
        }
    }
}
