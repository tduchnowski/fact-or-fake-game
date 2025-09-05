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
        return Task.FromResult(_playerInfos[roomId]);
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
        return Task.FromResult(_roundAnswers[roomId][roundId]);
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
    private GameService _gameServiceWithExceptions;

    private readonly string _defaultRoomId = "123";
    private readonly int _defaultMaxRounds = 10;
    private readonly int _defaultRoundTimeout = 5;
    private readonly double _defaultMidRoundDelay = 1.5;

    public GameServiceTests()
    {
        InMemoryRoomSync ims = new();
        InMemoryQuestionProvider imqp = new();
        _gameService = new GameService(imqp, ims);
        InMemoryRoomSync imsWithExceptions = new InMemoryRoomSync(true);
        _gameServiceWithExceptions = new GameService(imqp, imsWithExceptions);
    }

    private Task<bool> CreateStandardGame()
    {
        return _gameService.CreateGame(_defaultRoomId, _defaultMaxRounds, _defaultRoundTimeout, _defaultMidRoundDelay);
    }

    [Fact]
    public async Task TestGameService_GameCreation_GameCreated()
    {
        bool ok = await CreateStandardGame();
        Assert.True(ok);
        Assert.NotNull(_gameService.GetActiveRoom(_defaultRoomId));
    }

    [Fact]
    public async Task TestGameService_CallCreateTwiceForOneRoom_GameNotOverwritten()
    {
        _ = await CreateStandardGame();
        TrueFalseGame? createdGameFirst = _gameService.GetActiveRoom(_defaultRoomId);
        _ = await CreateStandardGame();
        TrueFalseGame? createdGameSecond = _gameService.GetActiveRoom(_defaultRoomId);
        Assert.True(createdGameFirst == createdGameSecond);
    }

    [Fact]
    public async Task TestGameService_CreateWithExceptions_ReturnFalse()
    {
        Assert.False(await _gameServiceWithExceptions.CreateGame(_defaultRoomId, _defaultMaxRounds, _defaultRoundTimeout, _defaultMidRoundDelay));
    }

    [Fact]
    public async Task TestGameService_StartGame_GameStarted()
    {
        _ = await CreateStandardGame();
        bool ok = _gameService.StartGame(_defaultRoomId);
        Assert.True(ok);
        TrueFalseGame? game = _gameService.GetActiveRoom(_defaultRoomId);
        Assert.NotNull(game);
        bool status = game!.GameTask!.Status == TaskStatus.Running || game!.GameTask!.Status == TaskStatus.WaitingForActivation;
        Assert.True(status);
    }

    [Fact]
    public void TestGameService_StartGameNotExistentRoom_ReturnFalse()
    {
        string roomId = "123";
        bool ok = _gameService.StartGame(roomId);
        Assert.False(ok);
    }
}
