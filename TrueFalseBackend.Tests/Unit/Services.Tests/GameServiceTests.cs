using TrueFalseBackend.Models;
using TrueFalseBackend.Infra.Redis;
using TrueFalseBackend.Services;

public class InMemoryRoomSync : IRoomSynchronizer
{
    private readonly Dictionary<string, RoomState> _roomStates = new();
    private readonly Dictionary<string, PlayersInfo> _playerInfos = new();
    private readonly Dictionary<string, Dictionary<int, RoundAnswers>> _roundAnswers = new();
    private readonly bool _throwsExceptions;

    public InMemoryRoomSync() { }

    public InMemoryRoomSync(bool throwsExceptions) => _throwsExceptions = throwsExceptions;

    public Task<RoomState?> GetRoomState(string roomId)
    {
        return Task.FromResult(_roomStates[roomId]);
    }

    public Task PublishRoomState(string roomId, RoomState roomState)
    {
        if (_throwsExceptions) throw new Exception("InMemoryroomSync Exception");
        _roomStates[roomId] = roomState;
        return Task.CompletedTask;
    }

    public Task<PlayersInfo?> GetPlayersInfo(string roomId)
    {
        if (_throwsExceptions) throw new Exception("InMemoryRoomSync Exception");
        if (!_playerInfos.TryGetValue(roomId, out var pi))
            pi = new();
        return Task.FromResult(pi);
    }

    public Task PublishPlayersInfo(string roomId, PlayersInfo playersInfo)
    {
        if (_throwsExceptions) throw new Exception("InMemoryRoomSync Exception");
        _playerInfos[roomId] = playersInfo;
        return Task.CompletedTask;
    }

    public Task<RoundAnswers?> GetRoundAnswers(string roomId, int roundId)
    {
        if (_throwsExceptions) throw new Exception("InMemoryRoomSync Exception");
        if (_roundAnswers.TryGetValue(roomId, out var ra))
        {
            if (ra.TryGetValue(roundId, out var answers))
                return Task.FromResult(answers);
            else
                return Task.FromResult(new RoundAnswers());
        }
        else
        {
            return Task.FromResult(new RoundAnswers());
        }
    }

    public Task PublishRoundAnswers(string roomId, int round, RoundAnswers roundAnswers)
    {
        if (_throwsExceptions) throw new Exception("InMemoryRoomSync Exception");
        _roundAnswers[roomId][round] = roundAnswers;
        return Task.CompletedTask;
    }
}

public class InMemoryQuestionProvider : IQuestionProvider
{
    private List<Question> _questions = [
        new() {Id = 1, Answer = true, Text = "Question 1"},
        new() {Id = 1, Answer = false, Text = "Question 2"},
        new() {Id = 1, Answer = true, Text = "Question 3"},
        new() {Id = 1, Answer = false, Text = "Question 4"},
        new() {Id = 1, Answer = true, Text = "Question 5"}
    ];

    public Task<List<Question>> GetNext(int size)
    {
        Console.WriteLine("Question Provider GetNext");
        size = Math.Min(size, _questions.Count);
        Random r = new();
        return Task.FromResult(_questions.OrderBy(q => r.Next(0, _questions.Count)).Take(size).ToList());
    }
}

public class GameServiceTests
{
    private GameService _gameService;
    private InMemoryRoomSync _roomSync;
    private InMemoryQuestionProvider _questionProvider;

    private readonly string _defaultRoomId = "123";
    private readonly RoomState _initialRoomState = new();

    public GameServiceTests()
    {
        _roomSync = new();
        _questionProvider = new();
        _gameService = new GameService(_questionProvider, _roomSync);
        _initialRoomState.RoundsNumber = 3;
        _initialRoomState.RoundTimeoutSeconds = 3;
        _initialRoomState.MidRoundDelay = 0;
    }

    [Fact]
    public async Task TestGameService_StartGame_GameStarted()
    {
        await _roomSync.PublishRoomState(_defaultRoomId, _initialRoomState);
        bool ok = await _gameService.StartGame(_defaultRoomId);
        Assert.True(ok);
        TrueFalseGame? game = _gameService.GetActiveRoom(_defaultRoomId);
        Assert.NotNull(game);
        bool status = game!.GameTask!.Status is TaskStatus.Running or TaskStatus.WaitingForActivation;
        Assert.True(status);
    }

    [Fact]
    public async Task TestGameService_StartGameTwiceSameRoom_SecondCallReturnsFalse()
    {
        await _roomSync.PublishRoomState(_defaultRoomId, _initialRoomState);
        bool ok = await _gameService.StartGame(_defaultRoomId);
        Assert.True(ok);
        ok = await _gameService.StartGame(_defaultRoomId);
        Assert.False(ok);
    }

    [Fact]
    public async Task TrueFalseService_CancelGame_GameTaskCancelled()
    {
        await _roomSync.PublishRoomState(_defaultRoomId, _initialRoomState);
        await _gameService.StartGame(_defaultRoomId);
        TrueFalseGame? game = _gameService.GetActiveRoom(_defaultRoomId);
        _gameService.CancelGame(_defaultRoomId);
        await Assert.ThrowsAsync<OperationCanceledException>(() => game!.GameTask!);
        Assert.True(game!.GameTask!.IsCanceled);
    }
}
