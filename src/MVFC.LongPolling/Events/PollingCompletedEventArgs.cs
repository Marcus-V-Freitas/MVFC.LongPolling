namespace MVFC.LongPolling.Events;

public sealed class PollingCompletedEventArgs(string channel, string? payload, bool timedOut) : EventArgs
{
    public string Channel { get; } = channel;
    
    public string? Payload { get; } = payload;

    public bool TimedOut { get; } = timedOut;
    
    public DateTimeOffset CompletedAt { get; } = DateTimeOffset.UtcNow;

    public static PollingCompletedEventArgs FromMessage(string channel, string payload)
        => new(channel, payload, timedOut: false);

    public static PollingCompletedEventArgs FromTimeout(string channel)
        => new(channel, payload: null, timedOut: true);
}