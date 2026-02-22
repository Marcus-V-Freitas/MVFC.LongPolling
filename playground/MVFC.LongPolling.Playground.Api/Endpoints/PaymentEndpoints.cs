namespace MVFC.LongPolling.Playground.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentsEndpoints(this IEndpointRouteBuilder app) =>
        app.MapPost("/payments/process", async(ProcessPaymentRequest req, ILongPollingService polling) =>
        {
            if (req.Amount <= 0)
                return Results.Accepted();

            var status = req.Amount <= 10_000m ? "approved" : "rejected";
            var delivered = await polling.PublishAsync(req.OrderId, status).ConfigureAwait(false);

            return delivered
            ? Results.Accepted()
            : Results.NotFound($"Nenhum subscriber aguardando o pedido '{req.OrderId}'.");
        });
}
