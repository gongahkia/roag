using System.Text.RegularExpressions;
using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record DialogueLine(long Speaker, string Name, string Sprite, string Text);

/// <summary>Resolves authored speech against real actors; never applies effects or advances RNG/time.</summary>
public static class WorldDialogue
{
    public static readonly string[] Tokens = ["name", "player", "background", "motivation", "job", "friend", "town", "faction", "result"];
    public static IReadOnlyList<DialogueLine> Lines(WorldSession world, long person, string? interaction = null, string result = "")
    {
        var actor = world.State.Actors.Single(a => a.Id == person);
        var player = world.Party.First(a => a.Life == LifeState.Alive);
        string consequence = world.ConsequenceDialogue(actor, player);
        if (interaction == null && consequence.Length > 0) return [new(actor.Id, actor.Name, actor.Sprite, consequence)];
        var definition = world.Content.All<DialogueDefinition>().FirstOrDefault(d => d.Interaction == interaction);
        if (definition == null) return [new(actor.Id, actor.Name, actor.Sprite, consequence.Length > 0 ? consequence : result.Length > 0 ? result : "Good to meet you. I am " + actor.Name + ".")];
        bool Eligible(DialogueVariant v) => v.When switch
        {
            "always" => true, "trusted" => actor.Trust.GetValueOrDefault(player.Id) >= 10,
            "injured" => actor.Body.Wounds > 0 || actor.Health < actor.MaxHealth / 2,
            "hungry" => actor.Body.Hunger >= world.Content.Get<SimulationDefinition>(world.Content.Game.Simulation).MealAt,
            _ => world.Content.Entries.GetValueOrDefault(actor.TraitId) is TraitDefinition t && t.Value == v.When
        };
        var variants = definition.Variants.Where(v => v.When != "always" && Eligible(v)).ToArray();
        if (variants.Length == 0) variants = definition.Variants.Where(v => v.When == "always").ToArray();
        // Stable voice/personality, independent of all simulation random streams.
        var variant = variants[(int)(WorldRandom.Hash(world.Seed, (int)actor.Id, (int)player.Id) % (uint)variants.Length)];
        var town = world.Plan.Towns[actor.HomeTown];
        var tokens = new Dictionary<string, string>
        {
            ["name"] = actor.Name, ["player"] = player.Name, ["background"] = ("aeiou".Contains(char.ToLowerInvariant(actor.Background.FirstOrDefault())) ? "an " : "a ") + actor.Background,
            ["motivation"] = Regex.Replace(actor.Motivation, @"\btheir\b", "my"), ["job"] = actor.Job,
            ["friend"] = world.State.Actors.FirstOrDefault(a => a.Id == actor.FriendId)?.Name ?? "an old friend far from here",
            ["town"] = town.Name, ["faction"] = world.State.Factions[town.FactionId].Name,
            ["result"] = result.StartsWith(actor.Name + ": ", StringComparison.Ordinal) ? result[(actor.Name.Length + 2)..] : result
        };
        string Resolve(string line) => Regex.Replace(line, "\\{([^{}]+)\\}", match => tokens[match.Groups[1].Value]);
        var lines = new List<DialogueLine>();
        if (variant.Player.Length > 0) lines.Add(new(player.Id, player.Name, player.Sprite, Resolve(variant.Player)));
        if (consequence.Length > 0) lines.Add(new(actor.Id, actor.Name, actor.Sprite, consequence));
        lines.AddRange(variant.Replies.Select(line => new DialogueLine(actor.Id, actor.Name, actor.Sprite, Resolve(line))));
        return lines;
    }
}
