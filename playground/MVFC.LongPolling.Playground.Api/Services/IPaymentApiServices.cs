namespace MVFC.LongPolling.Playground.Api.Services;

public interface IPaymentApiServices
{
    [Post("/payments/process")]
    internal Task<ApiResponse<string>> ProcessAsync([Body] ProcessPaymentRequest request);
}
