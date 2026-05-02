namespace DeskCall.Helper.Logging;

public sealed record LogEntry(string Id, DateTimeOffset Timestamp, LogLevel Level, string Source, string Message);

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public sealed class LogService
{
    private readonly List<LogEntry> _entries = [];
    private readonly object _gate = new();

    public event Action<LogEntry>? EntryAdded;

    public IReadOnlyList<LogEntry> GetEntries()
    {
        lock (_gate)
        {
            return _entries.OrderByDescending(entry => entry.Timestamp).Take(250).ToArray();
        }
    }

    public void Debug(string source, string message) => Add(LogLevel.Debug, source, message);
    public void Info(string source, string message) => Add(LogLevel.Info, source, message);
    public void Warning(string source, string message) => Add(LogLevel.Warning, source, message);
    public void Error(string source, string message) => Add(LogLevel.Error, source, message);

    private void Add(LogLevel level, string source, string message)
    {
        var entry = new LogEntry(Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, level, source, message);
        lock (_gate)
        {
            _entries.Insert(0, entry);
            if (_entries.Count > 500)
            {
                _entries.RemoveRange(500, _entries.Count - 500);
            }
        }

        Console.WriteLine($"[{entry.Timestamp:O}] {entry.Level} {entry.Source}: {entry.Message}");
        EntryAdded?.Invoke(entry);
    }
}
