using System.Text.Json;
using DeskCall.Helper.Audio;
using DeskCall.Helper.Bluetooth;
using DeskCall.Helper.Hfp;
using DeskCall.Helper.Logging;
using DeskCall.Helper.Storage;

namespace DeskCall.Helper.Bridge;

public sealed record BridgeEnvelope(string Type, string? RequestId, JsonElement? Payload);

public sealed record BridgeResponse(string Type, string RequestId, object? Payload = null, string? Error = null)
{
    public static BridgeResponse Ok(string requestId, object? payload = null) => new("bridge:response", requestId, payload);
    public static BridgeResponse Fail(string requestId, string error) => new("bridge:response", requestId, null, error);
}

public sealed record BridgeOutbound(string Type, object? Payload);

public sealed record SelectDevicePayload(string DeviceId);
public sealed record DialPayload(string Number);
public sealed record DeleteContactPayload(string ContactId);
public sealed record SetModePayload(HelperMode HelperMode);

public sealed record HelperStateDto(
    string? SelectedDeviceId,
    HelperMode HelperMode,
    DeviceStatus DeviceStatus,
    BluetoothDeviceInfo? SelectedDevice,
    IReadOnlyList<AudioEndpointInfo> AudioEndpoints,
    IReadOnlyList<RecentCallRecord> RecentCalls);
