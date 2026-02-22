namespace MVFC.LongPolling.Config;

public sealed record LongPollingConfig
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public string KeyPrefix { get; set; } = "longpolling";
}
