using System.Text.Json;
using Roag.Core.Content;

namespace Roag.Core.World;

public static class DialoguePreview
{
    public static object Run(ContentCatalog catalog, string id, int seed)
    {
        var definition = catalog.Get<DialogueDefinition>(id);
        var world = WorldSession.Create(seed, catalog);
        var person = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited
            && (definition.Interaction == null || world.Interactions(a.Id).Any(i => i.Id == definition.Interaction)));
        person.Position = world.State.Position.Offset(1, 0);
        world.Party.First().Health /= 2;
        Roag.Core.Expeditions.ActionResult? result = definition.Interaction == null ? null : world.PerformInteraction(person.Id, definition.Interaction);
        string before = JsonSerializer.Serialize(world.State);
        var lines = WorldDialogue.Lines(world, person.Id, definition.Interaction, result?.Message ?? "");
        return new { seed, dialogue = id, person.Name, lines, result, unchangedStateDuringSpeech = before == JsonSerializer.Serialize(world.State), catalog.PresentationHash };
    }
}
