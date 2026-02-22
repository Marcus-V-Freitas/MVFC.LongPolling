namespace MVFC.LongPolling.Playground.Api.Endpoints;

public static class DefaultEndpoints
{
    public static void MapDefaultEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/poll/{jobId}", async (string jobId, ILongPollingService polling, CancellationToken ct) =>
        {
            var result = await polling.WaitAsync(jobId, cancellationToken: ct).ConfigureAwait(false);

            return result is null ? Results.NoContent() : Results.Ok(new PollResponse(jobId, result));
        });

        app.MapGet("/poll/{jobId}/typed", async (string jobId, ILongPollingService polling, CancellationToken ct) =>
        {
            var result = await polling.WaitAsync<OrderCompletedEvent>(jobId, cancellationToken: ct);

            return result is null
                ? Results.NoContent()
                : Results.Ok(result);
        });

        app.MapGet("/poll/{jobId}/ready", async (string jobId, ILongPollingService polling, CancellationToken ct) =>
        {
            var isReady = await polling.WaitUntilReadyAsync(jobId, cancellationToken: ct).ConfigureAwait(false);

            return isReady
                ? Results.Ok()
                : Results.NotFound($"Nenhum poll ativo para o canal '{jobId}'.");
        });

        app.MapPost("/notify/{jobId}", async (string jobId, NotifyRequest req, ILongPollingService polling) =>
        {
            var delivered = await polling.PublishAsync(jobId, req.Payload).ConfigureAwait(false);

            return delivered
                ? Results.Accepted()
                : Results.NotFound($"Nenhum subscriber ativo para o canal '{jobId}'.");
        });
    }
}
