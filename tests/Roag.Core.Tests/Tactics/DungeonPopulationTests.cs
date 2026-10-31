using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonPopulationTests
{
    [Fact]
    public void SeededIdeaSelectionAndBiomeSquadRostersMatchOriginalActualPlans()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-population.json")));
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            int seed = sample.GetProperty("seed").GetInt32(); string floor = sample.GetProperty("floor").GetString()!;
            var squad = DungeonEncounterSelection.Squad(seed, sample.GetProperty("biome").GetString()!, floor);
            var idea = DungeonEncounterSelection.Idea(seed, floor); var expected = sample.GetProperty("expected");
            string name = squad.Kind switch { EnemySquadKind.WardedBodyguards => "warded_bodyguards", EnemySquadKind.HealerRushwings => "healer_rushwings",
                EnemySquadKind.SnaresAndSiege => "snares_and_siege", EnemySquadKind.ShieldAndArcher => "shield_and_archer", _ => "captain_and_followers" };
            Assert.Equal(expected.GetProperty("squad").GetString(), name);
            Assert.Equal(expected.GetProperty("followers").GetInt32(), squad.Followers);
            Assert.Equal(expected.GetProperty("derived").GetInt32(), idea.Seed);
            Assert.Equal(expected.GetProperty("idea").ValueKind == JsonValueKind.False ? null : expected.GetProperty("idea").GetString(), idea.Definition);
        }
    }

    [Fact]
    public void NaturalDungeonPopulationRetainsFiveChestBudgetAndFiniteLinkedSquadRoster()
    {
        for (int seed = 1; seed <= 12; seed++)
        {
            using var world = WorldSession.CreateDungeonRun(seed); var state = world.DungeonCombat.State; var run = world.State.Dungeon!;
            Assert.Equal(5, state.Chests.Chests.Count);
            Assert.Equal(2, state.Chests.Chests.Count(c => c.Position.Floor == 1));
            Assert.Equal(3, state.Chests.Chests.Count(c => c.Position.Floor == 2));
            foreach (var group in world.State.Actors.Where(a => a.Tactics?.Enemy?.Squad != null).GroupBy(a => a.Tactics!.Enemy!.Squad!.Id))
            {
                var squad = group.First().Tactics!.Enemy!.Squad!; var leader = Assert.Single(group, a => a.Id == squad.Leader);
                Assert.Equal(EnemySquads.Roster(squad.Kind).Leader, leader.Tactics!.DefinitionId);
                Assert.InRange(group.Count(), 3, 6); // Starting biome uses captain plus 2..5 followers.
                var site = run.Floors.SelectMany(f => f.Rooms).Single(r => r.Contains(squad.Home));
                Assert.DoesNotContain(site.Role, new[] { "start", "elite" });
                Assert.All(group, a => { Assert.Equal(squad, a.Tactics!.Enemy!.Squad); Assert.True(site.Contains(a.Position)); });
            }
            TacticalValidation.Validate(world.Plan, world.State);
        }
    }

    [Fact]
    public void EachPrimaryIdeaEnemyCanPopulateNaturallyWithSafeSpecialEncounterDependencies()
    {
        var wanted = EnemyCatalog.Default.Definitions.Where(e => e.Ai == EnemyBrainKind.IdeaRegular
            && e.Id is not ("core:enemy/bone_fragment" or "core:enemy/oath_twin_b")).Select(e => e.Id).ToHashSet();
        var seen = new HashSet<string>();
        string source = DungeonCatalog.Default.Biomes[0].FloorId + "/stage/1/depth/1";
        int worlds = 0;
        for (int seed = 1; seed < 20000 && seen.Count < wanted.Count; seed++)
        {
            int root = (int)(WorldRandom.Hash(seed, 0, salt: 9401) % (RoagRandom.Modulus - 1)) + 1;
            var selection = DungeonEncounterSelection.Idea(root, source);
            if (selection.Definition == null || seen.Contains(selection.Definition)) continue;
            using var world = WorldSession.CreateDungeonRun(seed); worlds++;
            Assert.True(worlds < 60, "Idea encounter coverage did not find a legal construction within its bounded sample.");
            var actor = world.State.Actors.FirstOrDefault(a => a.Position.Floor == 1 && a.Tactics?.DefinitionId == selection.Definition);
            if (actor == null) continue; // A failed physical fit must not force or repaint an encounter.
            seen.Add(selection.Definition);
            var floor = world.State.Dungeon!.Floors[0]; var room = floor.Rooms.Single(r => r.Contains(actor.Position));
            if (actor.Tactics!.DefinitionId == EnemySentinel.Definition) Assert.False(room.Critical);
            if (actor.Tactics.DefinitionId == EnemyOaths.A)
            {
                var partner = world.State.Actors.Single(a => a.Id == actor.Tactics.Enemy!.OathPartner);
                Assert.Equal(EnemyOaths.B, partner.Tactics!.DefinitionId); Assert.Equal(actor.Id, partner.Tactics.Enemy!.OathPartner);
            }
            if (actor.Tactics.DefinitionId == "core:enemy/grave_cantor")
                Assert.Contains(world.State.Actors, a => a.Id != actor.Id && a.Tactics?.Enemy != null
                    && EnemyRevivalRules.Revivable(a.Tactics.DefinitionId) && a.Position.Distance(actor.Position) <= 4);
            TacticalValidation.Validate(world.Plan, world.State);
        }
        Assert.Equal(wanted.Order(), seen.Order());
    }
}
