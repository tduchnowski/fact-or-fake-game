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
        Console.WriteLine($"GameService: start game for room: {roomId}");
        // if there is already a game in _activeRooms then it means there is a game
        // already in progress, so don't do anything
        if (_activeRooms.TryGetValue(roomId, out var game) && game != null) return false;
        // get state for this room for information about rounds number and delays
        RoomState? roomState = await _synchronizer.GetRoomState(roomId);
        if (roomState == null) return false;
        game = new TrueFalseGame(roomId, roomState.RoundsNumber, roomState.RoundTimeoutSeconds, 1.5, _questionProvider, _synchronizer);
        _ = _activeRooms.GetOrAdd(roomId, game);
        game.StartGame();
        return true;
    }

    public void CancelGame(string roomId)
    {
        if (_activeRooms.TryGetValue(roomId, out var game) && game != null)
        {
            Console.WriteLine("Attempting to cancel game");
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

    public async Task OnPlayersUpdated(string roomId, PlayersInfo playersInfo)
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
                _gameCancellationTokenSource.Token.ThrowIfCancellationRequested();
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
