---
name: muse-gadget-sdk-linux
description: Use the Muse.Gadget.Sdk.Linux NuGet package from .NET 10 or later applications on Linux devices with the Muse Gadget SDK already installed, configured, paired, and running. Covers package setup, client options, SDK status checks, main and side-chat messages, result handling, cancellation, two-way Muse integration, security, and Native AOT deployment on ARM64 and x86_64.
license: MIT
---

# Muse Gadget SDK for .NET applications

Use `Muse.Gadget.Sdk.Linux` when a .NET application runs on the same Linux device as the upstream Muse Gadget SDK.

The package communicates with the running SDK service through `/run/musegadget/musegadget.sock`. It does not start Python, shell out to the SDK, install the SDK, configure it's token, pair a device, or replace the service.

The installed service remains responsible for Muse authentication, Bluetooth, pairing, and the live Muse connection.

## Requirements

- .NET 10 or later
- Linux ARM64 or x86_64
- The upstream Muse Gadget SDK installed, configured, paired, and running
- Permission for the application account to connect to the SDK Unix socket

The package targets `net10.0` and `net11.0`. It supports framework-dependent, self-contained, trimmed, and Native AOT applications.

## Add the package

GitHub Packages requires a classic personal access token with `read:packages`:

```bash
dotnet nuget add source https://nuget.pkg.github.com/sirredbeard/index.json \
  --name sirredbeard-github \
  --username <github-user> \
  --password <github-token> \
  --store-password-in-clear-text

dotnet add package Muse.Gadget.Sdk.Linux \
  --version 0.1.0 \
  --source sirredbeard-github
```

Never put the GitHub token in a repository, project file, source file, log, command output, or agent response.

## Create a client

The default client uses the standard upstream paths:

```csharp
using Muse.Gadget.Sdk.Linux;

var client = new MuseGadgetClient();
```

Defaults:

- Socket: `/run/musegadget/musegadget.sock`
- State directory: `/var/lib/musegadget`
- Request timeout: 90 seconds
- SDK executable probes: `/usr/local/bin/musegadget` and `/opt/musegadget/venv/bin/musegadget`

Environment overrides:

- `MUSEGADGET_SOCKET`
- `MUSEGADGET_STATE_DIR`
- `MUSEGADGET_SDK_TOKEN`

Use `MuseGadgetClientOptions` for a non-standard installation or a shorter application timeout:

```csharp
var options = new MuseGadgetClientOptions
{
    SocketPath = "/run/musegadget/musegadget.sock",
    StateDirectory = "/var/lib/musegadget",
    RequestTimeout = TimeSpan.FromSeconds(15),
};

options.ExecutablePaths.Clear();
options.ExecutablePaths.Add("/opt/musegadget/venv/bin/musegadget");

var client = new MuseGadgetClient(options);
```

Configure options before creating the client. Do not change the options while requests are running.

## Check the local SDK

`GetStatusAsync` checks:

- whether a known SDK executable exists
- whether the SDK token is visibly configured and valid
- whether the local service socket accepts a connection

```csharp
MuseGadgetStatus status = await client.GetStatusAsync(cancellationToken);

Console.WriteLine($"Installation: {status.Installation}");
Console.WriteLine($"Token: {status.Token}");
Console.WriteLine($"Service: {status.Service}");

if (status.Detail is not null)
{
    Console.Error.WriteLine(status.Detail);
}
```

`MuseGadgetInstallationStatus`:

- `Installed`
- `NotInstalled`

`MuseGadgetTokenStatus`:

- `Configured`
- `Missing`
- `Invalid`
- `Unknown`

`MuseGadgetServiceStatus`:

- `Available`
- `Unavailable`

The upstream installer normally keeps the token in a root-only directory. `Unknown` means the application cannot inspect the token, not that the token is missing. Do not block message delivery solely because token status is `Unknown`.

`GetStatusAsync` does not send a Muse message. It can confirm the local socket is available, but it cannot prove that the service currently has a live Muse connection.

## Send a message to the main Muse chat

Use the main chat for device events, alerts, and state changes:

```csharp
MuseGadgetSendResult result = await client.SendMessageAsync(
    "ADS-B reports N12345 within five miles at 2,500 feet.",
    cancellationToken);

if (!result.IsSuccess)
{
    Console.Error.WriteLine($"{result.Status}: {result.Detail}");
}
```

The package reports whether the service accepted the message. It does not return Muse's conversational reply.

## Send a message to a side chat

Use a stable session ID to keep one integration in it's own side chat:

```csharp
MuseGadgetSendResult result = await client.SendMessageAsync(
    "N12345 is now descending through 2,000 feet.",
    "adsb-tracker",
    cancellationToken);
```

