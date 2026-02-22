namespace MVFC.LongPolling.Exceptions;

public sealed class LongPollingTimeoutException(string channel, TimeSpan timeout) : 
    TimeoutException($"Long polling timed out after {timeout.TotalSeconds}s on channel '{channel}'.")
{
    public string Channel { get; } = channel;

    public TimeSpan Timeout { get; } = timeout;
}