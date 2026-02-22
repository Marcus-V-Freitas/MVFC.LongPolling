namespace MVFC.LongPolling.Config;

public sealed record LongPollingOptions(TimeSpan? Timeout = null, string? KeyPrefix = null)
{
    internal TimeSpan ResolveTimeout(LongPollingConfig config) =>
        Timeout ?? config.DefaultTimeout;

    internal string ResolveKeyPrefix(LongPollingConfig config) =>
        KeyPrefix ?? config.KeyPrefix;
}