using RedLockNet.SERedis;

namespace TrueFalseBackend.Infra.Redis;

public interface IRedisLockerHelper
{
    Task ExecuteWithLock(string resource, Func<Task> operation);
}

public class RedisLocker : IRedisLockerHelper
{
    private readonly RedLockFactory _redlockFactory;

    public RedisLocker(RedLockFactory redLockFactory) => _redlockFactory = redLockFactory;

    public async Task ExecuteWithLock(string resource, Func<Task> operation)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(2);
        TimeSpan expiration = TimeSpan.FromSeconds(1);
        TimeSpan retryDelay = TimeSpan.FromMilliseconds(50);
        DateTime start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            using (var redlock = await _redlockFactory.CreateLockAsync(resource, expiration))
            {
                if (redlock.IsAcquired)
                {
                    try
                    {
                        await operation();
                    }
                    finally
                    {
                        await redlock.DisposeAsync();
                    }
                    return;
                }
            }
            await Task.Delay(retryDelay);
        }
        throw new TimeoutException("Couldn't acquire a lock");
    }
}
