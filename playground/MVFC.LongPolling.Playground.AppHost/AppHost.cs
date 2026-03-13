var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("longpolling-redis");

var api = builder.AddProject<Projects.MVFC_LongPolling_Playground_Api>("api")                 
                 .WaitFor(redis)
                 .WithReference(redis);

api.WithEnvironment("PaymentApi__BaseUrl", api.GetEndpoint("http"));

await builder.Build().RunAsync().ConfigureAwait(false);
