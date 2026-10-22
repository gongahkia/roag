using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

/// <summary>Isolated, reproducible setup; abilities still use the normal initiative and action resolver.</summary>
public static class CooperationPreview
{
    public static void Prepare(WorldSession world)
    {
        CampPreview.Prepare(world);
        int speed = 30;
        foreach (var actor in world.Party)
        {
            // This fixture measures cooperation itself, independent of generated combat dispositions.
            actor.CombatTrait = "talent.quick-footed"; actor.Talents.Clear();
            actor.Body.Wounds = 0; actor.Body.Fatigue = 0;
            actor.BaseSpeed = speed; speed -= 5;
            actor.Mana = actor.MaxMana; actor.Health = Math.Max(1, actor.MaxHealth - 12);
        }
        var enemy = world.State.Actors.First(a => a.Kind == ActorKind.Beast && a.EncounterLeader == 0);
        enemy.Position = world.State.Position.Offset(8, 0); world.Chunks.Edit(enemy.Position, Material.Floor);
        enemy.BaseHealth = enemy.Health = 200; enemy.BaseSpeed = 1; enemy.BasePower = 4;
        world.StartBattle(enemy.Id); world.Paused = true;
    }
    public static object Run(ContentCatalog content, string id, int seed)
    {
        var definition = content.Get<AbilityDefinition>(id);
        content = SimulationPreview.WithGame(content, game => game["cooperativeAbilities"] = new JsonArray(id));
        var world = WorldSession.Create(seed, content); Prepare(world); var battle = world.State.Battle!;
        int partner = battle.Units.Last(u => u.Friendly).Id, target = battle.Units.First(u => !u.Friendly).Id;
        if (definition.Target == TargetKind.AllAllies)
            foreach (var unit in battle.Units.Where(u => u.Friendly)) { unit.Burn = unit.Bleed = unit.Mark = 2; }
        var before = JsonSerializer.SerializeToNode(battle, ContentCatalog.Json);
        var preview = world.ActionPreview(6, target, partner); var result = world.Act(6, target, partner);
        return new { seed, definition, setup = "Four generated, wounded companions and a durable foe. The helper spends mana and their scheduled action; no save.",
            before, preview, result, after = battle, events = world.State.CombatEvents, world.State.Tick };
    }
}
