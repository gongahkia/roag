using System.Text.Json;
using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record RewardEquipmentPreview(string Id, string Base, string Name, string Slot, string[] Effects);
public sealed record EncounterRewardReport(int Seed, string Reward, EncounterReward Granted, Dictionary<string, int> InventoryDelta,
    RewardEquipmentPreview[] Equipment, CombatEvent[] Events, bool SaveRoundTripExact, bool SourceClaimPersisted, bool ReplayBlocked)
{
    public bool Passed => SaveRoundTripExact && SourceClaimPersisted && ReplayBlocked;
}

/// <summary>Grants an authored profile through a real victory, reloads it, then checks the persistent source cannot pay twice.</summary>
public static class EncounterRewardPreview
{
    public static EncounterRewardReport Run(ContentCatalog catalog, string id, int seed)
    {
        catalog.Get<RewardDefinition>(id);
        // Isolate the selected reward without changing its definition or bypassing combat grants.
        var selected = ContentCatalog.Load(catalog.Entries.Values.Select(definition =>
        {
            var node = JsonSerializer.SerializeToNode<ContentDefinition>(definition, ContentCatalog.Json)!;
            if (definition.Id == catalog.Game.Raider) node["reward"] = id;
            return (File: catalog.Source(definition.Id), Text: node.ToJsonString());
        }));
        if (selected.Issues.Count > 0) throw new InvalidDataException("Reward preview catalog is invalid.");
        var world = WorldSession.Create(seed, selected); CampPreview.Prepare(world);
        foreach (var actor in world.Party)
        {
            actor.Body.Wounds = 0; actor.Body.Fatigue = 0;
            actor.Health = actor.MaxHealth; actor.Mana = actor.MaxMana;
        }
        var hero = world.Party.First(); hero.Role = 1; hero.BaseSpeed = 30;
        long source = world.State.NextActorId++;
        var enemy = ActorFactory.Create(selected, selected.Game.Raider, seed, source, world.State.Position.Offset(6, 0), 0);
        enemy.BaseSpeed = 1; enemy.Health = 1; world.State.Actors.Add(enemy);
        var before = new Dictionary<string, int>(world.State.Inventory);
        Win(world);
        var granted = world.State.LastEncounterReward!; var events = world.State.CombatEvents.ToArray();
        var delta = world.State.Inventory.Keys.Union(before.Keys).Order(StringComparer.Ordinal)
            .Where(key => world.Count(key) != before.GetValueOrDefault(key))
            .ToDictionary(key => key, key => world.Count(key) - before.GetValueOrDefault(key));
        var equipment = granted.Items.Where(item => world.Item(item.Item).Slot != "none").Select(item =>
        {
            var definition = world.Item(item.Item);
            return new RewardEquipmentPreview(item.Item, world.State.GeneratedEquipment.GetValueOrDefault(item.Item)?.Base ?? item.Item,
                definition.Name, definition.Slot, definition.Effects);
        }).ToArray();
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-reward-preview-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "world.json"); world.Save(path);
            var loaded = WorldSession.Load(path, selected);
            bool exact = JsonSerializer.Serialize(world.State) == JsonSerializer.Serialize(loaded.State);
            bool claimed = loaded.State.RewardedEncounterIds.Contains(source);
            string inventory = JsonSerializer.Serialize(loaded.State.Inventory);
            string experience = JsonSerializer.Serialize(loaded.Party.Select(a => new { a.Id, a.Level, a.Experience }));
            // A deliberate replay fixture probes the idempotency guard after an actual save/load.
            var repeated = loaded.State.Actors.Single(a => a.Id == source); repeated.Life = LifeState.Alive; repeated.Health = 1;
            Win(loaded);
            bool blocked = loaded.State.LastEncounterReward is { Gold: 0, Food: 0, Experience: 0, Items.Length: 0, Sources.Length: 0 }
                && inventory == JsonSerializer.Serialize(loaded.State.Inventory)
                && experience == JsonSerializer.Serialize(loaded.Party.Select(a => new { a.Id, a.Level, a.Experience }));
            return new(seed, id, granted, delta, equipment, events, exact, claimed, blocked);
        }
        finally { Directory.Delete(directory, true); }

        void Win(WorldSession session)
        {
            session.StartBattle(source);
            var battle = session.State.Battle ?? throw new InvalidDataException("Reward preview did not start combat.");
            var result = session.Act(0, battle.ActorIds.IndexOf(source));
            if (!result.Success || session.State.Battle != null || session.State.LastEncounterReward == null)
                throw new InvalidDataException("Reward preview did not resolve the intended victory: " + result.Message);
        }
    }
}
