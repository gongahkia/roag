using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public static class AbilityPreview
{
    public static object Run(ContentCatalog content, string id, int seed)
    {
        var definition = content.Get<AbilityDefinition>(id);
        if (definition.Cooperation != null) return CooperationPreview.Run(content, id, seed);
        content = SimulationPreview.WithGame(content, game => game["commonAbilities"] = new JsonArray(id, content.Game.CommonAbilities[1], content.Game.CommonAbilities[2]));
        var s = WorldSession.Create(seed, content); var hero = s.Party.First();
        // Explicit diagnostic setup supplies mana, wounds and stress so every handler can be inspected.
        hero.BaseMana = hero.Mana = 100; hero.BaseSpeed = 30; hero.Health = Math.Max(1, hero.MaxHealth / 2); hero.Stress = 50;
        var enemy = s.State.Actors.First(a => a.Kind == ActorKind.Beast); enemy.BaseSpeed = 1; enemy.BaseHealth = enemy.Health = 200;
        s.StartBattle(enemy.Id); var battle = s.State.Battle!;
        var before = JsonSerializer.SerializeToNode(battle.Units, ContentCatalog.Json); var inventory = new Dictionary<string, int>(s.State.Inventory);
        int target = definition.Target == TargetKind.Ally ? battle.Active.Id : battle.Units.First(u => !u.Friendly).Id;
        var result = s.Act(3, target);
        return new { seed, definition, setup = "Half-health/stressed traveller, 100 mana and a durable foe; actual resolver, no save.", before, result,
            after = battle.Units, inventoryBefore = inventory, inventoryAfter = s.State.Inventory, partyAfter = s.Party, s.State.Tick, combatContinues = s.IsInCombat };
    }
}
