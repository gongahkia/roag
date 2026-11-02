using System.Collections.Frozen;
using System.Collections.Immutable;
using Roag.Core.Content;

namespace Roag.Core.Tactics;

public sealed record RetainedBoon(string Id, string Source, string Sprite, bool Active, ImmutableArray<ClassKind> Classes);

/// <summary>Explicit content adaptation; acquired through the ordinary owned boon inventory.</summary>
public static class RetainedBoons
{
    private static readonly Lazy<ImmutableArray<RetainedBoon>> Entries = new(BuildEntries);
    public static ImmutableArray<RetainedBoon> All => Entries.Value;
    private static readonly Lazy<FrozenDictionary<string, RetainedBoon>> Index = new(() => All.ToFrozenDictionary(e => e.Id));
    public static RetainedBoon? Find(string id) => Index.Value.GetValueOrDefault(id);
    public static string Id(string source) => "core:boon/expedition_" + source[(source.IndexOf('.') + 1)..].Replace('-', '_');

    private static ImmutableArray<RetainedBoon> BuildEntries()
    {
        var result = ImmutableArray.CreateBuilder<RetainedBoon>();
        void Add(string source, bool active, params ClassKind[] classes)
        {
            var content = ContentCatalog.Default.Entries[source];
            result.Add(new(Id(source), source, content.Sprite!, active, [.. classes]));
        }
        Add("ability.shield-bash", true, ClassKind.Sentinel, ClassKind.Adventurer, ClassKind.Geomancer);
        Add("ability.stand-fast", true, ClassKind.Sentinel, ClassKind.Geomancer, ClassKind.Spiritbinder);
        Add("ability.hunters-mark", true, ClassKind.Ranger, ClassKind.Trapper, ClassKind.Wayfinder);
        Add("ability.volley", true, ClassKind.Ranger, ClassKind.Trapper);
        Add("ability.ember", true, ClassKind.Arcanist, ClassKind.Alchemist);
        Add("ability.chain-arc", true, ClassKind.Arcanist, ClassKind.Stormcaller);
        Add("ability.mend", true, ClassKind.Spiritbinder, ClassKind.Alchemist);
        Add("ability.absolution", true, ClassKind.Spiritbinder, ClassKind.Sentinel);
        foreach (var talent in ContentCatalog.Default.All<TalentDefinition>().OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            ClassKind[] classes = talent.Class switch
            {
                "class.warden" => [ClassKind.Sentinel, ClassKind.Adventurer, ClassKind.Geomancer, ClassKind.Berserker],
                "class.ranger" => [ClassKind.Ranger, ClassKind.Trapper, ClassKind.Wayfinder],
                "class.arcanist" => [ClassKind.Arcanist, ClassKind.Stormcaller, ClassKind.Alchemist],
                "class.vestal" => [ClassKind.Spiritbinder, ClassKind.Alchemist, ClassKind.Sentinel],
                null or "" => [],
                _ => throw new InvalidDataException("Unmapped retained talent class: " + talent.Id)
            };
            Add(talent.Id, false, classes);
        }
        return result.ToImmutable();
    }

    internal static IEnumerable<BoonDefinition> Definitions()
    {
        foreach (var entry in All)
        {
            var source = ContentCatalog.Default.Entries[entry.Source];
            string description = entry.Active
                ? "Unlock " + source.Name + ". Uses your class recovery; existing moves remain available. Stronger copies increase its power."
                : source is TalentDefinition talent ? "Per strength: " + Describe(talent) + " Each bonus is capped at 20."
                    : throw new InvalidDataException("Invalid retained boon source.");
            yield return new()
            {
                Id = entry.Id, Name = source.Name, Description = description, Tags = ["expedition", entry.Active ? "active-skill" : "development"],
                Triggers = [], Scope = new(BoonScopeKind.Owner), StackPolicy = BoonStackPolicy.AddPower,
                Parameters = BoonCatalog.Rarities.Select((rarity, index) => (rarity, value: new BoonStrength(0, index + 1))).ToImmutableDictionary(p => p.rarity, p => p.value),
                Effects = [], EligibleClasses = entry.Classes.Select(c => ClassCatalog.Get(c).Id).ToImmutableArray(), Unit = "strength"
            };
        }
    }

    private static string Describe(TalentDefinition talent) => string.Join(" ", talent.Bonuses.Select(p => p.Key switch
    {
        "wounded-damage" => $"Direct power +{p.Value} against targets already at half HP or lower.",
        "marked-damage" => $"Direct power +{p.Value} against marked targets.",
        "stunned-damage" => $"Direct power +{p.Value} against stunned targets.",
        "all-target-damage" => $"Area skill power +{p.Value} per target.",
        "burn-damage" => $"Burning attack power +{p.Value}; ongoing burn keeps its own power.",
        "cooperative-damage" => $"Initiated cooperative attack power +{p.Value}.",
        "healing" => $"Healing, medicine and cleansing restore {p.Value} extra HP.",
        "initiative" => $"Speed +{p.Value}; committed recovery stays unchanged.",
        "mana-discount" => $"Initiated retained and cooperative skills cost {p.Value * 10} less base recovery; helper costs stay unchanged.",
        "calm" => $"Calming removes {p.Value} extra stress.",
        "protect" => $"Cover another nearby wounded companion for up to {p.Value} damage once before your next turn.",
        _ => throw new InvalidDataException("Unmapped retained talent effect: " + p.Key)
    }));
}