A session ID contains 1 to 64 letters, digits, or dashes:

```text
adsb-tracker
backup-alerts
weather-station-1
```

Use one stable ID per integration or conversation. Do not put secrets or user data in a session ID.

## Handle send results

Use `MuseGadgetSendStatus`. Do not parse `Detail` to determine behavior.

- `Sent`: the local SDK service accepted the message
- `SdkNotInstalled`: the SDK executable was not found
- `SdkTokenNotConfigured`: the token is definitely missing
- `SdkTokenInvalid`: a readable token has an invalid format
- `ServiceUnavailable`: the local service cannot be reached or timed out
- `MuseUnavailable`: the service is running but is not connected to Muse
- `RequestRejected`: the service rejected the request
- `InvalidResponse`: the service returned empty, malformed, unrecognized, or oversized data

Example:

```csharp
switch (result.Status)
{
    case MuseGadgetSendStatus.Sent:
        break;

    case MuseGadgetSendStatus.MuseUnavailable:
    case MuseGadgetSendStatus.ServiceUnavailable:
        Console.Error.WriteLine("Muse is temporarily unavailable.");
        break;

    case MuseGadgetSendStatus.SdkNotInstalled:
    case MuseGadgetSendStatus.SdkTokenNotConfigured:
    case MuseGadgetSendStatus.SdkTokenInvalid:
        Console.Error.WriteLine(
            $"The device needs Muse Gadget SDK attention: {result.Detail}");
        break;

    default:
        Console.Error.WriteLine($"{result.Status}: {result.Detail}");
        break;
}
```

Applications decide whether and when to retry. The client sends one request and does not run a background retry loop.

## Cancellation, timeouts, and limits

- Empty or whitespace-only messages throw `ArgumentException`.
- Invalid session IDs throw `ArgumentException`.
- A serialized request, including its newline delimiter, cannot exceed 64 KiB.
- A service response cannot exceed 64 KiB.
- Caller cancellation propagates as `OperationCanceledException`.
- Expiration of `RequestTimeout` returns `ServiceUnavailable`.
- Socket and stream failures return `ServiceUnavailable`.
- Malformed JSON returns `InvalidResponse`.

Pass the application's cancellation token to status and send operations:

```csharp
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

MuseGadgetSendResult result = await client.SendMessageAsync(
    "The backup completed.",
    "backup-alerts",
    timeout.Token);
```

## Build a two-way Muse-aware application

Messages from the application to Muse use `MuseGadgetClient`.

The upstream local socket is outbound-only. It does not register .NET callbacks or application commands. For requests from Muse to the application, expose a narrow local interface that Muse can call through the upstream SDK's existing `system.run` capability.

Good interfaces:

- a small command-line mode in the application
- a companion CLI for a long-running service
- a loopback HTTP endpoint
- a local Unix socket

For an ADS-B application:

```text
/opt/adsb-tracker/AdsBTracker nearby --json
```

Return machine-readable output, validate every argument, bind network endpoints to loopback, and give the Muse device account only the permissions it needs.

This is two-way application integration through the installed SDK. It is not native command registration by this NuGet package.

## Common application patterns

Event publisher:

1. Watch local hardware or a service.
2. Send meaningful state changes to the main chat or a stable side chat.
3. Handle `MuseUnavailable` and `ServiceUnavailable` without losing the local workload.
4. Retry with application-specific backoff if the event still matters.

On-demand device query:

1. Expose a small CLI or local endpoint.
2. Let Muse call it through `system.run`.
3. Return bounded JSON or concise text.
4. Use `MuseGadgetClient` when the application needs to publish a later update.

Long-running monitor:

1. Reuse one `MuseGadgetClient`.
2. Use one stable session ID.
3. Send changes, alerts, and summaries instead of every raw sample.
4. Keep the device workload running when Muse is offline.

## Security

- Do not read or copy pairing credentials.
- Do not return or log the SDK token.
- Prefer the root-owned token file used by the upstream installer.
- Use `MUSEGADGET_SDK_TOKEN` only when the deployment already manages environment secrets safely.
- Treat message contents as data sent to Muse.
- Validate inbound CLI or endpoint arguments as untrusted input.
- Keep inbound interfaces local and narrowly permissioned.

## Publish the application

ARM64:

```bash
dotnet publish MyApp.csproj \
  --configuration Release \
  --runtime linux-arm64 \
  --self-contained true \
  -p:PublishAot=true
```

x86_64:

```bash
dotnet publish MyApp.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  -p:PublishAot=true
```

The NuGet package contains architecture-neutral managed code. The consuming application selects `linux-arm64` or `linux-x64` when it publishes.
