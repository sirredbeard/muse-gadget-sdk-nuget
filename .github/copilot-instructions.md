# Copilot instructions

Read [`README.md`](../README.md) first.

This repository builds `Muse.Gadget.Sdk.Linux`, a trimming-safe and Native AOT-safe library for .NET 8 or later applications using a Muse Gadget SDK already installed, configured, paired, and running on the same Linux device.

## Scope

`MuseGadgetClient` sends messages directly to `/run/musegadget/musegadget.sock`.

The installed service owns Muse authentication, SDK-token use, Bluetooth, pairing, and the live connection. Do not add installation, token configuration, pairing, credential loading, token refresh, or a competing Muse transport.

The library does not launch Python or shell out when it sends a message. The running upstream service is an intentional process boundary, not an application subprocess.

The upstream local socket is outbound-only. Inbound integration uses the upstream `system.run` capability against an application CLI, loopback endpoint, or local Unix socket. Do not claim local command registration that the upstream socket does not provide.

## Targets and performance

Target `net8.0` and `net11.0`. Build with the .NET 11 SDK and the current C# language version. Keep public APIs compatible with .NET 8.

The NuGet package contains architecture-neutral IL. Consuming applications can publish self-contained or Native AOT builds for `linux-arm64` and `linux-x64`.

Keep trimming and AOT analyzers enabled. Use source-generated JSON metadata for reachable serialization paths. Keep Unix-socket requests and responses bounded.

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
