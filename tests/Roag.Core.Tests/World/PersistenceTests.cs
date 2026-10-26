using System.Text.Json;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class PersistenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-world-tests-" + Guid.NewGuid().ToString("N"));
    private string Slot => Path.Combine(_directory, "campaign.json");
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    [Fact]
    public void SeedWhoseMixedCombatHashIsZeroStillHasAValidRandomStream()
    {
        int seed = unchecked((int)(600u * 0xc2b2ae35u));
        Assert.Equal(0u, WorldRandom.Hash(seed, 0, salt: 600));
        var s = WorldSession.Create(seed); s.Save(Slot);
        Assert.NotEqual(0u, WorldSession.Load(Slot).State.CombatRandom);
    }
    [Fact]
    public void AtomicManifestRestoresTerrainDiscoveryActorsClockAndExactRandomContinuation()
    {
        var s = WorldSession.Create(1337); s.Chunks.Edit(s.State.Position.Offset(1, 0), Material.Camp);
        s.Paused = false; s.AdvanceTicks(317);
        s.StartBattle(s.State.Actors.First(a => a.Kind == ActorKind.Beast).Id);
        s.Save(Slot);
        var loaded = Assert.IsType<WorldSession>(SessionStore.Load(Slot));
        Assert.True(loaded.Paused); Assert.Equal(1337, loaded.Seed);
        Assert.Equal(Material.Camp, loaded.Chunks.At(s.State.Position.Offset(1, 0)));
        Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        for (int i = 0; i < 8 && s.IsInCombat; i++)
        {
            if (s.State.Battle!.Active.Friendly)
            {
                int enemy = s.State.Battle.Units.First(u => !u.Friendly && u.Alive).Id;
                s.Act(0, enemy); loaded.Act(0, enemy);
            }
            else { s.AdvanceEnemy(); loaded.AdvanceEnemy(); }
            Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        }
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }
    [Fact]
    public void ACompanionsFallKeepsCombatAndMemoryStressInTheSameSavedState()
    {
        var s = WorldSession.Create(42); var hero = s.Party.First(); hero.BaseHealth = hero.Health = 200;
        var npc = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited); npc.Position = s.State.Position;
        s.Submit(new(WorldCommandKind.Recruit, ActorId: npc.Id)); npc.Health = 1; npc.BaseSpeed = 1;
        s.StartBattle(s.State.Actors.First(a => a.Kind == ActorKind.Beast).Id);
        var battle = s.State.Battle!; var enemy = battle.Units.First(u => !u.Friendly);
        battle.Plans[enemy.Id] = new("ability.strike", 1); enemy.IntentTarget = 1;
        int budget = 20;
        while (npc.Life == LifeState.Alive && budget-- > 0)
        { if (s.State.Battle!.Active.Friendly) s.Act(3); else s.AdvanceEnemy(); }
        Assert.Equal(LifeState.Downed, npc.Life); Assert.Contains(hero.Memories, m => m.Other == npc.Id && m.Key == "fallen-friend");
        s.Save(Slot); Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(WorldSession.Load(Slot).State));
        var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Slot))!;
        root["state"]!["battle"]!["units"]![0]!["health"] = hero.Health - 1; File.WriteAllText(Slot, root.ToJsonString());
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
    [Fact]
    public void CorruptedChunkAndGenerationVersionAreRejected()
    {
        var s = WorldSession.Create(42); s.Save(Slot);
        var snapshot = Directory.GetFiles(Slot + ".chunks")[0]; File.AppendAllText(snapshot, " ");
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
    [Fact]
    public void WipeIsDurableBeforeContinuationAndNewHeroKeepsWorldHistory()
    {
        var s = WorldSession.Create(42); s.SavePath = Slot;
        long hero = Assert.Single(s.State.Party); var actor = s.Party.Single(); actor.Health = 1;
        var enemy = s.State.Actors.First(a => a.Kind == ActorKind.Beast); enemy.Health = 18;
        s.StartBattle(enemy.Id);
        int budget = 100;
        while (s.IsInCombat && budget-- > 0)
        {
            if (s.State.Battle!.Active.Friendly) s.Act(3);
            else s.AdvanceEnemy();
        }
        Assert.False(s.IsInCombat); Assert.True(s.State.AwaitingContinuation); Assert.False(s.DeathPending);
        var resumed = WorldSession.Load(Slot); Assert.True(resumed.State.AwaitingContinuation);
        Assert.Equal(LifeState.Dead, resumed.State.Actors.Single(a => a.Id == hero).Life);
        string worldId = resumed.Plan.Id;
        Assert.True(resumed.Submit(new(WorldCommandKind.Continue)).Success);
        Assert.NotEqual(hero, Assert.Single(resumed.State.Party)); Assert.Equal(worldId, resumed.Plan.Id);
        Assert.Equal(LifeState.Dead, resumed.State.Actors.Single(a => a.Id == hero).Life);
        Assert.Contains(resumed.State.Chunks, c => c.Storage.Values.Any(v => v.GetValueOrDefault("wood") == 20));
    }
    [Fact]
    public void DeadPatronDoesNotLeaveAnUnfinishableArcAndWorldRunsAfterResolution()
    {
        var s = WorldSession.Create(1337); var arc = s.State.Arc; arc.Kind = ArcKind.TradeConflict;
        s.State.Actors.Single(a => a.Id == arc.PatronId).Life = LifeState.Dead;
        s.State.Position = s.Plan.Sites[arc.SiteId].Center with { Floor = 1 };
        s.Chunks.Edit(s.State.Position.Offset(1, 0), Material.Floor);
        Assert.True(s.Submit(new(WorldCommandKind.Move, s.State.Position.Offset(1, 0))).Success);
        Assert.Equal(1, arc.Progress);
        s.State.Position = s.Plan.Towns[arc.TownId].Center; s.State.Inventory["food"] = 20;
        Assert.True(s.Submit(new(WorldCommandKind.ResolveArc)).Success); Assert.Equal(ArcStatus.Resolved, arc.Status);
        s.Paused = false; Assert.Equal(300, s.AdvanceTicks(300));
        Assert.Contains(s.State.Journal, line => line.Contains("patron's death", StringComparison.Ordinal));
    }
}
