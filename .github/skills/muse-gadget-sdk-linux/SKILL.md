# Muse Gadget SDK for .NET

Use this when an app is running on a Linux device that already has the Muse Linux Device SDK installed and configured.

## What this package does

`Muse.Gadget.Sdk.Linux` wraps the local `musegadget` socket and reports the device state without installing the SDK or changing its configuration.

## The important checks

1. Make sure the upstream SDK is installed.
2. Make sure the SDK token is configured.
3. Make sure the socket is reachable.
4. Make sure the service is connected to the Muse app.

If any of those fail, return that fact plainly and do not try to fix the machine.

## Add the package

```bash
dotnet nuget add source https://nuget.pkg.github.com/sirredbeard/index.json \
  --name sirredbeard-github \
  --username <github-user> \
  --password <github-token>

dotnet add package Muse.Gadget.Sdk.Linux --version 0.1.0 --source sirredbeard-github
```

## Use it in code

```csharp
using Muse.Gadget.Sdk.Linux;

var client = new MuseGadgetClient();
var result = await client.SendMessageAsync("The garage door has been open for an hour.");

if (!result.IsSuccess)
{
    Console.Error.WriteLine($"{result.Status}: {result.Detail}");
}
```

## Report the exact status

Prefer the enum values and details over generic error strings:

- `SdkNotInstalled`
- `SdkTokenNotConfigured`
- `SdkTokenInvalid`
- `ServiceUnavailable`
- `MuseUnavailable`

Do not leak the SDK token. Do not read it from disk unless the process can inspect the root-owned file and you intend to validate only the format. The library does not return it.

## AOT and self-contained usage

Use the package in an app that is already built for Linux:

```bash
dotnet publish MyApp.csproj -r linux-arm64 --self-contained true -p:PublishAot=true
```

The library is not a native binary. It works with native AOT and self-contained deployment when the app is compiled for `linux-arm64` or `linux-x64`.
