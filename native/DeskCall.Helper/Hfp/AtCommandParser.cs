using System.Text.RegularExpressions;

namespace DeskCall.Helper.Hfp;

public sealed class AtCommandParser
{
    public AtParseResult Parse(string line)
    {
        var trimmed = line.Trim();
        if (string.Equals(trimmed, "RING", StringComparison.OrdinalIgnoreCase))
        {
            return new AtParseResult(AtParseResultKind.Ring);
        }

        if (string.Equals(trimmed, "OK", StringComparison.OrdinalIgnoreCase))
        {
            return new AtParseResult(AtParseResultKind.Ok);
        }

        if (string.Equals(trimmed, "ERROR", StringComparison.OrdinalIgnoreCase))
        {
            return new AtParseResult(AtParseResultKind.Error);
        }

        var clip = Regex.Match(trimmed, "\\+CLIP:\\s*\"(?<number>[^\"]+)\"");
        if (clip.Success)
        {
            return new AtParseResult(AtParseResultKind.CallerId, clip.Groups["number"].Value);
        }

        var clcc = Regex.Match(trimmed, "\\+CLCC:\\s*(?<index>\\d+),(?<direction>\\d+),(?<status>\\d+)");
        if (clcc.Success)
        {
            return new AtParseResult(AtParseResultKind.CallState, Status: clcc.Groups["status"].Value);
        }

        return new AtParseResult(AtParseResultKind.Unknown, Raw: trimmed);
    }
}

public sealed record AtParseResult(AtParseResultKind Kind, string? Value = null, string? Status = null, string? Raw = null);

public enum AtParseResultKind
{
    Unknown,
    Ring,
    CallerId,
    CallState,
    Ok,
    Error
}
