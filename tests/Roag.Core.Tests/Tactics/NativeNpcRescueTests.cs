using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeNpcRescueTests
{
    private static Actor PrepareRescuer(WorldSession world, int distance = 2)
    {
        foreach (var person in world.State.Actors.Where(a => !a.Recruited && a.Kind == ActorKind.Person)) person.Cargo.Remove("medicine");
        var hero = world.Party.Single(); var resident = world.ActorById(world.State.Dungeon!.Recruits[0].Actor);
        resident.Position = hero.Position.Offset(0, distance); resident.Cargo["medicine"] = 1;
        resident.Trust[hero.Id] = 40; resident.TraitId = "trait.generous";
        for (int y = 1; y <= distance; y++) world.Chunks.Edit(hero.Position.Offset(0, y), Material.Floor);
        world.Reveal();
        return resident;
    }

    [Fact]
    public void AnExistingWillingNpcWalksAndSpendsTheirOwnMedicineToRescueTheLastDownedMember()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var hero = world.Party.Single(); var npc = PrepareRescuer(world);
        long[] identities = world.State.Actors.Select(a => a.Id).ToArray();
        int medicine = world.Count("medicine"); long consumed = world.State.ResourcesConsumed; var initial = npc.Position;
        DungeonAbandonmentTests.Down(world, hero);
        Assert.NotNull(npc.Tactics?.Resident); Assert.False(world.State.AwaitingContinuation);
        Assert.Equal(hero.Id, npc.Tactics.Resident.Target);
        Assert.Same(hero, world.DungeonCombat.AdvanceToPartyDecision());
        Assert.Equal(LifeState.Alive, hero.Life); Assert.Equal(Math.Max(1, hero.MaxHealth / 4), hero.Health);
        Assert.Equal(0, npc.Cargo["medicine"]); Assert.Equal(medicine, world.Count("medicine"));
        Assert.Equal(consumed + 1, world.State.ResourcesConsumed);
        Assert.Equal(identities, world.State.Actors.Select(a => a.Id)); Assert.False(npc.Recruited);
        Assert.Contains(world.State.Tactics!.Events, e => e.Kind == "Moved" && e.Source == npc.Id);
        var cure = Assert.Single(world.State.Tactics.Events, e => e.Kind == "Revived" && e.Source == npc.Id && e.Target == hero.Id);
        var approach = world.State.Tactics.Events.Last(e => e.Kind == "Moved" && e.Source == npc.Id && e.Time <= cure.Time);
        Assert.Equal(1, approach.Position.Distance(initial)); Assert.Equal(1, approach.Position.Distance(hero.Position));
        Assert.True(cure.Time > 0); Assert.True(world.DungeonCombat.Clock > cure.Time);
        Assert.True(hero.Trust[npc.Id] >= 12); Assert.Equal(0, hero.Tactics!.BleedTurns);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData("medicine")] [InlineData("relationship")] [InlineData("unseen")] [InlineData("injured")] [InlineData("danger")]
    public void NpcRescueIsNotInventedWhenAnExistingPersonCannotOrWillNotHelp(string reason)
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var hero = world.Party.Single(); var npc = PrepareRescuer(world);
        if (reason == "medicine") npc.Cargo.Remove("medicine");
        if (reason == "relationship") npc.Trust[hero.Id] = -100;
        if (reason == "unseen") world.Chunks.Edit(hero.Position.Offset(0, 1), Material.Wall);
        if (reason == "injured") npc.Health = 1;
        if (reason == "danger")
        {
            npc.TraitId = "trait.wary"; npc.Trust[hero.Id] = 0;
            var enemy = world.State.Actors.First(a => a.Position.Floor == 1 && a.Tactics?.Enemy is { Dormant: false });
            enemy.Position = npc.Position.Offset(1, 0); world.Chunks.Edit(enemy.Position, Material.Floor);
        }
        DungeonAbandonmentTests.Down(world, hero);
        Assert.Null(npc.Tactics); Assert.Equal(LifeState.Downed, hero.Life);
        Assert.DoesNotContain(world.State.Tactics!.Events, e => e.Kind == "RescueNoticed");
    }

    [Fact]
    public void PendingNpcApproachAndMedicineRoundTripAndSettleIdentically()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var hero = world.Party.Single(); var npc = PrepareRescuer(world, 3);
        DungeonAbandonmentTests.Down(world, hero);
        string root = Path.Combine(Path.GetTempPath(), "roag-npc-rescue-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(npc.Tactics!.Resident!.LastKnown, loaded.ActorById(npc.Id).Tactics!.Resident!.LastKnown);
            Assert.NotNull(world.DungeonCombat.AdvanceToPartyDecision()); Assert.NotNull(loaded.DungeonCombat.AdvanceToPartyDecision());
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(LifeState.Alive, loaded.Party.Single().Life); Assert.Equal(0, loaded.ActorById(npc.Id).Cargo["medicine"]);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void RecruitingAnAlreadyActingRescuerPreservesTheirFixedBuildAndPaidRecoveryAndSharesOnlyTheirRealMedicine()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var hero = world.Party.Single(); var npc = PrepareRescuer(world);
        var member = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2);
        DungeonAbandonmentTests.Down(world, hero);
        var scheduled = world.State.Tactics!.Timeline.Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == npc.Id);
        string build = JsonSerializer.Serialize(npc.Tactics!.Build);
        Assert.Same(member, world.DungeonCombat.AdvanceToPartyDecision());
        member.Position = world.State.Position = npc.Position.Offset(1, 0); world.Chunks.Edit(member.Position, Material.Floor); world.Reveal();
        int medicine = world.Count("medicine");
        Assert.True(world.SubmitDungeon(new(TacticalAction.Recruit, Target: npc.Id)).Success);
        Assert.True(npc.Recruited); Assert.Null(npc.Tactics.Resident); Assert.Equal(build, JsonSerializer.Serialize(npc.Tactics.Build));
        Assert.Equal(medicine + 1, world.Count("medicine")); Assert.False(npc.Cargo.ContainsKey("medicine"));
        long recovery = ActionTiming.Recovery(100, ClassCatalog.Get(npc.Tactics.Build!.Class).Speed);
        var ready = world.State.Tactics.Timeline.Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == npc.Id);
        Assert.Equal(scheduled.Due + recovery, ready.Due); Assert.True(ready.PlayerControlled);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void NaturallySeededMedicineIsFiniteAndExistsBeforeAnyDowning()
    {
        int carriers = 0;
        for (int seed = 1; seed <= 8; seed++)
        {
            using var world = WorldSession.CreateDungeonRun(seed);
            carriers += world.State.Dungeon!.Recruits.Count(r => world.ActorById(r.Actor).Cargo.GetValueOrDefault("medicine") == 1);
            Assert.All(world.State.Dungeon.Recruits, r => Assert.InRange(world.ActorById(r.Actor).Cargo.GetValueOrDefault("medicine"), 0, 1));
        }
        Assert.InRange(carriers, 1, 31);
    }

    [Fact]
    public void AnAcceptedRescueStillFailsWhenThePhysicalRouteBecomesBlocked()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var hero = world.Party.Single(); var npc = PrepareRescuer(world);
        DungeonAbandonmentTests.Down(world, hero); Assert.NotNull(npc.Tactics?.Resident);
        var origin = npc.Position;
        foreach (var (x, y) in new[] { (0, -1), (1, 0), (0, 1), (-1, 0) }) world.Chunks.Edit(origin.Offset(x, y), Material.Wall);
        Assert.Null(world.DungeonCombat.AdvanceToPartyDecision());
        Assert.True(world.State.AwaitingContinuation); Assert.Equal(LifeState.Dead, hero.Life);
        Assert.Equal(origin, npc.Position); Assert.Equal(1, npc.Cargo["medicine"]);
        Assert.DoesNotContain(world.State.Tactics!.Events, e => e.Kind == "Revived");
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
