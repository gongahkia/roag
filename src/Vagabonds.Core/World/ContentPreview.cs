using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public static class ContentPreview
{
    public static object Run(ContentCatalog content, string kind, string id, int seed)
    {
        if (!content.Entries.TryGetValue(id, out var definition) || ContentCatalog.Kind(definition) != kind) throw new ArgumentException("ID must match the preview kind.");
        if (definition is ItemDefinition item)
        {
            var world = WorldSession.Create(seed, content); var hero = world.Party.First();
            world.State.Inventory[item.Stack] = 1; var before = JsonSerializer.SerializeToNode(hero, ContentCatalog.Json);
            var result = item.Slot == "none" ? new Vagabonds.Core.Expeditions.ActionResult(true, "Inventory stack; mechanics consume it through configured costs.")
                : world.Submit(new(WorldCommandKind.Equip, ActorId: hero.Id, Item: id));
            return new { seed, definition = item, before, result, after = hero, inventory = world.State.Inventory };
        }
        if (definition is VariantDefinition)
        {
            string boss = content.Game.Bosses[0];
            content = Edit(content, boss, n => n["variants"] = new JsonArray(id));
            return EncounterPreview.Run(content, boss, seed);
        }
        if (definition is GameDefinition)
        {
            var world = WorldSession.Create(seed, content);
            return new { seed, world.Plan.Id, world.State.Inventory, party = world.Party, world.FinalObjective, guardians = world.State.Actors.Count(a => a.Id == a.EncounterLeader) };
        }
        string traveller = content.Game.Traveller;
        if (definition is ClassDefinition) content = Edit(content, traveller, n => n["classes"] = new JsonArray(id));
        else if (definition is NamesDefinition) content = Edit(content, traveller, n => n["names"] = id);
        else if (definition is TraitDefinition)
        {
            string names = content.Get<CharacterDefinition>(traveller).Names ?? throw new InvalidDataException("Traveller needs a name pool to preview traits.");
            content = Edit(content, names, n => n["traits"] = new JsonArray(id));
        }
        else throw new ArgumentException("Use a registered runtime preview kind.");
        var session = WorldSession.Create(seed, content);
        session.Submit(new(WorldCommandKind.Wait, Amount: 30));
        var actor = session.Party.First(); return new { seed, actor, workRate = session.WorkRate(actor), session.State.Tick };
    }
    private static ContentCatalog Edit(ContentCatalog content, string id, Action<JsonObject> edit) => ContentCatalog.Load(content.Entries.Values.Select(d =>
    {
        var node = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!.AsObject();
        if (d.Id == id) edit(node);
        return (File: content.Source(d.Id), Text: node.ToJsonString());
    }));
}
