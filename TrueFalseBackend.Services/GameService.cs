using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TrueFalseBackend.Models;
using TrueFalseBackend.Infra.Redis;

namespace TrueFalseBackend.Services;

public class GameService
{
    private readonly ConcurrentDictionary<string, TrueFalseGame> _activeRooms = [];
    private readonly IQuestionProvider _questionProvider;
    private readonly IRoomSynchronizer _synchronizer;
    private readonly IRedisLockerHelper _redisLocker;
    private readonly ILogger<GameService> _logger;

    public GameService(IQuestionProvider questionProvider, IRoomSynchronizer stateSynchronizer, IRedisLockerHelper redisLocker, ILogger<GameService> logger)
    {
        _questionProvider = questionProvider;
        _synchronizer = stateSynchronizer;
        _redisLocker = redisLocker;
        _logger = logger;
    }

    public TrueFalseGame? GetActiveRoom(string roomId)
    {
        if (_activeRooms.TryGetValue(roomId, out var game) && game != null)
        {
            return game;
        }
        return null;
    }

    public async Task<bool> StartGame(string roomId)
    {
        // if there is already a game in _activeRooms then it means there is a game
        // already in progress, so don't do anything
        if (_activeRooms.TryGetValue(roomId, out var game) && game != null) return false;
        try
        {
            return await _redisLocker.ExecuteWithLock($"lock:states:{roomId}", async () =>
            {
                // get state for this room for information about rounds number and delays
                RoomState? roomState = await _synchronizer.GetRoomState(roomId);
                if (roomState == null || roomState.Stage != "notStarted") return false;
                game = new TrueFalseGame(roomId, _questionProvider, _synchronizer, _logger);
                _ = _activeRooms.GetOrAdd(roomId, game);
                roomState!.Stage = "waitingForStart";
                await game.StartGame();
                await _synchronizer.PublishRoomState(roomId, roomState);
                _logger.LogInformation("Starting a game for room {roomId} and RoomState = {roomState}", roomId, roomState.ToJsonString());
                return true;
            });
        }
        catch (TimeoutException)
        {
            _logger.LogError("Lock timeout while starting a game for room {roomId}", roomId);
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError("Exception: {e}", e);
            return false;
        }
    }

    public void CancelGame(string roomId)
    {
        if (_activeRooms.TryGetValue(roomId, out var game) && game != null)
        {
            _logger.LogInformation("Attempting to cancel the game for room {roomId}", roomId);
            game.CancelGame();
        }
    }

    public async Task RemoveRoom(string roomId)
    {
        CancelGame(roomId);
        if (_activeRooms.TryRemove(roomId, out var _))
        {
            _logger.LogInformation("Removed a game for room {roomId}", roomId);
        }
        await _synchronizer.RemoveSaved(roomId);
    }

    public async Task OnAnswersUpdated(string roomId, int roundId, RoundAnswers answers)
    {
        if (_activeRooms.TryGetValue(roomId, out var game) && game != null)
        {
            if (roundId != game.CurrentRound) return;
            PlayersInfo? currentPlayers = await _synchronizer.GetPlayersInfo(roomId);
            if (currentPlayers != null && answers.PlayersAnswers.Count == currentPlayers.Count())
            {
                _logger.LogDebug("Room ID: {roomId} -- number of answers is equal to the number of players. Finishing round {roundId}", roomId, roundId);
                game.FinishCurrentRound();
            }
        }
    }

    public async Task OnPlayersUpdated(string roomId, PlayersInfo playersInfo)
    {
        if (playersInfo.Count() == 0)
        {
            _logger.LogDebug("Room ID: {roomId} -- PlayersInfo count is 0, removing room", roomId);
            await RemoveRoom(roomId);
        }
    }

    public int CountRooms()
    {
        return _activeRooms.Count;
    }
}

public class TrueFalseGame
{
    public Task? GameTask { get; private set; }
    public int CurrentRound { get; private set; }
    private readonly IQuestionProvider _questionProvider;
    private readonly IRoomSynchronizer _synchronizer;
    private readonly ILogger<GameService> _logger;
    private readonly string _roomId;
    private RoundTimer? _timer;
    private readonly CancellationTokenSource _gameCancellationTokenSource = new();

    public TrueFalseGame(string roomId, IQuestionProvider questionProvider, IRoomSynchronizer stateSynchronizer, ILogger<GameService> logger)
    {
        _questionProvider = questionProvider;
        _synchronizer = stateSynchronizer;
        _roomId = roomId;
        _logger = logger;
    }

    // TODO: try catch in case of errors with the db or when the game can't continue
    public async Task StartGame()
    {
        RoomState? state = await _synchronizer.GetRoomState(_roomId);
        if (state == null) return;
        GameTask = Task.Run(async () =>
        {
            await StartGameLoop(state);
        }, _gameCancellationTokenSource.Token);
    }

    public async Task StartGameLoop(RoomState state)
    {
        _logger.LogDebug("StartGameLoop state = {state}", state.ToJsonString());
        for (int i = 1; i <= state.RoundsNumber; i++)
        {
            await StartNewRound(state);
        }
        state.Stage = "finished";
        await _synchronizer.PublishRoomState(_roomId, state);
    }

    public async Task StartNewRound(RoomState state)
    {
        _gameCancellationTokenSource.Token.ThrowIfCancellationRequested();
        _logger.LogDebug("StartNewRound state = {state}", state.ToJsonString());
        _timer = new RoundTimer(state.RoundTimeoutSeconds);
        if (state.CurrentRound == null) state.CurrentRound = new() { Id = 0, RoundQuestion = new() };
        List<Question> q = await _questionProvider.GetNext(1);
        if (q.Count == 0) throw new Exception("Couldn't fetch the questions");
        state.AdvanceToNextRound(q[0]);
        CurrentRound = state.CurrentRound.Id;
        await _synchronizer.PublishRoomState(_roomId, state);
        await _timer.Start();
        await UpdateScores(q[0], CurrentRound);
        await Task.Delay((int)(state.MidRoundDelay * 1000));
    }

    public async Task UpdateScores(Question q, int roundId)
    {
        PlayersInfo? playersInfo = await _synchronizer.GetPlayersInfo(_roomId);
        RoundAnswers? roundAnswers = await _synchronizer.GetRoundAnswers(_roomId, roundId);
        if (playersInfo == null || roundAnswers == null) return;
        foreach (var (playerId, answer) in roundAnswers.PlayersAnswers)
        {
            if (answer == q.Answer) playersInfo.Players[playerId].Score++;
        }
        await _synchronizer.PublishPlayersInfo(_roomId, playersInfo);
        _logger.LogDebug("Players after scores update: {players}", playersInfo.ToJsonString());
    }

    public void FinishCurrentRound()
    {
        _logger.LogDebug("Round cancelation requested");
        _timer?.CancelTimer();
    }

    public void CancelGame()
    {
        _logger.LogDebug("CancelGame requested");
        _gameCancellationTokenSource.Cancel();
    }
}

public class RoundTimer
{
    private readonly int _roundTimeout; // in seconds
    private CancellationTokenSource? _tokenSource;

    public RoundTimer(int roundTimeout) => _roundTimeout = roundTimeout;

    public async Task Start()
    {
        _tokenSource = new();
        try
        {
            await Task.Delay(_roundTimeout * 1000, _tokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            // ignore it, nothing bad happend
        }
    }

    public void CancelTimer()
    {
        _tokenSource?.Cancel();
    }
}
