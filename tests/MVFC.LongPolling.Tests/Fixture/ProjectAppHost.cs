namespace MVFC.LongPolling.Tests.Fixture;

internal sealed class ProjectAppHost() : DistributedApplicationFactory(typeof(MVFC_LongPolling_Playground_AppHost))
{
    protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
        applicationOptions.AllowUnsecuredTransport = true;
        hostOptions.Args = ["--testmode=true"];
    }

    protected override void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder) =>
        applicationBuilder.Services.ConfigureHttpClientDefaults(x => x.AddStandardResilienceHandler(c =>
        {
            var timeSpan = TimeSpan.FromMinutes(2);
            c.AttemptTimeout.Timeout = timeSpan;
            c.CircuitBreaker.SamplingDuration = timeSpan * 2;
            c.TotalRequestTimeout.Timeout = timeSpan * 3;
        }));
}
