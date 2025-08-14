using System.Collections.Concurrent;

namespace TrueFalseBackend.Shared;

public static class RoomTokens
{
    private static ConcurrentDictionary<string, ConcurrentDictionary<int, CancellationTokenSource>> _cancellationTokens = [];

    public static CancellationTokenSource CreateRoomTokenSource(string roomId, int round)
    {
        ConcurrentDictionary<int, CancellationTokenSource> roundTokens = _cancellationTokens.GetOrAdd(roomId, new ConcurrentDictionary<int, CancellationTokenSource>());
        roundTokens.AddOrUpdate(round, new CancellationTokenSource(), (key, oldValue) => new CancellationTokenSource());
        return _cancellationTokens[roomId][round];
    }

    public static void CancelRoomTokenSource(string roomId, int round)
    {
        if (_cancellationTokens.TryGetValue(roomId, out var roundTokens) && roundTokens != null && roundTokens.TryGetValue(round, out var token))
        {
            token.Cancel();
            token.Dispose();
            roundTokens.TryRemove(round, out _);
            if (roundTokens.Count() == 0) _cancellationTokens.TryRemove(roomId, out _);
        }
    }
}

// public class InMemoryStates
// {
//     // public static Dictionary<string, QuizState> States = [];
//     public static Dictionary<string, string> ConnectionToRoomMap = [];
//     public static Dictionary<string, Dictionary<int, CancellationTokenSource>> CancellationTokens = [];
//     // private static List<Question> _questions = [
//     //   new (){Id = 1, Text = "Is Lerusha super lazy?", Answer = true},
//     //   new (){Id = 2, Text = "Does Lerusha suffer from smartphonosis?", Answer = true},
//     //   new (){Id = 3, Text = "Is Tommy Boy dope?", Answer = true},
//     //   new (){Id = 4, Text = "Is Tommy Boy ugly?", Answer = false},
//     //   new (){Id = 5, Text = "Is Tommy Boy 1.90m tall?", Answer = false},
//     //   new (){Id = 6, Text = "Is Leorio the coolest character in Hunter x Hunter?", Answer = false},
//     //   new (){Id = 7, Text = "Does Lerusha hate working?", Answer = true},
//     // ];
//
//     // public static Question GetRandomQuestion()
//     // {
//     //     Random r = new();
//     //     int idx = r.Next(_questions.Count);
//     //     return _questions[idx];
//     // }
// }
//
