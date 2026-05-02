using System.Text.Json;
using System.Text.Json.Serialization;
using DeskCall.Helper.Hfp;
using DeskCall.Helper.Logging;

namespace DeskCall.Helper.Storage;

public sealed class AppStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly LogService _log;
    private readonly string _statePath;
    private readonly string _contactsPath;

    public AppStateStore(LogService log)
    {
        _log = log;
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(Path.GetTempPath(), "DeskCall");
        }

        var directory = Path.Combine(root, "DeskCall");
        Directory.CreateDirectory(directory);
        _statePath = Path.Combine(directory, "appstate.json");
        _contactsPath = Path.Combine(directory, "contacts.json");
    }

    public DeskCallData Data { get; private set; } = new();

    public async Task LoadAsync()
    {
        var shouldSave = false;
        if (File.Exists(_statePath))
        {
            var json = await File.ReadAllTextAsync(_statePath);
            Data = JsonSerializer.Deserialize<DeskCallData>(json, JsonOptions) ?? new DeskCallData();
            _log.Info("Storage", $"Loaded DeskCall state from {_statePath}.");
        }
        else
        {
            shouldSave = true;
            _log.Info("Storage", $"DeskCall state will be created at {_statePath}.");
        }

        if (File.Exists(_contactsPath))
        {
            var contactsJson = await File.ReadAllTextAsync(_contactsPath);
            Data.Contacts = JsonSerializer.Deserialize<List<ContactRecord>>(contactsJson, JsonOptions) ?? [];
            _log.Info("Storage", $"Loaded contacts from {_contactsPath}.");
        }
        else
        {
            SeedContacts();
            shouldSave = true;
            _log.Info("Storage", $"Local contacts file will be created at {_contactsPath}.");
        }

        if (shouldSave)
        {
            await SaveAsync();
        }
    }

    public Task SaveAsync()
    {
        var stateJson = JsonSerializer.Serialize(Data, JsonOptions);
        var contactsJson = JsonSerializer.Serialize(Data.Contacts, JsonOptions);
        return Task.WhenAll(
            File.WriteAllTextAsync(_statePath, stateJson),
            File.WriteAllTextAsync(_contactsPath, contactsJson));
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

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}

public sealed class DeskCallData
{
    public string? SelectedDeviceId { get; set; }
    public string? SelectedDeviceName { get; set; }
    public HelperMode HelperMode { get; set; } = HelperMode.MockMode;
    [JsonIgnore]
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
