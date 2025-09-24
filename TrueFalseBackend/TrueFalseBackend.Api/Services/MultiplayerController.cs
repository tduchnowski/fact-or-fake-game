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
            Ok = true,
            Content = await _questionProvider.GetNext(size)
        };
        return Ok(result);
    }

    [HttpGet("createRoom")]
    public async Task<IActionResult> CreateRoom([FromQuery][Range(1, 50)] int roundsNum, [FromQuery][Range(5, 20)] int roundTimeout, [FromQuery][Range(1, 5)] double midRoundDelay = 1.5)
    {
        byte[] code = new byte[8];
        RandomNumberGenerator.Fill(code);
        string roomId = Convert.ToBase64String(code);
        roomId = roomId.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        RoomState initialRoomState = new() { RoundsNumber = roundsNum, RoundTimeoutSeconds = roundTimeout, MidRoundDelay = midRoundDelay };
        try
        {
            await _roomSync.PublishRoomState(roomId, initialRoomState);
            return Ok(new { Ok = true, Content = roomId });
        }
        catch (RedisDbException ex)
        {
            _logger.LogError("CreateRoom({roundsNum}, {roundTimeout}, {minRoundDelay}) {ex}", roundsNum, roundTimeout, midRoundDelay, ex);
            return StatusCode(500, "Server error. Try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateRoom({roundsNum}, {roundTimeout}, {minRoundDelay}) {ex}", roundsNum, roundTimeout, midRoundDelay, ex);
            return StatusCode(500, "Unexpected server error. Try again later.");
        }
    }
}
