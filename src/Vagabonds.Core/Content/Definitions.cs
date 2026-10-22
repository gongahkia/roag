using System.Text.Json.Serialization;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.World;

namespace Vagabonds.Core.Content;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ClassDefinition), "class")]
[JsonDerivedType(typeof(AbilityDefinition), "ability")]
[JsonDerivedType(typeof(CharacterDefinition), "character")]
[JsonDerivedType(typeof(NamesDefinition), "names")]
[JsonDerivedType(typeof(TraitDefinition), "trait")]
[JsonDerivedType(typeof(InteractionDefinition), "interaction")]
[JsonDerivedType(typeof(DialogueDefinition), "dialogue")]
[JsonDerivedType(typeof(ItemDefinition), "item")]
[JsonDerivedType(typeof(RecipeDefinition), "recipe")]
[JsonDerivedType(typeof(BossDefinition), "boss")]
[JsonDerivedType(typeof(VariantDefinition), "variant")]
[JsonDerivedType(typeof(GameDefinition), "game")]
[JsonDerivedType(typeof(GenerationDefinition), "generation")]
[JsonDerivedType(typeof(SimulationDefinition), "simulation")]
[JsonDerivedType(typeof(IncidentDefinition), "incident")]
[JsonDerivedType(typeof(DiscoveryDefinition), "discovery")]
public abstract class ContentDefinition
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public string? Sprite { get; set; }
    public string[] Tags { get; set; } = [];
}
public sealed class GameDefinition : ContentDefinition
{
    public required string Traveller { get; set; }
    public required string Villager { get; set; }
    public required string Healer { get; set; }
    public required string Beast { get; set; }
    public required string Raider { get; set; }
    public required string Caravan { get; set; }
    public required string[] Classes { get; set; }
    public required string[] CommonAbilities { get; set; }
    public required string[] Bosses { get; set; }
    public required string[] Recipes { get; set; }
    public required Dictionary<string, int> Inventory { get; set; }
    public required string Generation { get; set; }
    public required string EnemyFallback { get; set; }
    public required string Simulation { get; set; }
    public required string[] Incidents { get; set; }
    public string[] Discoveries { get; set; } = [];
}
public sealed class DiscoveryDefinition : ContentDefinition
{
    public required string Action { get; set; }
    public required string Text { get; set; }
    public string[] Layout { get; set; } = [".....", ".....", ".....", ".....", "....."];
    public Dictionary<string, int> Supplies { get; set; } = [];
    public string[] Rewards { get; set; } = [];
    public string[] Enemies { get; set; } = [];
}
public sealed class SimulationDefinition : ContentDefinition
{
    public double HungerPerTick { get; set; } = .04;
    public double FatiguePerTick { get; set; } = .015;
    public double RestPerTick { get; set; } = .15;
    public int MealAt { get; set; } = 35;
    public int MealReduction { get; set; } = 35;
    public int StarveAt { get; set; } = 85;
    public double StructureWearPerTick { get; set; } = .003;
    public int MemoryTicks { get; set; } = 1800;
    public int ForageWork { get; set; } = 30;
}
public sealed class IncidentDefinition : ContentDefinition
{
    public required string Condition { get; set; }
    public required string Effect { get; set; }
    public int Mood { get; set; }
    public int Cooldown { get; set; } = 1800;
}

public sealed class GenerationDefinition : ContentDefinition
{
    public int SettlementFinds { get; set; } = 4;
    public int RoadDiscoverySpacing { get; set; } = 280;
    public int WildernessFinds { get; set; } = 48;
    public required Dictionary<string, string[]> Layouts { get; set; }
    public required Dictionary<Biome, TerrainProfile> Biomes { get; set; }
}
public sealed class TerrainProfile
{
    public int TreesPerThousand { get; set; }
    public double RockThreshold { get; set; } = 1;
    public double WaterThreshold { get; set; } = 1;
    public double WaterElevation { get; set; } = 1;
}

