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
    private readonly IRoomStateSynchronizer _redisGame;
    private readonly GameService _gameService;

    public MultiplayerHub(IRoomStateSynchronizer redisGame, GameService gameService)
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
        RoomState? roomState = await _redisGame.GetRoomState(roomId);
        if (roomState == null) return;
        roomState.AddPlayer(Context.ConnectionId);
        if (roomState.IsPlayerHost(Context.ConnectionId))
        {
            await _gameService.CreateGame(roomId);
        }
        await _redisGame.PublishRoomState(roomId, roomState);
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
        RoomState? roomState = await _redisGame.GetRoomState(roomId);
        if (roomState != null && roomState.Players.TryGetValue(Context.ConnectionId, out var p) && p != null)
        {
            p.PlayerName = name;
            await _redisGame.PublishRoomState(roomId, roomState);
        }
    }

    public async Task StartGame(string roomId)
    {
        _gameService.StartGame(roomId);
    }

    // TODO: lock the database channel. right now there might be some conflicts
    // when multiple people answer at roughly the same time. do some room lock or smth
    // like that
    public async Task SendAnswer(string roomId, int round, string answer)
    {
        Console.WriteLine($"Send answer [RoomId: {roomId}, Round: {round}, Answer: {answer}]");
        RoomState? roomState = await _redisGame.GetRoomState(roomId);
        if (roomState == null) return;
        roomState.AddAnswer(Context.ConnectionId, round, answer);
        await _redisGame.PublishRoomState(roomId, roomState);
    }
}
