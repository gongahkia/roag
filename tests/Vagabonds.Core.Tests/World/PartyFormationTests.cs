using System.Text.Json;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class PartyFormationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-formation-" + Guid.NewGuid().ToString("N"));
    private string Slot => Path.Combine(_directory, "world.json");
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }

    private static WorldSession Group(int size = 4)
    {
        var s = WorldSession.Create(42);
        for (int y = -9; y <= 9; y++) for (int x = -9; x <= 9; x++) s.Chunks.Edit(s.State.Position.Offset(x, y), Material.Floor);
        int parked = 0;
        foreach (var actor in s.State.Actors.Where(a => !a.Recruited && a.Position.Distance(s.State.Position) < 20))
        { actor.Position = s.State.Position.Offset(30 + parked++, 0); s.Chunks.Edit(actor.Position, Material.Floor); }
        foreach (var person in s.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(size - 1).ToArray())
        {
            person.Position = s.State.Position.Offset(0, -1);
            Assert.True(s.Submit(new(WorldCommandKind.Recruit, ActorId: person.Id)).Success);
            person.Position = s.State.Position.Offset(1 - s.State.Party.Count, 0);
            AssertFormation(s);
        }
        s.Reveal(); return s;
    }

    private static void AssertFormation(WorldSession s)
    {
        var party = s.Party.Where(a => a.Life != LifeState.Dead).ToArray();
        Assert.Equal(party.Length, party.Select(a => a.Position).Distinct().Count());
        Assert.Equal(s.State.Position, party[0].Position);
        Assert.All(party, a => { Assert.Equal(s.State.Position.Floor, a.Position.Floor); Assert.True(Materials.Walkable(s.Chunks.At(a.Position))); });
    }

    private static void Place(WorldSession s, WorldPosition anchor)
    {
        Assert.True(s.TryPartyPlacement(anchor, out var positions));
        s.State.Position = anchor;
        foreach (var a in s.Party) a.Position = positions[a.Id];
        s.Reveal(); AssertFormation(s);
    }

    [Fact]
    public void RecruitmentAndEveryWalkingStepKeepSeparateCellsWithoutTeleportingOrSpendingTime()
    {
        var s = Group(); var origin = s.State.Position;
        var route = new[] { (1, 0), (2, 0), (2, 1), (2, 2), (1, 2), (0, 2), (0, 1), (0, 0), (0, -1), (0, -2) };
        uint random = s.State.CombatRandom; long tick = s.State.Tick;
        s.Party.Last().Life = LifeState.Downed; s.Party.Last().Health = 0;
        foreach (var (x, y) in route)
        {
            var previous = s.Party.ToDictionary(a => a.Id, a => a.Position);
            var next = origin.Offset(x, y); Assert.True(s.Chunks.Seen(next));
            var moved = s.Submit(new(WorldCommandKind.Move, next)); Assert.True(moved.Success, next + ": " + moved.Message);
            AssertFormation(s);
            Assert.All(s.Party, a => { Assert.InRange(a.Position.Distance(previous[a.Id]), 0, 1); Assert.True(s.Chunks.Seen(a.Position)); });
        }
        Assert.Equal(random, s.State.CombatRandom); Assert.Equal(tick, s.State.Tick);
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Move, origin.Offset(8, 8))).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }

    [Fact]
    public void FourTravellersFollowCornersAndCanReverseInAOneTileCorridor()
    {
        var s = Group(); var origin = s.State.Position;
        for (int y = -9; y <= 9; y++) for (int x = -9; x <= 9; x++) s.Chunks.Edit(origin.Offset(x, y), Material.Wall);
        var corridor = new[] { (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (2, 1), (2, 2), (3, 2), (4, 2) }.Select(p => origin.Offset(p.Item1, p.Item2)).ToArray();
        foreach (var p in corridor) s.Chunks.Edit(p, Material.Floor);
        var members = s.Party.ToArray();
        for (int i = 0; i < members.Length; i++) members[i].Position = origin.Offset(-i, 0);
        s.Reveal();
        foreach (var next in corridor.Skip(4))
        {
            var before = s.Party.ToDictionary(a => a.Id, a => a.Position);
            var moved = s.Submit(new(WorldCommandKind.Move, next)); Assert.True(moved.Success, next + ": " + moved.Message);
            AssertFormation(s);
            Assert.All(s.Party, a => { Assert.Contains(a.Position, corridor); Assert.InRange(a.Position.Distance(before[a.Id]), 0, 1); });
        }
        string blocked = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Move, s.Party.Skip(1).First().Position)).Success);
        Assert.Equal(blocked, JsonSerializer.Serialize(s.State));
        var oldCells = s.Party.ToDictionary(a => a.Id, a => a.Position);
        Assert.True(s.Submit(new(WorldCommandKind.LeadParty, ActorId: s.Party.Last().Id)).Success);
        Assert.All(s.Party, a => Assert.Equal(oldCells[a.Id], a.Position));
        int index = Array.IndexOf(corridor, s.State.Position);
        foreach (var next in corridor.Take(index).Reverse())
        {
            var moved = s.Submit(new(WorldCommandKind.Move, next)); Assert.True(moved.Success, next + ": " + moved.Message);
            AssertFormation(s);
        }
    }

    [Fact]
    public void StairTransfersPlaceEveryoneSeparatelyAndRejectAnOvercrowdedLandingWithoutMutation()
    {
        var s = Group(); var stair = s.Plan.Sites[0].Center.Offset(2, 0);
        Place(s, stair);
        Assert.True(s.Submit(new(WorldCommandKind.Interact, stair)).Success);
        Assert.Equal(1, s.State.Position.Floor); AssertFormation(s);
        Assert.True(s.Submit(new(WorldCommandKind.Interact, s.State.Position)).Success);
        Assert.Equal(stair, s.State.Position); AssertFormation(s);
        var arrival = stair.Offset(-4, 0) with { Floor = 1 };
        foreach (var (x, y) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) s.Chunks.Edit(arrival.Offset(x, y), Material.Wall);
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Interact, stair)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State)); AssertFormation(s);
    }

    [Fact]
    public void RetreatPreservesFormationAndCanResumeWalking()
    {
        var s = Group(); var origin = s.State.Position;
        foreach (var a in s.Party) a.BaseSpeed = 30;
        s.StartBattle(s.State.Actors.First(a => a.Kind == ActorKind.Beast).Id);
        Assert.True(s.State.Battle!.Active.Friendly);
        int flee = s.ActiveSkills().Select((skill, index) => (skill, index)).Single(p => p.skill.Effect == "flee").index;
        Assert.True(s.Act(flee).Success); Assert.False(s.IsInCombat);
        Assert.Equal(5, s.State.Position.Distance(origin)); AssertFormation(s);
        Assert.True(s.Submit(new(WorldCommandKind.Move, new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }.Select(d => s.State.Position.Offset(d.Item1, d.Item2)).First(p => s.CanEnter(p)))).Success); AssertFormation(s);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemovingTheLeaderPromotesTheNextMemberAtTheirOwnCell(bool abandon)
    {
        var s = Group(); var leader = s.Party.First(); var successor = s.Party.Skip(1).First(); var expected = successor.Position;
        if (abandon) { leader.Life = LifeState.Downed; leader.Health = 0; }
        Assert.True(s.Submit(new(abandon ? WorldCommandKind.Abandon : WorldCommandKind.Dismiss, ActorId: leader.Id)).Success);
        Assert.Equal(expected, s.State.Position); Assert.Equal(successor.Id, s.State.Party[0]); AssertFormation(s);
        s.Save(Slot); AssertFormation(WorldSession.Load(Slot));
    }

    [Fact]
    public void EnemiesCanContactASeparatedFollower()
    {
        var s = Group(2); var origin = s.State.Position; s.Party.Last().Position = origin.Offset(-3, 0);
        var enemy = s.State.Actors.First(a => a.Kind == ActorKind.Beast); enemy.Position = origin.Offset(-2, 1);
        Assert.True(s.Submit(new(WorldCommandKind.Move, origin.Offset(1, 0))).Success);
        Assert.True(s.IsInCombat); AssertFormation(s);
    }

    [Fact]
    public void SavedFormationContinuesExactlyAndOldStackedRepresentationLoadsWithoutRewritingTheSlot()
    {
        var s = Group(); s.Save(Slot); var loaded = WorldSession.Load(Slot);
        Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        for (int i = 0; i < 6; i++)
        {
            if (i == 3)
            {
                s.Submit(new(WorldCommandKind.LeadParty, ActorId: s.Party.Last().Id));
                loaded.Submit(new(WorldCommandKind.LeadParty, ActorId: loaded.Party.Last().Id));
            }
            var next = s.State.Position.Offset(i < 3 ? 1 : -1, 0);
            var moved = s.Submit(new(WorldCommandKind.Move, next)); Assert.True(moved.Success, next + ": " + moved.Message);
            Assert.True(loaded.Submit(new(WorldCommandKind.Move, next)).Success);
            Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State)); AssertFormation(loaded);
        }
        foreach (var a in s.Party) a.Position = s.State.Position; // The former save representation.
        s.Save(Slot); byte[] bytes = File.ReadAllBytes(Slot); uint random = s.State.CombatRandom;
        loaded = WorldSession.Load(Slot); AssertFormation(loaded);
        Assert.Equal(random, loaded.State.CombatRandom); Assert.Equal(s.State.Tick, loaded.State.Tick); Assert.Equal(bytes, File.ReadAllBytes(Slot));
        Assert.Equal(JsonSerializer.Serialize(loaded.State), JsonSerializer.Serialize(WorldSession.Load(Slot).State));
    }

    [Fact]
    public void MalformedPartialOverlapAndBlockedOldStacksAreRejectedWithoutOverwritingTheSave()
    {
        var s = Group(); var members = s.Party.ToArray(); members[2].Position = members[1].Position;
        s.Save(Slot); byte[] before = File.ReadAllBytes(Slot);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot)); Assert.Equal(before, File.ReadAllBytes(Slot));
        foreach (var a in s.Party) a.Position = s.State.Position;
        foreach (var (x, y) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) s.Chunks.Edit(s.State.Position.Offset(x, y), Material.Wall);
        s.Save(Slot); before = File.ReadAllBytes(Slot);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot)); Assert.Equal(before, File.ReadAllBytes(Slot));
    }
}
