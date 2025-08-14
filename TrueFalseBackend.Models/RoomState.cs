using System.Collections.Concurrent;
using System.Text.Json;

namespace TrueFalseBackend.Models;

public class Round
{
    public required int Id { get; set; }
    public required Question RoundQuestion { get; set; }
    public required ConcurrentDictionary<string, bool> PlayersAnswers { get; set; } = [];

    public void AddAnswer(string playerId, bool ans)
    {
        // check if a player already answered and if so, don't update
        PlayersAnswers.GetOrAdd(playerId, key => ans);
    }

    public void Next(Question q)
    {
        Id++;
        RoundQuestion = q;
        PlayersAnswers = [];
    }
}

public class RoomState
{
    // description of a current stage of the game
    // "notStarted", "roundInProgress", "finished"
    public string Stage { get; set; } = "notStarted";
    public int RoundsNumber { get; set; } = 10;
    public int RoundTimeoutSeconds { get; set; } = 10;
    public Dictionary<string, Player> Players { get; set; } = [];
    public Round CurrentRound { get; set; } = new() { Id = 0, RoundQuestion = new(), PlayersAnswers = [] };

    public void AddPlayer(string playerId)
    {
        Player pl = new() { PlayerName = $"Player{Players.Count}", IsHost = Players.Count == 0 };
        Players[playerId] = pl;
    }

    public void AddAnswer(string playerId, int roundId, string answer)
    {
        if (CurrentRound.Id != roundId) return;
        CurrentRound.AddAnswer(playerId, answer == "True");
    }

    public void AdvanceToNextRound(Question q)
    {
        Stage = "roundInProgress";
        CurrentRound.Next(q);
    }

    public void ScoreRound()
    {
        Console.WriteLine("Scoring players:");
        bool trueAnswer = CurrentRound.RoundQuestion.Answer;
        Console.WriteLine(CurrentRound.PlayersAnswers.ToString());
        foreach (var (playerId, ans) in CurrentRound.PlayersAnswers)
        {
            Player p = Players[playerId];
            p.Score += ans == trueAnswer ? 1 : 0;
            Console.WriteLine(p.PlayerName, p.Score);
        }
    }

    public bool IsPlayerHost(string playerId)
    {
        if (Players.TryGetValue(playerId, out var p) && p != null) return p.IsHost;
        return false;
    }

    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this);
    }
}

// public class RoomStateOld
// {
//     // description of a current stage of the game
//     // "notStarted", "roundInProgress", "finished"
//     public string Stage { get; set; } = "notStarted";
//     public int RoundsNumber { get; set; }
//     public int RoundTimeoutSeconds { get; set; }
//     public Dictionary<string, Player> Players { get; set; } = [];
//     public int CurrentRound { get; set; }
//     public Question CurrentQuestion { get; set; } = new Question();
//     public List<Answer> CurrentAnswers { get; set; } = [];
//
//     [JsonIgnore]
//     public Dictionary<int, CancellationTokenSource> RoundCancellationTokenSources { get; set; } = [];
//
//     public void AddPlayer(string playerId)
//     {
//         Player pl = new() { PlayerName = $"Player{Players.Count}", IsHost = Players.Count == 0 };
//         Players[playerId] = pl;
//     }
//
//
//     public void AddAnswer(string playerId, string answer)
//     {
//         Answer ans = new() { PlayerConnectionId = playerId, PlayerAnswer = answer == "True" };
//         CurrentAnswers.Add(ans);
//     }
//
//     public void AdvanceToNextRound(Question q)
//     {
//         CurrentQuestion = q;
//         Stage = "roundInProgress";
//         CurrentRound++;
//         CurrentAnswers = [];
//     }
//
//     public void ScorePlayers()
//     {
//         bool trueAnswer = CurrentQuestion.Answer;
//         foreach (Answer playerAns in CurrentAnswers)
//         {
//             Player p = Players[playerAns.PlayerConnectionId];
//             p.Score += playerAns.PlayerAnswer == trueAnswer ? 1 : 0;
//         }
//     }
//
//     public bool IsPlayerHost(string playerId)
//     {
//         if (Players.TryGetValue(playerId, out var p) && p != null) return p.IsHost;
//         return false;
//     }
//
//     public string ToJsonString()
//     {
//         return JsonSerializer.Serialize(this);
//     }
// }
