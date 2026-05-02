using DeskCall.Helper.Logging;
using DeskCall.Helper.Storage;

namespace DeskCall.Helper.Hfp;

public sealed class HfpController
{
    private readonly LogService _log;
    private readonly IHfpTransport _transport = new WindowsHfpTransport();

    public HfpController(LogService log, AppStateStore store)
    {
        _log = log;
    }
    public ActiveCallInfo? CurrentCall { get; private set; }

    public void SetMode(HelperMode mode)
    {
        _log.Info("HFP", $"HFP controller mode is {mode} using {_transport.Name}.");
    }

    public Task<ActiveCallInfo?> GetCallStateAsync() => Task.FromResult(CurrentCall);

    public async Task DialAsync(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new InvalidOperationException("Phone number is required.");
        }

        var result = await _transport.DialAsync(number);
        EnsureTransportAvailable("dial", result);

        _log.Info("HFP", result.Message);
    }

    public async Task AnswerAsync()
    {
        if (CurrentCall is null)
        {
            throw new InvalidOperationException("No call is available to answer.");
        }

        var result = await _transport.AnswerAsync();
        EnsureTransportAvailable("answer", result);
        _log.Info("HFP", result.Message);
    }

    public async Task RejectAsync()
    {
        if (CurrentCall is null)
        {
            throw new InvalidOperationException("No call is available to reject.");
        }

        var result = await _transport.RejectAsync();
        EnsureTransportAvailable("reject", result);
        _log.Info("HFP", result.Message);
    }

    public async Task EndAsync()
    {
        if (CurrentCall is null)
        {
            throw new InvalidOperationException("No active call is available to end.");
        }

        var result = await _transport.EndAsync();
        EnsureTransportAvailable("end", result);
        _log.Info("HFP", result.Message);
    }

    private void EnsureTransportAvailable(string operation, HfpTransportResult result)
    {
        if (result.Success)
        {
            return;
        }

        _log.Warning("HFP", $"Unable to {operation} with {_transport.Name}. Command: {result.Command}. {result.Message}");
        throw new InvalidOperationException(result.Message);
    }
}
