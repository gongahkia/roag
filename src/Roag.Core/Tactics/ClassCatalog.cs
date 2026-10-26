using System.Collections.Frozen;

namespace Roag.Core.Tactics;

public enum ClassKind
{
    Adventurer, Ranger, Arcanist, Berserker, Sentinel, Alchemist, Shade, Hookrunner,
    Trapper, Duelist, Geomancer, Spiritbinder, Tinkerer, Stormcaller, Wayfinder
}

public enum TacticalAction { Move, BasicAttack, Special, Dash, Wait, Guard, Revive, Equip, Eat, Interact, Cooperate }
public readonly record struct ClassCosts(int Move, int BasicAttack, int Special, int Dash)
{
    public int For(TacticalAction action) => action switch
    {
        TacticalAction.Move => Move,
        TacticalAction.BasicAttack => BasicAttack,
        TacticalAction.Special => Special,
        TacticalAction.Dash => Dash,
        _ => throw new ArgumentOutOfRangeException(nameof(action), "This action uses shared rules rather than class costs.")
    };
}

/// <summary>Immutable authored class data; mutable build/resources belong to each actor.</summary>
public sealed record TacticalClass(ClassKind Kind, string Key, string Name, int Health, int Damage,
    int SpecialDamage, int Defense, int Speed, ClassCosts Costs, int DashDistance,
    string BasicKey, string SpecialKey, string BasicName, string SpecialName, string DashName)
{
    public string Id => "core:actor/" + Key;
    public string BasicAbilityId => "core:ability/" + BasicKey;
    public string SpecialAbilityId => "core:ability/" + SpecialKey;
    public int CriticalChancePartsPerMillion => 0;
    public int CriticalDamagePercent => 150;
}

/// <summary>Handwritten preservation of classes.lua and its six extension classes.</summary>
public static class ClassCatalog
{
    public static IReadOnlyList<TacticalClass> All { get; } = Array.AsReadOnly<TacticalClass>([
        new(ClassKind.Adventurer, "adventurer", "Adventurer", 24, 4, 3, 0, 100, new(100, 100, 150, 130), 2,
            "basic_attack", "sweeping_slash", "Sword Strike", "Sweeping Slash", "Dash"),
        new(ClassKind.Ranger, "ranger", "Ranger", 20, 3, 3, 0, 115, new(100, 100, 155, 115), 3,
            "ranger_shot", "ranger_volley", "Bow Shot", "Piercing Volley", "Evasive Dash"),
        new(ClassKind.Arcanist, "arcanist", "Arcanist", 21, 3, 3, 0, 100, new(100, 110, 170, 115), 2,
            "arcanist_bolt", "arcanist_sigil", "Arc Bolt", "Fire Sigil", "Phase Step"),
        new(ClassKind.Berserker, "berserker", "Berserker", 32, 6, 5, 1, 75, new(115, 130, 180, 165), 2,
            "berserker_crush", "berserker_cleave", "Crushing Blow", "Broad Cleave", "Heavy Lunge"),
        new(ClassKind.Sentinel, "sentinel", "Sentinel", 29, 4, 3, 2, 90, new(105, 105, 165, 145), 2,
            "sentinel_strike", "sentinel_pulse", "Shield Strike", "Shield Pulse", "Guard Step"),
        new(ClassKind.Alchemist, "alchemist", "Alchemist", 22, 3, 4, 0, 105, new(100, 105, 165, 130), 2,
            "alchemist_vial", "alchemist_flask", "Vial Shot", "Blast Flask", "Quick Step"),
        new(ClassKind.Shade, "shade", "Shade", 20, 3, 0, 0, 115, new(100, 100, 155, 115), 2,
            "shade_blade", "shade_decoy", "Thrown Blade", "Place Decoy", "Shadow Swap"),
        new(ClassKind.Hookrunner, "hookrunner", "Hookrunner", 22, 4, 3, 0, 115, new(100, 95, 145, 110), 2,
            "hookrunner_knife", "hookrunner_hook", "Knife", "Hook Pull", "Sidestep"),
        new(ClassKind.Trapper, "trapper", "Trapper", 22, 4, 3, 0, 105, new(100, 100, 150, 120), 2,
            "trapper_knife", "trapper_snare", "Knife", "Place Snare", "Roll"),
        new(ClassKind.Duelist, "duelist", "Duelist", 20, 4, 0, 0, 115, new(100, 100, 100, 85), 1,
            "duelist_thrust", "duelist_parry", "Thrust", "Timed Parry", "Sidestep"),
        new(ClassKind.Geomancer, "geomancer", "Geomancer", 24, 5, 0, 1, 85, new(100, 120, 130, 130), 2,
            "geomancer_bolt", "geomancer_raise", "Stone Bolt", "Raise Bulwark", "Grounded Dash"),
        new(ClassKind.Spiritbinder, "spiritbinder", "Spiritbinder", 20, 3, 3, 0, 100, new(100, 110, 140, 120), 2,
            "spiritbinder_bolt", "spiritbinder_command", "Tether Bolt", "Command Spirit", "Spirit Step"),
        new(ClassKind.Tinkerer, "tinkerer", "Tinkerer", 23, 3, 0, 0, 95, new(100, 100, 160, 120), 2,
            "tinkerer_bolt", "tinkerer_deploy", "Hand Bolt", "Deploy Sentry", "Utility Roll"),
        new(ClassKind.Stormcaller, "stormcaller", "Stormcaller", 21, 2, 2, 0, 100, new(100, 100, 150, 130), 2,
            "stormcaller_bolt", "stormcaller_discharge", "Charging Bolt", "Locked Discharge", "Grounding Dash"),
        new(ClassKind.Wayfinder, "wayfinder", "Wayfinder", 22, 3, 0, 0, 110, new(100, 100, 100, 110), 2,
            "wayfinder_knife", "wayfinder_plant", "Scout Knife", "Plant Beacon", "Return / Dash")
    ]);

    private static readonly FrozenDictionary<string, TacticalClass> ById = All.ToFrozenDictionary(c => c.Id, StringComparer.Ordinal);
    private static readonly FrozenDictionary<ClassKind, TacticalClass> ByKind = All.ToFrozenDictionary(c => c.Kind);
    public static IReadOnlyList<string> Ids { get; } = Array.AsReadOnly(All.Select(c => c.Id).ToArray());
    public static TacticalClass Get(string id) => ById.TryGetValue(id, out var value) ? value : throw new ArgumentException("Unknown Roag class.", nameof(id));
    public static TacticalClass Get(ClassKind kind) => ByKind.TryGetValue(kind, out var value) ? value : throw new ArgumentOutOfRangeException(nameof(kind));
}
