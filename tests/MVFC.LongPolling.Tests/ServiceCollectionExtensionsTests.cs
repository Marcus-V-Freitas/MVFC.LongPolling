namespace MVFC.LongPolling.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services = new ServiceCollection();
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();

    [Fact]
    public void AddLongPolling_RegistersMultiplexerAsSingleton()
    {
        // Arrange & Act
        _services.AddLongPolling(_redis);

        // Assert
        _services.Should().Contain(sd =>
            sd.ServiceType == typeof(IConnectionMultiplexer) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddLongPolling_RegistersLongPollingServiceAsSingleton()
    {
        // Arrange & Act
        _services.AddLongPolling(_redis);

        // Assert
        _services.Should().Contain(sd =>
            sd.ServiceType == typeof(ILongPollingService) &&
            sd.ImplementationType == typeof(LongPollingService) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddLongPolling_InvokesConfigureCallback()
    {
        // Arrange
        var customPrefix = "custom";

        // Act
        _services.AddLongPolling(_redis, config => config.KeyPrefix = customPrefix);

        // Assert
        var provider = _services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LongPollingConfig>>();
        options.Value.KeyPrefix.Should().Be(customPrefix);
    }

    [Fact]
    public void AddLongPolling_WithoutConfigure_UsesDefaults()
    {
        // Arrange & Act
        _services.AddLongPolling(_redis);

        // Assert
        var provider = _services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LongPollingConfig>>();
        options.Value.KeyPrefix.Should().Be("longpolling");
        options.Value.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddLongPolling_ReturnsSameServiceCollection()
    {
        // Arrange & Act
        var result = _services.AddLongPolling(_redis);

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddLongPolling_WithConnectionString_NullOrWhitespace_ThrowsArgumentException(string? connectionString)
    {
        // Arrange & Act
        var act = () => _services.AddLongPolling(connectionString!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddLongPolling_WithInvalidConnectionString_ThrowsRedisConnectionException()
    {
        // Arrange & Act
        var act = () => _services.AddLongPolling("invalid-host-that-does-not-exist:6379,abortConnect=true,connectTimeout=1");

        // Assert
        act.Should().Throw<RedisConnectionException>();
    }
}
