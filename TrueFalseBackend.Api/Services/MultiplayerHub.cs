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
    private readonly ILogger<MultiplayerHub> _logger;

    public MultiplayerHub(IRoomSynchronizer redisGame, IRedisLockerHelper redisLocker, GameService gameService, ILogger<MultiplayerHub> logger)
    {
        _redisGame = redisGame;
        _redisLocker = redisLocker;
        _gameService = gameService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("User connected: {connectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("User disconnected: {connectionId}", Context.ConnectionId);
        try
        {
            string roomId = await _redisGame.GetRoomForUser(Context.ConnectionId) ?? "";
            bool ok = await _redisLocker.ExecuteWithLock($"lock:players:{roomId}", async () =>
            {
                PlayersInfo? pi = await _redisGame.GetPlayersInfo(roomId);
                if (pi == null) return false;
                pi.RemovePlayer(Context.ConnectionId);
                await _redisGame.PublishPlayersInfo(roomId, pi);
                await _redisGame.RemoveConnectionToRoomMapping(Context.ConnectionId);
                return true;
            });
            if (!ok) _logger.LogWarning("Failed to delete a room {roomId}", roomId);
            else _logger.LogInformation("Successfully deleted room {roomId}", roomId);
        }
        catch (TimeoutException)
        {
            _logger.LogError("Lock timeout exception while deleting room");
        }
        catch (Exception e)
        {
            _logger.LogError("Error while deleting room. {e}", e);

        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<OperationResult> JoinRoom(string roomId)
    {
        // TODO: first there needs to be some checking if roomId is even registered
        try
        {
            if (await _redisGame.GetRoomState(roomId) == null)
            {
                _logger.LogWarning("JoinRoom roomId = {roomId}. state is null", roomId);
                return OperationResult.Fail("There's no room registered for this room id");
            }
            bool ok = await _redisLocker.ExecuteWithLock($"lock:players:{roomId}", async () =>
            {
                PlayersInfo playersInfo = await _redisGame.GetPlayersInfo(roomId) ?? new();
                playersInfo.AddPlayer(Context.ConnectionId);
                await _redisGame.PublishPlayersInfo(roomId, playersInfo);
                await _redisGame.AddConnectionToRoomMapping(Context.ConnectionId, roomId);
                return true;
            });
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            _logger.LogInformation("JoinRoom roomId = {roomId}. Added a player", roomId);
            return OperationResult.Success();
        }
        catch (TimeoutException)
        {
            _logger.LogError("JoinRoom roomId = {roomId}. Acquiring lock timed out", roomId);
            return OperationResult.Fail("Internal server error");
        }
        catch (Exception e)
        {
            _logger.LogError("JoinRoom roomId = {roomId}. Exception {e}", roomId, e);
            return OperationResult.Fail("Internal server error");
        }
    }

    public async Task<RoomState?> GetState(string roomId)
    {
        return await _redisGame.GetRoomState(roomId);
    }

    public async Task<OperationResult> SetName(string roomId, string name)
    {
        try
        {
            bool ok = await _redisLocker.ExecuteWithLock($"lock:players:{roomId}", async () =>
            {
                PlayersInfo? playersInfo = await _redisGame.GetPlayersInfo(roomId);
                if (playersInfo == null)
                {
                    _logger.LogWarning("SetName roomId = {roomId}, name = {name}. No PlayerInfo found", roomId, name);
                    return false;
                }
                Player? p = playersInfo.GetPlayer(Context.ConnectionId);
                if (p == null)
                {
                    _logger.LogWarning("SetName roomId = {roomId}, name = {name}. Couldn't find a player in PlayersInfo", roomId, name);
                    return false;
                }
                p.PlayerName = name;
                await _redisGame.PublishPlayersInfo(roomId, playersInfo);
                return true;
            });
            if (!ok) return OperationResult.Fail("Internal server error");
            _logger.LogInformation("SetName roomId = {roomId}, name = {name}. Changed the name.", roomId, name);
            return OperationResult.Success();
        }
        catch (TimeoutException)
        {
            _logger.LogError("SetName roomId = {roomId}, name = {name}. Acquiring lock timed out", roomId, name);
            return OperationResult.Fail("Internal server error");
        }
        catch (Exception e)
        {
            _logger.LogError("SetName roomId = {roomId}, name = {name}. Exception {e}", roomId, name, e);
            return OperationResult.Fail("Internal server error");
        }
    }

    // TODO: check if a player requesting the start of the game is a host
    public async Task<OperationResult> StartGame(string roomId)
    {
        if (await _gameService.StartGame(roomId))
        {
            _logger.LogInformation("StartGame roomId = {roomId}. Game started", roomId);
            return OperationResult.Success();
        }
        _logger.LogInformation("Couldn't start the game");
        return OperationResult.Fail("Couldn't start the game");
    }

    public async Task<OperationResult> SendAnswer(string roomId, int round, string answer)
    {
        try
        {
            bool ok = await _redisLocker.ExecuteWithLock($"lock:answers:{roomId}:{round}", async () =>
            {
                RoomState? roomState = await _redisGame.GetRoomState(roomId);
                if (roomState == null)
                {
                    _logger.LogWarning("SendAnswer roomId = {roomId}, round = {round}, answer = {answer}", roomId, round, answer);
                    return false;
                }
                if (roomState.CurrentRound.Id != round)
                {
                    _logger.LogWarning("SendAnswer roomId = {roomId}, round = {round}, answer = {answer}. Wrong round.", roomId, round, answer);
                    return false;
                }
                RoundAnswers roundAnswers = await _redisGame.GetRoundAnswers(roomId, round) ?? new();
                roundAnswers.AddAnswer(Context.ConnectionId, answer);
                await _redisGame.PublishRoundAnswers(roomId, round, roundAnswers);
                return true;
            });
            if (!ok) return OperationResult.Fail("Couldn't send the answer");
            _logger.LogInformation("SendAnswer roomId = {roomId}, round = {round}, answer = {answer}. Answer added.", roomId, round, answer);
            return OperationResult.Success();
        }
        catch (TimeoutException)
        {
            _logger.LogError("SendAnswer roomId = {roomId}, round = {round}, answer = {answer}. Acquiring lock timed out.", roomId, round, answer);
            return OperationResult.Fail("Internal server error");
        }
        catch (Exception e)
        {
            _logger.LogError("SendAnswer roomId = {roomId}, round = {round}, answer = {answer}. Exception {e}", roomId, round, answer, e);
            return OperationResult.Fail("Internal server error");
        }
    }
}
