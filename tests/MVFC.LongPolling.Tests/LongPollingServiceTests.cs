namespace MVFC.LongPolling.Tests;

public sealed class LongPollingServiceTests
{
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly ISubscriber _subscriber = Substitute.For<ISubscriber>();    
    private readonly ILogger<LongPollingService> _logger = Substitute.For<ILogger<LongPollingService>>();    
    private readonly LongPollingConfig _config = MockEntities.CreatePollingConfig();
    private readonly IOptions<LongPollingConfig> _configOptions;
    private readonly LongPollingService _sut;

    public LongPollingServiceTests()
    {        
        _configOptions = Options.Create(_config);
        _redis.GetSubscriber().Returns(_subscriber);
        _sut = new(_redis, _configOptions, _logger);
    }

    [Fact]
    public async Task PublishAsync_WithSubscribers_ReturnsTrue()
    {
        // Arrange
        _subscriber.PublishAsync(
            Arg.Is<RedisChannel>(c => c == "test:my-channel"),
            Arg.Is<RedisValue>(v => v == "payload"),
            Arg.Any<CommandFlags>())
                   .Returns(1L);

        // Act
        var result = await _sut.PublishAsync("my-channel", "payload", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        await _subscriber.Received(1).PublishAsync(RedisChannel.Literal("test:my-channel"), "payload");
    }

    [Fact]
    public async Task PublishAsync_NoSubscribers_ReturnsFalse()
    {
        // Arrange
        _subscriber.PublishAsync(
            Arg.Is<RedisChannel>(c => c == "test:my-channel"),
            Arg.Is<RedisValue>(v => v == "payload"),
            Arg.Any<CommandFlags>())
                   .Returns(0L);

        // Act
        var result = await _sut.PublishAsync("my-channel", "payload", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
        await _subscriber.Received(1).PublishAsync(RedisChannel.Literal("test:my-channel"), "payload");
    }

    [Fact]
    public async Task WaitUntilReadyAsync_WithoutSubscription_ReturnsFalse()
    {
        // Arrange & Act
        var result = await _sut.WaitUntilReadyAsync("my-channel", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WaitAsync_TimeoutReached_ReturnsNull()
    {
        // Arrange
        _subscriber.SubscribeAsync(
            Arg.Is<RedisChannel>(c => c == "test:my-channel"),
            Arg.Any<Action<RedisChannel, RedisValue>>(),
            Arg.Any<CommandFlags>())
                   .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.WaitAsync("my-channel", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
        await _subscriber.Received(1).UnsubscribeAsync(RedisChannel.Literal("test:my-channel"));
    }

    [Fact]
    public async Task WaitAsync_MessageReceived_ReturnsPayload()
    {
        // Arrange
        _subscriber.When(x => x.SubscribeAsync(
            Arg.Is<RedisChannel>(c => c == "test:my-channel"),
            Arg.Any<Action<RedisChannel, RedisValue>>(),
            Arg.Any<CommandFlags>()))
            .Do(callInfo =>
            {
                var handler = callInfo.Arg<Action<RedisChannel, RedisValue>>();
                _ = Task.Run(async () =>
                {
                    await Task.Delay(50).ConfigureAwait(true);
                    handler.Invoke(RedisChannel.Literal("test:my-channel"), "mocked-payload");
                });
            });

        // Act
        var result = await _sut.WaitAsync("my-channel", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be("mocked-payload");
        await _subscriber.Received(1).UnsubscribeAsync(RedisChannel.Literal("test:my-channel"));
    }
    
    [Fact]
    public async Task WaitAsync_Typed_MessageReceived_ReturnsDeserializedPayload()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var status = "completed";
        var jsonPayload = MockEntities.CreateOrderCompleteEventJson(orderId, status);

        _subscriber.When(x => x.SubscribeAsync(
            Arg.Is<RedisChannel>(c => c == "test:my-channel"),
            Arg.Any<Action<RedisChannel, RedisValue>>(),
            Arg.Any<CommandFlags>()))
            .Do(callInfo =>
            {
                var handler = callInfo.Arg<Action<RedisChannel, RedisValue>>();
                _ = Task.Run(async () =>
                {
                    await Task.Delay(50).ConfigureAwait(true);
                    handler.Invoke(RedisChannel.Literal("test:my-channel"), jsonPayload);
                });
            });

        // Act
        var result = await _sut.WaitAsync<OrderCompletedEvent>("my-channel", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result!.Status.Should().Be(status);
    }
}
