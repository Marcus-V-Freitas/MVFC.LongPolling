namespace MVFC.LongPolling.Tests.Services;

internal interface ILongPollingApiService
{
    [Get("/poll/{jobId}")]
    internal Task<ApiResponse<PollResponse>> PollAsync(string jobId, CancellationToken ct = default);

    [Get("/poll/{jobId}/typed")]
    internal Task<ApiResponse<OrderCompletedEvent>> PollTypedAsync(string jobId, CancellationToken ct = default);

    [Get("/poll/{jobId}/ready")]
    internal Task<IApiResponse> WaitReadyAsync(string jobId, CancellationToken ct = default);

    [Post("/notify/{jobId}")]
    internal Task<IApiResponse> NotifyAsync(string jobId, [Body] NotifyRequest request);

    [Post("/orders")]
    internal Task<ApiResponse<OrderResponse>> CreateOrderAsync([Body] CreateOrderRequest request, CancellationToken ct = default);

    [Post("/payments/process")]
    internal Task<ApiResponse<string>> ProcessPaymentAsync([Body] ProcessPaymentRequest request, CancellationToken ct = default);
}
