using TrueFalseBackend.Models;

namespace TrueFalseBackend.Services;

public interface IQuestionProvider
{
    Task<List<Question>> GetNext(int size);
}

public class InMemoryQuestionProvider : IQuestionProvider
{
    private readonly List<Question> _questions;

    public InMemoryQuestionProvider(List<Question> questions)
    {
        _questions = questions;
    }

    public Task<List<Question>> GetNext(int size)
    {
        Console.WriteLine("Question Provider GetNext");
        size = Math.Min(size, _questions.Count);
        Random r = new();
        return Task.FromResult(_questions.OrderBy(q => r.Next(0, _questions.Count)).Take(size).ToList());
    }
}
