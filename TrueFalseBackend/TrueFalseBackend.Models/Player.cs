namespace TrueFalseBackend.Models;

public class Player
{
    public required string PlayerName { get; set; }
    public int Score { get; set; }
    public bool IsHost { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PlayersInfo : JsonStringer
{
    public Dictionary<string, Player> Players { get; set; } = [];

    public void AddPlayer(string playerId)
    {
        Player pl = new() { PlayerName = $"Player{Players.Count}", IsHost = Players.Count == 0 };
        Players[playerId] = pl;
    }

    public void RemovePlayer(string playerId)
    {
        Player? p = GetPlayer(playerId);
        if (p == null) return;
        Players.Remove(playerId);
        if (p.IsHost && Players.Count > 0)
        {
            string newHostId = Players.Keys.First();
            Players[newHostId].IsHost = true;
        }
    }

    public void SetPlayerName(string playerId, string name)
    {

    }

    public bool IsPlayerHost(string playerId)
    {
        if (Players.TryGetValue(playerId, out var p) && p != null) return p.IsHost;
        return false;
    }

    public Player? GetPlayer(string playerId)
    {
        if (Players.TryGetValue(playerId, out var p) && p != null) return p;
        return null;
    }

    public int Count()
    {
        return Players.Count;
    }

}
