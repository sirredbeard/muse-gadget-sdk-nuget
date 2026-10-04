---
name: muse-gadget-sdk-linux
description: Use Muse.Gadget.Sdk.Linux from .NET 8 or later applications on Linux devices with the Muse Gadget SDK already installed, configured, paired, and running. Use for local Unix-socket messaging, failure handling, two-way application integration through upstream system.run, GitHub Packages setup, or self-contained Native AOT publishing on ARM64 and x86_64.
license: MIT
---

# Muse Gadget SDK for .NET

Use `Muse.Gadget.Sdk.Linux` when the application runs on the same Linux device as the upstream Muse Gadget SDK.

This does not install the upstream SDK, configure its SDK token, pair the device, expose credentials, or replace the running service.

Read `README.md` before changing this repository.

## Architecture

Use `MuseGadgetClient` to send JSON directly to `/run/musegadget/musegadget.sock`.

The client does not launch Python. The installed service remains responsible for authentication, Bluetooth, pairing, and the live Muse connection.

Do not add a second Muse transport that reads pairing credentials or refreshes Muse tokens. It would compete with the installed service and move authentication into each application.

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

Never put that token in a repository, project file, source file, log, or agent response.

## Send a message

```csharp
using Muse.Gadget.Sdk.Linux;

var client = new MuseGadgetClient();
MuseGadgetSendResult result = await client.SendMessageAsync(
    "The backup failed.",
    "backup-alerts");

if (!result.IsSuccess)
{
    Console.Error.WriteLine($"{result.Status}: {result.Detail}");
}
```

Use a stable session ID for one integration. Session IDs can contain 1 to 64 letters, digits, or dashes.

## Receive requests

The upstream local socket is outbound-only. For two-way integration, expose a narrow CLI, loopback HTTP endpoint, or Unix socket that Muse can call through the upstream `system.run` command.

Validate every argument. Prefer machine-readable output. Keep network listeners bound to loopback or use a local Unix socket. Do not give the Muse device account broad permissions.

## Handle failures

Handle `MuseGadgetSendStatus` instead of parsing `Detail`:

- `SdkNotInstalled` - the upstream SDK executable was not found
- `SdkTokenNotConfigured` - the SDK token is definitely missing
- `SdkTokenInvalid` - a readable SDK token has an invalid format
- `ServiceUnavailable` - the local service cannot be reached or times out
- `MuseUnavailable` - the local service cannot reach Muse
- `RequestRejected` - the local service rejected the request
- `InvalidResponse` - the local service returned empty, malformed, or oversized data

The token is root-owned by default. `GetStatusAsync` can report `Unknown` when the application cannot inspect it. Do not treat `Unknown` as missing.

## Publish

The package targets `net8.0` and `net11.0`, and is built with the .NET 11 SDK. Publish the consuming application for its device.

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