public sealed class ClassDefinition : ContentDefinition
{
    public required int Role { get; set; }
    public required int Health { get; set; }
    public required int Mana { get; set; }
    public required int Armor { get; set; }
    public required int Speed { get; set; }
    public required string[] Abilities { get; set; }
}
public sealed class AbilityDefinition : ContentDefinition
{
    public int Mana { get; set; }
    public TargetKind Target { get; set; }
    public int Power { get; set; }
    public required string Effect { get; set; }
    public int Duration { get; set; } = 3;
    public Skill AsSkill() => new(Name, Sprite ?? "sword", Mana, Target, Power, Effect, Description, Duration);
}
public sealed class BehaviorRule
{
    public required string Ability { get; set; }
    public int Weight { get; set; } = 1;
    public int Every { get; set; } = 1;
    public int FromRound { get; set; } = 1;
    public int BelowHealthPercent { get; set; } = 100;
    public string Target { get; set; } = "random";
}
public class CharacterDefinition : ContentDefinition
{
    public ActorKind ActorKind { get; set; } = ActorKind.Person;
    public string? Names { get; set; }
    public string[] Classes { get; set; } = [];
    public string[] Interactions { get; set; } = [];
    public int Health { get; set; } = 30;
    public int Mana { get; set; } = 12;
    public int Armor { get; set; } = 1;
    public int Speed { get; set; } = 6;
    public int Power { get; set; } = 6;
    public BehaviorRule[] Behaviors { get; set; } = [];
}
public sealed class BossDefinition : ContentDefinition
{
    public required string Family { get; set; }
    public required string Weakness { get; set; }
    public required string Tactics { get; set; }
    public int Health { get; set; } = 240;
    public int Mana { get; set; } = 100;
    public int Power { get; set; } = 10;
    public int Armor { get; set; } = 3;
    public int Speed { get; set; } = 6;
    public int PhasePercent { get; set; } = 50;
    public required BehaviorRule[] Behaviors { get; set; }
    public required BehaviorRule[] Enraged { get; set; }
    public string[] Minions { get; set; } = [];
    public required string[] Variants { get; set; }
}
public sealed class VariantDefinition : ContentDefinition
{
    public int HealthPercent { get; set; } = 100;
    public int Power { get; set; }
    public int Armor { get; set; }
    public int Speed { get; set; }
    public BehaviorRule[] Behaviors { get; set; } = [];
}
public sealed class NamesDefinition : ContentDefinition
{
    public required string[] First { get; set; }
    public required string[] Last { get; set; }
    public required string[] Backgrounds { get; set; }
    public required string[] Motivations { get; set; }
    public required string[] Traits { get; set; }
}
public sealed class TraitDefinition : ContentDefinition
{
    public string Value { get; set; } = "security";
    public string Aversion { get; set; } = "danger";
    public int WorkPercent { get; set; } = 100;
    public int FatiguePercent { get; set; } = 100;
}
public sealed class InteractionDefinition : ContentDefinition
{
    public required string Action { get; set; }
    public int Distance { get; set; } = 2;
    public int Amount { get; set; }
    public Dictionary<string, int> Cost { get; set; } = [];
}
public sealed class ItemDefinition : ContentDefinition
{
    public required string Stack { get; set; }
    public string Slot { get; set; } = "none";
    public int Health { get; set; }
    public int Power { get; set; }
    public int Armor { get; set; }
    public int Speed { get; set; }
    public int Value { get; set; } = 2;
}
public sealed class RecipeDefinition : ContentDefinition
{
    public required Material Material { get; set; }
    public required int Work { get; set; }
    public required Dictionary<string, int> Cost { get; set; }
    public string? Output { get; set; }
    public int Amount { get; set; } = 1;
}

/// <summary>Presentation-only text; interaction mechanics remain in InteractionDefinition.</summary>
public sealed class DialogueDefinition : ContentDefinition
{
    public string? Interaction { get; set; }
    public DialogueVariant[] Variants { get; set; } = [];
}
public sealed class DialogueVariant
{
    public string When { get; set; } = "always";
    public string Player { get; set; } = "";
    public required string[] Replies { get; set; }
}
