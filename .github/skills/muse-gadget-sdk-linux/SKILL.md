---
name: muse-gadget-sdk-linux
description: Use Muse.Gadget.Sdk.Linux from .NET 11 applications running on Linux devices with the Muse Linux Device SDK already installed and configured. Use when adding the GitHub Packages source, referencing the NuGet package, sending messages to Muse, handling SDK status results, or publishing a self-contained Native AOT application for Linux ARM64 or x64.
---

# Muse Gadget SDK for .NET

Use `Muse.Gadget.Sdk.Linux` only when the application runs on the same Linux device as the upstream Muse Linux Device SDK.

Do not install the upstream SDK, configure its SDK token, pair the device, or expose the token. Report those requirements when they are missing.

## Add the package

GitHub Packages requires authentication, including for public packages. Use a classic personal access token with `read:packages`.

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

The password is stored in the user's NuGet configuration on Linux. Never place the token in a repository, project file, source file, log, or agent response.

## Send a message

```csharp
using Muse.Gadget.Sdk.Linux;

var client = new MuseGadgetClient();
var result = await client.SendMessageAsync("The garage door has been open for an hour.");

if (!result.IsSuccess)
{
    Console.Error.WriteLine($"{result.Status}: {result.Detail}");
}
```

Use the overload with a session ID for a side chat:

```csharp
var result = await client.SendMessageAsync(
    "The backup failed.",
    "backup-alerts");
```

## Handle failures

Handle the status rather than parsing `Detail`:

- `SdkNotInstalled` - the upstream SDK executable was not found
- `SdkTokenNotConfigured` - the SDK token is definitely missing
- `SdkTokenInvalid` - the configured token has an invalid format
- `ServiceUnavailable` - the local `musegadget` service cannot be reached
- `MuseUnavailable` - the service is running but is not connected to Muse
- `RequestRejected` - the local service rejected the request
- `InvalidResponse` - the local service returned an unreadable response

The token is root-owned by default. `GetStatusAsync` can report its token status as `Unknown` when the application cannot inspect the file. Do not treat `Unknown` as missing, and do not block message delivery solely because of it.

## Publish the application

The NuGet package contains architecture-neutral managed code. Publish the consuming application for its device.

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
