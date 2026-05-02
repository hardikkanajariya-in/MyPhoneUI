using DeskCall.Helper.Logging;
using DeskCall.Helper.Storage;

namespace DeskCall.Helper.Hfp;

public sealed class HfpController
{
    private readonly LogService _log;
    private readonly AppStateStore _store;
    private readonly object _gate = new();
    private readonly IHfpTransport _mockTransport = new MockHfpTransport();
    private readonly IHfpTransport _realModeTransport = new CapabilityReportingHfpTransport();
    private DateTimeOffset? _activeStartedAt;
    private IHfpTransport _activeTransport;

    public HfpController(LogService log, AppStateStore store)
    {
        _log = log;
        _store = store;
        _activeTransport = store.Data.HelperMode == HelperMode.MockMode ? _mockTransport : _realModeTransport;
    }

    public event Action<HfpCallEvent>? CallEvent;
    public ActiveCallInfo? CurrentCall { get; private set; }

    public void SetMode(HelperMode mode)
    {
        _activeTransport = mode == HelperMode.MockMode ? _mockTransport : _realModeTransport;
        _log.Info("HFP", $"HFP controller mode is {mode} using {_activeTransport.Name}.");
    }

    public Task<ActiveCallInfo?> GetCallStateAsync() => Task.FromResult(CurrentCall);

    public async Task DialAsync(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new InvalidOperationException("Phone number is required.");
        }

        var result = await _activeTransport.DialAsync(number);
        EnsureTransportAvailable("dial", result);

        lock (_gate)
        {
            CurrentCall = new ActiveCallInfo(Guid.NewGuid().ToString("N"), number.Trim(), null, CallDirection.Outgoing, CallStatus.Ringing, null, 0);
        }

        _log.Info("HFP", result.Message);
        Emit(HfpCallEventKind.Ringing, CurrentCall!);
        await Task.Delay(900);
        SetActive();
    }

    public Task MockIncomingAsync(string number, string? name)
    {
        if (!_activeTransport.SupportsMockIncoming)
        {
            throw new InvalidOperationException("Mock incoming calls are only available while DeskCall is in MockMode.");
        }

        lock (_gate)
        {
            CurrentCall = new ActiveCallInfo(Guid.NewGuid().ToString("N"), number, name, CallDirection.Incoming, CallStatus.Ringing, null, 0);
        }

        _log.Info("HFP", $"Mock incoming call generated for {number}.");
        Emit(HfpCallEventKind.Incoming, CurrentCall!);
        return Task.CompletedTask;
    }

    public async Task AnswerAsync()
    {
        if (CurrentCall is null)
        {
            throw new InvalidOperationException("No call is available to answer.");
        }

        var result = await _activeTransport.AnswerAsync();
        EnsureTransportAvailable("answer", result);
        _log.Info("HFP", result.Message);
        SetActive();
    }

    public async Task RejectAsync()
    {
        if (CurrentCall is null)
        {
            throw new InvalidOperationException("No call is available to reject.");
        }

        var result = await _activeTransport.RejectAsync();
        EnsureTransportAvailable("reject", result);
        _log.Info("HFP", result.Message);
        await EndCallAsync(RecentCallStatus.Rejected);
    }

    public async Task EndAsync()
    {
        if (CurrentCall is null)
        {
            throw new InvalidOperationException("No active call is available to end.");
        }

        var result = await _activeTransport.EndAsync();
        EnsureTransportAvailable("end", result);
        _log.Info("HFP", result.Message);
        await EndCallAsync(CurrentCall.Status == CallStatus.Active ? RecentCallStatus.Answered : RecentCallStatus.Missed);
    }

    private void SetActive()
    {
        if (CurrentCall is null)
        {
            return;
        }

        _activeStartedAt = DateTimeOffset.UtcNow;
        CurrentCall = CurrentCall with { Status = CallStatus.Active, StartedAt = _activeStartedAt, DurationSeconds = 0 };
        Emit(HfpCallEventKind.Active, CurrentCall);
        _ = TickDurationAsync(CurrentCall.Id);
    }

    private async Task TickDurationAsync(string callId)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync())
        {
            if (CurrentCall?.Id != callId || CurrentCall.Status != CallStatus.Active || _activeStartedAt is null)
            {
                return;
            }

            CurrentCall = CurrentCall with { DurationSeconds = Math.Max(0, (int)(DateTimeOffset.UtcNow - _activeStartedAt.Value).TotalSeconds) };
            Emit(HfpCallEventKind.Active, CurrentCall);
        }
    }

    private async Task EndCallAsync(RecentCallStatus status)
    {
        if (CurrentCall is null)
        {
            return;
        }

        var ended = CurrentCall with
        {
            Status = CallStatus.Ended,
            DurationSeconds = CurrentCall.Status == CallStatus.Active && _activeStartedAt is not null
                ? Math.Max(0, (int)(DateTimeOffset.UtcNow - _activeStartedAt.Value).TotalSeconds)
                : 0
        };

        CurrentCall = ended;
        _store.AddRecentCall(new RecentCallRecord(ended.Number, ended.Name, ended.Direction, status, ended.StartedAt ?? DateTimeOffset.UtcNow, ended.DurationSeconds));
        await _store.SaveAsync();
        Emit(HfpCallEventKind.Ended, ended);
    }

    private void EnsureTransportAvailable(string operation, HfpTransportResult result)
    {
        if (result.Success)
        {
            return;
        }

        _log.Warning("HFP", $"Unable to {operation} with {_activeTransport.Name}. Command: {result.Command}. {result.Message}");
        throw new InvalidOperationException(result.Message);
    }

    private void Emit(HfpCallEventKind kind, object payload)
    {
        CallEvent?.Invoke(new HfpCallEvent(kind, payload));
    }
}
