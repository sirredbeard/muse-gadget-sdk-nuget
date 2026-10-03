using Muse.Gadget.Sdk.Linux;

var options = new MuseGadgetClientOptions
{
    SocketPath = "/tmp/musegadget-aot-smoke.sock",
    StateDirectory = "/tmp/musegadget-aot-smoke",
};
options.ExecutablePaths.Clear();
options.ExecutablePaths.Add("/tmp/musegadget-aot-smoke");

var client = new MuseGadgetClient(options);
MuseGadgetSendResult result = await client.SendMessageAsync("native AOT smoke test");

return result.Status == MuseGadgetSendStatus.SdkNotInstalled ? 0 : 1;
