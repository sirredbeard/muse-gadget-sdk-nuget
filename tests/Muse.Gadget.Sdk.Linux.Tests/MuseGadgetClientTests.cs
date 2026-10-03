using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Muse.Gadget.Sdk.Linux.Tests;

public sealed class MuseGadgetClientTests : IDisposable
{
    private const string ValidToken = "mgst_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAw";
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"muse-gadget-tests-{Guid.NewGuid():N}");

    public MuseGadgetClientTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task ReportsSdkNotInstalled()
    {
        var client = CreateClient(installed: false, token: ValidToken);

        MuseGadgetStatus status = await client.GetStatusAsync();
        MuseGadgetSendResult send = await client.SendMessageAsync("hello");

        Assert.Equal(MuseGadgetInstallationStatus.NotInstalled, status.Installation);
        Assert.Equal(MuseGadgetSendStatus.SdkNotInstalled, send.Status);
    }

    [Fact]
    public async Task ReportsMissingSdkToken()
    {
        var client = CreateClient(installed: true, token: null);

        MuseGadgetStatus status = await client.GetStatusAsync();
        MuseGadgetSendResult send = await client.SendMessageAsync("hello");

        Assert.Equal(MuseGadgetTokenStatus.Missing, status.Token);
        Assert.Equal(MuseGadgetSendStatus.SdkTokenNotConfigured, send.Status);
    }

    [Fact]
    public async Task ReportsInvalidSdkToken()
    {
        var client = CreateClient(installed: true, token: "mgst_bad");

        MuseGadgetStatus status = await client.GetStatusAsync();
        MuseGadgetSendResult send = await client.SendMessageAsync("hello");

        Assert.Equal(MuseGadgetTokenStatus.Invalid, status.Token);
        Assert.Equal(MuseGadgetSendStatus.SdkTokenInvalid, send.Status);
    }

    [Fact]
    public async Task ReportsWhenSdkCannotReachMuse()
    {
        var client = CreateClient(installed: true, token: ValidToken);
        using var server = await FakeMuseGadgetServer.StartAsync(
            SocketPath,
            """{"ok":false,"error":"not connected to the Muse"}""");

        MuseGadgetSendResult result = await client.SendMessageAsync("garage door open");

        Assert.Equal(MuseGadgetSendStatus.MuseUnavailable, result.Status);
        Assert.Equal("garage door open", server.Request.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SendsMessageThroughInstalledSdk()
    {
        var client = CreateClient(installed: true, token: ValidToken);
        using var server = await FakeMuseGadgetServer.StartAsync(
            SocketPath,
            """{"ok":true,"status":200,"response":null}""");

        MuseGadgetSendResult result = await client.SendMessageAsync(
            "garage door open",
            "garage-alerts");

        Assert.Equal(MuseGadgetSendStatus.Sent, result.Status);
        Assert.Equal(
            "garage-alerts",
            server.Request.RootElement.GetProperty("session_id").GetString());
    }

    [Fact]
    public async Task ReportsUnavailableLocalService()
    {
        var client = CreateClient(installed: true, token: ValidToken);

        MuseGadgetSendResult result = await client.SendMessageAsync("hello");

        Assert.Equal(MuseGadgetSendStatus.ServiceUnavailable, result.Status);
    }

    private string SocketPath => Path.Combine(_directory, "musegadget.sock");

    private MuseGadgetClient CreateClient(bool installed, string? token)
    {
        string executable = Path.Combine(_directory, "musegadget");
        if (installed)
        {
            File.WriteAllText(executable, string.Empty);
        }

        if (token is not null)
        {
            File.WriteAllText(Path.Combine(_directory, "sdk_token"), token);
        }

        var options = new MuseGadgetClientOptions
        {
            SocketPath = SocketPath,
            StateDirectory = _directory,
            RequestTimeout = TimeSpan.FromSeconds(2),
        };
        options.ExecutablePaths.Clear();
        options.ExecutablePaths.Add(executable);
        return new MuseGadgetClient(options);
    }

    private sealed class FakeMuseGadgetServer : IDisposable
    {
        private readonly Socket _listener;
        private readonly Task _serveTask;

        private FakeMuseGadgetServer(Socket listener, Task serveTask)
        {
            _listener = listener;
            _serveTask = serveTask;
        }

        public JsonDocument Request { get; private set; } = null!;

        public static Task<FakeMuseGadgetServer> StartAsync(string path, string response)
        {
            var listener = new Socket(
                AddressFamily.Unix,
                SocketType.Stream,
                ProtocolType.Unspecified);
            listener.Bind(new UnixDomainSocketEndPoint(path));
            listener.Listen();

            FakeMuseGadgetServer? server = null;
            Task serveTask = Task.Run(async () =>
            {
                using Socket connection = await listener.AcceptAsync();
                using var stream = new NetworkStream(connection);
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                string request = (await reader.ReadLineAsync())!;
                server!.Request = JsonDocument.Parse(request);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(response + "\n"));
                await stream.FlushAsync();
            });

            server = new FakeMuseGadgetServer(listener, serveTask);
            return Task.FromResult(server);
        }

        public void Dispose()
        {
            _serveTask.GetAwaiter().GetResult();
            Request.Dispose();
            _listener.Dispose();
        }
    }
}
