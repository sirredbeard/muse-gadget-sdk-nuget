# Muse Gadget SDK for .NET

.NET libraries for leveraging the [Muse Gadget SDK](https://github.com/facebookincubator/muse-gadget-sdk) on Linux devices.

`Muse.Gadget.Sdk.Linux` lets a .NET 8 or later application use a Muse Gadget SDK that is already installed, configured with an SDK token, paired, and running on the same device.

It does not install the SDK, configure the token, pair the device, read pairing credentials, refresh Muse authentication, or replace the upstream service.

The library writes directly to `/run/musegadget/musegadget.sock`. It does not launch Python or shell out for each message. The installed service remains the single owner of authentication, Bluetooth, pairing, and the live Muse connection.

## Requirements

- .NET 8 or later
- Linux ARM64 or x86_64
- The Muse Gadget SDK installed, configured, paired, and running
- The application account allowed to use `/run/musegadget/musegadget.sock`

## Add to a .NET project

GitHub Packages requires authentication, including for public packages. Create a classic personal access token with `read:packages`, then add the source:

```bash
dotnet nuget add source https://nuget.pkg.github.com/sirredbeard/index.json \
  --name sirredbeard-github \
  --username <github-user> \
  --password <github-token> \
  --store-password-in-clear-text
```

Add the package:

```bash
dotnet add package Muse.Gadget.Sdk.Linux --version 0.1.0 --source sirredbeard-github
```

On Linux, NuGet stores that password in the user's NuGet configuration. Do not place the token in this repository or a project-level `nuget.config`.

## Send data to Muse

Use the main chat for device events:

```csharp
using Muse.Gadget.Sdk.Linux;

var client = new MuseGadgetClient();
MuseGadgetSendResult result = await client.SendMessageAsync(
    "ADS-B reports N12345 within five miles at 2,500 feet.");

if (!result.IsSuccess)
{
    Console.Error.WriteLine($"{result.Status}: {result.Detail}");
}
```

Use a stable session ID to keep one integration in a side chat:

```csharp
MuseGadgetSendResult result = await client.SendMessageAsync(
    "N12345 is now descending through 2,000 feet.",
    "adsb-tracker");
```

Session IDs can contain 1 to 64 letters, digits, or dashes.

## Receive requests from Muse

The upstream service does not expose inbound command registration through its local socket. Muse already has the upstream `system.run` capability, so an application that needs two-way integration should expose a narrow local interface that command can call.

For a command-line application:

```text
/opt/adsb-tracker/AdsBTracker nearby --json
```

For a long-running application, expose a loopback HTTP endpoint, Unix socket, or small companion CLI. Keep it local, authenticate it where appropriate, validate every argument, return machine-readable output, and give the Muse device account only the permissions it needs.

This preserves one authenticated Muse connection in the installed service. Reimplementing the Muse API, tokens, WebSocket, Noise transport, or pairing inside each .NET application would duplicate authentication state and compete with that service.

## Handle failures

Handle `MuseGadgetSendStatus` instead of parsing `Detail`:

- `SdkNotInstalled` when the upstream SDK executable was not found
- `SdkTokenNotConfigured` when the SDK token is definitely missing
- `SdkTokenInvalid` when a readable token has an invalid format
- `ServiceUnavailable` when the local service cannot be reached or times out
- `MuseUnavailable` when the service is running but cannot reach Muse
- `RequestRejected` when the local service rejects the request
- `InvalidResponse` when the local service returns empty, malformed, or oversized data

The upstream installer normally stores the token in a root-only directory. `GetStatusAsync` reports the token as `Unknown` when the application cannot inspect it. Do not treat `Unknown` as missing. Message delivery can still work through the SDK socket, and the token is never returned or sent by this library.

## Install the skill file for GitHub Copilot CLI

Copilot CLI discovers `.github/skills/muse-gadget-sdk-linux/SKILL.md` in a trusted checkout.

Install a personal copy:

```bash
copilot skill add .github/skills/muse-gadget-sdk-linux/SKILL.md
```

Or install it from GitHub:

```bash
copilot skill add https://raw.githubusercontent.com/sirredbeard/muse-gadget-sdk-nuget/main/.github/skills/muse-gadget-sdk-linux/SKILL.md
```

If Copilot CLI is already running, use `/skills reload`, then `/skills info muse-gadget-sdk-linux`.

## Build and publish

The package targets `net8.0` and `net11.0`. It is built with the .NET 11 SDK, current C# conventions, trimming analysis, and Native AOT analysis.

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

The package is architecture-neutral. Publish the application for its device:

```bash
dotnet publish MyApp.csproj \
  --configuration Release \
  --runtime linux-arm64 \
  --self-contained true \
  -p:PublishAot=true

dotnet publish MyApp.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  -p:PublishAot=true
```

The package version follows the upstream Muse Gadget SDK version. GitHub Actions lints the workflow, builds and tests both target frameworks, verifies Native AOT on ARM64 and x86_64, publishes to GitHub Packages, and replaces the matching GitHub release and attached `.nupkg`.

## License

MIT. See [`LICENSE`](LICENSE).
