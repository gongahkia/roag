using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class AcceptanceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-world-acceptance-" + Guid.NewGuid().ToString("N"));
    private string Slot => Path.Combine(_root, "world.json");
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    [Fact]
    public void IsolatedRoadBlockadeProducesRequestsAndRepairedTradeRecoversStocks()
    {
        var s = WorldSession.Create(42); int town = 1;
        var barriers = new List<WorldPosition>();
        foreach (var r in s.Plan.Roads.Where(r => r.FromTown == town || r.ToTown == town))
        {
            bool horizontal = r.A.X != r.B.X;
            var p = horizontal ? new WorldPosition((r.A.X + r.B.X) / 2, r.A.Y) : new WorldPosition(r.B.X, (r.A.Y + r.B.Y) / 2);
            for (int i = -1; i <= 1; i++) { var wall = p.Offset(horizontal ? 0 : i, horizontal ? i : 0); s.Chunks.Edit(wall, Material.Wall); barriers.Add(wall); }
        }
        Assert.Empty(s.RegionalRoute(0, 1));
        s.State.Towns[town].Stocks["food"] = 0; s.Paused = false; s.AdvanceTicks(1800);
        int shortage = s.State.Towns[town].Shortage;
        Assert.True(shortage > 0); Assert.Contains(s.State.Tasks, t => t.TownId == town && t.RoadId == null && !t.Complete);
        Assert.Contains(s.State.Tasks, t => t.RoadId != null && !t.Complete);
        foreach (var p in barriers) s.Chunks.Edit(p, Material.Earth);
        s.AdvanceTicks(2400);
        Assert.NotEmpty(s.RegionalRoute(0, 1));
        Assert.All(s.State.Tasks.Where(t => t.RoadId != null), t => Assert.True(t.Complete));
        Assert.True(s.State.Towns[town].Stocks["food"] > 0);
        Assert.Contains(s.State.Journal, j => j.Contains("Trade route repaired", StringComparison.Ordinal));
    }
    [Fact]
    public void DownedCompanionsReviveAndDeliberateAbandonmentIsImmediatelyDurable()
    {
        var s = WorldSession.Create(7); s.SavePath = Slot;
        var a = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited); a.Position = s.State.Position;
        Assert.True(s.Submit(new(WorldCommandKind.Recruit, ActorId: a.Id)).Success);
        a.Life = LifeState.Downed; a.Health = 0;
        Assert.True(s.Submit(new(WorldCommandKind.Revive, ActorId: a.Id)).Success); Assert.True(a.Health > 0);
        a.Life = LifeState.Downed; a.Health = 0;
        Assert.True(s.Submit(new(WorldCommandKind.Abandon, ActorId: a.Id)).Success);
        var loaded = WorldSession.Load(Slot);
        Assert.Equal(LifeState.Dead, loaded.State.Actors.Single(actor => actor.Id == a.Id).Life); Assert.DoesNotContain(a.Id, loaded.State.Party);
    }
    [Fact]
    public void FailedDeathSaveFreezesContinuationUntilRetrySucceeds()
    {
        var s = WorldSession.Create(7);
        var a = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited); a.Position = s.State.Position;
        s.Submit(new(WorldCommandKind.Recruit, ActorId: a.Id)); a.Life = LifeState.Downed; a.Health = 0;
        Directory.CreateDirectory(_root); File.WriteAllText(Path.Combine(_root, "file"), "blocking file");
        s.SavePath = Path.Combine(_root, "file", "save.json");
        Assert.ThrowsAny<IOException>(() => s.Submit(new(WorldCommandKind.Abandon, ActorId: a.Id)));
        Assert.True(s.DeathPending); Assert.True(s.Frozen);
        Assert.False(s.Submit(new(WorldCommandKind.Move, s.State.Position.Offset(1, 0))).Success);
        s.Save(Slot); Assert.False(s.DeathPending);
        Assert.Equal(LifeState.Dead, WorldSession.Load(Slot).State.Actors.Single(actor => actor.Id == a.Id).Life);
    }
    [Theory]
    [InlineData("actors", "null")]
    [InlineData("party", "[]")]
    [InlineData("towns", "[null]")]
    [InlineData("factions", "[]")]
    [InlineData("combatRandom", "0")]
    [InlineData("tasks", "null")]
    public void MalformedStateIsRejectedBeforeReturningASession(string key, string value)
    {
        var s = WorldSession.Create(42); s.Save(Slot);
        var json = JsonNode.Parse(File.ReadAllText(Slot))!.AsObject(); json["state"]![key] = JsonNode.Parse(value); File.WriteAllText(Slot, json.ToJsonString());
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
    [Fact]
    public void SampleVersionChangesCannotSilentlyRegenerateSavedWorlds()
    {
        var s = WorldSession.Create(42); s.Save(Slot);
        var json = JsonNode.Parse(File.ReadAllText(Slot))!; json["plan"]!["sampleHash"] = "changed"; File.WriteAllText(Slot, json.ToJsonString());
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
    [Fact]
    public void UnreferencedInterruptedSnapshotsDoNotChangeTheManifest()
    {
        var s = WorldSession.Create(42); s.Save(Slot); byte[] before = File.ReadAllBytes(Slot);
        File.WriteAllText(Path.Combine(Slot + ".chunks", "unreferenced.json"), "partial new chunk");
        var loaded = WorldSession.Load(Slot);
        Assert.Equal(42, loaded.Seed); Assert.Equal(before, File.ReadAllBytes(Slot));
    }
    [Theory]
    [InlineData("chunks")]
    [InlineData("biomes")]
    [InlineData("battle")]
    public void NullNestedSaveObjectsAreRejectedAsInvalidData(string kind)
    {
        var s = WorldSession.Create(42); s.StartBattle(s.State.Actors.First(a => a.Kind == ActorKind.Beast).Id); s.Save(Slot);
        var json = JsonNode.Parse(File.ReadAllText(Slot))!;
        if (kind == "chunks") json["chunks"] = JsonNode.Parse("[null]");
        else if (kind == "biomes") json["plan"]!["biomeRegions"] = null;
        else json["state"]!["battle"]!["units"] = JsonNode.Parse("[null,null]");
        File.WriteAllText(Slot, json.ToJsonString());
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
    [Fact]
    public void FailedStorageTakeDoesNotCreateOrMutateAStack()
    {
        var s = WorldSession.Create(42); var p = s.State.Position.Offset(1, 0); s.Chunks.Edit(p, Material.Storage);
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Take, p, Item: "wood", Amount: 999)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
    [Fact]
    public void AllPartySizesAndClassOrdersCanFightWithoutTreatingRoleAsIdentity()
    {
        for (int size = 1; size <= 4; size++)
        {
            var s = WorldSession.Create(1337 + size);
            foreach (var a in s.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(size - 1).ToArray())
            { a.Position = s.State.Position; s.Submit(new(WorldCommandKind.Recruit, ActorId: a.Id)); }
            foreach (var a in s.Party) { a.Role = 2; a.Health = a.MaxHealth; a.Mana = a.MaxMana; }
            var enemy = s.State.Actors.First(a => a.Kind == ActorKind.Beast); s.StartBattle(enemy.Id);
            Assert.Equal(size, s.State.Battle!.Units.Count(u => u.Friendly));
            int budget = 500;
            while (s.IsInCombat && budget-- > 0)
            {
                if (s.State.Battle!.Active.Friendly)
                {
                    var target = s.State.Battle.Units.First(u => !u.Friendly && u.Alive);
                    s.Act(0, target.Id);
                }
                else s.AdvanceEnemy();
            }
            Assert.True(budget > 0); Assert.False(s.State.AwaitingContinuation);
        }
    }
    [Fact]
    public void FailingTheMainArcLeavesSimulationAndExplorationAvailable()
    {
        var s = WorldSession.Create(42); s.State.Tick = 21649; s.State.Towns[s.State.Arc.TownId].Shortage = 22; s.Paused = false;
        s.AdvanceTicks(1); Assert.Equal(ArcStatus.Failed, s.State.Arc.Status); Assert.Equal(10, s.AdvanceTicks(10));
        Assert.Contains(s.State.Journal, line => line.Contains("main arc has failed", StringComparison.Ordinal));
    }
}
