var builder = WebApplication.CreateBuilder(args);

var redis = builder.Configuration.GetConnectionString("longpolling-redis")!;

builder.Services.AddLongPolling(redis, cfg =>
{
    cfg.DefaultTimeout = TimeSpan.FromSeconds(10);
    cfg.KeyPrefix = "poll";
});

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
