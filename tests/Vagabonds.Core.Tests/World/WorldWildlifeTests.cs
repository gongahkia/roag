using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class WorldWildlifeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-wildlife-" + Guid.NewGuid().ToString("N"));
    private string Slot { get { Directory.CreateDirectory(_directory); return Path.Combine(_directory, "world.json"); } }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }

    [Theory]
    [InlineData("character.rabbit")] [InlineData("character.spider")] [InlineData("character.bee")] [InlineData("character.bat")]
    public void RealHabitatsAreSeededRepeatableAndInspectionDoesNotCreateAnimals(string id)
    {
        var s = WorldSession.Create(42); string before = JsonSerializer.Serialize(s.State);
        var habitat = WildlifePreview.Find(s, id);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
        Assert.Equal(habitat, s.WildlifeHabitatAt(habitat.Home.Chunk));
        var another = WorldSession.Create(42); Assert.Equal(habitat, another.WildlifeHabitatAt(habitat.Home.Chunk));
        var rules = s.Content.Get<CharacterDefinition>(id).Ecology!;
        Assert.Equal(rules.Underground, habitat.Home.Floor > 0);
        Assert.Contains(s.Chunks.Get(habitat.Home.Chunk).Biomes[habitat.Home.LocalIndex], rules.Biomes);
        Assert.True(s.GroundOpen(habitat.Position)); Assert.NotEqual(habitat.Home, habitat.Position);
        if (rules.Resource != null) Assert.Equal(rules.Resource, s.ResourceAt(habitat.Home)!.Definition.Id);
        Assert.NotEqual(habitat, WildlifePreview.Find(WorldSession.Create(1337), id));
    }

    [Fact]
    public void RabbitFleesWithCollisionAndNeverStartsAnEncounter()
    {
        var s = WorldSession.Create(42); var animal = WildlifePreview.Prepare(s, "character.rabbit");
        var origin = animal.Position.Offset(1, 0); var hero = s.Party.First();
        s.Chunks.Edit(origin, Material.Floor); hero.Position = s.State.Position = origin;
        foreach (var d in WorldSession.Directions) s.Chunks.Edit(animal.Position.Offset(d.X, d.Y), Material.Floor);
        var blocked = animal.Position.Offset(-1, 0); s.Chunks.Edit(blocked, Material.Wall);
        int distance = animal.Position.Distance(origin); s.Reveal(); s.Paused = false; s.AdvanceTicks(5);
        Assert.False(s.IsInCombat); Assert.False(s.IsHostile(animal));
        Assert.True(animal.Position.Distance(origin) > distance); Assert.NotEqual(blocked, animal.Position);
        Assert.NotEqual(hero.Position, animal.Position); Assert.Contains("flees", s.WildlifeDescription(animal));
        s.Save(Slot); var loaded = WorldSession.Load(Slot); loaded.Paused = false;
        s.AdvanceTicks(20); loaded.AdvanceTicks(20);
        Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
    }

    [Theory]
    [InlineData("character.spider")] [InlineData("character.bee")] [InlineData("character.bat")]
    public void DefendersReactToTheirTerritoryAndReturnWhenThePartyLeaves(string id)
    {
        var s = WorldSession.Create(42); var animal = WildlifePreview.Prepare(s, id); var home = animal.Habitat!.Value;
        Assert.False(s.IsHostile(animal)); var hero = s.Party.First();
        hero.Position = s.State.Position = home.Offset(2, 0); s.Reveal();
        Assert.True(s.IsHostile(animal)); s.Paused = false; s.AdvanceTicks(30);
        Assert.True(s.IsInCombat); Assert.Contains(animal.Id, s.State.Battle!.ActorIds);
        // The return test isolates movement after the party has withdrawn, without changing the saved habitat.
        s.State.Battle = null; hero.Position = s.State.Position = home.Offset(s.Ecology(animal)!.Leash + 2, 0); s.Chunks.Edit(hero.Position, Material.Floor); animal.Position = home.Offset(5, 0); s.Reveal();
        Assert.False(s.IsHostile(animal)); int distance = animal.Position.Distance(home); s.AdvanceTicks(10);
        Assert.False(s.IsInCombat); Assert.True(animal.Position.Distance(home) < distance);
    }

    [Fact]
    public void WorkingAHoneyTreeProvokesItsDefenderAndDoesNotExposeHiddenAnimals()
    {
        var s = WorldSession.Create(42); var animal = WildlifePreview.Prepare(s, "character.bee"); var home = animal.Habitat!.Value;
        s.Party.First().Position = s.State.Position = home.Offset(1, 0); animal.Position = home.Offset(4, 0); s.Reveal();
        Assert.Contains("provokes", s.GatheringDanger(home)); s.Visible.Remove(animal.Position); Assert.Equal("", s.GatheringDanger(home)); s.Reveal();
        Assert.Equal(1, s.DesignateWork(WorldCommandKind.Harvest, [home]).Queued); s.Paused = false; s.AdvanceTicks(1);
        Assert.True(animal.ProvokedUntil > s.State.Tick);
        s.Party.First().Position = s.State.Position = home.Offset(6, 0); s.Reveal(); Assert.True(s.IsHostile(animal));
        s.Save(Slot); var loaded = WorldSession.Load(Slot); Assert.Equal(animal.ProvokedUntil, loaded.State.Actors.Single(a => a.Id == animal.Id).ProvokedUntil);
        s.State.Tick = animal.ProvokedUntil; Assert.False(s.IsHostile(animal));
    }

    [Fact]
    public void HuntingYieldsOnePersistentIngredientCacheWithoutGoldOrRespawns()
    {
        var s = WorldSession.Create(1337); var animal = WildlifePreview.Prepare(s, "character.rabbit"); var hero = s.Party.First();
        hero.Role = 0; hero.BaseSpeed = 30; hero.BasePower = 100;
        s.StartBattle(animal.Id); Assert.True(s.State.Battle!.Active.Friendly); Assert.True(s.Act(0, 1).Success);
        Assert.False(s.IsInCombat); Assert.Equal(LifeState.Dead, animal.Life);
        var cache = Assert.Single(s.State.Loot, c => c.SourceActor == animal.Id);
        Assert.Equal(2, cache.Contents["meat"]); Assert.Equal(0, s.State.LastEncounterReward!.Gold); Assert.Equal(6, s.State.LastEncounterReward.Experience);
        hero.Position = s.State.Position = animal.Position.Offset(1, 0); s.Chunks.Edit(hero.Position, Material.Floor); s.Reveal();
        Assert.True(s.GroundOpen(cache.Position)); Assert.True(s.Submit(new(WorldCommandKind.Loot, cache.Position)).Success);
        Assert.Equal(2, s.Count("meat")); Assert.False(s.Submit(new(WorldCommandKind.Loot, cache.Position)).Success);
        s.Save(Slot); var loaded = WorldSession.Load(Slot);
        for (int i = 0; i < 30; i++) loaded.Chunks.Get(new(i, 40));
        loaded.Reveal(); Assert.Single(loaded.State.Actors, a => a.Habitat == animal.Habitat);
        Assert.Single(loaded.State.Loot, c => c.SourceActor == animal.Id); Assert.Equal(2, loaded.Count("meat"));
        Assert.False(loaded.Submit(new(WorldCommandKind.Loot, cache.Position)).Success);
    }

    [Fact]
    public void NeutralWildlifeDoesNotJoinAnUnrelatedPartyBattle()
    {
        var s = WorldSession.Create(42); var rabbit = WildlifePreview.Prepare(s, "character.rabbit"); var hero = s.Party.First();
        var friend = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited); friend.Position = hero.Position.Offset(1, 0);
        Assert.True(s.Submit(new(WorldCommandKind.Recruit, ActorId: friend.Id)).Success);
        var foe = s.State.Actors.First(a => a.Archetype == s.Content.Game.Beast); foe.Position = rabbit.Position.Offset(1, 0);
        s.StartBattle(foe.Id); Assert.DoesNotContain(rabbit.Id, s.State.Battle!.ActorIds);
    }

    [Fact]
    public void InvalidHabitatAndForgedRemainsAreRejectedWithoutMutatingTheSlot()
    {
        var s = WorldSession.Create(42); var animal = WildlifePreview.Prepare(s, "character.spider");
        s.State.EcologyChunks.Remove(animal.Habitat!.Value.Chunk); s.Save(Slot); string before = File.ReadAllText(Slot);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot)); Assert.Equal(before, File.ReadAllText(Slot));
        s.State.EcologyChunks.Add(animal.Habitat.Value.Chunk);
        s.State.Loot.Add(new() { Id = "wildlife-" + animal.Id, SourceActor = animal.Id, Position = animal.Position, Contents = new() { ["gold"] = 999 } });
        s.Save(Slot); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }

    [Fact]
    public void EcologyContentIsStrictAndDiscoverableByAuthoringTools()
    {
        var c = ContentCatalog.Default;
        Assert.Contains(c.References, r => r.Source == "character.bee" && r.Target == "resource.honey-tree" && r.Field == "ecology.resource");
        Assert.Contains(c.References, r => r.Source == "character.spider" && r.Target == "item.silk" && r.Field == "ecology.drops");
        foreach (string field in new[] { "behavior", "resource", "leash", "drops" })
        {
            var changed = ContentCatalog.Load(c.Entries.Values.Select(d =>
            {
                var node = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!;
                if (d.Id == "character.bee")
                {
                    var e = node["ecology"]!;
                    if (field == "leash") e[field] = 1;
                    else if (field == "drops") e[field] = new JsonObject { ["item.iron-sword"] = 1 };
                    else e[field] = "unknown";
                }
                return (File: c.Source(d.Id), Text: node.ToJsonString());
            }));
            Assert.Contains(changed.Issues, i => i.Field.StartsWith("ecology.", StringComparison.Ordinal));
        }
    }
}
