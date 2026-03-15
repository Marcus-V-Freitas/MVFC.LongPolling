namespace MVFC.LongPolling.Services;

public sealed class LongPollingService(
    IConnectionMultiplexer redis,
    IOptions<LongPollingConfig> config,
    ILogger<LongPollingService> logger) : ILongPollingService, IAsyncDisposable
{
    private int _disposed;

    private readonly ISubscriber _subscriber = redis.GetSubscriber();
    private readonly LongPollingConfig _config = config.Value;
    private readonly ILogger<LongPollingService> _logger = logger;

    private readonly ConcurrentDictionary<string, ChannelLockEntry> _channelLocks = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _readySignals = new();

    public async Task<T?> WaitAsync<T>(string channel, LongPollingOptions? options = null, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        var payload = await WaitAsync(channel, options, cancellationToken).ConfigureAwait(false);

        if (payload is null)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(payload, jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogDeserializationFailed(channel, typeof(T).Name, ex);
            return default;
        }
    }

    public async Task<string?> WaitAsync(string channel, LongPollingOptions? options = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);

        var resolvedOptions = options ?? new LongPollingOptions();
        var timeout = resolvedOptions.ResolveTimeout(_config);
        var fullChannel = GetFullChannelName(channel, resolvedOptions);

        _logger.LogWaitingChannelAndTimeout(fullChannel, timeout.TotalSeconds);

        _readySignals.GetOrAdd(fullChannel, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var channelLock = await AcquireChannelLockAsync(fullChannel, cancellationToken).ConfigureAwait(false);

        try
        {
            return await SubscribeChannelAsync(fullChannel, timeout, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (channelLock.Release())
                _channelLocks.TryRemove(new KeyValuePair<string, ChannelLockEntry>(fullChannel, channelLock));
        }
    }

    private async Task<ChannelLockEntry> AcquireChannelLockAsync(string fullChannel, CancellationToken cancellationToken)
    {
        var entry = _channelLocks.AddOrUpdate(
            fullChannel,
            static _ => ChannelLockEntry.CreateWithRef(),
            static (_, existing) =>
            {
                existing.AddRef();
                return existing;
            });

        try
        {
            await entry.WaitAsync(cancellationToken).ConfigureAwait(false);
            return entry;
        }
        catch
        {
            // If we are here, WaitAsync failed/cancelled, so the lock was NOT acquired.
            // We must only decrement the reference count (ReleaseRef).
            if (entry.ReleaseRef())
                _channelLocks.TryRemove(new KeyValuePair<string, ChannelLockEntry>(fullChannel, entry));
            throw;
        }
    }

    public async Task<bool> WaitUntilReadyAsync(string channel, LongPollingOptions? options = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);

        var resolvedOptions = options ?? new LongPollingOptions();
        var fullChannel = GetFullChannelName(channel, resolvedOptions);

        if (!_readySignals.TryGetValue(fullChannel, out var readyTcs))
            return false;

        await readyTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> PublishAsync(string channel, string payload, LongPollingOptions? options = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var resolvedOptions = options ?? new LongPollingOptions();
        var fullChannel = GetFullChannelName(channel, resolvedOptions);

        _logger.LogPublishingChannel(fullChannel);

        var receivers = await _subscriber.PublishAsync(RedisChannel.Literal(fullChannel), payload).ConfigureAwait(false);

        if (receivers == 0)
            _logger.LogNoActiveSubscriber(fullChannel);

        return receivers > 0;
    }

    private async Task<string?> SubscribeChannelAsync(string fullChannel, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        _readySignals.TryGetValue(fullChannel, out var readyTcs);

        try
        {
            await RegisterRedisSubscriberAsync(fullChannel, tcs, linkedCts).ConfigureAwait(false);
            readyTcs?.TrySetResult();

            return await WaitForMessageAsync(fullChannel, timeout, tcs, linkedCts, timeoutCts, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _readySignals.TryRemove(fullChannel, out _);
            await UnsubscribeSafelyAsync(fullChannel).ConfigureAwait(false);
        }
    }

    private async Task RegisterRedisSubscriberAsync(string fullChannel, TaskCompletionSource<string?> tcs, CancellationTokenSource linkedCts)
    {
        await _subscriber.SubscribeAsync(RedisChannel.Literal(fullChannel),
            (_, value) =>
            {
                tcs.TrySetResult((string?)value);

                if (linkedCts.IsCancellationRequested)
                    return;

                try
                {
                    linkedCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Ignorar
                }
            }).ConfigureAwait(false);
    }

    private async Task<string?> WaitForMessageAsync(
        string fullChannel,
        TimeSpan timeout,
        TaskCompletionSource<string?> tcs,
        CancellationTokenSource linkedCts,
        CancellationTokenSource timeoutCts,
        CancellationToken cancellationToken)
    {
        await using var registration = linkedCts.Token.Register(() => tcs.TrySetCanceled(linkedCts.Token)).ConfigureAwait(false);

        string? result;

        try
        {
            result = await tcs.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogTimeoutReached(timeout.TotalSeconds, fullChannel, ex);
            return null;
        }

        _logger.LogMessageReceived(fullChannel);

        return result;
    }

    private async Task UnsubscribeSafelyAsync(string fullChannel)
    {
        try
        {
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal(fullChannel)).ConfigureAwait(false);
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogIgnoredUnsubscriptionFailure(fullChannel, ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogIgnoredUnsubscriptionFailure(fullChannel, ex);
        }
    }

    private string GetFullChannelName(string channel, LongPollingOptions options)
    {
        var prefix = options.ResolveKeyPrefix(_config);
        return $"{prefix}:{channel}";
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await _subscriber.UnsubscribeAllAsync().ConfigureAwait(false);

        foreach (var (_, entry) in _channelLocks)
            entry.Dispose();

        _channelLocks.Clear();
        _readySignals.Clear();
    }
}
