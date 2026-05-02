namespace DeskCall.Helper.Hfp;

public enum HelperMode
{
    RealMode
}

public enum CallStatus
{
    Idle,
    Ringing,
    Dialing,
    Active,
    Ended,
    Error
}

public enum CallDirection
{
    Incoming,
    Outgoing
}

public enum HfpCallEventKind
{
    Incoming,
    Ringing,
    Active,
    Ended,
    Error
}

public sealed record ActiveCallInfo(
    string Id,
    string Number,
    string? Name,
    CallDirection Direction,
    CallStatus Status,
    DateTimeOffset? StartedAt,
    int DurationSeconds);

public sealed record HfpCallEvent(HfpCallEventKind Kind, object Payload);
