namespace Muse.Gadget.Sdk.Linux;

/// <summary>Describes whether the Muse Linux Device SDK is installed.</summary>
public enum MuseGadgetInstallationStatus
{
    /// <summary>The SDK executable was not found.</summary>
    NotInstalled,

    /// <summary>The SDK executable was found.</summary>
    Installed,
}

/// <summary>Describes the SDK token configuration visible to the application.</summary>
public enum MuseGadgetTokenStatus
{
    /// <summary>No SDK token was configured.</summary>
    Missing,

    /// <summary>A valid SDK token was configured.</summary>
    Configured,

    /// <summary>The configured SDK token has an invalid format.</summary>
    Invalid,

    /// <summary>The root-owned SDK token could not be inspected.</summary>
    Unknown,
}

/// <summary>Describes whether the local Muse Gadget service can be reached.</summary>
public enum MuseGadgetServiceStatus
{
    /// <summary>The local SDK service could not be reached.</summary>
    Unavailable,

    /// <summary>The local SDK service accepted a socket connection.</summary>
    Available,
}

/// <summary>Reports the local SDK state without changing it.</summary>
public sealed record MuseGadgetStatus(
    MuseGadgetInstallationStatus Installation,
    MuseGadgetTokenStatus Token,
    MuseGadgetServiceStatus Service,
    string? Detail = null);

/// <summary>Describes the outcome of sending a message through the SDK.</summary>
public enum MuseGadgetSendStatus
{
    /// <summary>The Muse accepted the message.</summary>
    Sent,

    /// <summary>The Muse Linux Device SDK was not found.</summary>
    SdkNotInstalled,

    /// <summary>No SDK token was configured.</summary>
    SdkTokenNotConfigured,

    /// <summary>The configured SDK token has an invalid format.</summary>
    SdkTokenInvalid,

    /// <summary>The local SDK service could not be reached.</summary>
    ServiceUnavailable,

    /// <summary>The SDK service is running but is not connected to the Muse.</summary>
    MuseUnavailable,

    /// <summary>The local SDK service rejected the request.</summary>
    RequestRejected,

    /// <summary>The local SDK service returned an invalid response.</summary>
    InvalidResponse,
}

/// <summary>Reports the outcome of a message sent through the SDK.</summary>
public sealed record MuseGadgetSendResult(
    MuseGadgetSendStatus Status,
    string? Detail = null)
{
    /// <summary>Gets whether the Muse accepted the message.</summary>
    public bool IsSuccess => Status == MuseGadgetSendStatus.Sent;
}
