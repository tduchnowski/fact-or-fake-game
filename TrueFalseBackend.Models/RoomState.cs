using System.Text.Json;

namespace TrueFalseBackend.Models;

public class JsonStringer
{
    public virtual string ToJsonString()
    {
        return JsonSerializer.Serialize(this, GetType());
    }
}

public class RoomState : JsonStringer
{
    // description of a current stage of the game
    // "notStarted", "waitingForStart", "roundInProgress", "finished"
    public string Stage { get; set; } = "notStarted";
    public int RoundsNumber { get; set; } = 10;
    public int RoundTimeoutSeconds { get; set; } = 10;
    public double MidRoundDelay { get; set; } = 1.5;
    public Round CurrentRound { get; set; } = new() { Id = 0, RoundQuestion = new() };

    public void AdvanceToNextRound(Question q)
    {
        if (Stage == "finished") return;
        Stage = "roundInProgress";
        CurrentRound.Next(q);
    }

    public RoomState Clone()
    {
        return new RoomState()
        {
            Stage = this.Stage,
            RoundsNumber = this.RoundsNumber,
            RoundTimeoutSeconds = this.RoundTimeoutSeconds,
            MidRoundDelay = this.MidRoundDelay,
            CurrentRound = this.CurrentRound.Clone()
        };
    }
}

public class Round
{
    public required int Id { get; set; }
    public required Question RoundQuestion { get; set; }

    public void Next(Question q)
    {
        Id++;
        RoundQuestion = q;
    }

    public Round Clone()
    {
        return new Round()
        {
            Id = this.Id,
            RoundQuestion = this.RoundQuestion
        };
    }
}
