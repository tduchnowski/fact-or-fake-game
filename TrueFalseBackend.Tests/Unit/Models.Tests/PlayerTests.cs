using TrueFalseBackend.Models;

public class PlayerTests
{
    [Fact]
    public void TestPlayerInfo_AddPlayerToEmpty_CorrectCount()
    {
        PlayersInfo pi = new();
        pi.AddPlayer("player");
        Assert.Equal(1, pi.Count());
    }

    [Fact]
    public void TestPlayersInfo_AddPlayer_PlayerIsHost()
    {
        PlayersInfo pi = new();
        pi.AddPlayer("player");
        Assert.True(pi.IsPlayerHost("player"));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(1234)]
    [InlineData(2345)]
    public void TestPlayersInfo_NotExistingPlayerIsHost_ReturnFalse(int input)
    {
        PlayersInfo pi = new();
        for (int i = 0; i < input; i++)
        {
            pi.AddPlayer($"player{i}");
        }
        Assert.False(pi.IsPlayerHost("p"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(1234)]
    [InlineData(2345)]
    public void TestPlayersInfo_AddMultiplePlayers_CorrectCount(int input)
    {
        PlayersInfo pi = new();
        for (int i = 0; i < input; i++)
        {
            pi.AddPlayer($"player{i}");
        }
        Assert.Equal(input, pi.Count());
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(1234)]
    [InlineData(2345)]
    public void TestPlayerInfo_AddMultiplePlayers_OnlyFirstPlayerIsHost(int input)
    {
        PlayersInfo pi = new();
        string playerHost = "host";
        pi.AddPlayer(playerHost);
        List<string> playerIds = [];
        for (int i = 1; i < input; i++)
        {
            string playerId = $"player{i}";
            pi.AddPlayer(playerId);
            playerIds.Add(playerId);
        }
        Assert.True(pi.IsPlayerHost(playerHost));
        Assert.DoesNotContain(playerIds, id => pi.IsPlayerHost(id));
    }

    [Theory]
    [InlineData(100, 50)]
    [InlineData(200, 101)]
    [InlineData(300, 299)]
    [InlineData(1234, 1000)]
    [InlineData(2345, 1999)]
    public void TestPlayerInfo_RemovePlayer_PlayerNotInPlayersDictionary(int numberOfPlayers, int idToRemove)
    {
        PlayersInfo pi = new();
        string toRemove = $"player{idToRemove}";
        for (int i = 1; i < numberOfPlayers; i++)
        {
            string playerId = $"player{i}";
            pi.AddPlayer(playerId);
        }
        pi.RemovePlayer(toRemove);
        Assert.Null(pi.GetPlayer(toRemove));
    }

    [Theory]
    [InlineData(0, 200)]
    [InlineData(100, 200)]
    [InlineData(200, 201)]
    [InlineData(300, 576)]
    [InlineData(1234, 28997)]
    [InlineData(2345, 10082)]
    public void TestPlayersInfo_RemoveNotPresentPlayer_DoNothing(int numberOfPlayers, int notExistingId)
    {
        PlayersInfo pi = new();
        string toRemove = $"player{notExistingId}";
        for (int i = 1; i <= numberOfPlayers; i++)
        {
            string playerId = $"player{i}";
            pi.AddPlayer(playerId);
        }
        pi.RemovePlayer(toRemove);
        Assert.Equal(numberOfPlayers, pi.Count());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(1000)]
    [InlineData(12345)]
    public void TestPlayersInfo_RemoveHost_AssignedNewHost(int numberOfPlayers)
    {
        PlayersInfo pi = new();
        List<string> playerIds = [];
        for (int i = 1; i <= numberOfPlayers; i++)
        {
            string playerId = $"player{i}";
            playerIds.Add(playerId);
            pi.AddPlayer(playerId);
        }
        pi.RemovePlayer("player1");
        playerIds.Remove("player1");
        int countHost = 0;
        int countNoHost = 0;
        foreach (string id in playerIds)
        {
            if (pi.IsPlayerHost(id))
                countHost++;
            else
                countNoHost++;
        }
        Assert.Equal(1, countHost);
        Assert.Equal(numberOfPlayers - 2, countNoHost);
    }

    [Theory]
    [InlineData(100, 50)]
    [InlineData(200, 101)]
    [InlineData(300, 199)]
    [InlineData(1234, 1000)]
    [InlineData(2345, 100)]
    public void TestPlayersInfo_AddAndRemovePlayers_CorrectCount(int numberOfPlayers, int numberToRemove)
    {
        PlayersInfo pi = new();
        for (int i = 1; i <= numberOfPlayers; i++)
        {
            string playerId = $"player{i}";
            pi.AddPlayer(playerId);
        }
        Random r = new();
        List<int> randomIds = Enumerable.Range(1, numberOfPlayers).OrderBy(_ => r.Next()).Take(numberToRemove).ToList();
        foreach (int id in randomIds) pi.RemovePlayer($"player{id}");
        Assert.Equal(numberOfPlayers - numberToRemove, pi.Count());
    }
}
