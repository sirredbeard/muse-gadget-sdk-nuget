# Copilot instructions

Read [`README.md`](../README.md) first.

This repository builds `Muse.Gadget.Sdk.Linux`, a trimming-safe and Native AOT-safe library for .NET 10 or later applications using a Muse Gadget SDK already installed, configured, paired, and running on the same Linux device.

## Scope

`MuseGadgetClient` sends messages directly to `/run/musegadget/musegadget.sock`.

The installed service owns Muse authentication, SDK-token use, Bluetooth, pairing, and the live connection. Do not add installation, token configuration, pairing, credential loading, token refresh, or a competing Muse transport.

The library does not launch Python or shell out when it sends a message. The running upstream service is an intentional process boundary, not an application subprocess.

The upstream local socket is outbound-only. Inbound integration uses the upstream `system.run` capability against an application CLI, loopback endpoint, or local Unix socket. Do not claim local command registration that the upstream socket does not provide.

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

## Targets and performance

Target `net10.0` and `net11.0`. Build with the .NET 11 SDK and the current C# language version. Keep public APIs compatible with .NET 10.

The NuGet package contains architecture-neutral IL. Consuming applications can publish self-contained or Native AOT builds for `linux-arm64` and `linux-x64`.

Keep trimming, single-file, and AOT analyzers enabled. Keep the library marked `IsTrimmable` and `IsAotCompatible`. Use source-generated JSON metadata for reachable serialization paths. Keep Unix-socket requests and responses bounded.

The package stays architecture-neutral IL. Do not set `RuntimeIdentifier`, `SelfContained`, `PublishTrimmed`, or `PublishAot` on the library project. Exercise those deployment modes through the AOT smoke application.

## Behavior

Preserve these status distinctions:

- `SdkNotInstalled`
- `SdkTokenNotConfigured`
- `SdkTokenInvalid`
- `ServiceUnavailable`
- `MuseUnavailable`
- `RequestRejected`
- `InvalidResponse`

The SDK token is root-readable by default. An unreadable token is `Unknown`, not necessarily missing. Never return or log the token.

Do not add broad catches, silent defaults, or success-shaped fallbacks.

## Repository changes

Read the root [`AGENTS.md`](../AGENTS.md) before changing the repository. Preserve manual edits and unrelated work already in the worktree.

When adding or changing a public API:

1. Keep it compatible with .NET 10.
2. Add XML documentation.
3. Define input limits, timeout behavior, and cancellation behavior.
4. Preserve explicit status and exception behavior.
5. Add tests for success, malformed input, timeout, cancellation, and failure classification where applicable.
6. Check both `net10.0` and `net11.0`.

When changing the socket protocol, compare the request and response shape with the upstream SDK first. Keep the newline-delimited JSON contract. Do not invent an inbound protocol.

Do not add reflection-heavy serializers or dependencies for behavior already available in the BCL.

## Build and release

Run:

```bash
dotnet restore Muse.Gadget.Sdk.slnx
dotnet build Muse.Gadget.Sdk.slnx --configuration Release --no-restore
dotnet test Muse.Gadget.Sdk.slnx --configuration Release --no-build
dotnet pack src/Muse.Gadget.Sdk.Linux/Muse.Gadget.Sdk.Linux.csproj \
  --configuration Release \
  --no-build \
  --output artifacts
```

GitHub Actions must lint the workflow, build and test both target frameworks, publish Native AOT smoke applications for ARM64 and x86_64, verify the package version matches the upstream SDK, remove stale GitHub Package versions before publishing the current version, and replace the matching GitHub release and attached `.nupkg`.

Use Node 24 actions, including `actions/checkout@v7.0.1`.

Never add co-author metadata to commits.

Do not amend commits unless the user explicitly asks. Do not revert unrelated changes.

## Build and publish

The package targets `net10.0` and `net11.0`. It is built with the .NET 11 SDK, current C# conventions, trimming analysis, single-file analysis, and Native AOT analysis.

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

The package version follows the upstream Muse Gadget SDK version. GitHub Actions lints the workflow, builds and tests both target frameworks, verifies Native AOT on ARM64 and x86_64, replaces the package in GitHub Packages, and replaces the matching GitHub release and attached `.nupkg`.

## Notes

- Keep the SDK token secure and root-readable only.
- Avoid broad exception handling that could mask failures.
- Ensure that all published artifacts are verified for architecture and target framework compatibility.
- Maintain backward compatibility with .NET 10 while using .NET 11 for development and build processes.
- Verify that all GitHub Actions workflows are correctly configured for both target frameworks and architecture-specific builds.
- Regularly review and update the instructions to reflect changes in the build and release process.
- Document any deviations from the standard build and release procedures to ensure consistency and reproducibility.
- Regularly verify that the published artifacts match the expected architecture and target framework specifications.
- Ensure that any changes to the build and release process are reflected in the documentation and instructions.
- Regularly audit the build and release process to identify potential improvements and ensure compliance with best practices.
- Continuously monitor for updates to the .NET SDK and related tools to maintain compatibility and leverage new features.
- Regularly test the build and release process on both target frameworks and architectures to catch potential issues early.
- Maintain a changelog to document all significant changes to the build and release process.
- Regularly review and update the changelog to ensure it accurately reflects the current state of the project.

## Validation details

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

For Native AOT changes, test at least `net10.0/linux-x64` and `net11.0/linux-x64`. GitHub Actions runs native ARM64 checks.

Lint workflow changes with `actionlint`.

## Documentation and license

Keep `README.md` short and focused on package installation and the public project description.

Put architecture, protocol boundaries, repository maintenance, test requirements, release mechanics, and agent instructions in this file.

The skill at [`.github/skills/muse-gadget-sdk-linux/SKILL.md`](skills/muse-gadget-sdk-linux/SKILL.md) is for agents helping .NET applications use the NuGet package. Do not put repository build or release instructions in the skill.

The project is MIT licensed. Preserve `LICENSE` and the package metadata when changing project files.