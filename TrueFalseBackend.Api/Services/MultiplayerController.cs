using System.Text.Json;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

using TrueFalseBackend.Models;
using TrueFalseBackend.Services;

[ApiController]
[Route("/api/[controller]")]
public class RestController : ControllerBase
{
    private readonly IConnectionMultiplexer _conn;
    private readonly IDatabase _db;
    private readonly ISubscriber _subscriber;
    private readonly IQuestionProvider _questionProvider;

    public RestController(IQuestionProvider questionProvider, IConnectionMultiplexer conn)
    {
        _conn = conn;
        _db = _conn.GetDatabase();
        _subscriber = _conn.GetSubscriber();
        _questionProvider = questionProvider;
    }

    [HttpGet("randomQuestions/{size}")]
    public async Task<IActionResult> GetQuestions([FromRoute][Range(1, 10)] int size)
    {
        object result = new
        {
            status = "success",
            questions = await _questionProvider.GetNext(size)
        };
        return Ok(result);
    }

    [HttpGet("createRoom/{roundsNum}")]
    public async Task<IActionResult> CreateRoom([FromRoute] int roundsNum)
    {
        byte[] code = new byte[8];
        RandomNumberGenerator.Fill(code);
        string roomId = Convert.ToBase64String(code);
        roomId = roomId.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        RoomState initialRoomState = new() { RoundsNumber = roundsNum };
        await _db.StringSetAsync($"states:{roomId}", JsonSerializer.Serialize(initialRoomState));
        return Ok(new { status = "success", roomId });
    }
}
