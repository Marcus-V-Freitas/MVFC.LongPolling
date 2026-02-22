var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("longpolling-redis")
                   .WithRedisCommander();

builder.AddProject<Projects.MVFC_LongPolling_Playground_Api>("api")
       .WithReference(redis)
       .WaitFor(redis);

await builder.Build().RunAsync();