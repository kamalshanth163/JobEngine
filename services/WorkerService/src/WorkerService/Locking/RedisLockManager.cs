using StackExchange.Redis;

namespace WorkerService.Locking;

public sealed class RedisLockManager(IDatabase _db) : IDistributedLockManager
{
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string resource, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = $"lock:{resource}";
        // SET key value NX PX ms — single atomic operation, no race condition
        var acquired = await _db.StringSetAsync(
            key, _instanceId, ttl, When.NotExists);

        return acquired ? new RedisLock(_db, key, _instanceId, ttl) : null;
    }
}

internal sealed class RedisLock : IAsyncDisposable
{
    private readonly IDatabase _db;
    private readonly string _key, _value;
    private readonly Timer _renew;
    private bool _disposed;

    // Lua script: only DELETE the key if we still own it (value matches)
    // Without this: worker A's lock expires, worker B acquires it,
    // worker A finishes and deletes worker B's lock. Catastrophic.
    private const string ReleaseLua = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else return 0 end";

    internal RedisLock(IDatabase db, string key, string val, TimeSpan ttl)
    {
        _db = db; _key = key; _value = val;
        // Auto-renew at 2/3 of TTL so long jobs don't lose their lock
        var interval = TimeSpan.FromMilliseconds(ttl.TotalMilliseconds * 2 / 3);
        _renew = new Timer(_ => { if (!_disposed) _db.KeyExpire(_key, ttl); },
            null, interval, interval);
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await _renew.DisposeAsync();
        await _db.ScriptEvaluateAsync(ReleaseLua,
            new RedisKey[] { _key },
            new RedisValue[] { _value });
    }
}