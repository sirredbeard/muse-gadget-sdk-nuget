# Muse Gadget SDK for .NET

.NET libraries for leveraging the [Muse Gadget SDK](https://github.com/facebookincubator/muse-gadget-sdk) on Linux devices.

`Muse.Gadget.Sdk.Linux` lets a .NET application use a Muse Gadget SDK that is already installed, configured with an SDK token, paired, and running on the same device.

It does not install the SDK, configure the token, pair the device, read pairing credentials, refresh Muse authentication, or replace the upstream service.

The library writes directly to `/run/musegadget/musegadget.sock`. It does not launch Python or shell out for each message. The installed service remains the single owner of authentication, Bluetooth, pairing, and the live Muse connection.

## Requirements

- .NET 8+
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

## Install the skill file in GitHub Copilot CLI

```bash
copilot skill add https://raw.githubusercontent.com/sirredbeard/muse-gadget-sdk-nuget/main/.github/skills/muse-gadget-sdk-linux/SKILL.md
```

## License

[`MIT`](LICENSE)
