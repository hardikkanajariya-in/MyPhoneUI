using DeskCall.Helper.Logging;
using DeskCall.Helper.Storage;

namespace DeskCall.Helper.Hfp;

public sealed class HfpController
{
    private readonly LogService _log;
    private readonly AppStateStore _store;
    private readonly object _gate = new();
    private DateTimeOffset? _activeStartedAt;

    public HfpController(LogService log, AppStateStore store)
    {
        _log = log;
        _store = store;
    }

    public event Action<HfpCallEvent>? CallEvent;
    public ActiveCallInfo? CurrentCall { get; private set; }

    public void SetMode(HelperMode mode)
    {
        _log.Info("HFP", $"HFP controller mode is {mode}.");
    }

    public Task<ActiveCallInfo?> GetCallStateAsync() => Task.FromResult(CurrentCall);

    public async Task DialAsync(string number)
    {
        EnsureMockModeOrThrow("dial", HfpCommand.Dial(number));
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new InvalidOperationException("Phone number is required.");
        }

        lock (_gate)
        {
            CurrentCall = new ActiveCallInfo(Guid.NewGuid().ToString("N"), number.Trim(), null, CallDirection.Outgoing, CallStatus.Ringing, null, 0);
        }

        _log.Info("HFP", $"Mock outgoing call started with {HfpCommand.Dial(number)}.");
        Emit(HfpCallEventKind.Ringing, CurrentCall!);
        await Task.Delay(900);
        SetActive();
    }

    public Task MockIncomingAsync(string number, string? name)
    {
        EnsureMockModeOrThrow("mock incoming call", "RING / +CLIP");
        lock (_gate)
        {
            CurrentCall = new ActiveCallInfo(Guid.NewGuid().ToString("N"), number, name, CallDirection.Incoming, CallStatus.Ringing, null, 0);
        }

        _log.Info("HFP", $"Mock incoming call generated for {number}.");
        Emit(HfpCallEventKind.Incoming, CurrentCall!);
        return Task.CompletedTask;
    }

    public Task AnswerAsync()
    {
        EnsureMockModeOrThrow("answer", HfpCommand.Answer);
        if (CurrentCall is null)
        {
            throw new InvalidOperationException("No call is available to answer.");
        }

        _log.Info("HFP", $"Answering call with {HfpCommand.Answer}.");
        SetActive();
        return Task.CompletedTask;
    }

    public async Task RejectAsync()
    {
        EnsureMockModeOrThrow("reject", HfpCommand.HangUp);
        if (CurrentCall is null)
        {
            throw new InvalidOperationException("No call is available to reject.");
        }

        _log.Info("HFP", $"Rejecting call with {HfpCommand.HangUp}.");
        await EndCallAsync(RecentCallStatus.Rejected);
    }

    public async Task EndAsync()
    {
        EnsureMockModeOrThrow("end", HfpCommand.HangUp);
        if (CurrentCall is null)
        {
            throw new InvalidOperationException("No active call is available to end.");
        }

        _log.Info("HFP", $"Ending call with {HfpCommand.HangUp}.");
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

    private void EnsureMockModeOrThrow(string operation, string command)
    {
        if (_store.Data.HelperMode == HelperMode.MockMode)
        {
            return;
        }

        _log.Warning("HFP", $"RealMode requested '{operation}' using {command}, but this build does not have a live RFCOMM HFP socket. Windows commonly restricts desktop apps from acting as an HFP audio gateway without driver/OEM support.");
        throw new InvalidOperationException($"RealMode HFP {operation} is unavailable on this machine. Switch to MockMode for UI testing, or inspect logs for Bluetooth/HFP service detection.");
    }

    private void Emit(HfpCallEventKind kind, object payload)
    {
        CallEvent?.Invoke(new HfpCallEvent(kind, payload));
    }
}
