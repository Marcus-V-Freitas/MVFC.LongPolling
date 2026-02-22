# MVFC.LongPolling

Uma biblioteca leve e eficiente de **long polling via Redis Pub/Sub** para .NET,
com suporte a `CancellationToken` configurável e payload tipado.

## Objetivo

Permitir que clientes HTTP aguardem resultados assíncronos (jobs, webhooks,
processamentos) de forma simples, sem polling cego ou WebSockets, usando o
canal Pub/Sub do Redis como mecanismo de notificação.

---

## Funcionalidades

- **Redis Pub/Sub**: Subscription real por canal, sem polling de chave.
- **Timeout configurável**: Tempo máximo de espera global ou por chamada via `LongPollingOptions`.
- **CancellationToken**: Respeita desconexão do cliente HTTP automaticamente.
- **Payload tipado**: `WaitAsync<T>` desserializa o resultado diretamente via `System.Text.Json`.
- **Entrega confirmada**: `PublishAsync` retorna `false` se nenhum subscriber estava ativo.
- **Sincronização determinística**: `WaitUntilReadyAsync` garante que a subscription está ativa antes de publicar.
- **Configuração Fluente**: Setup simples no `Program.cs`.
- **Prefixo de canal**: Isolamento de ambientes via `KeyPrefix`.

---

## Instalação

```bash
dotnet add package MVFC.LongPolling
```

---

## Configuração

### Básica

```csharp
builder.Services.AddLongPolling("localhost:6379", cfg =>
{
    cfg.DefaultTimeout = TimeSpan.FromSeconds(30);
    cfg.KeyPrefix = "poll";
});
```

### Com `IConnectionMultiplexer` existente

```csharp
builder.Services.AddLongPolling(existingMultiplexer, cfg =>
{
    cfg.DefaultTimeout = TimeSpan.FromSeconds(20);
});
```

---

## Uso

### Aguardar resultado (string)

```csharp
app.MapGet("/poll/{jobId}", async (
    string jobId,
    ILongPollingService polling,
    CancellationToken ct) =>
{
    var result = await polling.WaitAsync(jobId, cancellationToken: ct);

    return result is null
        ? Results.NoContent()       // timeout
        : Results.Ok(result);
});
```

### Aguardar resultado tipado

```csharp
app.MapGet("/poll/{jobId}/typed", async (
    string jobId,
    ILongPollingService polling,
    CancellationToken ct) =>
{
    var result = await polling.WaitAsync<OrderCompletedEvent>(jobId, cancellationToken: ct);

    return result is null
        ? Results.NoContent()
        : Results.Ok(result);
});
```

### Notificar conclusão de job

```csharp
app.MapPost("/notify/{jobId}", async (
    string jobId,
    NotifyRequest req,
    ILongPollingService polling) =>
{
    var delivered = await polling.PublishAsync(jobId, req.Payload);

    return delivered
        ? Results.Accepted()
        : Results.NotFound($"Nenhum subscriber ativo para o canal '{jobId}'.");
});
```

### Opções por chamada

```csharp
var options = new LongPollingOptions(
    Timeout: TimeSpan.FromSeconds(10),
    KeyPrefix: "custom");

var result = await polling.WaitAsync(jobId, options, cancellationToken: ct);
```

---

## Parâmetros de Configuração

| Parâmetro        | Tipo       | Padrão        | Descrição                            |
|:-----------------|:-----------|:--------------|:-------------------------------------|
| `DefaultTimeout` | `TimeSpan` | `30 segundos` | Tempo máximo de espera por mensagem  |
| `KeyPrefix`      | `string`   | `longpolling` | Prefixo do canal Redis               |

---

## Fluxo de funcionamento

```
Cliente HTTP          Servidor                     Redis
────────────          ────────                     ─────
GET /poll/{id} ──►   WaitAsync()
                      SubscribeAsync(canal) ──►   SUBSCRIBE poll:id
                      WaitUntilReadyAsync() ◄──   (confirmado)
                                            ◄──   Worker: PublishAsync()
                      mensagem recebida     ◄──   PUBLISH poll:id payload
GET retorna    ◄──   Results.Ok(payload)
```

## Exemplo de Fluxo de funcionamento de cenário em produção para pagamento

### Fluxo padrão — mensagem recebida

```
Cliente HTTP       API Pedidos            API Pagamentos
────────────       ───────────            ──────────────
POST /orders ──►  Cria pedido
                   WaitAsync(orderId) ──► Processa pagamento
                                          PublishAsync(orderId, "approved")
                   ◄── "approved"
200 OK       ◄──  Results.Ok(status)
```

### Timeout — sem resposta do pagamento

```
Cliente HTTP       API Pedidos            API Pagamentos
────────────       ───────────            ──────────────
POST /orders ──►  Cria pedido
                   WaitAsync(orderId) ──► Processa pagamento
                   (aguarda...)           (sem PublishAsync)
                   timeout atingido
504 GW Timeout ◄── Results.StatusCode(504)
```

### Pagamento recusado

```
Cliente HTTP       API Pedidos            API Pagamentos
────────────       ───────────            ──────────────
POST /orders ──►  Cria pedido
                   WaitAsync(orderId) ──► Processa pagamento
                                          PublishAsync(orderId, "rejected")
                   ◄── "rejected"
422 Unprocessable ◄── Results.UnprocessableEntity(status)
```

### Cliente desconecta antes da resposta

```
Cliente HTTP       API Pedidos            API Pagamentos
────────────       ───────────            ──────────────
POST /orders ──►  Cria pedido
                   WaitAsync(orderId) ──► Processa pagamento
✗ desconecta
                   CancellationToken cancelado
                   WaitAsync lança OperationCanceledException
                   (resposta descartada)
```

### Publish sem subscriber ativo

```
Cliente HTTP       API Pedidos            API Pagamentos
────────────       ───────────            ──────────────
                                          PublishAsync(orderId, "approved")
                                          delivered = false
                   (nenhum WaitAsync ativo para orderId)
404 Not Found  ◄── Results.NotFound(...)
```

---

## Estrutura do Projeto

- **[src](src/)**: Código-fonte da biblioteca `MVFC.LongPolling`.
- **[playground](playground/)**: API de exemplo para validar o comportamento com Aspire.
- **[tests](tests/)**: Testes de integração com Aspire + Redis.

---

## Licença

Apache License 2.0. Consulte o arquivo [LICENSE](LICENSE).