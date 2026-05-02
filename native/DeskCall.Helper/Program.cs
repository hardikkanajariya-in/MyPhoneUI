using DeskCall.Helper.Audio;
using DeskCall.Helper.Bluetooth;
using DeskCall.Helper.Bridge;
using DeskCall.Helper.Hfp;
using DeskCall.Helper.Logging;
using DeskCall.Helper.Storage;

var port = 49321;
for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--port" && int.TryParse(args[i + 1], out var parsedPort))
    {
        port = parsedPort;
    }
}

var log = new LogService();
var store = new AppStateStore(log);
var hfpDetector = new HfpServiceDetector(log);
var bluetooth = new BluetoothManager(log, hfpDetector);
var audio = new AudioDeviceManager(log);
var hfp = new HfpController(log, store);
var bridge = new BridgeServer(port, log, store, bluetooth, audio, hfp);

log.Info("Startup", $"DeskCall helper starting on ws://127.0.0.1:{port}/deskcall/");
await store.LoadAsync();
hfp.SetMode(store.Data.HelperMode);
await bridge.StartAsync();
