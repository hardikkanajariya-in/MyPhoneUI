namespace DeskCall.Helper.Hfp;

public interface IHfpTransport
{
    string Name { get; }
    bool SupportsMockIncoming { get; }
    Task<HfpTransportResult> DialAsync(string number, CancellationToken cancellationToken = default);
    Task<HfpTransportResult> AnswerAsync(CancellationToken cancellationToken = default);
    Task<HfpTransportResult> RejectAsync(CancellationToken cancellationToken = default);
    Task<HfpTransportResult> EndAsync(CancellationToken cancellationToken = default);
}

public sealed record HfpTransportResult(bool Success, string Command, string Message, string? ErrorCode = null);

public sealed class MockHfpTransport : IHfpTransport
{
    public string Name => "Mock transport";
    public bool SupportsMockIncoming => true;

    public Task<HfpTransportResult> DialAsync(string number, CancellationToken cancellationToken = default) =>
        Task.FromResult(new HfpTransportResult(true, HfpCommand.Dial(number), "Mock outgoing call command accepted."));

    public Task<HfpTransportResult> AnswerAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new HfpTransportResult(true, HfpCommand.Answer, "Mock answer command accepted."));

    public Task<HfpTransportResult> RejectAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new HfpTransportResult(true, HfpCommand.HangUp, "Mock reject command accepted."));

    public Task<HfpTransportResult> EndAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new HfpTransportResult(true, HfpCommand.HangUp, "Mock end command accepted."));
}

public sealed class CapabilityReportingHfpTransport : IHfpTransport
{
    private const string Message = "RealMode HFP control is unavailable on this machine. DeskCall can detect Bluetooth and audio readiness, but Windows is not exposing a usable RFCOMM/HFP control socket for app-level AT commands.";

    public string Name => "Capability-reporting transport";
    public bool SupportsMockIncoming => false;

    public Task<HfpTransportResult> DialAsync(string number, CancellationToken cancellationToken = default) =>
        Task.FromResult(Fail(HfpCommand.Dial(number)));

    public Task<HfpTransportResult> AnswerAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Fail(HfpCommand.Answer));

    public Task<HfpTransportResult> RejectAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Fail(HfpCommand.HangUp));

    public Task<HfpTransportResult> EndAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Fail(HfpCommand.HangUp));

    private static HfpTransportResult Fail(string command) =>
        new(false, command, Message, "realmode-hfp-unavailable");
}
