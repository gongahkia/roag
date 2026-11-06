using Roag.Core.Profiles;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private NativeProfileStore? _profileStore;
    private string? _profileDirectory;
    private readonly HashSet<string> _permanentAchievements = [];
    public string? ProfileDirectory => _profileDirectory;
    public bool HasPermanentProfile => State.Tactics?.Profile != null;
    public IReadOnlySet<string> PermanentAchievements => _permanentAchievements;

    /// <summary>Enroll an explicit fresh campaign only. Existing saves/fixtures remain ineligible without conversion.</summary>
    public void EnablePermanentProfile(string savePath, string? profileDirectory = null)
    {
        if (!HasNativeParty || State.Tactics is not { NextAction: 1, Profile: null } state)
            throw new InvalidOperationException("Permanent progression can be enabled only on an explicit fresh native campaign.");
        string directory = Path.GetFullPath(profileDirectory ?? savePath + ".profile");
        var store = new NativeProfileStore(directory);
        var progress = store.AllocateRun();
        state.Chests.AvailableBoons = AchievementCatalog.Pool(state.Chests.AvailableBoons, progress.StartingAchievements);
        state.Profile = progress; _profileStore = store; _profileDirectory = directory;
        _permanentAchievements.UnionWith(progress.StartingAchievements);
    }

    private void RestorePermanentProfile(string savePath, string? profileDirectory)
    {
        if (State.Tactics?.Profile is not { } progress) return;
        _profileDirectory = Path.GetFullPath(profileDirectory ?? savePath + ".profile");
        _profileStore = new(_profileDirectory);
        _profileStore.ValidateContinuation(progress, State.AwaitingContinuation);
        _permanentAchievements.UnionWith(_profileStore.Inspect().Data!.Earned);
    }

    public void FlushPermanentProfile()
    {
        if (State.Tactics?.Profile is not { } progress || progress.Pending.Count == 0 && !State.AwaitingContinuation) return;
        if (_profileStore == null) throw new IOException("The campaign's permanent profile is not attached.");
        _profileStore.CommitProgress(progress, State.AwaitingContinuation);
        _permanentAchievements.UnionWith(progress.Earned);
    }

    private void PreparePermanentProfileSave()
    {
        if (State.Tactics?.Profile is not { } progress) return;
        progress.Validate(State.Tactics, State.NextActorId);
        if (_profileStore == null) throw new IOException("The campaign's permanent profile is not attached.");
        _profileStore.ValidateContinuation(progress, State.AwaitingContinuation);
        FlushPermanentProfile();
    }

    private void CommitPermanentProfileCheckpoint()
    {
        if (State.Tactics?.Profile is { } progress) _profileStore!.CommitCheckpoint(progress, State.AwaitingContinuation);
    }

    /// <summary>Called by physical-complex entry after the committed member actually crosses into that stage.</summary>
    public void RecordDungeonEntry(long member, int stage, bool sanctum = false)
    {
        if (HasNativeParty) DungeonCombat.RecordDungeonEntry(member, stage, sanctum);
    }

    /// <summary>Called only after a special site's real confined pursuer has arrived, never on registration alone.</summary>
    public void RecordPursuerArrival(long pursuer)
    {
        if (HasNativeParty) DungeonCombat.RecordPursuerArrival(pursuer);
    }

    /// <summary>Root campaign settlement owns final-role choice. This hook only observes settled final victory.</summary>
    public void RecordCampaignVictory(long boss)
    {
        if (HasNativeParty && State.Endgame.Victory && State.Endgame.ActorId == boss)
            DungeonCombat.RecordCampaignVictory(boss);
    }
}
