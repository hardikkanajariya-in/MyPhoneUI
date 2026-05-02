using DeskCall.Helper.Logging;

namespace DeskCall.Helper.Bluetooth;

public sealed class HfpServiceDetector
{
    public static readonly Guid HandsFreeProfileGuid = Guid.Parse("0000111E-0000-1000-8000-00805F9B34FB");
    public static readonly Guid HandsFreeAudioGatewayGuid = Guid.Parse("0000111F-0000-1000-8000-00805F9B34FB");
    public static readonly Guid HeadsetProfileGuid = Guid.Parse("00001108-0000-1000-8000-00805F9B34FB");

    private readonly LogService _log;

    public HfpServiceDetector(LogService log)
    {
        _log = log;
    }

    public HfpDetectionResult DetectFromPnpDevice(string friendlyName, string instanceId)
    {
        var haystack = $"{friendlyName} {instanceId}".ToLowerInvariant();
        var likely = haystack.Contains("hands-free", StringComparison.Ordinal)
            || haystack.Contains("handsfree", StringComparison.Ordinal)
            || haystack.Contains("headset", StringComparison.Ordinal)
            || haystack.Contains("hfp", StringComparison.Ordinal)
            || haystack.Contains("ag audio", StringComparison.Ordinal);

        var status = likely
            ? "HFP-like Bluetooth service name detected."
            : "No HFP service name exposed through PnP. Windows may hide RFCOMM/HFP details behind the Bluetooth stack.";

        _log.Debug("HFP Detector", $"{friendlyName}: {status}");
        return new HfpDetectionResult(likely, status);
    }
}

public sealed record HfpDetectionResult(bool LikelyHandsFree, string StatusText);
