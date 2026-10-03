namespace Muse.Gadget.Sdk.Linux;

/// <summary>Configures access to an installed Muse Linux Device SDK.</summary>
public sealed class MuseGadgetClientOptions
{
    /// <summary>Gets or sets the SDK Unix socket path.</summary>
    public string SocketPath { get; set; } =
        Environment.GetEnvironmentVariable("MUSEGADGET_SOCKET")
        ?? "/run/musegadget/musegadget.sock";

    /// <summary>Gets or sets the directory containing the SDK token file.</summary>
    public string StateDirectory { get; set; } =
        Environment.GetEnvironmentVariable("MUSEGADGET_STATE_DIR")
        ?? "/var/lib/musegadget";

    /// <summary>Gets the executable paths used to detect an SDK installation.</summary>
    public IList<string> ExecutablePaths { get; } =
    [
        "/usr/local/bin/musegadget",
        "/opt/musegadget/venv/bin/musegadget",
    ];

    /// <summary>Gets or sets the timeout for a local SDK request.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(90);
}
