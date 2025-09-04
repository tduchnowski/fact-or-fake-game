using TrueFalseBackend.Models;

public class RoundAnswerTest
{
    [Fact]
    public void TestAnswer_InitiallyEmpty()
    {
        RoundAnswers roundAnswers = new();
        Assert.Equal(0, roundAnswers.Count());
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(1000)]
    [InlineData(2000)]
    [InlineData(12345)]
    [InlineData(456789)]
    public void TestAddAnswer_EmptyAnswers_AnswerAdded(int numberOfAnswers)
    {
        RoundAnswers roundAnswers = new();
        for (int i = 0; i < numberOfAnswers; i++)
        {
            string playerId = $"player{i}";
            roundAnswers.AddAnswer(playerId, (i % 2 == 0).ToString());
        }
        Assert.Equal(numberOfAnswers, roundAnswers.Count());
    }

    [Fact]
    public void TestAddAnswer_AddTwiceForSamePlayer_NotOverwrite()
    {
        RoundAnswers roundAnswers = new();
        string playerId = "player";
        roundAnswers.AddAnswer(playerId, "True");
        roundAnswers.AddAnswer(playerId, "False");
        Assert.True(roundAnswers.GetAnswer(playerId));
        roundAnswers = new();
        roundAnswers.AddAnswer(playerId, "False");
        roundAnswers.AddAnswer(playerId, "True");
        Assert.False(roundAnswers.GetAnswer(playerId));
    }

    [Fact]
    public void TestAddAnswer_Add_CorrectBool()
    {
        RoundAnswers roundAnswers = new();
        string firstPlayerId = "player";
        roundAnswers.AddAnswer(firstPlayerId, "True");
        Assert.True(roundAnswers.GetAnswer(firstPlayerId));
        string secondPlayerId = "player1";
        roundAnswers.AddAnswer(secondPlayerId, "False");
        Assert.False(roundAnswers.GetAnswer(secondPlayerId));
    }
}
