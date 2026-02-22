namespace MVFC.LongPolling.Playground.Api.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this IEndpointRouteBuilder app) =>
        app.MapPost("/orders", async (CreateOrderRequest req, ILongPollingService polling, IPaymentApiServices paymentApi, CancellationToken ct) =>
        {
            var orderId = Guid.NewGuid().ToString();
            var statusTask = polling.WaitAsync(orderId, cancellationToken: ct);

            await paymentApi.ProcessAsync(new ProcessPaymentRequest(orderId, req.Amount)).ConfigureAwait(false);
            var status = await statusTask.ConfigureAwait(false);

            return status switch
            {
                "approved" => Results.Ok(new OrderResponse(orderId, status)),
                "rejected" => Results.UnprocessableEntity(new OrderResponse(orderId, status)),
                _ => Results.StatusCode(StatusCodes.Status504GatewayTimeout)
            };
        });
}
