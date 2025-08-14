namespace TrueFalseBackend.Models;

public class Player
{
    public required string PlayerName { get; set; }
    public int Score { get; set; }
    public bool IsHost { get; set; }
    public bool IsActive { get; set; } = true;
}
