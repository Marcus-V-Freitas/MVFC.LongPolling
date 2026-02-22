var builder = WebApplication.CreateBuilder(args);

var redis = builder.Configuration.GetConnectionString("longpolling-redis")!;

builder.Services.AddLongPolling(redis, cfg =>
{
    cfg.DefaultTimeout = TimeSpan.FromSeconds(10);
    cfg.KeyPrefix = "poll";
});

builder.Services.AddRefitClient<IPaymentApiServices>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(builder.Configuration["PaymentApi:BaseUrl"]!));

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapOrdersEndpoints();
app.MapPaymentsEndpoints();

await app.RunAsync().ConfigureAwait(false);
