using System.Collections.Concurrent;
using TrueFalseBackend.Models;
using TrueFalseBackend.Infra.Redis;


namespace TrueFalseBackend.Services;

public class GameService
{
    private readonly ConcurrentDictionary<string, TrueFalseGame> _activeGames = [];
    private readonly IQuestionProvider _questionProvider;
    private readonly IRoomSynchronizer _synchronizer;

    public GameService(IQuestionProvider questionProvider, IRoomSynchronizer stateSynchronizer)
    {
        _questionProvider = questionProvider;
        _synchronizer = stateSynchronizer;
    }

    // TODO: return bool value indicating success or failure
    public async Task CreateGame(string roomId)
    {
        Console.WriteLine($"GameService: create game for room: {roomId}");
        // RoomState? state = await _synchronizer.GetRoomState(roomId);
        // if (state == null) return;
        _activeGames.GetOrAdd(roomId, new TrueFalseGame(roomId, _questionProvider, _synchronizer));
        RoomState roomState = new();
        await _synchronizer.PublishRoomState(roomId, roomState);
    }

    public void StartGame(string roomId)
    {
        Console.WriteLine($"GameService: start game for room: {roomId}");
        // try to access the game from this object, if it can't find it that means
        // the game wasn't created on this server and the function should do nothing
        // in this case
        if (_activeGames.TryGetValue(roomId, out var game) && game != null)
        {
            _ = Task.Run(game.StartGame);
        }
        else
        {
        }
    }

    public void CancelGame(string roomId)
    {

    }

    public void RemoveGame(string roomId)
    {

    }

    public async Task OnAnswersUpdated(string roomId, int roundId, RoundAnswers answers)
    {
        Console.WriteLine("Game service Update state");
        if (_activeGames.TryGetValue(roomId, out var game) && game != null)
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
    private readonly IQuestionProvider _questionProvider;
    private readonly IRoomSynchronizer _synchronizer;
    private readonly string _roomId;
    public int CurrentRound { get; private set; }
    private int _maxRounds;
    private RoundTimer? _timer;

    public TrueFalseGame(string roomId, IQuestionProvider questionProvider, IRoomSynchronizer stateSynchronizer)
    {
        _questionProvider = questionProvider;
        _synchronizer = stateSynchronizer;
        _roomId = roomId;
    }

    public async Task StartGame()
    {
        Console.WriteLine($"Starting a game for room: {_roomId}");
        RoomState? state = await _synchronizer.GetRoomState(_roomId);
        if (state == null) return;
        _maxRounds = state.RoundsNumber;
        int timeout = state.RoundTimeoutSeconds;
        _timer = new RoundTimer(timeout);
        Console.WriteLine($"Start game round limit = {_maxRounds}, timeout = {timeout}");
        Console.WriteLine($"Current round: {state.CurrentRound}");
        for (int i = 1; i <= _maxRounds; i++)
        {
            // TODO: should be throwing errors if something goes wrong with fetching
            // questions and states
            state = await _synchronizer.GetRoomState(_roomId);
            if (state == null) break;
            if (state.CurrentRound == null) state.CurrentRound = new() { Id = i - 1, RoundQuestion = new() };
            Console.WriteLine($"state is {state?.ToJsonString()}");
            List<Question> q = await _questionProvider.GetNext(1);
            if (q.Count == 0) break;
            state!.AdvanceToNextRound(q[0]);
            CurrentRound = state.CurrentRound.Id;
            await _synchronizer.PublishRoomState(_roomId, state);
            await _timer.Start();
            await UpdateScores(q[0], state.CurrentRound.Id);
            await Task.Delay(1500); // time after round to see the answer
        }
        RoomState? finishedState = await _synchronizer.GetRoomState(_roomId);
        if (finishedState == null) return;
        finishedState.Stage = "finished";
        await _synchronizer.PublishRoomState(_roomId, finishedState);
    }

    private async Task UpdateScores(Question q, int roundId)
    {
        PlayersInfo? playersInfo = await _synchronizer.GetPlayersInfo(_roomId);
        RoundAnswers? roundAnswers = await _synchronizer.GetRoundAnswers(_roomId, roundId);
        foreach (var (playerId, answer) in roundAnswers.PlayersAnswers)
        {
            if (answer == q.Answer) playersInfo.Players[playerId].Score++;
        }
        await _synchronizer.PublishPlayersInfo(_roomId, playersInfo);
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
