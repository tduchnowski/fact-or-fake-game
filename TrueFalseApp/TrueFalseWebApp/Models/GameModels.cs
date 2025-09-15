namespace TrueFalseWebApp.Models;

public record Question(
    int Id,
    string Text,
    bool Answer)
{
    public static Question Empty => new(0, string.Empty, false);
}


public record RoomState(
    string Stage,
    int RoundsNumber,
    int RoundTimeoutSeconds,
    double MidRoundDelay,
    Round CurrentRound)
{
    public static RoomState Empty => new(string.Empty, 0, 0, 0, Round.Empty);
}


public record Round(
    int Id,
    Question RoundQuestion
)
{
    public static Round Empty => new(0, Question.Empty);
}

public record Player(
    string PlayerName,
    int Score,
    bool IsHost,
    bool IsActive
);

public record PlayersInfo(
    Dictionary<string, Player> Players
)
{
    public static PlayersInfo Empty => new(new Dictionary<string, Player>());
}