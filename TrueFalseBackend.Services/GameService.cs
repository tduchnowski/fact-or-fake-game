using System.Collections.Concurrent;
using TrueFalseBackend.Models;
using TrueFalseBackend.Infra.Redis;


namespace TrueFalseBackend.Services;

public class GameService
{
    private readonly ConcurrentDictionary<string, TrueFalseGame> _activeGames = [];
    private readonly IQuestionProvider _questionProvider;
    private readonly IRoomStateSynchronizer _stateSynchronizer;

    public GameService(IQuestionProvider questionProvider, IRoomStateSynchronizer stateSynchronizer)
    {
        _questionProvider = questionProvider;
        _stateSynchronizer = stateSynchronizer;
    }

    // TODO: return bool value indicating success or failure
    public async Task CreateGame(string roomId)
    {
        Console.WriteLine($"GameService: create game for room: {roomId}");
        RoomState? state = await _stateSynchronizer.GetRoomState(roomId);
        if (state == null) return;
        _activeGames.GetOrAdd(roomId, new TrueFalseGame(roomId, _questionProvider, _stateSynchronizer));
    }

    public void StartGame(string roomId)
    {
        Console.WriteLine($"GameService: start game for room: {roomId}");
        // try access the game from this object, if it can't find it that means
        // the game wasn't created on this server and the function should do nothing
        // in this case
        if (_activeGames.TryGetValue(roomId, out var game) && game != null)
        {
            _ = Task.Run(game.StartGame);
        }
    }

    public void CancelGame(string roomId)
    {

    }

    public void RemoveGame(string roomId)
    {

    }

    public void UpdateState(string roomId, RoomState roomState)
    {
        Console.WriteLine("Game service Update state");
        if (_activeGames.TryGetValue(roomId, out var game) && game != null)
        {
            if (roomState.CurrentRound?.Id != game.CurrentRound) return;
            if (roomState.CurrentRound?.PlayersAnswers.Count == roomState.Players.Count) game.FinishCurrentRound();
        }
    }
}

public class TrueFalseGame
{
    private readonly IQuestionProvider _questionProvider;
    private readonly IRoomStateSynchronizer _stateSynchronizer;
    private readonly string _roomId;
    public int CurrentRound { get; private set; }
    private int _maxRounds;
    private RoundTimer? _timer;

    public TrueFalseGame(string roomId, IQuestionProvider questionProvider, IRoomStateSynchronizer stateSynchronizer)
    {
        _questionProvider = questionProvider;
        _stateSynchronizer = stateSynchronizer;
        _roomId = roomId;
    }

    public async Task StartGame()
    {
        Console.WriteLine($"Starting a game for room: {_roomId}");
        RoomState? state = await _stateSynchronizer.GetRoomState(_roomId);
        if (state == null) return;
        _maxRounds = state.RoundsNumber;
        Console.WriteLine($"Start game round limit = {_maxRounds}, timeout = {state.RoundTimeoutSeconds}");
        Console.WriteLine($"Current round: {state.CurrentRound}");
        for (int i = 1; i <= _maxRounds; i++)
        {
            // TODO: should be throwing errors if something goes wrong with fetching
            // questions and states
            state = await _stateSynchronizer.GetRoomState(_roomId);
            if (state == null) break;
            if (state.CurrentRound == null) state.CurrentRound = new() { Id = i - 1, RoundQuestion = new(), PlayersAnswers = [] };
            Console.WriteLine($"state is {state?.ToJsonString()}");
            List<Question> q = await _questionProvider.GetNext(1);
            if (q.Count == 0) break;
            state!.AdvanceToNextRound(q[0]);
            CurrentRound = state.CurrentRound.Id;
            await _stateSynchronizer.PublishRoomState(_roomId, state);
            _timer = new RoundTimer(state.RoundTimeoutSeconds);
            await _timer.Start();
            state = await _stateSynchronizer.GetRoomState(_roomId);
            state!.ScoreRound();
            await _stateSynchronizer.PublishRoomState(_roomId, state);
            await Task.Delay(1500); // time after round to see the answer
        }
        RoomState? finishedState = await _stateSynchronizer.GetRoomState(_roomId);
        if (finishedState == null) return;
        finishedState.Stage = "finished";
        await _stateSynchronizer.PublishRoomState(_roomId, finishedState);
    }

    public void CancelGame() { }

    public void FinishCurrentRound()
    {
        Console.WriteLine("CancelCurrentTimer cancel");
        _timer?.CancelTimer();
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
