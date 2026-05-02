using System.Diagnostics;
using System.Text.Json;
using DeskCall.Helper.Logging;

namespace DeskCall.Helper.Audio;

public sealed record AudioEndpointInfo(string Id, string Name, AudioDirection Direction, bool IsBluetoothHandsFreeCandidate, string State);

public enum AudioDirection
{
    Input,
    Output,
    Unknown
}

public sealed class AudioDeviceManager
{
    private static readonly string[] HandsFreeKeywords = ["hands-free", "handsfree", "ag audio", "bluetooth", "headset"];
    private readonly LogService _log;

    public AudioDeviceManager(LogService log)
    {
        _log = log;
    }

    public async Task<IReadOnlyList<AudioEndpointInfo>> ListEndpointsAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            _log.Warning("Audio", "Windows audio endpoint detection is unavailable on this OS.");
            return [];
        }

        try
        {
            var json = await RunPowerShellAsync("Get-CimInstance Win32_SoundDevice | Select-Object Name,DeviceID,Status | ConvertTo-Json -Compress");
            var devices = ParseDevices(json)
                .Select(device =>
                {
                    var name = device.Name ?? "Audio endpoint";
                    var handsFree = HandsFreeKeywords.Any(keyword => name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                    return new AudioEndpointInfo(device.DeviceID ?? name, name, AudioDirection.Unknown, handsFree, device.Status ?? "Unknown");
                })
                .OrderByDescending(device => device.IsBluetoothHandsFreeCandidate)
                .ThenBy(device => device.Name)
                .ToArray();

            if (devices.Any(device => device.IsBluetoothHandsFreeCandidate))
            {
                _log.Info("Audio", "Found likely Bluetooth hands-free audio endpoint. Windows still controls actual routing.");
            }
            else
            {
                _log.Warning("Audio", "No likely Bluetooth hands-free endpoint was found. Pair/connect the phone for calls in Windows Bluetooth settings.");
            }

            return devices;
        }
        catch (Exception ex)
        {
            _log.Error("Audio", $"Unable to enumerate audio endpoints: {ex.Message}");
            return [];
        }
    }

    private static IEnumerable<AudioDeviceDto> ParseDevices(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var trimmed = json.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            return JsonSerializer.Deserialize<AudioDeviceDto[]>(trimmed, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
        }

        var single = JsonSerializer.Deserialize<AudioDeviceDto>(trimmed, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return single is null ? [] : [single];
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

    private sealed record AudioDeviceDto(string? Name, string? DeviceID, string? Status);
}
