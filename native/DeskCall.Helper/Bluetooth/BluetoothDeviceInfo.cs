namespace DeskCall.Helper.Bluetooth;

public sealed record BluetoothDeviceInfo(
    string Id,
    string Name,
    string? Address,
    bool IsConnected,
    bool CanAdvertiseHfp,
    string StatusText);

public enum DeviceStatus
{
    Disconnected,
    Pairing,
    Connected,
    CallActive,
    Error
}
