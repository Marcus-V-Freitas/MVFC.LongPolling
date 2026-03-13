var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("longpolling-redis")
                   .WithRedisCommander();

var api = builder.AddProject<Projects.MVFC_LongPolling_Playground_Api>("api")
                 .WithReference(redis)
                 .WaitFor(redis);

api.WithEnvironment("PaymentApi__BaseUrl", api.GetEndpoint("http"));

await builder.Build().RunAsync().ConfigureAwait(false);
