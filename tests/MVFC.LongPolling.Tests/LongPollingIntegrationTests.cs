namespace MVFC.LongPolling.Tests;

[Collection("Aspire")]
public sealed class LongPollingIntegrationTests(AspireFixture fixture) : IClassFixture<AspireFixture>
{
    private readonly ILongPollingApiService _api = fixture.Api;

    [Fact]
    public async Task Poll_WhenNotified_ReturnsPayload()
    {
        // Arrange
        var jobId = MockEntities.NewJobId();
        var expected = MockEntities.Payload("order-completed");
        var ct = TestContext.Current.CancellationToken;

        // Act
        var pollTask = _api.PollAsync(jobId, ct);

        await _api.WaitReadyAsync(jobId, ct);
        await _api.NotifyAsync(jobId, expected);
        var result = await pollTask.ConfigureAwait(true);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content!.Result.Should().Be(expected.Payload);
        result.Content!.JobId.Should().Be(jobId);
    }

    [Fact]
    public async Task Poll_WhenNotifiedWithDifferentJobs_EachReceivesOwnPayload()
    {
        // Arrange
        var jobId1 = MockEntities.NewJobId();
        var jobId2 = MockEntities.NewJobId();
        var payload1 = MockEntities.Payload("result-1");
        var payload2 = MockEntities.Payload("result-2");
        var ct = TestContext.Current.CancellationToken;

        // Act
        var poll1 = _api.PollAsync(jobId1, ct);
        var poll2 = _api.PollAsync(jobId2, ct);

        await Task.WhenAll(
            _api.WaitReadyAsync(jobId1, ct),
            _api.WaitReadyAsync(jobId2, ct));

        await _api.NotifyAsync(jobId1, payload1);
        await _api.NotifyAsync(jobId2, payload2);

        var res1 = await poll1.ConfigureAwait(true);
        var res2 = await poll2.ConfigureAwait(true);

        // Assert
        res1.Content!.Result.Should().Be("result-1");
        res2.Content!.Result.Should().Be("result-2");
        res1.Content!.Result.Should().NotBe(res2.Content!.Result);
    }

    [Fact]
    public async Task Poll_WhenTimeout_ReturnsNoContent()
    {
        // Arrange
        var jobId = MockEntities.NewJobId();
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await _api.PollAsync(jobId, ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Poll_CancelledByClient_DoesNotBlock()
    {
        // Arrange
        var jobId = MockEntities.NewJobId();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Act
        var act = async () => await _api.PollAsync(jobId, cts.Token).ConfigureAwait(true);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task Poll_WhenReadyCalledBeforePoll_ReturnsNotFound()
    {
        // Arrange
        var jobId = MockEntities.NewJobId();
        var ct = TestContext.Current.CancellationToken;

        // Act
        var ready = await _api.WaitReadyAsync(jobId, ct);

        // Assert
        ready.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Notify_WhenNoPollActive_ReturnsNotFound()
    {
        // Arrange
        var jobId = MockEntities.NewJobId();

        // Act
        var result = await _api.NotifyAsync(jobId, MockEntities.Payload("orphan"));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Poll_Typed_WhenNotified_ReturnsDeserializedPayload()
    {
        // Arrange
        var jobId = MockEntities.NewJobId();
        var orderId = Guid.NewGuid();
        var payload = MockEntities.TypedPayload(orderId, "completed");
        var ct = TestContext.Current.CancellationToken;

        // Act
        var pollTask = _api.PollTypedAsync(jobId, ct);
        await _api.WaitReadyAsync(jobId, ct);
        await _api.NotifyAsync(jobId, payload);
        var result = await pollTask.ConfigureAwait(true);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content!.OrderId.Should().Be(orderId);
        result.Content!.Status.Should().Be("completed");
    }

    [Fact]
    public async Task Poll_Typed_WhenTimeout_ReturnsNoContent()
    {
        // Arrange
        var jobId = MockEntities.NewJobId();

        // Act
        var result = await _api.PollTypedAsync(jobId, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Poll_Typed_WhenPayloadIsInvalid_ReturnsNoContent()
    {
        // Arrange
        var jobId = MockEntities.NewJobId();
        var payload = MockEntities.Payload("isso-nao-e-json");
        var ct = TestContext.Current.CancellationToken;

        // Act
        var pollTask = _api.PollTypedAsync(jobId, ct);
        await _api.WaitReadyAsync(jobId, ct);
        await _api.NotifyAsync(jobId, payload);
        var result = await pollTask.ConfigureAwait(true);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Order_WhenPaymentApproved_Returns200WithApprovedStatus()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = new CreateOrderRequest(Amount: 500m);

        // Act
        var result = await _api.CreateOrderAsync(payload, ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content!.Status.Should().Be("approved");
        result.Content!.OrderId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Order_WhenPaymentRejected_Returns422WithRejectedStatus()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = new CreateOrderRequest(Amount: 99_999m);

        // Act
        var result = await _api.CreateOrderAsync(payload, ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        result.Content!.Status.Should().Be("rejected");
    }

    [Fact]
    public async Task Order_WhenPaymentApiDoesNotRespond_Returns504()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = new CreateOrderRequest(Amount: -1m);

        // Act
        var result = await _api.CreateOrderAsync(payload, ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
    }

    [Fact]
    public async Task Order_WhenClientDisconnects_RequestIsCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var payload = new CreateOrderRequest(Amount: -1m);

        // Act
        Func<Task> act = async () => await _api.CreateOrderAsync(payload, cts.Token).ConfigureAwait(false);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Payment_WhenNoSubscriberActive_Returns404()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = new ProcessPaymentRequest(OrderId: Guid.NewGuid().ToString(), Amount: 500m);

        // Act
        var result = await _api.ProcessPaymentAsync(payload, ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
