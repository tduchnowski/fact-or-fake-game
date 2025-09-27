using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using TrueFalseBackend.Models;
using TrueFalseBackend.Infra.Data;

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
        size = Math.Min(size, _questions.Count);
        Random r = new();
        return Task.FromResult(_questions.OrderBy(q => r.Next(0, _questions.Count)).Take(size).ToList());
    }
}

public class DbQuestionProvider : IQuestionProvider
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly Channel<Question> _questionsChan;
    private readonly QuestionProducer _questionProducer;

    public DbQuestionProvider(IDbContextFactory<AppDbContext> dbContextFactory, int questionChannelCapacity)
    {
        _dbContextFactory = dbContextFactory;
        _questionsChan = Channel.CreateBounded<Question>(questionChannelCapacity);
        _questionProducer = new QuestionProducer(_dbContextFactory, _questionsChan);
        _questionProducer.StartProducing();
    }

    public async Task<List<Question>> GetNext(int size)
    {
        List<Question> questions = [];
        for (int i = 0; i < size; i++)
        {
            Question q = await _questionsChan.Reader.ReadAsync();
            questions.Add(q);
        }
        return questions;
    }
}

public class QuestionProducer
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly Channel<Question> _questionsChan;

    public QuestionProducer(IDbContextFactory<AppDbContext> dbContextFactory, Channel<Question> questionsChan)
    {
        _dbContextFactory = dbContextFactory;
        _questionsChan = questionsChan;
    }

    public void StartProducing()
    {
        _ = Task.Run(async () =>
        {
            int limit = 500;
            while (true)
            {
                // first fetch questions from db
                AppDbContext dbContext = _dbContextFactory.CreateDbContext();
                List<Question> questions = dbContext.Questions.FromSqlRaw($"SELECT * FROM questions ORDER BY RANDOM() LIMIT {limit}").ToList();
                foreach (Question q in questions)
                {
                    await _questionsChan.Writer.WriteAsync(q);
                }
            }
        });
    }
}
