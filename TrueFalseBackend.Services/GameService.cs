using System.Collections.Concurrent;
using TrueFalseBackend.Models;
using TrueFalseBackend.Infra.Redis;


namespace TrueFalseBackend.Services;

public class GameService
{
    private readonly ConcurrentDictionary<string, TrueFalseGame> _activeRooms = [];
    private readonly IQuestionProvider _questionProvider;
    private readonly IRoomSynchronizer _synchronizer;

    public GameService(IQuestionProvider questionProvider, IRoomSynchronizer stateSynchronizer)
    {
        _questionProvider = questionProvider;
        _synchronizer = stateSynchronizer;
    }

    // TODO: return bool value indicating success or failure
    public async Task<bool> CreateGame(string roomId, int maxRounds, int roundTimeout, double midRoundDelay)
    {
        Console.WriteLine($"GameService: create game for room: {roomId}");
        _ = _activeRooms.GetOrAdd(roomId, new TrueFalseGame(roomId, maxRounds, roundTimeout, midRoundDelay, _questionProvider, _synchronizer));
        try
        {
            await _synchronizer.PublishRoomState(roomId, new RoomState());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
        return true;
    }

    public TrueFalseGame? GetActiveRoom(string roomId)
    {
        if (_activeRooms.TryGetValue(roomId, out var game) && game != null)
        {
            return game;
        }
        return null;
    }

    public bool StartGame(string roomId)
    {
        Console.WriteLine($"GameService: start game for room: {roomId}");
        // try to access the game from this object, if it can't find it that means
        // the game wasn't created on this server and the function should do nothing
        // in this case
        if (_activeRooms.TryGetValue(roomId, out var game) && game != null)
        {
            game.StartGame();
            return true;
        }
        return false;
    }

    public void CancelGame(string roomId)
    {
        if (_activeRooms.TryGetValue(roomId, out var game) && game != null)
        {
            game.CancelGame();
        }
    }

    public void RemoveRoom(string roomId)
    {

    }

    public async Task OnAnswersUpdated(string roomId, int roundId, RoundAnswers answers)
    {
        Console.WriteLine("Game service Update state");
        if (_activeRooms.TryGetValue(roomId, out var game) && game != null)
        {
            if (roundId != game.CurrentRound) return;
            PlayersInfo? currentPlayers = await _synchronizer.GetPlayersInfo(roomId);
            if (currentPlayers != null && answers.PlayersAnswers.Count == currentPlayers.Count())
            {
                game.FinishCurrentRound();
            }
        }
    }

    public void OnPlayerDisconnected(string roomId, PlayersInfo pl)
    {

    }
}

public class TrueFalseGame
{
    public Task? GameTask { get; private set; }
    public int CurrentRound { get; private set; }
    private readonly IQuestionProvider _questionProvider;
    private readonly IRoomSynchronizer _synchronizer;
    private readonly string _roomId;
    private readonly int _maxRounds;
    private readonly int _roundTimeout;
    private readonly double _midRoundDelay;
    private readonly RoundTimer _timer;
    private readonly CancellationTokenSource _gameCancellationTokenSource = new();

    public TrueFalseGame(string roomId, int maxRounds, int roundTimeout, double midRoundDelay, IQuestionProvider questionProvider, IRoomSynchronizer stateSynchronizer)
    {
        _questionProvider = questionProvider;
        _synchronizer = stateSynchronizer;
        _roomId = roomId;
        _maxRounds = maxRounds;
        _roundTimeout = roundTimeout;
        _midRoundDelay = midRoundDelay;
        _timer = new RoundTimer(roundTimeout);
    }

    public void StartGame()
    {
        GameTask = Task.Run(async () =>
        {
            Console.WriteLine($"Starting a game for room: {_roomId}");
            Console.WriteLine($"Start game round limit = {_maxRounds}, timeout = {_roundTimeout}, midRoundDelay = {_midRoundDelay}");
            RoomState state = new RoomState();
            for (int i = 1; i <= _maxRounds; i++)
            {
                if (state.CurrentRound == null) state.CurrentRound = new() { Id = i - 1, RoundQuestion = new() };
                Console.WriteLine($"state is {state.ToJsonString()}");
                List<Question> q = await _questionProvider.GetNext(1);
                if (q.Count == 0) break;
                state.AdvanceToNextRound(q[0]);
                CurrentRound = state.CurrentRound.Id;
                await _synchronizer.PublishRoomState(_roomId, state);
                await _timer.Start();
                await UpdateScores(q[0], CurrentRound);
                await Task.Delay((int)(_midRoundDelay * 1000));
            }
            state.Stage = "finished";
            await _synchronizer.PublishRoomState(_roomId, state);
        }, _gameCancellationTokenSource.Token);
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
    }

    public void FinishCurrentRound()
    {
        Console.WriteLine("CancelCurrentTimer cancel");
        _timer?.CancelTimer();
    }

    public void CancelGame()
    {
        Console.WriteLine("Cancelling game");
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
            Console.WriteLine("Cancelling timer");
        }
    }

    public void CancelTimer()
    {
        _tokenSource?.Cancel();
    }
}
