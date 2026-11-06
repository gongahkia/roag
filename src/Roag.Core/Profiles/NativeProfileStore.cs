using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roag.Core.Profiles;

/// <summary>Injectable only for isolated storage-failure checks. Writes must atomically replace one record.</summary>
public interface IProfileStorage
{
    byte[]? Read(string name);
    void Write(string name, byte[] bytes);
    IDisposable Lock();
}

public sealed class ProfileData
{
    [JsonRequired]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [JsonRequired]
    public long NextRun { get; set; } = 1;
    [JsonRequired]
    public long CurrentRun { get; set; }
    [JsonRequired]
    public long TerminalThrough { get; set; }
    [JsonRequired]
    public HashSet<string> Earned { get; set; } = [];
    internal void Validate()
    {
        if (!Guid.TryParseExact(Id, "N", out _) || NextRun is <= 0 or >= long.MaxValue || CurrentRun < 0 || CurrentRun >= NextRun
            || TerminalThrough < 0 || TerminalThrough > CurrentRun || Earned == null || Earned.Any(id => !AchievementCatalog.Contains(id)))
            throw new InvalidDataException("Invalid native permanent profile.");
    }
}

public enum ProfileStatus { Absent, Active, Recovered, Corrupt, Incompatible }
public sealed record ProfileInspection(ProfileStatus Status, ProfileData? Data, int InvalidRecords);

