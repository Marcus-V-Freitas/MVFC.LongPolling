namespace MVFC.LongPolling.Playground.Api.Requests;

public sealed record ProcessPaymentRequest(string OrderId, decimal Amount);
