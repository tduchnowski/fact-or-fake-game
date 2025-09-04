using TrueFalseBackend.Models;

public class RoomStateTests
{
    [Fact]
    public void TestRound_Advancing_IncrementRoundId()
    {
        Round round = new() { Id = 1, RoundQuestion = new() };
        for (int i = 2; i < 100; i++)
        {
            Question nextQuestion = new() { Id = i, Answer = true, Text = $"Text{i}" };
            round.Next(nextQuestion);
            Assert.Equal(i, round.Id);
            Assert.Equal(i, round.RoundQuestion.Id);
        }
    }

    [Fact]
    public void TestRoomState_AdvancingInProgressGame()
    {
        RoomState roomState = new();
        for (int i = 1; i < 100; i++)
        {
            roomState.AdvanceToNextRound(new Question() { Id = i, Answer = false, Text = "New Question" });
            Assert.Equal(i, roomState.CurrentRound.Id);
            Assert.Equal(i, roomState.CurrentRound.RoundQuestion.Id);
            Assert.Equal("roundInProgress", roomState.Stage);
        }
    }

    [Fact]
    public void TestRoomState_AdvancingFinishedGame_DoNothing()
    {
        RoomState roomState = new();
        roomState.Stage = "finished";
        roomState.AdvanceToNextRound(new Question() { Id = 1, Answer = false, Text = "New Question" });
        Assert.Equal("finished", roomState.Stage);
        Assert.Equal(0, roomState.CurrentRound.RoundQuestion.Id);
    }
}
