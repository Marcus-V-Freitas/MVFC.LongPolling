namespace MVFC.LongPolling.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLongPolling(
        this IServiceCollection services,
        string redisConnectionString,
        Action<LongPollingConfig>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.Configure<LongPollingConfig>(config => configure?.Invoke(config));
        services.AddSingleton<ILongPollingService, LongPollingService>();

        return services;
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
