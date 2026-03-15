namespace MVFC.LongPolling.Internals;

internal sealed class ChannelLockEntry : IDisposable
{
    private int _refCount;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public static ChannelLockEntry CreateWithRef()
    {
        var entry = new ChannelLockEntry();
        entry.AddRef();
        return entry;
    }

    public void AddRef() =>
        Interlocked.Increment(ref _refCount);

    public bool Release()
    {
        _semaphore.Release();
        return ReleaseRef();
    }

    public bool ReleaseRef() =>
        Interlocked.Decrement(ref _refCount) == 0;

    public Task WaitAsync(CancellationToken ct) =>
        _semaphore.WaitAsync(ct);

    public void Dispose() =>
        _semaphore.Dispose();
}
