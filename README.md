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

Add the GitHub Packages source first:

```bash
dotnet nuget add source https://nuget.pkg.github.com/sirredbeard/index.json \
  --name sirredbeard-github \
  --username sirredbeard \
  --password <github-token>
```

Then add the package:

```bash
dotnet add package Muse.Gadget.Sdk.Linux --version 0.1.0 --source sirredbeard-github
```

If you want it in `nuget.config` instead:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="sirredbeard-github" value="https://nuget.pkg.github.com/sirredbeard/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <sirredbeard-github>
      <add key="Username" value="sirredbeard" />
      <add key="ClearTextPassword" value="<github-token>" />
    </sirredbeard-github>
  </packageSourceCredentials>
</configuration>
```

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

The repo includes a local skill at `.github/skills/muse-gadget-sdk-linux/SKILL.md`.

In GitHub Copilot CLI:

1. run `/skills`
2. choose "install skill from local folder"
3. point it at `.github/skills/muse-gadget-sdk-linux`

The skill is then available to the CLI for agent work on this package.

## Publish and build

```console
dotnet restore
dotnet build
dotnet test
dotnet pack src/Muse.Gadget.Sdk.Linux/Muse.Gadget.Sdk.Linux.csproj --output artifacts
```

For ARM64 or x86_64 self-contained AOT builds, publish the app rather than the package:

```bash
dotnet publish MyApp.csproj -r linux-arm64 --self-contained true -p:PublishAot=true

dotnet publish MyApp.csproj -r linux-x64 --self-contained true -p:PublishAot=true
```

The workflow in `.github/workflows/build-and-publish.yml` builds the package, tests it, and pushes it to the GitHub Packages feed for this repo.

## License

No license has been selected yet.
