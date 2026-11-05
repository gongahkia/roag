using System.Diagnostics;
using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;
using Xunit.Abstractions;

namespace Roag.Core.Tests.World;

public sealed class NativeCampaignTests(ITestOutputHelper output)
{
    [Fact]
    public void NativeWorldDecisionProfileKeepsEveryRealCreatureOnItsOwnPaidTurn()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var combat = world.DungeonCombat; var hero = world.Party.Single();
        Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
        var ready = new Stopwatch(); var turns = new Stopwatch();
        var profile = new Dictionary<string, (int Count, double Milliseconds)>();
        for (int i = 0; i < 5000; i++)
        {
            ready.Start(); var actor = combat.AdvanceToDecision(); ready.Stop(); Assert.NotNull(actor);
            if (actor.Id == hero.Id) break;
            turns.Restart(); combat.CommitWorldEnemyTurn(); turns.Stop();
            var prior = profile.GetValueOrDefault(actor.Archetype); profile[actor.Archetype] = (prior.Count + 1, prior.Milliseconds + turns.Elapsed.TotalMilliseconds);
        }
        Assert.Equal(hero.Id, combat.Active!.Id);
        output.WriteLine($"ready_ms={ready.Elapsed.TotalMilliseconds:F2}; commit_ms={profile.Values.Sum(p => p.Milliseconds):F2}; turns={profile.Values.Sum(p => p.Count)}");
        foreach (var row in profile.OrderByDescending(p => p.Value.Milliseconds).Take(12))
            output.WriteLine($"{row.Key}: turns={row.Value.Count}; ms={row.Value.Milliseconds:F2}");
    }

    [Fact]
    public void CampaignRecruitmentAndIndividualPaidStairsKeepTheCompanionPhysicallyOutside()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var hero = world.Party.Single();
        var recruit = world.NativeAdventurers[0];
        var companion = world.State.Actors.Single(a => a.Id == recruit.Actor);
        hero.Position = world.State.Position = new[] { companion.Position.Offset(0, -1), companion.Position.Offset(1, 0), companion.Position.Offset(0, 1), companion.Position.Offset(-1, 0) }
            .First(p => world.CanEnter(p, hero.Id));
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Recruit, Target: companion.Id)).Success);
        Assert.Equal(recruit.Class, companion.Tactics!.Build!.Class); Assert.True(world.SplitDungeonMember(companion.Id).Success);
        // Unit setup starts at an existing generated entrance; the traversal itself
        // uses unchanged paired terrain, real occupants and each actor's paid turn.
        var safeSite = world.Plan.Sites.First(site => !world.State.Actors.Any(a => a.Life == LifeState.Alive && a.Kind is ActorKind.Beast or ActorKind.Raider
            && a.Position.Distance(site.Center.Offset(-2, 0) with { Floor = 1 }) <= 6));
        var entrance = safeSite.Center.Offset(2, 0);
        Assert.Equal(Material.StairsDown, world.Chunks.At(entrance));
        hero.Position = entrance.Offset(-1, 0); var outside = companion.Position;
        ReadyHero();
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        Assert.Equal(entrance.Offset(-4, 0) with { Floor = 1 }, hero.Position); Assert.Equal(outside, companion.Position);
        ReadyHero();
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
        Assert.Equal(entrance, hero.Position); Assert.Equal(outside, companion.Position);
        TacticalValidation.Validate(world.Plan, world.State);
        void ReadyHero()
        {
            for (int i = 0; i < 12 && world.DungeonCombat.Active?.Id != hero.Id && !world.State.AwaitingContinuation; i++)
                Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(hero.Id, world.DungeonCombat.Active?.Id); Assert.Equal(LifeState.Alive, hero.Life);
        }
    }

    [Fact]
    public void ExtendedPhysicalFloorsKeepPairedStairsThroughTheRealConfiguredDepth()
    {
        using var world = WorldSession.CreateNativeCampaign(37); var site = world.Plan.Sites[0];
        for (int floor = 0; floor < world.Plan.FloorCount - 1; floor++)
        {
            var down = site.Center.Offset(2, 0) with { Floor = floor };
            var up = down.Offset(-4, 0) with { Floor = floor + 1 };
            Assert.Equal(Material.StairsDown, world.Chunks.At(down)); Assert.Equal(Material.StairsUp, world.Chunks.At(up));
            Assert.Equal(up, world.StairLanding(down)); Assert.Equal(down, world.StairLanding(up));
        }
        Assert.NotEqual(Material.StairsDown, world.Chunks.At(site.Center.Offset(2, 0) with { Floor = world.Plan.FloorCount - 1 }));
    }

    [Fact]
    public void LivingWorldStartsWithNativePartyAndAuthoredFoesOnOnePersistentTimeline()
    {
        var startup = Stopwatch.StartNew();
        using var world = WorldSession.CreateNativeCampaign(42); startup.Stop(); var hero = world.Party.Single();
        Assert.NotNull(world.State.NativeCampaign); Assert.Null(world.State.Dungeon); Assert.False(world.Plan.RoagNative);
        Assert.Equal(8, world.Plan.FloorCount); Assert.Equal(0, hero.Position.Floor);
        Assert.Equal(ClassKind.Adventurer, hero.Tactics!.Build!.Class);
        Assert.Equal(hero.Id, world.DungeonMember.Id); Assert.Equal(hero.Id, world.DungeonCombat.Active!.Id);
        Assert.All(world.State.Actors.Where(a => a.Life == LifeState.Alive && a.Kind is ActorKind.Beast or ActorKind.Raider), a =>
        { Assert.NotNull(a.Tactics?.Enemy?.Retained); Assert.Contains(a.Id, world.State.Tactics!.Participants); });
        Assert.Contains(world.State.Actors, a => a.Kind == ActorKind.Person && !a.Recruited && a.Tactics == null);
        long time = world.DungeonCombat.Clock; var watch = Stopwatch.StartNew();
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); watch.Stop();
        Assert.True(world.DungeonCombat.Clock > time); Assert.Equal(world.State.NativeCampaign!.StartedAt, world.State.Tick);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30), "One ordinary party wait must remain practical with the real living-world population.");
        output.WriteLine($"Native world actors={world.State.Actors.Count}; participants={world.State.Tactics!.Participants.Count}; startup_ms={startup.Elapsed.TotalMilliseconds:F2}; first_wait_ms={watch.Elapsed.TotalMilliseconds:F2}");
        watch.Restart();
        for (int i = 0; i < 3; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        output.WriteLine($"three_warm_waits_ms={watch.Elapsed.TotalMilliseconds:F2}");
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void CampaignWorldIdleWaitsKeepTheOwnersRealWaitBoonAndNeverBorrowItsTrigger()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var hero = world.Party.Single();
        hero.Tactics!.Boons.Inventory = hero.Tactics.Boons.Inventory.Grant("core:boon/ration_reserve", BoonRarity.Common, ClassKind.Adventurer);
        hero.Tactics.AutoEat = false; hero.Body.Hunger = 40; hero.Health -= 4; int hp = hero.Health;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Eat)).Success);
        Assert.Equal(hp, hero.Health); // Other actors' waits must not spend this member's food prime.
        long waiting = world.State.Tactics!.NextAction;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(hp + 1, hero.Health);
        Assert.Single(world.State.Tactics.Events, e => e.Kind == "BoonActivated" && e.Ability == "core:boon/ration_reserve" && e.Source == hero.Id && e.Action == waiting);
    }

    [Fact]
    public void NativeCampaignSaveKeepsWorldActorsPartyAndQueuesWithoutPrototypeState()
    {
        using var world = WorldSession.CreateNativeCampaign(37, ClassKind.Ranger);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        string path = Path.Combine(Path.GetTempPath(), "roag-native-world-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(path); Assert.Equal("roag-world", SessionStore.Format(path));
            using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Empty(world.State.RoagCombatState); Assert.Empty(world.State.RoagDungeonStates);
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }
}
