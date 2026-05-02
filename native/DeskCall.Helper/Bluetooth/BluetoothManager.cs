using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using DeskCall.Helper.Logging;

namespace DeskCall.Helper.Bluetooth;

public sealed class BluetoothManager
{
    private readonly LogService _log;
    private readonly HfpServiceDetector _hfpServiceDetector;

    public BluetoothManager(LogService log, HfpServiceDetector hfpServiceDetector)
    {
        _log = log;
        _hfpServiceDetector = hfpServiceDetector;
    }

    public async Task<IReadOnlyList<BluetoothDeviceInfo>> ListPairedDevicesAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            _log.Warning("Bluetooth", "Bluetooth device discovery is only available on Windows. Returning an empty list.");
            return [];
        }

        try
        {
            var json = await RunPowerShellAsync("Get-PnpDevice -Class Bluetooth | Select-Object FriendlyName,InstanceId,Status | ConvertTo-Json -Compress");
            var devices = ParsePnpDevices(json)
                .Where(device => !string.IsNullOrWhiteSpace(device.FriendlyName))
                .Select(device =>
                {
                    var services = _hfpServiceDetector.DetectFromPnpDevice(device.FriendlyName ?? "", device.InstanceId ?? "");
                    return new BluetoothDeviceInfo(
                        device.InstanceId ?? device.FriendlyName ?? Guid.NewGuid().ToString("N"),
                        device.FriendlyName ?? "Bluetooth device",
                        ExtractAddress(device.InstanceId),
                        string.Equals(device.Status, "OK", StringComparison.OrdinalIgnoreCase),
                        services.LikelyHandsFree,
                        services.StatusText);
                })
                .GroupBy(device => device.Id)
                .Select(group => group.First())
                .OrderBy(device => device.Name)
                .ToArray();

            _log.Info("Bluetooth", $"Detected {devices.Length} paired Bluetooth PnP device entries.");
            return devices;
        }
        catch (Exception ex)
        {
            _log.Error("Bluetooth", $"Unable to list paired Bluetooth devices: {ex.Message}");
            return [];
        }
    }

    public Task SelectDeviceAsync(string deviceId)
    {
        _log.Info("Bluetooth", $"Selected Bluetooth device id '{deviceId}'.");
        return Task.CompletedTask;
    }

    public Task ConnectDeviceAsync(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new InvalidOperationException("Select a paired phone before connecting.");
        }

        _log.Warning("Bluetooth", "Windows does not expose a stable public API for forcing every phone HFP connection from a desktop app. DeskCall validates the selected device and reports available services; use Windows Bluetooth settings if the OS driver blocks programmatic connect.");
        return Task.CompletedTask;
    }

    public Task DisconnectDeviceAsync(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new InvalidOperationException("No selected phone is available to disconnect.");
        }

        _log.Info("Bluetooth", "Disconnect requested. DeskCall avoids unsafe driver or radio resets; disconnect the phone in Windows Bluetooth settings if the OS does not honor app-level HFP control.");
        return Task.CompletedTask;
    }

    private static IEnumerable<PnpDeviceDto> ParsePnpDevices(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var trimmed = json.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            return JsonSerializer.Deserialize<PnpDeviceDto[]>(trimmed, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
        }

        var single = JsonSerializer.Deserialize<PnpDeviceDto>(trimmed, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return single is null ? [] : [single];
    }

    private static string? ExtractAddress(string? instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return null;
        }

        var match = Regex.Match(instanceId, "([0-9A-F]{12})", RegexOptions.IgnoreCase);
        return match.Success ? string.Join(":", Enumerable.Range(0, 6).Select(i => match.Value.Substring(i * 2, 2))).ToUpperInvariant() : null;
    }

    private static async Task<string> RunPowerShellAsync(string command)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start PowerShell.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(error.Trim());
        }

        return output;
    }

    private sealed record PnpDeviceDto(string? FriendlyName, string? InstanceId, string? Status);
}
