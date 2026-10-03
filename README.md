# Muse Gadget SDK for .NET

.NET 11 libraries for building Linux devices with the [Muse Gadget SDK](https://github.com/facebookincubator/muse-gadget-sdk).

`Muse.Gadget.Sdk.Linux` lets an application use a Muse Linux Device SDK that is already installed, configured with an SDK token, paired, and running on the same device.

It does not install the SDK, configure the token, or pair the device.

## Requirements

- .NET 11
- The Muse Linux Device SDK installed on the same Linux device
- An SDK token configured by the upstream installer
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

Then add the package:

```bash
dotnet add package Muse.Gadget.Sdk.Linux --version 0.1.0 --source sirredbeard-github
```

On Linux, NuGet stores that password in the user's NuGet configuration. Do not place the token in this repository or a project-level `nuget.config`.

## Use

```csharp
using Muse.Gadget.Sdk.Linux;

var client = new MuseGadgetClient();
var result = await client.SendMessageAsync("The garage door has been open for an hour.");

if (!result.IsSuccess)
{
    Console.Error.WriteLine($"{result.Status}: {result.Detail}");
}
```

`SendMessageAsync` reports:

- `SdkNotInstalled` when the upstream SDK is not installed
- `SdkTokenNotConfigured` when the SDK token is definitely missing
- `SdkTokenInvalid` when the configured token has an invalid format
- `ServiceUnavailable` when the local SDK service cannot be reached
- `MuseUnavailable` when the service is running but cannot reach the Muse

The upstream installer stores the token in a root-only directory. `GetStatusAsync` reports the token as `Unknown` when the application cannot inspect it, but message delivery still works through the SDK socket. The token is never returned by this library or sent by the application.

## Install the skill file for GitHub Copilot CLI

The repo includes `.github/skills/muse-gadget-sdk-linux/SKILL.md`. Copilot CLI discovers it automatically when you work in a trusted checkout of this repository.

To install a personal copy from a checkout:

```bash
copilot skill add .github/skills/muse-gadget-sdk-linux/SKILL.md
```

Or install it directly from GitHub:

```bash
copilot skill add https://raw.githubusercontent.com/sirredbeard/muse-gadget-sdk-nuget/main/.github/skills/muse-gadget-sdk-linux/SKILL.md
```

If Copilot CLI is already running, use `/skills reload`, then `/skills info muse-gadget-sdk-linux`.

## Publish and build

```console
dotnet restore
dotnet build
dotnet test
dotnet pack src/Muse.Gadget.Sdk.Linux/Muse.Gadget.Sdk.Linux.csproj --output artifacts
```

The package is architecture-neutral. Publish the application for the device, primarily `linux-arm64` or otherwise `linux-x64`:

```bash
dotnet publish MyApp.csproj -r linux-arm64 --self-contained true -p:PublishAot=true

dotnet publish MyApp.csproj -r linux-x64 --self-contained true -p:PublishAot=true
```

The workflow builds and tests the package once, lints the GitHub Actions file, publishes Native AOT smoke applications on native ARM64 and x64 runners, and then pushes the tested NuGet package to this repository's GitHub Packages feed. Package version automation and nuget.org publishing are intentionally not included yet.

## License

MIT. See [`LICENSE`](LICENSE).
