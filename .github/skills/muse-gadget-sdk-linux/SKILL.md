---
name: muse-gadget-sdk-linux
description: Use Muse.Gadget.Sdk.Linux from .NET 8 or later applications on Linux devices with the Muse Gadget SDK already installed, configured, paired, and running. Use for local Unix-socket messaging, status and failure handling, two-way application integration through upstream system.run, GitHub Packages setup, Native AOT publishing, or changes to this repository.
license: MIT
---

# Muse Gadget SDK for .NET

Use `Muse.Gadget.Sdk.Linux` when a .NET application runs on the same Linux device as the upstream Muse Gadget SDK.

This library does not install the upstream SDK, configure its SDK token, pair a device, expose credentials, refresh Muse authentication, or replace the running service.

Before changing this repository:

1. Read `README.md` for the public setup and package instructions.
2. Read `.github/copilot-instructions.md` for the repository architecture, supported behavior, build commands, and release rules.
3. Read the nearest `AGENTS.md` and any more-specific instruction files that apply to the files being changed.
4. Preserve manual documentation edits. Do not move technical detail back into `README.md` when it belongs in the Copilot instructions.

## Architecture

`MuseGadgetClient` writes one bounded JSON request directly to:

```text
/run/musegadget/musegadget.sock
```

The client does not launch Python, invoke a shell, or call the upstream CLI for each message. The Unix socket is the intentional process boundary.

The installed service remains the single owner of:

- SDK-token use and Muse authentication
- Bluetooth
- device pairing
- the live Muse connection

Do not add a second Muse transport that reads `identity.json`, `pairing.json`, or token files. Do not add token refresh, WebSocket, Noise, protobuf, Bluetooth, or pairing code to this library. A second transport would duplicate authentication state and compete with the upstream service.

The local socket is outbound-only. It accepts messages from the application and does not provide inbound command registration.

## Send application data to Muse

Use the main chat for a device event:

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

Use a stable session ID for one integration:

```csharp
MuseGadgetSendResult result = await client.SendMessageAsync(
    "N12345 is now descending through 2,000 feet.",
    "adsb-tracker");
```

Session IDs contain 1 to 64 letters, digits, or dashes. Do not put secrets in messages or session IDs.

`MuseGadgetClientOptions` supports an explicit socket path, state directory, executable probe paths, and request timeout. Use explicit options in tests and non-standard installations. Keep the default paths aligned with the upstream SDK:

- socket: `/run/musegadget/musegadget.sock`
- state directory: `/var/lib/musegadget`

## Receive requests from Muse

The upstream socket does not register application commands. For two-way integration, expose a narrow local interface that the upstream `system.run` capability can call:

- a small application CLI
- a loopback HTTP endpoint
- a local Unix socket

Validate every argument. Prefer machine-readable output. Keep network listeners on loopback. Use a Unix socket where practical. Give the Muse device account only the permissions required for the integration.

For an ADS-B application, a command could look like:

```text
/opt/adsb-tracker/AdsBTracker nearby --json
```

Do not describe this as native command registration by `Muse.Gadget.Sdk.Linux`. It is an application interface called through the upstream SDK's existing command capability.

## Handle status and failures

Handle `MuseGadgetSendStatus` instead of parsing `Detail`:

- `SdkNotInstalled`: the upstream SDK executable was not found
- `SdkTokenNotConfigured`: the SDK token is definitely missing
- `SdkTokenInvalid`: a readable SDK token has an invalid format
- `ServiceUnavailable`: the local service cannot be reached or times out
- `MuseUnavailable`: the service is running but cannot reach Muse
- `RequestRejected`: the local service rejected the request
- `InvalidResponse`: the service returned empty, malformed, or oversized data

`GetStatusAsync` reports installation, token visibility, and local service availability. It cannot prove a live Muse connection without sending a message.

The token is normally root-readable only. An application that cannot inspect it receives `MuseGadgetTokenStatus.Unknown`, not `Missing`. Do not block message delivery solely because status is `Unknown`. Never return, log, or include the token in an exception.

Preserve distinct errors. Do not add broad catches, silent defaults, success-shaped fallbacks, or `Detail` parsing requirements.

## Package use

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

Never put the token in a repository, project file, source file, log, command output, or agent response.

The package is architecture-neutral and targets `net8.0` and `net11.0`. The package is built with the .NET 11 SDK. Consuming applications publish for the target device:

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

## Repository changes

Keep public APIs compatible with .NET 8. Use current C# conventions and preserve nullable annotations.

Keep trimming and Native AOT analyzers enabled. Use source-generated JSON metadata for reachable serialization paths. Keep socket requests and responses bounded. Do not add reflection-heavy serializers or dependencies for behavior already provided by the BCL.

When adding a public API:

1. Add XML documentation.
2. Define input limits and cancellation behavior.
3. Preserve explicit status and exception behavior.
4. Add tests for success, malformed input, timeout, cancellation, and failure classification where applicable.
5. Check both `net8.0` and `net11.0`.

When changing the socket protocol, compare the request and response shape with the upstream SDK before editing. Keep the newline-delimited JSON contract and do not invent an inbound protocol.

## Validation

Use the installed local SDK when available:

```bash
PATH="$PWD/.dotnet:$PATH" dotnet restore Muse.Gadget.Sdk.slnx
PATH="$PWD/.dotnet:$PATH" dotnet build Muse.Gadget.Sdk.slnx --configuration Release --no-restore
PATH="$PWD/.dotnet:$PATH" dotnet test Muse.Gadget.Sdk.slnx --configuration Release --no-build
PATH="$PWD/.dotnet:$PATH" dotnet pack \
  src/Muse.Gadget.Sdk.Linux/Muse.Gadget.Sdk.Linux.csproj \
  --configuration Release \
  --no-build \
  --output artifacts
```

For Native AOT changes, test at least `net8.0/linux-x64` and `net11.0/linux-x64`. GitHub Actions also runs native ARM64 checks.

Lint workflow changes with `actionlint`. The workflow uses Node 24 actions, including `actions/checkout@v7.0.1`.

## Release rules

The package version follows the upstream Muse Gadget SDK version. GitHub Actions verifies the version, builds and tests both target frameworks, runs Native AOT smoke applications on ARM64 and x86_64, removes stale package versions, publishes to GitHub Packages, and replaces the matching GitHub release and `.nupkg`.

Do not manually publish an unverified package. Do not create a second version just to avoid replacing an existing package or release.

Never add co-author metadata to commits. Do not amend commits unless the user explicitly asks for it. Do not revert unrelated work already in the worktree.

## Documentation

Keep `README.md` short and focused on what a consuming .NET application needs to install and use the package.

Put architecture, protocol boundaries, test requirements, release mechanics, and agent instructions in `.github/copilot-instructions.md`, not back into the README.

Keep this skill focused on instructions an agent needs while using or changing the package. When the architecture changes, update the README, Copilot instructions, and this skill together, without copying the entire technical guide into the README.

## License

The project is MIT licensed. Preserve `LICENSE` and the package metadata when changing project files.
