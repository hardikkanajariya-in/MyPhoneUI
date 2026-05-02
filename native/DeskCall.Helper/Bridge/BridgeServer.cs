using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeskCall.Helper.Audio;
using DeskCall.Helper.Bluetooth;
using DeskCall.Helper.Hfp;
using DeskCall.Helper.Logging;
using DeskCall.Helper.Storage;

namespace DeskCall.Helper.Bridge;

public sealed class BridgeServer
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly int _port;
    private readonly LogService _log;
    private readonly AppStateStore _store;
    private readonly BluetoothManager _bluetooth;
    private readonly AudioDeviceManager _audio;
    private readonly HfpController _hfp;
    private readonly HttpListener _listener = new();
    private readonly List<WebSocket> _clients = [];
    private IReadOnlyList<AudioEndpointInfo> _lastAudioEndpoints = [];
    private IReadOnlyList<BluetoothDeviceInfo> _lastDevices = [];

    public BridgeServer(int port, LogService log, AppStateStore store, BluetoothManager bluetooth, AudioDeviceManager audio, HfpController hfp)
    {
        _port = port;
        _log = log;
        _store = store;
        _bluetooth = bluetooth;
        _audio = audio;
        _hfp = hfp;
        _listener.Prefixes.Add($"http://127.0.0.1:{_port}/deskcall/");
        _log.EntryAdded += entry => _ = BroadcastAsync("log:entry", entry);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        _log.Info("Bridge", "WebSocket bridge listening.");
        _lastAudioEndpoints = await _audio.ListEndpointsAsync();
        _lastDevices = await _bluetooth.ListPairedDevicesAsync();

        while (true)
        {
            var context = await _listener.GetContextAsync();
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                continue;
            }

            _ = HandleClientAsync(context);
        }
    }

    private async Task HandleClientAsync(HttpListenerContext context)
    {
        WebSocket? socket = null;
        try
        {
            socket = (await context.AcceptWebSocketAsync(null)).WebSocket;
            lock (_clients)
            {
                _clients.Add(socket);
            }

            _log.Info("Bridge", "Electron renderer connected.");
            await SendAsync(socket, "helper:status", BuildState());
            await SendAsync(socket, "contacts:listResult", _store.Data.Contacts);
            await SendAsync(socket, "logs:listResult", _log.GetEntries());
            await SendAsync(socket, "device:listResult", _lastDevices);
            await SendAsync(socket, "audio:devicesChanged", _lastAudioEndpoints);

            var buffer = new byte[64 * 1024];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var envelope = JsonSerializer.Deserialize<BridgeEnvelope>(json, JsonOptions);
                if (envelope is not null)
                {
                    await HandleMessageAsync(socket, envelope);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Warning("Bridge", $"Client disconnected: {ex.Message}");
        }
        finally
        {
            if (socket is not null)
            {
                lock (_clients)
                {
                    _clients.Remove(socket);
                }
                socket.Dispose();
            }
        }
    }

    private async Task HandleMessageAsync(WebSocket socket, BridgeEnvelope envelope)
    {
        try
        {
            var payload = await DispatchAsync(envelope);
            if (!string.IsNullOrWhiteSpace(envelope.RequestId))
            {
                await SendRawAsync(socket, BridgeResponse.Ok(envelope.RequestId!, payload));
            }
        }
        catch (Exception ex)
        {
            _log.Error("Bridge", $"{envelope.Type} failed: {ex.Message}");
            if (!string.IsNullOrWhiteSpace(envelope.RequestId))
            {
                await SendRawAsync(socket, BridgeResponse.Fail(envelope.RequestId!, ex.Message));
            }
            await SendAsync(socket, "call:error", new { message = ex.Message, code = envelope.Type });
        }
    }

    private async Task<object?> DispatchAsync(BridgeEnvelope envelope)
    {
        switch (envelope.Type)
        {
            case "helper:getStatus":
                return BuildState();
            case "devices:list":
                _lastDevices = await _bluetooth.ListPairedDevicesAsync();
                await BroadcastAsync("device:listResult", _lastDevices);
                await BroadcastAsync("device:statusChanged", BuildState());
                return _lastDevices;
            case "device:select":
            {
                var payload = ReadPayload<SelectDevicePayload>(envelope);
                var selected = _lastDevices.FirstOrDefault(device => device.Id == payload.DeviceId)
                    ?? (await _bluetooth.ListPairedDevicesAsync()).FirstOrDefault(device => device.Id == payload.DeviceId);
                await _bluetooth.SelectDeviceAsync(payload.DeviceId);
                _store.Data.SelectedDeviceId = payload.DeviceId;
                _store.Data.SelectedDeviceName = selected?.Name;
                await _store.SaveAsync();
                await BroadcastAsync("device:statusChanged", BuildState());
                return BuildState();
            }
            case "device:connect":
                await _bluetooth.ConnectDeviceAsync(_store.Data.SelectedDeviceId);
                _lastAudioEndpoints = await _audio.ListEndpointsAsync();
                await BroadcastAsync("audio:devicesChanged", _lastAudioEndpoints);
                await BroadcastAsync("device:statusChanged", BuildState(DeviceStatus.Connected));
                return BuildState(DeviceStatus.Connected);
            case "device:disconnect":
                await _bluetooth.DisconnectDeviceAsync(_store.Data.SelectedDeviceId);
                await BroadcastAsync("device:statusChanged", BuildState(DeviceStatus.Disconnected));
                return BuildState(DeviceStatus.Disconnected);
            case "call:dial":
            {
                var payload = ReadPayload<DialPayload>(envelope);
                await _hfp.DialAsync(payload.Number);
                return _hfp.CurrentCall;
            }
            case "call:answer":
                await _hfp.AnswerAsync();
                return _hfp.CurrentCall;
            case "call:reject":
                await _hfp.RejectAsync();
                return _hfp.CurrentCall;
            case "call:end":
                await _hfp.EndAsync();
                return _hfp.CurrentCall;
            case "contacts:list":
                await BroadcastAsync("contacts:listResult", _store.Data.Contacts);
                return _store.Data.Contacts;
            case "contacts:create":
            {
                var draft = ReadPayload<ContactDraft>(envelope);
                var contact = _store.CreateContact(draft);
                await _store.SaveAsync();
                await BroadcastAsync("contacts:listResult", _store.Data.Contacts);
                return contact;
            }
            case "contacts:update":
            {
                var contact = ReadPayload<ContactRecord>(envelope);
                _store.UpdateContact(contact);
                await _store.SaveAsync();
                await BroadcastAsync("contacts:listResult", _store.Data.Contacts);
                return contact;
            }
            case "contacts:delete":
            {
                var payload = ReadPayload<DeleteContactPayload>(envelope);
                _store.DeleteContact(payload.ContactId);
                await _store.SaveAsync();
                await BroadcastAsync("contacts:listResult", _store.Data.Contacts);
                return payload;
            }
            case "logs:list":
                await BroadcastAsync("logs:listResult", _log.GetEntries());
                return _log.GetEntries();
            default:
                throw new InvalidOperationException($"Unknown bridge message type '{envelope.Type}'.");
        }
    }

    private HelperStateDto BuildState(DeviceStatus? overrideStatus = null)
    {
        var selectedDevice = _lastDevices.FirstOrDefault(device => device.Id == _store.Data.SelectedDeviceId);
        var status = overrideStatus ?? (selectedDevice?.IsConnected == true ? DeviceStatus.Connected : DeviceStatus.Disconnected);
        if (_hfp.CurrentCall is { Status: CallStatus.Active })
        {
            status = DeviceStatus.CallActive;
        }

        return new HelperStateDto(
            _store.Data.SelectedDeviceId,
            _store.Data.HelperMode,
            status,
            selectedDevice,
            _lastAudioEndpoints,
            _store.Data.RecentCalls);
    }

    private static T ReadPayload<T>(BridgeEnvelope envelope)
    {
        if (envelope.Payload is null)
        {
            throw new InvalidOperationException($"Missing payload for {envelope.Type}.");
        }

        return envelope.Payload.Value.Deserialize<T>(JsonOptions)
            ?? throw new InvalidOperationException($"Invalid payload for {envelope.Type}.");
    }

    private async Task BroadcastAsync(string type, object? payload)
    {
        WebSocket[] clients;
        lock (_clients)
        {
            clients = _clients.Where(client => client.State == WebSocketState.Open).ToArray();
        }

        foreach (var client in clients)
        {
            await SendAsync(client, type, payload);
        }
    }

    private static Task SendAsync(WebSocket socket, string type, object? payload)
    {
        return SendRawAsync(socket, new BridgeOutbound(type, payload));
    }

    private static async Task SendRawAsync(WebSocket socket, object message)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var json = JsonSerializer.Serialize(message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
