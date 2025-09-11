using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using TrueFalseBackend.Models;
using TrueFalseBackend.Infra.Redis;
using TrueFalseBackend.Services;

public class InMemoryRoomSync : IRoomSynchronizer
{
    private readonly ConcurrentDictionary<string, RoomState> _roomStates = new();
    private readonly ConcurrentDictionary<string, PlayersInfo> _playerInfos = new();
    private readonly ConcurrentDictionary<string, Dictionary<int, RoundAnswers>> _roundAnswers = new();
    private readonly bool _throwsExceptions;

    public InMemoryRoomSync() { }

    public InMemoryRoomSync(bool throwsExceptions) => _throwsExceptions = throwsExceptions;

    public Task<RoomState> GetRoomState(string roomId)
    {
        return Task.FromResult(_roomStates[roomId]);
    }

    public Task PublishRoomState(string roomId, RoomState roomState)
    {
        if (_throwsExceptions) throw new Exception("InMemoryroomSync Exception");
        _roomStates[roomId] = roomState;
        return Task.CompletedTask;
    }

    public Task<PlayersInfo> GetPlayersInfo(string roomId)
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

    public Task<RoundAnswers> GetRoundAnswers(string roomId, int roundId)
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
        if (!_roundAnswers.TryGetValue(roomId, out var _))
        {
            _roundAnswers[roomId] = [];
        }
        _roundAnswers[roomId][round] = roundAnswers;
        return Task.CompletedTask;
    }

    public Task RemoveSaved(string roomId)
    {
        return Task.CompletedTask;
    }

    public Task<string?> GetRoomForUser(string connectionId)
    {
        return Task.FromResult<string?>(null);
    }

    public Task AddConnectionToRoomMapping(string connectionId, string roomId)
    {
        return Task.CompletedTask;
    }

    public Task RemoveConnectionToRoomMapping(string connectionId)
    {
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
        size = Math.Min(size, _questions.Count);
        Random r = new();
        return Task.FromResult(_questions.OrderBy(q => r.Next(0, _questions.Count)).Take(size).ToList());
    }
}

public class FakeRedisLocker : IRedisLockerHelper
{
    public async Task<bool> ExecuteWithLock(string resource, Func<Task<bool>> operation)
    {
        return await operation();
    }
}

public class GameServiceTests
{
    private GameService _gameService;
    private InMemoryRoomSync _roomSync;
    private InMemoryQuestionProvider _questionProvider;
    private readonly IRedisLockerHelper _fakeLocker = new FakeRedisLocker();
    private readonly ILogger<GameService> _nullLogger = NullLogger<GameService>.Instance;

    private readonly string _defaultRoomId = "123";
    private readonly RoomState _initialRoomState = new();

    public GameServiceTests()
    {
        _roomSync = new();
        _questionProvider = new();
        _gameService = new GameService(_questionProvider, _roomSync, _fakeLocker, _nullLogger);
        _initialRoomState.RoundsNumber = 3;
        _initialRoomState.RoundTimeoutSeconds = 3;
        _initialRoomState.MidRoundDelay = 0;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(476)]
    [InlineData(1234)]
    public async Task TestGameService_StartGames_CorrectCount(int numberOfGames)
    {
        int activeGamesCount = numberOfGames;
        while (activeGamesCount-- > 0)
        {
            string roomId = $"room{numberOfGames - activeGamesCount}";
            await _roomSync.PublishRoomState(roomId, _initialRoomState.Clone());
            bool ok = await _gameService.StartGame(roomId);
            Assert.True(ok);
            TrueFalseGame? game = _gameService.GetActiveRoom(roomId);
            Assert.NotNull(game);
            bool status = game!.GameTask!.Status is TaskStatus.Running or TaskStatus.WaitingForActivation;
            Assert.True(status);
        }
        Assert.Equal(numberOfGames, _gameService.CountRooms());
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

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(1000)]
    public void TestGameService_StartGamesInParallel_CorrectCount(int numberOfGames)
    {
        IEnumerable<string> roomIds = Enumerable.Range(1, numberOfGames).Select(i => $"room{i}");
        Parallel.ForEach(roomIds, async id =>
        {
            await _roomSync.PublishRoomState(id, _initialRoomState.Clone());
            bool ok = await _gameService.StartGame(id);
        });
        Assert.Equal(numberOfGames, _gameService.CountRooms());
    }

