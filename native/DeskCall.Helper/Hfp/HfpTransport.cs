namespace DeskCall.Helper.Hfp;

public interface IHfpTransport
{
    string Name { get; }
    Task<HfpTransportResult> DialAsync(string number, CancellationToken cancellationToken = default);
    Task<HfpTransportResult> AnswerAsync(CancellationToken cancellationToken = default);
    Task<HfpTransportResult> RejectAsync(CancellationToken cancellationToken = default);
    Task<HfpTransportResult> EndAsync(CancellationToken cancellationToken = default);
}

public sealed record HfpTransportResult(bool Success, string Command, string Message, string? ErrorCode = null);

public sealed class WindowsHfpTransport : IHfpTransport
{
    private const string Message = "DeskCall is running in live mode, but Windows is not exposing a usable RFCOMM/HFP control socket for this phone. Bluetooth and audio discovery remain available, but live AT command call control is unavailable on the current adapter or driver stack.";
    public string Name => "Windows live HFP transport";

    public Task<HfpTransportResult> DialAsync(string number, CancellationToken cancellationToken = default) =>
        Task.FromResult(Fail(HfpCommand.Dial(number)));

    public Task<HfpTransportResult> AnswerAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Fail(HfpCommand.Answer));

    public Task<HfpTransportResult> RejectAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Fail(HfpCommand.HangUp));

    public Task<HfpTransportResult> EndAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Fail(HfpCommand.HangUp));

    private static HfpTransportResult Fail(string command) =>
        new(false, command, Message, "live-hfp-unavailable");
}
