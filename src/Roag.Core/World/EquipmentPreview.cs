using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

public static class EquipmentPreview
{
    public static object Run(ContentCatalog catalog, string id, int seed)
    {
        var affix = catalog.Get<AffixDefinition>(id);
        var basis = catalog.All<ItemDefinition>().First(i => affix.Slots.Contains(i.Slot));
        // Isolate the requested registered effect on real equipment for its runtime preview.
        catalog = ContentCatalog.Load(catalog.Entries.Values.Select(d =>
        {
            var node = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!;
            if (d.Id == basis.Id) node["effects"] = new JsonArray(id);
            return (File: catalog.Source(d.Id), Text: node.ToJsonString());
        }));
        var world = WorldSession.Create(seed, catalog); var actor = world.Party.First();
        if (affix.Effect is "dig" or "mine" or "fish")
        {
            var target = ResourcePreview.Prepare(world, affix.Effect == "fish" ? "resource.fishing-bank" : "resource.iron-deposit");
            world.State.Inventory[basis.Stack] = 1; var equipped = world.Submit(new(WorldCommandKind.Equip, ActorId: actor.Id, Item: basis.Id));
            var result = world.DesignateWork(affix.Effect == "dig" ? WorldCommandKind.Dig : WorldCommandKind.Harvest, [target]);
            double rate = world.WorkMultiplier(actor, world.State.Work.Single());
            for (int t = 0; t < 200 && world.State.Work.Count > 0; t++) world.AdvanceTicks(1);
            return new { seed, id, equipped, result, rate, ticks = world.State.Tick, remainingOrders = world.State.Work.Count, workDone = actor.Body.WorkDone };
        }
        CampPreview.Prepare(world); actor.Role = affix.Effect == "healing" ? 3 : 2; actor.Health = actor.MaxHealth; actor.Body.Wounds = 0;
        world.State.Inventory[basis.Stack] = 1; var equip = world.Submit(new(WorldCommandKind.Equip, ActorId: actor.Id, Item: basis.Id));
        world.StartBattle(world.State.Actors.First(a => a.Kind == ActorKind.Beast).Id); var battle = world.State.Battle!;
        var foe = battle.Units.First(u => !u.Friendly); ActionResult action;
        if (affix.Effect is "protect" or "resolve")
        {
            var target = affix.Effect == "protect" ? battle.Units[1] : battle.Units[0]; target.Health = Math.Max(1, target.MaxHealth / 2);
            battle.Order = new[] { foe.Id }.Concat(battle.Units.Where(u => u.Friendly).Select(u => u.Id)).ToList(); battle.TurnIndex = 0;
            battle.Plans[foe.Id] = new(affix.Effect == "resolve" ? "ability.dread" : "ability.strike", target.Id);
            action = world.AdvanceEnemy();
        }
        else
        {
            battle.Order = battle.Units.Select(u => u.Id).ToList(); battle.TurnIndex = 0;
            string effect = affix.Effect == "healing" ? "heal" : "burn";
            int index = world.ActiveSkills().Select((skill, i) => (skill, i)).First(p => p.skill.Effect == effect).i;
            if (effect == "heal") battle.Units[1].Health = Math.Max(1, battle.Units[1].MaxHealth / 2);
            action = world.Act(index, effect == "heal" ? 1 : foe.Id);
        }
        return new { seed, id, equip, action, effects = world.EquipmentDescription(world.Item(basis.Id)), events = world.State.CombatEvents };
    }
}
