using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

public static class AbilityPreview
{
    public static object Run(ContentCatalog content, string id, int seed)
    {
        var definition = content.Get<AbilityDefinition>(id);
        if (definition.PreparationTurns > 0) return Prepared(content, definition, seed);
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
    private static object Prepared(ContentCatalog content, AbilityDefinition definition, int seed)
    {
        string archetype = "character.prepared-preview";
        while (content.Entries.ContainsKey(archetype)) archetype += "-x";
        var diagnostic = JsonSerializer.SerializeToNode<ContentDefinition>(content.Get<CharacterDefinition>(content.Game.Beast), ContentCatalog.Json)!;
        diagnostic["id"] = archetype; diagnostic["name"] = "Prepared ability preview";
        diagnostic["behaviors"] = JsonSerializer.SerializeToNode(new[] { new BehaviorRule { Ability = definition.Id } }, ContentCatalog.Json);
        diagnostic["health"] = 1000; diagnostic["mana"] = 100; diagnostic["speed"] = 1;
        diagnostic["ecology"] = null;
        var original = content;
        content = ContentCatalog.Load(original.Entries.Values.Select(d =>
            (File: original.Source(d.Id), Text: JsonSerializer.Serialize<ContentDefinition>(d, ContentCatalog.Json)))
            .Append((File: "preview/prepared-character.json", Text: diagnostic.ToJsonString())));
        var world = WorldSession.Create(seed, content); var hero = world.Party.First();
        hero.BaseHealth = hero.Health = 1000; hero.BaseSpeed = 30;
        var position = world.State.Position.Offset(8, 0); world.Chunks.Edit(position, Material.Floor);
        var enemy = ActorFactory.Create(content, archetype, seed, world.State.NextActorId++, position, 0);
        world.State.Actors.Add(enemy);
        world.StartBattle(enemy.Id);
        var frames = new List<object>(); ActionResult result = new(false, "Prepared ability did not release within the preview budget.");
        for (int i = 0; i < 24 && world.State.Battle is { } battle; i++)
        {
            long cursor = world.State.NextCombatEvent;
            var action = battle.Active.Friendly ? world.Act(3) : world.AdvanceEnemy();
            frames.Add(new { battle.Round, action, intentions = battle.Plans.ToDictionary(p => p.Key, p => p.Value), prepared = battle.Prepared.ToArray() });
            if (world.State.CombatEvents.Any(e => e.Sequence >= cursor && e.Kind == "ability" && e.AbilityId == definition.Id))
            { result = action; break; }
        }
        return new { seed, definition, setup = "Durable guarding traveller against an enemy with the selected prepared ability; actual preparation and release.", result,
            frames, events = world.State.CombatEvents, world.State.Tick, combatContinues = world.IsInCombat };
    }

}
