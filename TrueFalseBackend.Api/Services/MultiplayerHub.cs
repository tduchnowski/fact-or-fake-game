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

    public MultiplayerHub(IRoomSynchronizer redisGame, GameService gameService)
    {
        _redisGame = redisGame;
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

    public async Task JoinRoom(string roomId)
    {
        // TODO: first there needs to be some checking if roomId is even registered
        Console.WriteLine($"Join Room: {roomId}");
        PlayersInfo? playersInfo = await _redisGame.GetPlayersInfo(roomId);
        if (playersInfo == null)
        {
            Console.WriteLine("PlayersInfo is null. Creating a new object");
            playersInfo = new();
        }
        playersInfo.AddPlayer(Context.ConnectionId);
        if (playersInfo.Players.Count == 1) await _gameService.CreateGame(roomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await _redisGame.PublishPlayersInfo(roomId, playersInfo);
    }

    public async Task GetState(string roomId)
    {
        RoomState? roomState = await _redisGame.GetRoomState(roomId);
        if (roomState == null) return;
        await Clients.Caller.SendAsync("state", roomState.ToJsonString());
    }

    public async Task SetName(string roomId, string name)
    {
        Console.WriteLine($"SetName [Room ID: {roomId}, name: {name}]");
        PlayersInfo? playersInfo = await _redisGame.GetPlayersInfo(roomId);
        if (playersInfo == null)
        {
            Console.WriteLine("PlayersInfo is null in SetName");
            return;
        }
        Player? p = playersInfo.GetPlayer(Context.ConnectionId);
        if (p != null)
        {
            p.PlayerName = name;
            await _redisGame.PublishPlayersInfo(roomId, playersInfo);
        }
    }

    public async Task StartGame(string roomId)
    {
        _gameService.StartGame(roomId);
    }

    public async Task SendAnswer(string roomId, int round, string answer)
    {
        Console.WriteLine($"Send answer [RoomId: {roomId}, Round: {round}, Answer: {answer}]");
        RoundAnswers? roundAnswers = await _redisGame.GetRoundAnswers(roomId, round);
        if (roundAnswers == null) roundAnswers = new();
        roundAnswers.AddAnswer(Context.ConnectionId, answer);
        await _redisGame.PublishRoundAnswers(roomId, round, roundAnswers);
    }
}
