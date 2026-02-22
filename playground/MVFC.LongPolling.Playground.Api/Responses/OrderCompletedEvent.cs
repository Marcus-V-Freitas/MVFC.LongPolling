namespace MVFC.LongPolling.Playground.Api.Responses;

public sealed record OrderCompletedEvent(Guid OrderId, string Status);
