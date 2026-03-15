# MVFC.LongPolling

> 🇧🇷 [Leia em Português](README.pt-BR.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.LongPolling/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.LongPolling/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.LongPolling/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.LongPolling)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)
![NuGet Version](https://img.shields.io/nuget/v/MVFC.LongPolling)
![NuGet Downloads](https://img.shields.io/nuget/dt/MVFC.LongPolling)

A lightweight and efficient **long polling via Redis Pub/Sub** library for .NET,
with configurable `CancellationToken` support and typed payload.

## Objective

Allow HTTP clients to await asynchronous results (jobs, webhooks,
processing) in a simple way, without blind polling or WebSockets, using the
Redis Pub/Sub channel as the notification mechanism.

---

## Features

- **Redis Pub/Sub**: Real subscription per channel, no key polling.
- **Configurable timeout**: Maximum wait time globally or per call via `LongPollingOptions`.
- **CancellationToken**: Automatically respects HTTP client disconnection.
- **Typed payload**: `WaitAsync<T>` deserializes the result directly via `System.Text.Json`.
- **Confirmed delivery**: `PublishAsync` returns `false` if no subscriber was active.
- **Deterministic synchronization**: `WaitUntilReadyAsync` guarantees the subscription is active before publishing.
- **Fluent configuration**: Simple setup in `Program.cs`.
- **Channel prefix**: Environment isolation via `KeyPrefix`.

---

## Packages

| Package | Downloads |
|---|---|
| [MVFC.LongPolling](src/MVFC.LongPolling/README.md) | ![Downloads](https://img.shields.io/nuget/dt/MVFC.LongPolling) |

***

## Installation

```bash
dotnet add package MVFC.LongPolling
```

---

## Configuration

### Basic

```csharp
builder.Services.AddLongPolling("localhost:6379", cfg =>
{
    cfg.DefaultTimeout = TimeSpan.FromSeconds(30);
    cfg.KeyPrefix = "poll";
});
```

### With existing `IConnectionMultiplexer`

```csharp
builder.Services.AddLongPolling(existingMultiplexer, cfg =>
{
    cfg.DefaultTimeout = TimeSpan.FromSeconds(20);
});
```

---

## Usage

### Await result (string)

```csharp
app.MapGet("/poll/{jobId}", async (
    string jobId,
    ILongPollingService polling,
    CancellationToken ct) =>
{
    var result = await polling.WaitAsync(jobId, cancellationToken: ct);

    return result is null
        ? Results.NoContent()
        : Results.Ok(result);
});
```

### Await typed result

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

### Notify job completion

```csharp
app.MapPost("/notify/{jobId}", async (
    string jobId,
    NotifyRequest req,
    ILongPollingService polling) =>
{
    var delivered = await polling.PublishAsync(jobId, req.Payload);

    return delivered
        ? Results.Accepted()
        : Results.NotFound($"No active subscriber for channel '{jobId}'.");
});
```

### Per-call options

```csharp
var options = new LongPollingOptions(
    Timeout: TimeSpan.FromSeconds(10),
    KeyPrefix: "custom");

var result = await polling.WaitAsync(jobId, options, cancellationToken: ct);
```

---

## Configuration Parameters

| Parameter        | Type       | Default       | Description                      |
|:-----------------|:-----------|:--------------|:---------------------------------|
| `DefaultTimeout` | `TimeSpan` | `30 seconds`  | Maximum wait time per message    |
| `KeyPrefix`      | `string`   | `longpolling` | Redis channel prefix             |

---

## How It Works

```
HTTP Client           Server                       Redis
───────────           ──────                       ─────
GET /poll/{id} ──►   WaitAsync()
                      SubscribeAsync(channel) ──►  SUBSCRIBE poll:id
                      WaitUntilReadyAsync()   ◄──  (confirmed)
                                              ◄──  Worker: PublishAsync()
                      message received        ◄──  PUBLISH poll:id payload
GET returns    ◄──   Results.Ok(payload)
```

## Production Flow Example — Payment Scenario

### Default flow — message received

```
HTTP Client        Orders API             Payments API
───────────        ──────────             ────────────
POST /orders ──►  Creates order
                   WaitAsync(orderId) ──► Processes payment
                                          PublishAsync(orderId, "approved")
                   ◄── "approved"
200 OK       ◄──  Results.Ok(status)
```

### Timeout — no payment response

```
HTTP Client        Orders API             Payments API
───────────        ──────────             ────────────
POST /orders ──►  Creates order
                   WaitAsync(orderId) ──► Processes payment
                   (waiting...)           (no PublishAsync)
                   timeout reached
504 GW Timeout ◄── Results.StatusCode(504)
```

### Payment declined

```
HTTP Client        Orders API             Payments API
───────────        ──────────             ────────────
POST /orders ──►  Creates order
                   WaitAsync(orderId) ──► Processes payment
                                          PublishAsync(orderId, "rejected")
                   ◄── "rejected"
422 Unprocessable ◄── Results.UnprocessableEntity(status)
```

### Client disconnects before response

```
HTTP Client        Orders API             Payments API
───────────        ──────────             ────────────
POST /orders ──►  Creates order
                   WaitAsync(orderId) ──► Processes payment
✗ disconnects
                   CancellationToken cancelled
                   WaitAsync throws OperationCanceledException
                   (response discarded)
```

### Publish with no active subscriber

```
HTTP Client        Orders API             Payments API
───────────        ──────────             ────────────
                                          PublishAsync(orderId, "approved")
                                          delivered = false
                   (no active WaitAsync for orderId)
404 Not Found  ◄── Results.NotFound(...)
```

---

## Project Structure

- **[src](src/)**: Source code for the `MVFC.LongPolling` library.
- **[playground](playground/)**: Sample API to validate behavior with Aspire.
- **[tests](tests/)**: Integration tests with Aspire + Redis.

---

## Requirements
.NET 9.0+

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[Apache-2.0](LICENSE)
