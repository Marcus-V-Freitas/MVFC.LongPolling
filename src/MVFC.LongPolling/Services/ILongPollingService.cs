namespace MVFC.LongPolling.Services;

public interface ILongPollingService
{
    /// <summary>
    /// Aguarda uma mensagem no canal Redis via Pub/Sub.
    /// Retorna o payload assim que chegar ou null se atingir o timeout.
    /// Dispara o evento <see cref="OnMessageReceived"/> em ambos os casos.
    /// </summary>
    public Task<string?> WaitAsync(string channel, LongPollingOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aguarda uma mensagem no canal e desserializa o payload como <typeparamref name="T"/>.
    /// Retorna <c>null</c> se timeout atingido ou payload inválido.
    /// </summary>
    public Task<T?> WaitAsync<T>(string channel, LongPollingOptions? options = null, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publicar uma mensagem em um canal, liberando quem estiver aguardando.
    /// </summary>
    public Task<bool> PublishAsync(string channel, string payload, LongPollingOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aguarda até que a subscription no canal esteja ativa.
    /// Use antes de PublishAsync para garantir entrega determinística.
    /// </summary>
    public Task<bool> WaitUntilReadyAsync(string channel, LongPollingOptions? options = null, CancellationToken cancellationToken = default);
}
