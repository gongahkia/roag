using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static Actor[] Fragments(Arena a) => a.World.State.Actors
        .Where(x => x.Tactics?.Enemy?.FragmentOf == a.Enemy.Id && x.Life == LifeState.Alive).OrderBy(x => x.Id).ToArray();

    private static void RunBoneEnemies(Arena a)
    {
        for (int i = 0; ; i++)
        {
            Assert.True(i < 50, "Enemy commands did not return control");
            var actor = a.Combat.AdvanceToDecision(); Assert.NotNull(actor);
            if (actor.Id == a.Hero.Id) return;
            a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyPlanningTests.FirstStep(from, goals,
                p => p.X is >= 95 and <= 110 && p.Y is >= 95 and <= 110 && a.World.CanEnter(p, actor.Id)));
        }
    }

    [Fact]
    public void BonePublicCommandsMatchOriginalDeathOrderFragmentCombatAndRewardExemption()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-bone-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = sample.GetProperty("input").GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer);
            a.Hero.Tactics!.Build = CharacterBuild.Create(ClassKind.Adventurer, 37);
            var origin = a.Hero.Position;
            EnemyProfile(a, "bone_splitter"); a.Enemy.Position = origin.Offset(1, 0); a.Enemy.Health = 3;
            a.Friend.Position = new(110, 110); a.Friend.Kind = ActorKind.Raider;
            if (name == "batch")
            {
                a.Friend.Position = origin.Offset(1, -1); a.Combat.RegisterEnemy(a.Friend, "core:enemy/mossbound_guard", 10000); a.Friend.Health = 2;
            }
            if (name == "cramped")
            {
                a.World.Chunks.Edit(origin.Offset(1, -1), Material.StairsDown); a.World.Chunks.Edit(origin.Offset(2, 0), Material.Wall);
                a.Friend.Position = origin.Offset(1, 1); a.Combat.RegisterEnemy(a.Friend, "core:enemy/mossbound_guard", 10000);
            }
            if (name == "exempt") a.Enemy.Tactics!.RewardExempt = true;
            a.Combat.State.RandomState = 37; a.World.State.Inventory["gold"] = 0; a.World.Reveal(); int index = 0;
            foreach (var command in sample.GetProperty("input").GetProperty("commands").EnumerateArray())
            {
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetString() switch { "basic_attack" => TacticalAction.BasicAttack, "sweeping_slash" => TacticalAction.Special, _ => TacticalAction.Wait });
                RunBoneEnemies(a);
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "BoneFragmented" => $"fragment:{e.Time}:{e.Position.X - origin.X}:{e.Position.Y - origin.Y}",
                    "DamageTaken" => $"damage:{e.Time}:{e.Amount}",
                    "ExperienceGained" or "CurrencyDropped" => $"{e.Kind}:{e.Time}:{e.Amount}",
                    _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(expected.GetProperty("xp").GetInt32(), a.Hero.Tactics.Build.Experience);
                // These scenes have no critical chance: the original's only random draw is
                // the batch kill's level-up. Native party builds own that progression stream.
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Hero.Tactics.Build.RandomState);
                Assert.Equal(37, a.Combat.State.RandomState);
                Assert.Equal(expected.GetProperty("drops").GetInt32(), a.Combat.State.Loot.Drops.Count);
                var fragments = Fragments(a); Assert.Equal(expected.GetProperty("fragments").GetArrayLength(), fragments.Length);
                int f = 0;
                foreach (var fragment in fragments)
                {
                    var original = expected.GetProperty("fragments")[f++];
                    Assert.Equal(original.GetProperty("hp").GetInt32(), fragment.Health);
                    Assert.Equal(origin.Offset(original.GetProperty("x").GetInt32(), original.GetProperty("y").GetInt32()), fragment.Position);
                    Assert.True(fragment.Tactics!.RewardExempt); Assert.Equal(2, fragment.MaxHealth);
                    foreach (var (key, kind) in new[] { ("ready", ScheduledKind.ActorReady), ("impact", ScheduledKind.Impact) })
                        Assert.Equal(original.GetProperty(key).ValueKind == JsonValueKind.Null ? (long?)null : original.GetProperty(key).GetInt64(),
                            a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == fragment.Id && w.Kind == kind)?.Due);
                }
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(16, decisions);
    }

    [Fact]
    public void BoneFragmentsUseCellsFreedByTheWholePrimaryBatchAndDoNotInheritItsDamage()
    {
        using var a = new Arena(ClassKind.Adventurer);
        EnemyProfile(a, "bone_splitter"); a.Enemy.Position = new(101, 100); a.Enemy.Health = 3;
        a.Friend.Position = new(101, 99); a.Friend.Kind = ActorKind.Raider;
        a.Combat.RegisterEnemy(a.Friend, "core:enemy/mossbound_guard", 10000); a.Friend.Health = 2;
        a.Act(TacticalAction.Special);
        Assert.Equal(LifeState.Dead, a.Enemy.Life); Assert.Equal(LifeState.Dead, a.Friend.Life);
        var fragments = Fragments(a);
        Assert.Equal(new[] { a.Enemy.Position, a.Friend.Position }, fragments.Select(f => f.Position));
        Assert.All(fragments, f => Assert.Equal(2, f.Health));
        Assert.True(a.Combat.State.Events.Where(e => e.Kind == "DamageTaken").Max(e => e.Sequence)
            < a.Combat.State.Events.Where(e => e.Kind == "BoneFragmented").Min(e => e.Sequence));
        Assert.Equal(9, a.Hero.Tactics!.Build!.Experience);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void BoneFragmentsSpawnOffscreenOnTheParentsFloorAndKeepItsPhysicalFaction()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        EnemyProfile(a, "bone_splitter"); a.Enemy.Position = new(101, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.World.Visible.Clear(); a.Hit(a.Enemy, 1000, source: a.Friend);
        var fragments = Fragments(a); Assert.Equal(2, fragments.Length);
        Assert.All(fragments, f =>
        {
            Assert.Equal(1, f.Position.Floor); Assert.Equal(a.Enemy.FactionId, f.FactionId);
            Assert.Equal(a.Combat.Clock + 1, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == f.Id).Due);
        });
        Assert.Equal(4, a.Friend.Tactics!.Build!.Experience); Assert.Equal(0, a.Hero.Tactics!.Build!.Experience);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ClassGeneratedSentryKillsStillSplitBonesWithoutGrantingFragmentRewards()
    {
        using var a = new Arena(ClassKind.Tinkerer);
        EnemyProfile(a, "bone_splitter"); a.Enemy.Health = 3;
        a.Act(TacticalAction.Special); a.Next(); a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(LifeState.Dead, a.Enemy.Life);
        Assert.Equal(2, a.Combat.State.Events.Count(e => e.Kind == "BoneFragmented"));
        Assert.Equal(4, a.Hero.Tactics!.Build!.Experience);
        int drops = a.Combat.State.Loot.Drops.Count;
        foreach (var f in Fragments(a)) a.Hit(f, 1000, source: a.Hero);
        Assert.Empty(Fragments(a)); Assert.Equal(drops, a.Combat.State.Loot.Drops.Count);
        Assert.Equal(4, a.Hero.Tactics.Build.Experience);
        Assert.Equal(2, a.Combat.State.Events.Count(e => e.Kind == "BoneFragmented"));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void BoneFragmentWindupsResumeFromActualWorldSavesWithExactRewardsAndRandomState()
    {
        using var a = new Arena(ClassKind.Adventurer);
        EnemyProfile(a, "bone_splitter"); a.Enemy.Position = new(101, 100); a.Enemy.Health = 3;
        a.Act(TacticalAction.BasicAttack); RunBoneEnemies(a);
        Assert.NotEmpty(a.Combat.State.Attacks);
        string directory = Path.Combine(Path.GetTempPath(), "roag-bone-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            a.Act(TacticalAction.Special); Assert.True(resumed.Commit(new(TacticalAction.Special, Facing.East)).Success);
            RunBoneEnemies(a);
            for (int i = 0; ; i++)
            {
                Assert.True(i < 50); var actor = resumed.AdvanceToDecision(); Assert.NotNull(actor);
                if (actor.Id == a.Hero.Id) break;
                resumed.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyPlanningTests.FirstStep(from, goals,
                    p => p.X is >= 95 and <= 110 && p.Y is >= 95 and <= 110 && loaded.CanEnter(p, actor.Id)));
            }
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.Equal(a.Hero.Tactics!.Build, loaded.Party.Single().Tactics!.Build);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void BoneSplitSkipsProtectedAndOccupiedCellsWithoutChangingTerrain()
    {
        using var a = new Arena(ClassKind.Adventurer);
        EnemyProfile(a, "bone_splitter"); a.Enemy.Position = new(101, 100); a.Enemy.Health = 3;
        a.World.Chunks.Edit(new(101, 99), Material.StairsUp); a.World.Chunks.Edit(new(102, 100), Material.Wall);
        a.Friend.Position = new(101, 101);
        a.Act(TacticalAction.BasicAttack);
        Assert.Equal(a.Enemy.Position, Assert.Single(Fragments(a)).Position);
        Assert.Equal(Material.StairsUp, a.World.Chunks.At(new(101, 99)));
        Assert.Equal(Material.Wall, a.World.Chunks.At(new(102, 100)));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void FragmentValidationRejectsRenewedRewardsForgedParentsAndExcessBodies()
    {
        using var a = new Arena(ClassKind.Adventurer);
        EnemyProfile(a, "bone_splitter"); a.Enemy.Position = new(101, 100); a.Enemy.Health = 3;
        a.Act(TacticalAction.BasicAttack); var fragment = Fragments(a)[0]; var memory = fragment.Tactics!.Enemy!;
        fragment.Tactics.RewardExempt = false;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        fragment.Tactics.RewardExempt = true;
        foreach (long parent in new[] { 0L, fragment.Id, a.Hero.Id })
        {
            memory.FragmentOf = parent;
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        }
        memory.FragmentOf = a.Enemy.Id;
        var extra = ActorFactory.Create(a.World.Content, "character.roag-bone-fragment", a.World.Seed,
            a.World.State.NextActorId++, new(105, 105), a.Enemy.HomeTown, a.Enemy.Culture, a.Enemy.FactionId);
        a.World.State.Actors.Add(extra);
        var tactics = EnemyCatalog.Default.Get("core:enemy/bone_fragment").CreateTactics(rewardExempt: true);
        tactics.Enemy!.FragmentOf = a.Enemy.Id; a.Combat.Register(extra, tactics, 10000);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Fact]
    public void FragmentAncestryRetainsParentUntilChildrenAreReclaimed()
    {
        using var a = new Arena(ClassKind.Adventurer);
        EnemyProfile(a, "bone_splitter"); a.Enemy.Position = new(101, 100); a.Enemy.Health = 3;
        a.Act(TacticalAction.BasicAttack);
        a.Combat.State.Events.Clear(); a.Combat.State.Loot.Drops.Clear();
        Assert.True(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
        foreach (var f in Fragments(a)) f.Tactics!.Enemy!.FragmentOf = 0;
        Assert.False(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
    }
}
