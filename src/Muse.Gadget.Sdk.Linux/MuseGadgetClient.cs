using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Muse.Gadget.Sdk.Linux;

/// <summary>
/// Sends messages through an installed and configured Muse Linux Device SDK.
/// </summary>
public sealed partial class MuseGadgetClient
{
    private const int MaxRequestBytes = 64 * 1024;
    private readonly MuseGadgetClientOptions _options;

    /// <summary>Creates a client with the default SDK paths.</summary>
    public MuseGadgetClient()
        : this(new MuseGadgetClientOptions())
    {
    }

    /// <summary>Creates a client with explicit SDK paths and timeout settings.</summary>
    public MuseGadgetClient(MuseGadgetClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.SocketPath))
        {
            throw new ArgumentException("The socket path cannot be empty.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.StateDirectory))
        {
            throw new ArgumentException("The state directory cannot be empty.", nameof(options));
        }

        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The request timeout must be greater than zero.");
        }

        _options = options;
    }

    /// <summary>
    /// Inspects the local SDK installation, token configuration and service socket.
    /// </summary>
    /// <remarks>
    /// The SDK does not expose a side-effect-free Muse connection probe. A live Muse
    /// connection is therefore confirmed when
    /// <see cref="SendMessageAsync(string, CancellationToken)"/> succeeds.
    /// </remarks>
    public async Task<MuseGadgetStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsLinux() || !IsSdkInstalled())
        {
            return new(
                MuseGadgetInstallationStatus.NotInstalled,
                MuseGadgetTokenStatus.Unknown,
                MuseGadgetServiceStatus.Unavailable,
                "The Muse Linux Device SDK was not found.");
        }

        var (token, tokenDetail) = InspectToken();
        var service = await CanConnectToServiceAsync(cancellationToken).ConfigureAwait(false);

        return new(
            MuseGadgetInstallationStatus.Installed,
            token,
            service
                ? MuseGadgetServiceStatus.Available
                : MuseGadgetServiceStatus.Unavailable,
            tokenDetail ?? (service ? null : "Could not reach the local musegadget service."));
    }

    /// <summary>Sends a message to the device's main Muse chat.</summary>
    public Task<MuseGadgetSendResult> SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default) =>
        SendMessageAsync(message, sessionId: null, cancellationToken);

    /// <summary>Sends a message to a Muse side chat identified by <paramref name="sessionId"/>.</summary>
    public async Task<MuseGadgetSendResult> SendMessageAsync(
        string message,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (sessionId is not null && !SessionIdPattern().IsMatch(sessionId))
        {
            throw new ArgumentException(
                "The session id must contain 1 to 64 letters, digits or dashes.",
                nameof(sessionId));
        }

        if (!OperatingSystem.IsLinux() || !IsSdkInstalled())
        {
            return new(
                MuseGadgetSendStatus.SdkNotInstalled,
                "The Muse Linux Device SDK was not found.");
        }

        var (token, tokenDetail) = InspectToken();
        if (token == MuseGadgetTokenStatus.Missing)
        {
            return new(
                MuseGadgetSendStatus.SdkTokenNotConfigured,
                tokenDetail ?? "The Muse SDK token is not configured.");
        }

        if (token == MuseGadgetTokenStatus.Invalid)
        {
            return new(
                MuseGadgetSendStatus.SdkTokenInvalid,
                tokenDetail ?? "The configured Muse SDK token is invalid.");
        }

        byte[] request = JsonSerializer.SerializeToUtf8Bytes(
            sessionId is null
                ? new MessageRequest(message)
                : new MessageRequest(message, sessionId),
            MuseGadgetJsonContext.Default.MessageRequest);

        if (request.Length + 1 > MaxRequestBytes)
        {
            throw new ArgumentException(
                $"The serialized request cannot exceed {MaxRequestBytes} bytes.",
                nameof(message));
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.RequestTimeout);

        try
        {
            using var socket = new Socket(
                AddressFamily.Unix,
                SocketType.Stream,
                ProtocolType.Unspecified);
            await socket.ConnectAsync(
                new UnixDomainSocketEndPoint(_options.SocketPath),
                timeout.Token).ConfigureAwait(false);

            using var stream = new NetworkStream(socket, ownsSocket: false);
            await stream.WriteAsync(request, timeout.Token).ConfigureAwait(false);
            await stream.WriteAsync("\n"u8.ToArray(), timeout.Token).ConfigureAwait(false);
            await stream.FlushAsync(timeout.Token).ConfigureAwait(false);

            using var reader = new StreamReader(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            string? response = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
            return ParseResponse(response);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(
                MuseGadgetSendStatus.ServiceUnavailable,
                $"The local musegadget service did not respond within {_options.RequestTimeout}.");
        }
        catch (SocketException exception)
        {
            return new(
                MuseGadgetSendStatus.ServiceUnavailable,
                $"Could not reach the local musegadget service: {exception.Message}");
        }
        catch (IOException exception)
        {
            return new(
                MuseGadgetSendStatus.ServiceUnavailable,
                $"The local musegadget service connection failed: {exception.Message}");
        }
        catch (JsonException exception)
        {
            return new(
                MuseGadgetSendStatus.InvalidResponse,
                $"The local musegadget service returned invalid JSON: {exception.Message}");
        }
    }

    private bool IsSdkInstalled() =>
        _options.ExecutablePaths.Any(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));

    private (MuseGadgetTokenStatus Status, string? Detail) InspectToken()
    {
        string? environmentToken = Environment.GetEnvironmentVariable("MUSEGADGET_SDK_TOKEN");
        if (!string.IsNullOrWhiteSpace(environmentToken))
        {
            return ValidateToken(environmentToken);
        }

        string path = Path.Combine(_options.StateDirectory, "sdk_token");
        try
        {
            string token = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(token)
                ? (MuseGadgetTokenStatus.Missing, $"The SDK token file is empty: {path}")
                : ValidateToken(token);
        }
        catch (FileNotFoundException)
        {
            return (MuseGadgetTokenStatus.Missing, $"The SDK token file was not found: {path}");
        }
        catch (DirectoryNotFoundException)
        {
            return (MuseGadgetTokenStatus.Missing, $"The SDK state directory was not found: {_options.StateDirectory}");
        }
        catch (UnauthorizedAccessException)
        {
            return (
                MuseGadgetTokenStatus.Unknown,
                $"The SDK token is root-owned and cannot be inspected by this process: {path}");
        }
        catch (IOException exception)
        {
            return (
                MuseGadgetTokenStatus.Unknown,
                $"The SDK token configuration could not be inspected: {exception.Message}");
        }
    }

    private static (MuseGadgetTokenStatus Status, string? Detail) ValidateToken(string token) =>
        SdkTokenPattern().IsMatch(token.Trim())
            ? (MuseGadgetTokenStatus.Configured, null)
            : (MuseGadgetTokenStatus.Invalid, "The configured Muse SDK token is invalid.");

    private async Task<bool> CanConnectToServiceAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.RequestTimeout);

        try
        {
            using var socket = new Socket(
                AddressFamily.Unix,
                SocketType.Stream,
                ProtocolType.Unspecified);
            await socket.ConnectAsync(
                new UnixDomainSocketEndPoint(_options.SocketPath),
                timeout.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static MuseGadgetSendResult ParseResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return new(
                MuseGadgetSendStatus.InvalidResponse,
                "The local musegadget service returned an empty response.");
        }

        ServiceResponse? reply = JsonSerializer.Deserialize(
            response,
            MuseGadgetJsonContext.Default.ServiceResponse);
        if (reply?.Ok is true)
        {
            return new(MuseGadgetSendStatus.Sent);
        }

        if (reply?.Error?.Contains(
            "not connected to the Muse",
            StringComparison.OrdinalIgnoreCase) is true)
        {
            return new(MuseGadgetSendStatus.MuseUnavailable, reply.Error);
        }

        return reply is null
            ? new(
                MuseGadgetSendStatus.InvalidResponse,
                "The local musegadget service returned an unrecognized response.")
            : new(
                MuseGadgetSendStatus.RequestRejected,
                reply.Error ?? "The local musegadget service rejected the message.");
    }

    [GeneratedRegex(@"^mgst_[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$")]
    private static partial Regex SdkTokenPattern();

    [GeneratedRegex(@"^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex SessionIdPattern();
}

internal sealed record MessageRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("message")] string Message,
    [property: System.Text.Json.Serialization.JsonPropertyName("session_id")]
    string? SessionId = null);

internal sealed record ServiceResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("ok")] bool Ok,
    [property: System.Text.Json.Serialization.JsonPropertyName("error")] string? Error);
