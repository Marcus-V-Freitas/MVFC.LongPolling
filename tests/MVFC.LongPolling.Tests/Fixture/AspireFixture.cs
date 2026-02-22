namespace MVFC.LongPolling.Tests.Fixture;

public sealed class AspireFixture : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private HttpClient _http = null!;

    internal ILongPollingApiService Api = null!;

    public async ValueTask InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MVFC_LongPolling_Playground_AppHost>().ConfigureAwait(false);

        _app = await appHost.BuildAsync().ConfigureAwait(false);
        await _app.StartAsync().ConfigureAwait(false);

        _http = _app.CreateHttpClient("api");
        Api = RestService.For<ILongPollingApiService>(_http, new RefitSettings
        {
            ExceptionFactory = _ => Task.FromResult<Exception?>(null)
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync().ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
        _http.Dispose();
    }
}
