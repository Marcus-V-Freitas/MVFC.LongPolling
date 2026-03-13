namespace MVFC.LongPolling.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLongPolling(
        this IServiceCollection services,
        string redisConnectionString,
        Action<LongPollingConfig>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);

        var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);

        return services.AddLongPolling(multiplexer, configure);
    }

    public static IServiceCollection AddLongPolling(
        this IServiceCollection services,
        IConnectionMultiplexer multiplexer,
        Action<LongPollingConfig>? configure = null)
    {
        services.AddSingleton(multiplexer);
        services.Configure<LongPollingConfig>(config => configure?.Invoke(config));
        services.AddSingleton<ILongPollingService, LongPollingService>();

        return services;
    }
}
