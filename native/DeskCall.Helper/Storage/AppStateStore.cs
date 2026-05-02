using System.Text.Json;
using System.Text.Json.Serialization;
using DeskCall.Helper.Hfp;
using DeskCall.Helper.Logging;

namespace DeskCall.Helper.Storage;

public sealed class AppStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly LogService _log;
    private readonly string _statePath;

    public AppStateStore(LogService log)
    {
        _log = log;
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(Path.GetTempPath(), "DeskCall");
        }

        var directory = Path.Combine(root, "DeskCall");
        Directory.CreateDirectory(directory);
        _statePath = Path.Combine(directory, "appstate.json");
    }

    public DeskCallData Data { get; private set; } = new();

    public async Task LoadAsync()
    {
        if (!File.Exists(_statePath))
        {
            SeedContacts();
            await SaveAsync();
            _log.Info("Storage", $"Created DeskCall state at {_statePath}.");
            return;
        }

        var json = await File.ReadAllTextAsync(_statePath);
        Data = JsonSerializer.Deserialize<DeskCallData>(json, JsonOptions) ?? new DeskCallData();
        _log.Info("Storage", $"Loaded DeskCall state from {_statePath}.");
    }

    public Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(Data, JsonOptions);
        return File.WriteAllTextAsync(_statePath, json);
    }

    public ContactRecord CreateContact(ContactDraft draft)
    {
        var contact = new ContactRecord(Guid.NewGuid().ToString("N"), draft.Name.Trim(), draft.Phone.Trim(), draft.Favorite, DateTimeOffset.UtcNow);
        Data.Contacts.Add(contact);
        return contact;
    }

    public void UpdateContact(ContactRecord contact)
    {
        var index = Data.Contacts.FindIndex(existing => existing.Id == contact.Id);
        if (index < 0)
        {
            throw new InvalidOperationException("Contact was not found.");
        }

        Data.Contacts[index] = contact;
    }

    public void DeleteContact(string contactId)
    {
        Data.Contacts.RemoveAll(contact => contact.Id == contactId);
    }

    public void AddRecentCall(RecentCallRecord recentCall)
    {
        Data.RecentCalls.Insert(0, recentCall);
        if (Data.RecentCalls.Count > 50)
        {
            Data.RecentCalls.RemoveRange(50, Data.RecentCalls.Count - 50);
        }
    }

    private void SeedContacts()
    {
        Data.Contacts.Add(new ContactRecord(Guid.NewGuid().ToString("N"), "DeskCall Test", "+1 415 555 0198", true, DateTimeOffset.UtcNow));
        Data.Contacts.Add(new ContactRecord(Guid.NewGuid().ToString("N"), "Support Desk", "+1 212 555 0144", false, DateTimeOffset.UtcNow));
    }
}

public sealed class DeskCallData
{
    public string? SelectedDeviceId { get; set; }
    public string? SelectedDeviceName { get; set; }
    public HelperMode HelperMode { get; set; } = HelperMode.MockMode;
    public List<ContactRecord> Contacts { get; set; } = [];
    public List<RecentCallRecord> RecentCalls { get; set; } = [];
}

public sealed record ContactDraft(string Name, string Phone, bool Favorite);
public sealed record ContactRecord(string Id, string Name, string Phone, bool Favorite, DateTimeOffset CreatedAt);
public sealed record RecentCallRecord(string Number, string? Name, CallDirection Direction, RecentCallStatus Status, DateTimeOffset StartedAt, int DurationSeconds);

public enum RecentCallStatus
{
    Answered,
    Missed,
    Rejected
}
