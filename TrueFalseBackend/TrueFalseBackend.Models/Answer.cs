namespace TrueFalseBackend.Models;

public class Answer
{
    public required string PlayerConnectionId { get; set; }
    public bool PlayerAnswer { get; set; }
}

public class RoundAnswers : JsonStringer
{
    public Dictionary<string, bool> PlayersAnswers { get; set; } = [];

    public bool? GetAnswer(string playerId)
    {
        if (PlayersAnswers.ContainsKey(playerId)) return PlayersAnswers[playerId];
        return null;
    }

    public void AddAnswer(string playerId, string answer)
    {
        // ignore if player already answered
        if (PlayersAnswers.ContainsKey(playerId)) return;
        bool ans = answer == "True";
        PlayersAnswers[playerId] = ans;
    }

    public int Count()
    {
        return PlayersAnswers.Count;
    }
}
