using System.Reflection;
using System.Text.Json;
using Roag.Core.Profiles;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    private const string ProfilePrefix = AchievementCatalog.Prefix;
    private static void InProfileFolder(Action<string> check)
    {
        string directory = Path.Combine(Path.GetTempPath(), "roag-native-profile-tests-" + Guid.NewGuid().ToString("N"));
        try { check(Path.Combine(directory, "campaign.json")); }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void CommittedSecretAwardsOnceWithoutChangingTimeRandomOrTheFrozenChestPoolAndContinuesExactly()
    {
        InProfileFolder(slot =>
        {
            using var world = Floor(); using var control = Floor();
            world.EnablePermanentProfile(slot); var hero = world.Party.Single();
            PrepareSecret(world, true); PrepareSecret(control, true); world.Reveal(); control.Reveal();
            string[] pool = world.State.Tactics!.Chests.AvailableBoons.ToArray();
            Assert.Empty(world.State.Tactics.Profile!.Earned);
            var attack = new TacticalIntent(TacticalAction.BasicAttack, Facing.East);
            Assert.True(world.SubmitDungeon(attack).Success); Assert.True(control.SubmitDungeon(attack).Success);
            Assert.Contains(ProfilePrefix + "uncovered_truth", world.PermanentAchievements);
            Assert.Single(world.State.Tactics.Events, e => e.Kind == "AchievementEarned" && e.Ability == ProfilePrefix + "uncovered_truth");
            Assert.Equal(control.DungeonCombat.Clock, world.DungeonCombat.Clock); Assert.Equal(control.State.Tactics!.RandomState, world.State.Tactics.RandomState);
            Assert.Equal(control.Party.Single().Tactics!.Build!.RandomState, hero.Tactics!.Build!.RandomState);
            Assert.Equal(pool, world.State.Tactics.Chests.AvailableBoons); Assert.DoesNotContain("core:boon/prospectors_heart", pool);
            world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            using var next = WorldSession.CreateDungeonRun(42); next.EnablePermanentProfile(slot);
            Assert.Contains("core:boon/prospectors_heart", next.State.Tactics!.Chests.AvailableBoons);
            Assert.DoesNotContain("core:boon/prospectors_heart", world.State.Tactics.Chests.AvailableBoons);
            Assert.Empty(next.State.Tactics.Profile!.Earned); Assert.Empty(next.Party.Single().Tactics!.Boons.Inventory.Stacks);
            TacticalValidation.Validate(world.Plan, world.State); TacticalValidation.Validate(next.Plan, next.State);
        });
    }

    [Fact]
    public void ActualPaidAttackObservesThreeDistinctOwnedBoonActivationsInTheSameCausalChain()
    {
        InProfileFolder(slot =>
        {
            using var world = Floor(); world.EnablePermanentProfile(slot); var hero = world.Party.Single();
            var enemy = ReplaceRegular(world, "core:enemy/mossbound_guard"); enemy.Position = hero.Position.Offset(1, 0);
            enemy.Tactics!.InnateStats = enemy.Tactics.InnateStats! with { MaxHealth = 1000 }; enemy.Health = enemy.MaxHealth;
            world.DungeonCombat.State.Queue.CancelActor(enemy.Id, ScheduledKind.ActorReady);
            world.DungeonCombat.State.Queue.Schedule(ScheduledKind.ActorReady, 100000, enemy.Id);
            foreach (string boon in new[] { "emberbrand", "storm_conductor", "opening_cut" })
                hero.Tactics!.Boons.Inventory = hero.Tactics.Boons.Inventory.Grant("core:boon/" + boon, BoonRarity.Legendary, ClassKind.Adventurer);
            world.Reveal(); Assert.True(world.SubmitDungeon(new(TacticalAction.BasicAttack, Facing.East)).Success);
            Assert.Contains(ProfilePrefix + "chain_reaction", world.PermanentAchievements);
            Assert.Empty(world.State.Tactics!.Profile!.Chains);
            Assert.Single(world.State.Tactics.Events, e => e.Kind == "AchievementEarned" && e.Ability == ProfilePrefix + "chain_reaction");
            Assert.DoesNotContain("core:boon/circuit_chorus", world.State.Tactics.Chests.AvailableBoons);
        });
    }

    [Fact]
    public void DifferentMembersAndUnrelatedCausalChainsCannotManufactureChainReactionAndPartialChainsSaveExactly()
    {
        InProfileFolder(slot =>
        {
            using var world = Floor(); world.EnablePermanentProfile(slot); var combat = world.DungeonCombat; var hero = world.Party.Single();
            var other = DungeonPartyTests.AddMember(world, ClassKind.Adventurer, hero.Position.Floor);
            other.Position = hero.Position.Offset(0, 1);
            var observe = typeof(TacticalCombat).GetMethod("ObserveProfileEvent", BindingFlags.NonPublic | BindingFlags.Instance)!;
            void Activation(long owner, string boon, long chain)
            {
                combat.State.NextAction = Math.Max(combat.State.NextAction, chain + 1);
                combat.State.BoonEffects.Dispatch.Chains.TryAdd(chain, new());
                observe.Invoke(combat, [new TacticalEvent(1, combat.Clock, "BoonActivated", owner, 0, chain, hero.Position, Ability: "core:boon/" + boon), new BoonCause(chain, chain, [], 1, false)]);
            }
            Activation(hero.Id, "emberbrand", 1); Activation(hero.Id, "emberbrand", 1); Activation(hero.Id, "storm_conductor", 1);
            Activation(other.Id, "opening_cut", 1); Activation(hero.Id, "opening_cut", 2);
            Assert.DoesNotContain(ProfilePrefix + "chain_reaction", combat.State.Profile!.Earned);
            world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Activation(hero.Id, "opening_cut", 1); Assert.Contains(ProfilePrefix + "chain_reaction", combat.State.Profile.Earned);
            Assert.Empty(combat.State.Profile.Chains); TacticalValidation.Validate(world.Plan, world.State);
        });
    }

    [Fact]
    public void FinalDeathRetainsAwardsAndRejectsTheEarlierLivingSaveEvenWhenTheWorldManifestWriteFails()
    {
        InProfileFolder(slot =>
        {
            using var world = Floor(); world.EnablePermanentProfile(slot);
            PrepareSecret(world, true); world.Reveal(); Assert.True(world.SubmitDungeon(new(TacticalAction.BasicAttack, Facing.East)).Success);
            PrepareTerminalAttack(world); world.Save(slot); byte[] alive = File.ReadAllBytes(slot);
            world.SavePath = Path.Combine(slot, "blocked.json"); WaitThroughTerminalAction(world);
            Assert.True(world.DeathPending); Assert.Equal(alive, File.ReadAllBytes(slot));
            var store = new NativeProfileStore(slot + ".profile");
            Assert.Throws<IOException>(() => store.ValidateContinuation(world.State.Tactics!.Profile!, terminal: false));
            Assert.Throws<IOException>(() => WorldSession.Load(slot));
            Assert.Contains(ProfilePrefix + "uncovered_truth", store.Inspect().Data!.Earned);
            world.Save(slot); Assert.False(world.DeathPending);
            using var terminal = WorldSession.Load(slot); Assert.True(terminal.State.AwaitingContinuation);
            Assert.False(terminal.Submit(new(WorldCommandKind.Continue)).Success); Assert.False(terminal.SubmitDungeon(new(TacticalAction.Wait)).Success);
            using var next = WorldSession.CreateDungeonRun(99); next.EnablePermanentProfile(slot);
            Assert.False(next.State.AwaitingContinuation); Assert.Equal(0, next.DungeonCombat.Clock); Assert.Single(next.Party);
            Assert.Empty(next.State.Tactics!.Profile!.Earned); Assert.Contains("core:boon/prospectors_heart", next.State.Tactics.Chests.AvailableBoons);
            using (var stillEnded = WorldSession.Load(slot)) Assert.True(stillEnded.State.AwaitingContinuation);
            next.Save(slot); using var resumed = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(next.State), JsonSerializer.Serialize(resumed.State));
            Assert.Throws<IOException>(() => terminal.Save(slot));
        });
    }

    [Fact]
    public void OldNativeSavesRemainUnchangedAndIneligibleWithoutCreatingOrImportingAProfile()
    {
        InProfileFolder(slot =>
        {
            using var world = Floor(); world.Save(slot); byte[] before = File.ReadAllBytes(slot);
            using var loaded = WorldSession.Load(slot); Assert.False(loaded.HasPermanentProfile);
            Assert.Equal(before, File.ReadAllBytes(slot)); Assert.False(Directory.Exists(slot + ".profile"));
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Throws<InvalidOperationException>(() => loaded.EnablePermanentProfile(slot));
        });
    }

    [Fact]
    public void MissingProfileRejectsAnEligibleContinuingSaveWithoutOverwritingIt()
    {
        InProfileFolder(slot =>
        {
            using var world = Floor(); world.EnablePermanentProfile(slot); world.Save(slot); byte[] before = File.ReadAllBytes(slot);
            Directory.Delete(slot + ".profile", true);
            Assert.Throws<IOException>(() => WorldSession.Load(slot)); Assert.Equal(before, File.ReadAllBytes(slot));
            Assert.False(File.Exists(Path.Combine(slot + ".profile", NativeProfileStore.Slots[0])));
        });
    }

    [Fact]
    public void EntryBadgesRequireARealConsciousMemberInTheSuppliedPhysicalStage()
    {
        InProfileFolder(slot =>
        {
            using var world = Floor(); world.EnablePermanentProfile(slot); var hero = world.Party.Single();
            world.RecordDungeonEntry(hero.Id, 4, sanctum: true); world.RecordDungeonEntry(999999, 2);
            Assert.Empty(world.State.Tactics!.Profile!.Earned);
            world.RecordCampaignVictory(world.State.Dungeon!.Floors[1].Boss); Assert.Empty(world.State.Tactics.Profile.Earned);
            world.RecordPursuerArrival(world.State.Dungeon.Floors[1].Boss); Assert.Empty(world.State.Tactics.Profile.Earned);
        });
    }
}
