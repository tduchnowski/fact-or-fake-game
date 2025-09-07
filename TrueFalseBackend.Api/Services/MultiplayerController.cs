using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

using TrueFalseBackend.Models;
using TrueFalseBackend.Services;
using TrueFalseBackend.Infra.Redis;

[ApiController]
[Route("/api/[controller]")]
public class RestController : ControllerBase
{
    private readonly IRoomSynchronizer _roomSync;
    private readonly IQuestionProvider _questionProvider;
    private readonly ILogger<RestController> _logger;

    public RestController(IRoomSynchronizer roomSync, IQuestionProvider questionProvider, ILogger<RestController> logger)
    {
        _roomSync = roomSync;
        _questionProvider = questionProvider;
        _logger = logger;
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

    // TODO: add roundTimeout, midRoundDelay options
    [HttpGet("createRoom/{roundsNum}")]
    public async Task<IActionResult> CreateRoom([FromRoute] int roundsNum)
    {
        byte[] code = new byte[8];
        RandomNumberGenerator.Fill(code);
        string roomId = Convert.ToBase64String(code);
        roomId = roomId.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        RoomState initialRoomState = new() { RoundsNumber = roundsNum };
        await _roomSync.PublishRoomState(roomId, initialRoomState);
        return Ok(new { status = "success", roomId });
    }
}
