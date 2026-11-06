using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>Imported creatures keep their source identities, independently of the original Roag roster.</summary>
public static class RetainedEnemyCatalog
{
    private const string Prefix = "core:enemy/vagabonds/";
    public static string Id(string archetype) => Prefix + archetype;
    public static bool Contains(string id) => id.StartsWith(Prefix, StringComparison.Ordinal)
        && ContentCatalog.Default.Entries.GetValueOrDefault(id[Prefix.Length..]) is BossDefinition or CharacterDefinition { ActorKind: ActorKind.Beast or ActorKind.Raider };

    public static EnemyDefinition Definition(ContentCatalog content, Actor actor)
    {
        bool boss = content.Entries.GetValueOrDefault(actor.Archetype) is BossDefinition;
        var (hp, power, armor, speed, name) = content.Entries.GetValueOrDefault(actor.Archetype) switch
        {
            BossDefinition b => (b.Health, b.Power, b.Armor, b.Speed, b.Name),
            CharacterDefinition { ActorKind: ActorKind.Beast or ActorKind.Raider } c => (c.Health, c.Power, c.Armor, c.Speed, c.Name),
            _ => throw new ArgumentException("Only an imported creature or boss can use this controller.", nameof(actor))
        };
        if (actor.VariantId.Length > 0)
        {
            var variant = content.Get<VariantDefinition>(actor.VariantId);
            hp = checked(hp * variant.HealthPercent / 100); power = Math.Max(1, power + variant.Power);
            armor = Math.Max(0, armor + variant.Armor); speed = Math.Max(1, speed + variant.Speed);
        }
        // Fixed authored roles; neither party level nor party size scales an encounter.
        return new(Id(actor.Archetype), name, EnemyBrainKind.Guard, Math.Max(1, (hp + 2) / 3),
            Math.Max(1, (power + 1) / 2), (armor + 1) / 2, checked(10 + 15 * speed),
            100, 100, 100, 6, boss ? 24 : 4, 0, EnemyTargetPriority.NearestThreat, SpecialEncounter: boss);
    }
}

/// <summary>Only decision history: actor HP, location, variant and the timeline remain authoritative.</summary>
public sealed class RetainedEnemyMemory
{
    public int RandomState { get; set; } = 1;
    public long Actions { get; set; }
    public string LastAbility { get; set; } = "";

    internal void Validate()
    {
        if (RandomState is <= 0 or >= RoagRandom.Modulus || Actions < 0 || LastAbility == null
            || Actions == 0 && LastAbility.Length > 0
            || LastAbility.Length > 0 && ContentCatalog.Default.Entries.GetValueOrDefault(LastAbility) is not AbilityDefinition)
            throw new InvalidDataException("Invalid imported enemy decision history.");
    }
}

/// <summary>Pure planning result; the RNG is consumed only when this cast is committed.</summary>
public sealed record RetainedEnemyCast(long Target, int RandomState);

public sealed partial class TacticalCombat
{
    private EnemyDefinition EnemyDefinitionFor(Actor actor) => actor.Tactics!.Enemy?.Retained != null
        ? RetainedEnemyCatalog.Definition(_world.Content, actor) : actor.Tactics.Enemy?.CampaignBoss != null ? CampaignBossDefinition(actor)
            : _world.FinalEnemyDefinition(actor) ?? EnemyCatalog.Default.Get(actor.Tactics.DefinitionId);

    public void RegisterRetainedEnemy(Actor actor, long? readyAt = null)
    {
        if (Friendly(actor)) throw new ArgumentException("Party members use their own class controller.", nameof(actor));
        var definition = RetainedEnemyCatalog.Definition(_world.Content, actor);
        var tactics = definition.CreateTactics();
        tactics.ImmuneToPull = definition.SpecialEncounter;
        tactics.Enemy!.Retained = new() { RandomState = RoagRandom.Derive(State.RandomState, "retained-enemy:" + actor.Id) };
        Register(actor, tactics, readyAt);
        // Source sprites already name stored universal-atlas mappings.
        actor.SpriteId = _world.Content.Entries[actor.Archetype].Sprite!;
    }
}
