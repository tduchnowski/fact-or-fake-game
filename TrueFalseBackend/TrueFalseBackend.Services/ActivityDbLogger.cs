using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using TrueFalseBackend.Infra.Data;
using TrueFalseBackend.Models;

namespace TrueFalseBackend.Services;

public class ActivityLoggerQueue
{
    private readonly Channel<User> _logChan;
    public ChannelReader<User> Reader { get; private set; }

    public ActivityLoggerQueue()
    {
        _logChan = Channel.CreateUnbounded<User>();
        Reader = _logChan.Reader;
    }

    public async Task Enqueue(User user)
    {
        await _logChan.Writer.WriteAsync(user);
    }
}

public class ActivityDbLogger : BackgroundService
{
    private readonly AppDbContext _dbContext;
    private readonly ActivityLoggerQueue _queue;

    public ActivityDbLogger(AppDbContext dbContext, ActivityLoggerQueue queue)
    {
        _dbContext = dbContext;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (User user in _queue.Reader.ReadAllAsync(cancellationToken))
        {
            User? dbUser = await _dbContext.Users.FindAsync(user.Username);
            if (dbUser != null)
            {
                dbUser.TimeLastActivity = user.TimeLastActivity;
                _dbContext.Users.Update(user);
            }
            else
            {
                _dbContext.Users.Add(user);
            }
            await _dbContext.SaveChangesAsync();
        }
    }
}
