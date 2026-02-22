namespace MVFC.LongPolling.Tests.TestUtils;

internal static class MockEntities
{
    internal static string NewJobId() =>
        $"job-{Guid.NewGuid()}";

    internal static NotifyRequest Payload(string value = "done")
        => new(value);

    internal static NotifyRequest TypedPayload(Guid orderId, string status = "completed") =>
        new(CreateOrderCompleteEventJson(orderId, status));

    internal static string CreateOrderCompleteEventJson(Guid orderId, string status) =>
        JsonSerializer.Serialize<OrderCompletedEvent>(new(orderId, status));

    internal static LongPollingConfig CreatePollingConfig() =>
        new()
        {
            KeyPrefix = "test",
            DefaultTimeout = TimeSpan.FromMilliseconds(500),
        };
}
