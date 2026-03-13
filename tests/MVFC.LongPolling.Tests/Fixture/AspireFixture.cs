namespace MVFC.LongPolling.Tests.Fixture;

public sealed class AspireFixture : IAsyncLifetime
{
    private ProjectAppHost _appHost = null!;
    private HttpClient _http = null!;

    internal ILongPollingApiService Api = null!;

    public async ValueTask InitializeAsync()
    {
        _appHost = new ProjectAppHost();

        await _appHost.StartAsync().ConfigureAwait(false);

        _http = _appHost.CreateHttpClient("api");
        Api = RestService.For<ILongPollingApiService>(_http, new RefitSettings
        {
            ExceptionFactory = _ => Task.FromResult<Exception?>(null),
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _appHost.DisposeAsync().ConfigureAwait(false);
        _http.Dispose();
    }
}