/// <summary>Independent two-generation progression store. Durable awards and terminal markers precede world replacement.</summary>
public sealed class NativeProfileStore
{
    public const string Format = "roag-native-profile/1";
    public const int MaximumBytes = 131072;
    public static readonly string[] Slots = ["profile-a.roag", "profile-b.roag"];
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };
    private sealed record Record(string Format, string Content, long Sequence, string Checksum, string Payload);
    private sealed record Found(ProfileInspection Inspection, Record? Record, int Slot);
    private readonly IProfileStorage _storage;
    public NativeProfileStore(string directory) : this(new DiskStorage(directory)) { }
    public NativeProfileStore(IProfileStorage storage) => _storage = storage;
    public ProfileInspection Inspect() => Read().Inspection;

    private Found Read()
    {
        var records = new List<(Record Record, int Slot)>(); int invalid = 0;
        for (int i = 0; i < Slots.Length; i++)
        {
            try
            {
                byte[]? bytes = _storage.Read(Slots[i]); if (bytes == null) continue;
                if (bytes.Length > MaximumBytes) throw new InvalidDataException("Oversized profile.");
                var record = JsonSerializer.Deserialize<Record>(bytes, Json) ?? throw new InvalidDataException("Empty profile.");
                if (record.Sequence <= 0 || record.Sequence >= long.MaxValue || record.Payload == null || record.Checksum == null
                    || record.Checksum != Hash(System.Text.Encoding.UTF8.GetBytes(record.Payload))) throw new InvalidDataException("Profile checksum mismatch.");
                records.Add((record, i));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException) { invalid++; }
        }
        if (records.Count == 0) return new(new(invalid == 0 ? ProfileStatus.Absent : ProfileStatus.Corrupt, null, invalid), null, -1);
        var top = records.OrderByDescending(r => r.Record.Sequence).ThenByDescending(r => r.Slot).First();
        if (top.Record.Format != Format || top.Record.Content != AchievementCatalog.Content)
            return new(new(ProfileStatus.Incompatible, null, invalid), top.Record, top.Slot);
        try
        {
            var profile = JsonSerializer.Deserialize<ProfileData>(top.Record.Payload, Json) ?? throw new InvalidDataException("Missing profile payload.");
            profile.Validate();
            return new(new(invalid > 0 ? ProfileStatus.Recovered : ProfileStatus.Active, profile, invalid), top.Record, top.Slot);
        }
        catch (Exception e) when (e is JsonException or InvalidDataException) { return new(new(ProfileStatus.Corrupt, null, invalid + 1), top.Record, top.Slot); }
    }

    private Found Compatible(bool allowAbsent)
    {
        var found = Read();
        if (found.Inspection.Status is ProfileStatus.Corrupt or ProfileStatus.Incompatible || !allowAbsent && found.Inspection.Status == ProfileStatus.Absent)
            throw new IOException($"Permanent profile is {found.Inspection.Status.ToString().ToLowerInvariant()}; existing progress was preserved.");
        return found;
    }
    private static string Hash(byte[] payload) => Convert.ToHexString(SHA256.HashData(payload));
    private void Write(Found previous, ProfileData data)
    {
        data.Validate();
        string payload = JsonSerializer.Serialize(data, Json);
        var record = new Record(Format, AchievementCatalog.Content, checked((previous.Record?.Sequence ?? 0) + 1), Hash(System.Text.Encoding.UTF8.GetBytes(payload)), payload);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(record, Json);
        if (bytes.Length > MaximumBytes) throw new IOException("Permanent profile exceeds its storage limit.");
        int slot = previous.Slot == 0 ? 1 : 0;
        _storage.Write(Slots[slot], bytes);
        byte[]? readback = _storage.Read(Slots[slot]);
        if (readback == null || !bytes.AsSpan().SequenceEqual(readback)) throw new IOException("Permanent profile read-back failed.");
    }
    private void WriteBoth(Found previous, ProfileData data)
    {
        Write(previous, data);
        // Duplicate before returning a reserved identity, terminal receipt or award.
        // Losing either generation later must not reuse a run ID or resurrect a world.
        Write(Compatible(allowAbsent: false), data);
    }

    public CampaignProgress AllocateRun()
    {
        using var held = _storage.Lock();
        var found = Compatible(allowAbsent: true); var data = found.Inspection.Data ?? new ProfileData();
        long serial = data.NextRun; data.NextRun = checked(serial + 1);
        WriteBoth(found, data);
        return new() { Profile = data.Id, Run = serial, StartingAchievements = new(data.Earned, StringComparer.Ordinal) };
    }

    private static void Check(ProfileData data, CampaignProgress progress, bool terminal)
    {
        if (progress.Earned == null || progress.Pending == null || progress.StartingAchievements == null
            || progress.Earned.Any(id => !AchievementCatalog.Contains(id)) || progress.Pending.Any(id => !progress.Earned.Contains(id)))
            throw new InvalidDataException("Invalid pending permanent achievement.");
        if (progress.Profile != data.Id || progress.Run <= 0 || progress.Run >= data.NextRun
            || progress.StartingAchievements.Except(data.Earned).Any())
            throw new IOException("This campaign's permanent profile is missing or differs; progress was preserved.");
        if (progress.Run < data.CurrentRun) throw new IOException("A newer campaign replaced this continuing world. The older world cannot resume.");
        if (!terminal && progress.Run <= data.TerminalThrough) throw new IOException("This campaign has permanently ended. Its earlier living save cannot resume.");
    }
    public void ValidateContinuation(CampaignProgress progress, bool terminal)
    {
        using var held = _storage.Lock();
        Check(Compatible(allowAbsent: false).Inspection.Data!, progress, terminal);
    }
    public void CommitProgress(CampaignProgress progress, bool terminal)
    {
        using var held = _storage.Lock();
        var found = Compatible(allowAbsent: false); var data = found.Inspection.Data!;
        Check(data, progress, terminal);
        bool changed = progress.Earned.Except(data.Earned).Any();
        data.Earned.UnionWith(progress.Earned);
        if (terminal)
        {
            changed |= data.TerminalThrough < progress.Run;
            data.TerminalThrough = Math.Max(data.TerminalThrough, progress.Run);
            data.CurrentRun = Math.Max(data.CurrentRun, progress.Run);
        }
        // Retry both records even if an earlier first write already committed it.
        if (changed || progress.Pending.Count > 0 || terminal || found.Inspection.Status == ProfileStatus.Recovered) WriteBoth(found, data);
        progress.Pending.Clear();
    }
    public void CommitCheckpoint(CampaignProgress progress, bool terminal)
    {
        using var held = _storage.Lock();
        var found = Compatible(allowAbsent: false); var data = found.Inspection.Data!;
        Check(data, progress, terminal);
        data.CurrentRun = Math.Max(data.CurrentRun, progress.Run);
        // A previous first replica may already contain this checkpoint. Retrying
        // must still repair its second copy before reporting a successful save.
        WriteBoth(found, data);
    }

    private sealed class DiskStorage(string directory) : IProfileStorage
    {
        private readonly string _directory = Path.GetFullPath(directory);
        public IDisposable Lock()
        {
            Directory.CreateDirectory(_directory);
            return new FileStream(Path.Combine(_directory, "profile.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        public byte[]? Read(string name)
        {
            string file = Path.Combine(_directory, name);
            if (!File.Exists(file)) return null;
            if (new FileInfo(file).Length > MaximumBytes) throw new InvalidDataException("Oversized permanent profile.");
            return File.ReadAllBytes(file);
        }
        public void Write(string name, byte[] bytes)
        {
            Directory.CreateDirectory(_directory);
            string target = Path.Combine(_directory, name), temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { output.Write(bytes); output.Flush(true); }
                File.Move(temporary, target, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
