# Muse Gadget SDK for .NET

.NET 11 libraries for building Linux devices with the [Muse Gadget SDK](https://github.com/facebookincubator/muse-gadget-sdk).

`Muse.Gadget.Sdk.Linux` lets an application use a Muse Linux Device SDK that is already installed, configured with an SDK token, paired, and running on the same device.

It does not install the SDK, configure the token, or pair the device.

## Requirements

- .NET 11
- The Muse Linux Device SDK installed on the same Linux device
- An SDK token configured by the upstream installer
- The application account allowed to use `/run/musegadget/musegadget.sock`

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

## Build

```console
dotnet build
dotnet test
```

## License

No license has been selected yet.
