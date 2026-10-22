using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public static class SimulationPreview
{
    public static object Recipe(ContentCatalog content, string id, int seed)
    {
        var recipe = content.Get<RecipeDefinition>(id);
        content = WithGame(content, game => game["recipes"] = new JsonArray(new[] { id }.Concat(content.Game.Recipes.Where(other => other != id)).Select(value => (JsonNode?)JsonValue.Create(value)).ToArray()));
        var s = WorldSession.Create(seed, content);
        if (recipe.Output != null)
        {
            s.Submit(new(WorldCommandKind.Build, s.State.Position.Offset(1, 0), Material: Material.Camp));
            s.Submit(new(WorldCommandKind.Wait, Amount: 50));
        }
        var before = new Dictionary<string, int>(s.State.Inventory);
        var result = recipe.Output != null ? s.Submit(new(WorldCommandKind.Craft, Item: id))
            : s.Submit(new(WorldCommandKind.Build, s.State.Position.Offset(1, 0), Material: recipe.Material));
        var queued = s.State.Work.Select(w => new { w.Kind, w.Required, w.Reserved, w.Worker }).ToArray();
        s.Submit(new(WorldCommandKind.Wait, Amount: 100));
        return new { seed, recipe = id, result, queued, tick = s.State.Tick, before, after = s.State.Inventory, pending = s.State.Work, s.State.Structures, body = s.Party.First().Body };
    }
    public static object Simulation(ContentCatalog content, int seed)
    {
        var s = WorldSession.Create(seed, content); var actor = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        actor.Position = s.State.Position.Offset(1, 0); actor.Body.Hunger = 40; actor.Body.Fatigue = 35; actor.Body.Wounds = 1;
        var before = JsonSerializer.SerializeToNode(actor, ContentCatalog.Json);
        var build = s.Submit(new(WorldCommandKind.Build, s.State.Position.Offset(-1, 0), Material: Material.Wall));
        var order = s.State.Work.FirstOrDefault();
        var assign = order != null ? s.Submit(new(WorldCommandKind.AssignWork, ActorId: actor.Id, WorkId: order.Id)) : build;
        s.Submit(new(WorldCommandKind.Wait, Amount: 100));
        s.State.Position = actor.Position; foreach (var member in s.Party) member.Position = actor.Position; s.Reveal();
        var recruit = s.Submit(new(WorldCommandKind.Recruit, ActorId: actor.Id));
        return new { seed, before, build, assign, recruit, actor, work = s.State.Work, structures = s.State.Structures, s.State.Tick, sameIdentity = s.State.Actors.Count(a => a.Id == actor.Id) == 1 };
    }
    public static object Incident(ContentCatalog content, string id, int seed)
    {
        var definition = content.Get<IncidentDefinition>(id);
        // Isolate the selected incident in a temporary catalog, using the real eligibility/handler path.
        content = WithGame(content, game => game["incidents"] = new JsonArray(id));
        var s = WorldSession.Create(seed, content); var actor = s.Party.First();
        actor.Body.Hunger = 100; actor.Body.Fatigue = 95; actor.Body.Wounds = 2; actor.Body.WorkDone = 30;
        actor.Trust[s.State.Actors.First(a => a.Id != actor.Id).Id] = 20;
        if (definition.Condition == "hunger") { s.State.Inventory["food"] = 0; s.State.Towns[0].Stocks["food"] = 0; }
        s.Submit(new(WorldCommandKind.Wait, Amount: 300));
        return new { seed, incident = id, happened = s.State.IncidentAfter.ContainsKey(id), actor.Body, actor.Memories, s.State.Journal, s.State.Tick };
    }
    public static ContentCatalog WithGame(ContentCatalog content, Action<JsonObject> edit) => ContentCatalog.Load(content.Entries.Values.Select(d =>
    {
        var node = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!.AsObject();
        if (d.Id == "game.vagabonds") edit(node);
        return (File: content.Source(d.Id), Text: node.ToJsonString());
    }));
}
