using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    [Fact]
    public void ArchiveContractCommandsMatchThePinnedOriginalPaidOutcomes()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-contracts.json")));
        int commands = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
            hero.Tactics!.Build = CharacterBuild.Create(ClassKind.Adventurer, 37);
            var (target, node) = Contract(world); target.Health = 1;
            combat.State.Queue.CancelActor(target.Id, ScheduledKind.ActorReady);
            combat.State.Queue.Schedule(ScheduledKind.ActorReady, 10000, target.Id);
            var input = sample.GetProperty("input"); int index = 0;
            if (input.GetProperty("mode").GetString() == "utility") hero.Tactics!.Supplies.Utility = new(UtilityKind.EscapeScroll);
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                string kind = command.GetString()!;
                if (kind == "leave")
                {
                    var room = world.State.Dungeon!.Floors[0].Rooms.Single(r => r.Index + 1 == node.Contract!.Room.Room);
                    hero.Position = world.State.Position = room.Origin.Offset(room.Width - 1, room.Height / 2);
                    world.Chunks.Edit(hero.Position, Material.Floor); world.Chunks.Edit(hero.Position.Offset(1, 0), Material.Floor); world.Reveal();
                }
                var intent = kind switch
                {
                    "accept" or "abandon" => new TacticalIntent(TacticalAction.Interact, Target: node.Id, Choice: 1),
                    "kill" => new(TacticalAction.BasicAttack, Facing.East), "dash" => new(TacticalAction.Dash, Facing.South),
                    "utility" => new(TacticalAction.UseUtility, Facing.South, Choice: 2), "leave" => new(TacticalAction.Move, Facing.East),
                    _ => new(TacticalAction.Wait)
                };
                long first = combat.State.NextEvent; var result = world.SubmitDungeon(intent); Assert.True(result.Success, result.Message);
                var expected = sample.GetProperty("expected")[index++]; var contract = node.Contract!;
                Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), hero.Health); Assert.Equal(expected.GetProperty("gold").GetInt32(), world.Count("gold"));
                Assert.Equal(expected.GetProperty("status").GetString(), contract.Status.ToString().ToLowerInvariant());
                Assert.Equal(expected.GetProperty("failure").GetString(), contract.Failure);
                Assert.Equal(expected.GetProperty("defeated").GetInt32(), contract.Defeated.Count);
                Assert.Equal(expected.GetProperty("spent").GetBoolean(), combat.State.Services.Nodes.Single(n => n.Id == node.Id).UsedBy != 0);
                Assert.Equal(expected.GetProperty("rewards").GetInt32(), combat.State.Events.Count(e => e.Sequence >= first && e.Kind == "CurrencyCollected" && e.Amount == 8));
                TacticalValidation.Validate(world.Plan, world.State); commands++;
            }
        }
        Assert.Equal(11, commands);
    }

    private static (Actor Target, DungeonService Node) Contract(WorldSession world)
    {
        var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var target = ReplaceRegular(world, "core:enemy/mossbound_guard"); target.Position = hero.Position.Offset(1, 0);
        var node = combat.SpawnDungeonService(DungeonServiceKind.ArchiveContract, "test:contract", hero.Position.Offset(0, 1));
        Assert.Equal(new[] { target.Id }, node.Contract!.Targets);
        return (target, node);
    }

    [Fact]
    public void ContractWaitsForARealRevivalCorpseAndSettlesOnTheNextOwningActionAfterItClears()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var (target, node) = Contract(world); target.Health = 1;
        var cantor = world.State.Actors.First(a => a.Id != target.Id && a.Position.Floor == hero.Position.Floor
            && a.Position.Distance(hero.Position) > 10 && a.Tactics?.Enemy is { Squad: null, OathPartner: 0 }
            && EnemyCatalog.Default.Get(a.Tactics.DefinitionId).Ai is EnemyBrainKind.Guard or EnemyBrainKind.Skitter or EnemyBrainKind.ExpansionRegular);
        combat.ReplaceUnactedEnemy(cantor, "core:enemy/grave_cantor");
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: node.Id, Choice: 1)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.BasicAttack, Facing.East)).Success);
        Assert.Contains(combat.State.Corpses, c => c.Actor == target.Id);
        Assert.Equal(DungeonContractStatus.Active, node.Contract!.Status); Assert.Equal(0, world.Count("gold"));
        combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [cantor.Position], "test:clear-cantor",
            AttackDelivery.Area, DamageElement.Physical, 10000, combat.Clock, IgnoresTerrain: true));
        Assert.DoesNotContain(combat.State.Corpses, c => c.Actor == target.Id);
        Assert.Equal(DungeonContractStatus.Active, node.Contract.Status); Assert.Equal(0, world.Count("gold"));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(DungeonContractStatus.Completed, node.Contract.Status); Assert.Equal(8, world.Count("gold"));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void RoomContractPaysOnceAfterItsOwningAttackAndKeepsTheSameCausalAction()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var (target, node) = Contract(world); target.Health = 1;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: node.Id, Choice: 1)).Success);
        Assert.Equal(DungeonContractStatus.Active, node.Contract!.Status); Assert.True(combat.IsContractTarget(target.Id));
        long action = combat.State.NextAction;
        Assert.True(world.SubmitDungeon(new(TacticalAction.BasicAttack, Facing.East)).Success);
        Assert.Equal(DungeonContractStatus.Completed, node.Contract.Status); Assert.Equal(8, world.Count("gold"));
        var reward = Assert.Single(combat.State.Events, e => e.Kind == "CurrencyCollected" && e.Amount == 8);
        Assert.Equal(action, reward.Action); Assert.Equal(hero.Id, combat.State.Services.Nodes.Single(n => n.Id == node.Id).UsedBy);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); Assert.Equal(8, world.Count("gold"));
        Assert.Empty(combat.DungeonServiceChoices(node.Id, hero.Id));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContractFailureDoesNotDisablePaidClassOrUtilityMovement(bool utility)
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var (_, node) = Contract(world);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: node.Id, Choice: 1)).Success);
        var origin = hero.Position;
        if (utility) hero.Tactics!.Supplies.Utility = new(UtilityKind.EscapeScroll);
        Assert.True(world.SubmitDungeon(new(utility ? TacticalAction.UseUtility : TacticalAction.Dash, Facing.South, Choice: 2)).Success);
        Assert.Equal(origin.Offset(0, 2), hero.Position);
        Assert.Equal(DungeonContractStatus.Failed, node.Contract!.Status); Assert.Equal("movement ability used", node.Contract.Failure);
        Assert.Equal(0, world.Count("gold")); Assert.Empty(combat.DungeonServiceChoices(node.Id, hero.Id));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void EnvironmentalTargetDeathFailsTheContractAndCannotAwardItsCurrency()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var (target, node) = Contract(world);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: node.Id, Choice: 1)).Success);
        combat.ResolveAttack(new(combat.State.NextAction++, 0, target.Position, [target.Position], "test:contract-environment",
            AttackDelivery.Area, DamageElement.Fire, 1000, combat.Clock, DamageCause.GroundHazard));
        Assert.Equal(DungeonContractStatus.Failed, node.Contract!.Status);
        Assert.Equal("target defeated by another faction", node.Contract.Failure);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); Assert.Equal(0, world.Count("gold"));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void ActiveContractTargetsAndOwnerContinueThroughAnActualSave()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var (target, node) = Contract(world);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: node.Id, Choice: 1)).Success);
        Assert.True(TacticalReferences.RetainsActor(world.State, target.Id));
        string root = Path.Combine(Path.GetTempPath(), "roag-contract-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(combat.State.Services), JsonSerializer.Serialize(loaded.DungeonCombat.State.Services));
            Assert.True(loaded.DungeonCombat.IsContractTarget(target.Id));
            Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: node.Id, Choice: 1)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Interact, Target: node.Id, Choice: 1)).Success);
            Assert.Equal(DungeonContractStatus.Failed, node.Contract!.Status);
            Assert.Equal(JsonSerializer.Serialize(combat.State.Services), JsonSerializer.Serialize(loaded.DungeonCombat.State.Services));
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
