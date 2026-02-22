namespace MVFC.LongPolling.Extensions;

internal static partial class LogDefinitions
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "LongPolling: aguardando canal '{Channel}' por {TimeoutSeconds}s.")]
    public static partial void LogWaitingChannelAndTimeout(this ILogger logger, string channel, double timeoutSeconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "LongPolling: timeout de {TimeoutSeconds}s atingido no canal '{Channel}'.")]
    public static partial void LogTimeoutReached(this ILogger logger, double timeoutSeconds, string channel, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "LongPolling: mensagem recebida no canal '{Channel}'.")]
    public static partial void LogMessageReceived(this ILogger logger, string channel);

    [LoggerMessage(Level = LogLevel.Debug, Message = "LongPolling: publicando no canal '{Channel}'.")]
    public static partial void LogPublishingChannel(this ILogger logger, string channel);

    [LoggerMessage(Level = LogLevel.Warning, Message = "LongPolling: nenhum subscriber ativo no canal '{Channel}'. Mensagem não entregue.")]
    public static partial void LogNoActiveSubscriber(this ILogger logger, string channel);

    [LoggerMessage(Level = LogLevel.Error, Message = "LongPolling: falha ao desserializar payload do canal '{Channel}' para o tipo '{Type}'.")]
    public static partial void LogDeserializationFailed(this ILogger logger, string channel, string type, Exception exception);

    [LoggerMessage(Level = LogLevel.Trace, Message = "LongPolling: ignorando falha no unsubscription do canal '{Channel}' para evitar mascarar exceções originais.")]
    public static partial void LogIgnoredUnsubscriptionFailure(this ILogger logger, string channel, Exception exception);
}