    [Theory]
    [InlineData("waitingForStart")]
    [InlineData("roundInProgress")]
    [InlineData("finished")]
    public async Task TestGameService_StartGameWhenStateIsInProgress_DoNothing(string stage)
    {
        RoomState waitingRoomState = _initialRoomState.Clone();
        waitingRoomState.Stage = stage;
        await _roomSync.PublishRoomState(_defaultRoomId, waitingRoomState);
        bool ok = await _gameService.StartGame(_defaultRoomId);
        Assert.False(ok);
    }

    [Fact]
    public async Task TestGameService_CancelGame_GameTaskCancelled()
    {
        await _roomSync.PublishRoomState(_defaultRoomId, _initialRoomState.Clone());
        await _gameService.StartGame(_defaultRoomId);
        TrueFalseGame? game = _gameService.GetActiveRoom(_defaultRoomId);
        _gameService.CancelGame(_defaultRoomId);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => game!.GameTask!);
        Assert.True(game!.GameTask!.IsCanceled);
    }

    [Fact]
    public async Task TestGameService_RemoveExistingGame_GameRemovedNoTasksInTheBackground()
    {
        await _roomSync.PublishRoomState(_defaultRoomId, _initialRoomState.Clone());
        await _gameService.StartGame(_defaultRoomId);
        TrueFalseGame? game = _gameService.GetActiveRoom(_defaultRoomId);
        await _gameService.RemoveRoom(_defaultRoomId);
        Assert.Equal(0, _gameService.CountRooms());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await game!.GameTask!);
        Assert.True(game!.GameTask!.IsCanceled);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 3)]
    [InlineData(50, 12)]
    [InlineData(100, 67)]
    [InlineData(476, 400)]
    [InlineData(1234, 1200)]
    public async Task TestGameService_RemoveExistingGames_CorrectCount(int numberOfGames, int gamesToRemove)
    {
        int activeGamesCount = numberOfGames;
        while (activeGamesCount-- > 0)
        {
            string roomId = $"room{numberOfGames - activeGamesCount}";
            await _roomSync.PublishRoomState(roomId, _initialRoomState.Clone());
            await _gameService.StartGame(roomId);
        }
        Random r = new();
        List<string> idsToRemove = Enumerable.Range(1, numberOfGames).OrderBy(_ => r.Next()).Take(gamesToRemove).Select(i => $"room{i}").ToList();
        foreach (string playerId in idsToRemove) await _gameService.RemoveRoom(playerId);
        Assert.Equal(numberOfGames - gamesToRemove, _gameService.CountRooms());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 3)]
    [InlineData(50, 12)]
    [InlineData(100, 67)]
    [InlineData(476, 400)]
    [InlineData(1234, 1200)]
    public async Task TestGameService_RemoveExistingGamesInParralel_CorrectCount(int numberOfGames, int gamesToRemove)
    {
        int activeGamesCount = numberOfGames;
        while (activeGamesCount-- > 0)
        {
            string roomId = $"room{numberOfGames - activeGamesCount}";
            await _roomSync.PublishRoomState(roomId, _initialRoomState.Clone());
            await _gameService.StartGame(roomId);
        }
        Random r = new();
        List<string> idsToRemove = Enumerable.Range(1, numberOfGames).OrderBy(_ => r.Next()).Take(gamesToRemove).Select(i => $"room{i}").ToList();
        Parallel.ForEach(idsToRemove, async id => await _gameService.RemoveRoom(id));
        Assert.Equal(numberOfGames - gamesToRemove, _gameService.CountRooms());
    }

    [Fact]
    public async Task TestTrueFalseGame_StartGame_TaskRunning()
    {
        string roomId = "roomId";
        await _roomSync.PublishRoomState(roomId, _initialRoomState.Clone());

        TrueFalseGame tfg = new TrueFalseGame(roomId, _questionProvider, _roomSync, _nullLogger);
        await tfg.StartGame();
        bool status = tfg.GameTask!.Status is TaskStatus.Running or TaskStatus.WaitingForActivation;
        Assert.True(status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(34)]
    [InlineData(59)]
    [InlineData(198)]
    [InlineData(1047)]
    public async Task TestTrueFalseGame_StartNewRound_StateUpdated(int initialRoundId)
    {
        string roomId = "roomId";
        RoomState rs = new();
        rs.RoundTimeoutSeconds = 2;
        rs.MidRoundDelay = 0;
        rs.RoundTimeoutSeconds = 0;
        rs.CurrentRound = new() { Id = initialRoundId, RoundQuestion = new() };
        await _roomSync.PublishRoomState(roomId, rs);
        TrueFalseGame tfg = new TrueFalseGame(roomId, _questionProvider, _roomSync, _nullLogger);
        await tfg.StartNewRound(rs);
        Assert.Equal(initialRoundId + 1, tfg.CurrentRound);
        RoomState? rsAfter = await _roomSync.GetRoomState(roomId);
        Assert.Equal(initialRoundId + 1, rsAfter!.CurrentRound.Id);
    }

    [Fact]
    public async Task TestTrueFalseGame_TimerCancel_StateUpdated()
    {
        string roomId = "roomId";
        int roundId = 1;
        RoomState rs = new();
        rs.RoundTimeoutSeconds = 5;
        rs.MidRoundDelay = 0;
        rs.CurrentRound = new() { Id = roundId, RoundQuestion = new() };
        TrueFalseGame tfg = new TrueFalseGame(roomId, _questionProvider, _roomSync, _nullLogger);
        TaskCompletionSource tcs = new TaskCompletionSource();
        Task t = Task.Run(async () =>
        {
            tcs.SetResult();
            await tfg.StartNewRound(rs);
        });
        await tcs.Task;
        tfg.FinishCurrentRound();
        await t;
        rs = await _roomSync.GetRoomState(roomId);
        Assert.Equal(roundId + 1, rs!.CurrentRound.Id);
        Assert.Equal(roundId + 1, tfg.CurrentRound);
    }

    [Fact]
    public async Task TestTrueFalseGame_UpdateScoresOnePlayer_ScoresUpdated()
    {
        string roomId = "roomId";
        Question q = new() { Id = 1, Text = "text", Answer = true };
        int roundId = 3;
        PlayersInfo pi = new();
        RoundAnswers ra = new();
        string playerId = "playerId";
        pi.AddPlayer(playerId);
        ra.AddAnswer(playerId, "True");
        await _roomSync.PublishPlayersInfo(roomId, pi);
        await _roomSync.PublishRoundAnswers(roomId, roundId, ra);
        TrueFalseGame tfg = new TrueFalseGame(roomId, _questionProvider, _roomSync, _nullLogger);
        await tfg.UpdateScores(q, roundId);
        pi = await _roomSync.GetPlayersInfo(roomId);
        Player? p = pi!.GetPlayer(playerId);
        Assert.Equal(1, p!.Score);

        ra = new();
        ra.AddAnswer(playerId, "False");
        await _roomSync.PublishRoundAnswers(roomId, roundId, ra);
        await tfg.UpdateScores(q, roundId);
        p = pi!.GetPlayer(playerId);
        Assert.Equal(1, p!.Score); // score stays the same
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(100)]
    public async Task TestTrueFalseGame_StartGameLoop_CorrectRoundAndStageInTheEnd(int numberOfRounds)
    {
        string roomId = "roomId";
        RoomState rs = new() { RoundsNumber = numberOfRounds, RoundTimeoutSeconds = 0, MidRoundDelay = 0 };
        await _roomSync.PublishRoomState(roomId, rs);
        TrueFalseGame tfg = new TrueFalseGame(roomId, _questionProvider, _roomSync, _nullLogger);
        await tfg.StartGameLoop(rs);
        rs = await _roomSync.GetRoomState(roomId);
        Assert.Equal("finished", rs!.Stage);
        Assert.Equal(numberOfRounds, rs!.CurrentRound.Id);
    }
}
