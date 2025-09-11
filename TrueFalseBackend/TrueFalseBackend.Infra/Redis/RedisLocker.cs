using RedLockNet.SERedis;

namespace TrueFalseBackend.Infra.Redis;

public interface IRedisLockerHelper
{
    Task<bool> ExecuteWithLock(string resource, Func<Task<bool>> operation);
}

public class RedisLocker : IRedisLockerHelper
{
    private readonly RedLockFactory _redlockFactory;

    public RedisLocker(RedLockFactory redLockFactory) => _redlockFactory = redLockFactory;

    public async Task<bool> ExecuteWithLock(string resource, Func<Task<bool>> operation)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(2);
        TimeSpan expiration = TimeSpan.FromSeconds(1);
        TimeSpan retryDelay = TimeSpan.FromMilliseconds(50);
        DateTime start = DateTime.UtcNow;
        bool ok = false;
        while (DateTime.UtcNow - start < timeout)
        {
            using (var redlock = await _redlockFactory.CreateLockAsync(resource, expiration))
            {
                if (redlock.IsAcquired)
                {
                    try
                    {
                        ok = await operation();
                    }
                    finally
                    {
                        await redlock.DisposeAsync();
                    }
                    return ok;
                }
            }
            await Task.Delay(retryDelay);
        }
        throw new TimeoutException("Couldn't acquire a lock");
    }
}
