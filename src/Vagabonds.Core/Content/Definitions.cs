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
[JsonDerivedType(typeof(ResourceDefinition), "resource")]
[JsonDerivedType(typeof(CampDefinition), "camp")]
[JsonDerivedType(typeof(AffixDefinition), "affix")]
[JsonDerivedType(typeof(TalentDefinition), "talent")]
[JsonDerivedType(typeof(EncounterDefinition), "encounter")]
[JsonDerivedType(typeof(RewardDefinition), "reward")]
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
    public string[] Resources { get; set; } = [];
    public string[] CampActivities { get; set; } = [];
    public string[] Wildlife { get; set; } = [];
    public string[] CooperativeAbilities { get; set; } = [];
    public string[] Encounters { get; set; } = [];
    public string DefaultReward { get; set; } = "reward.wanderers";
}
public sealed class DiscoveryDefinition : ContentDefinition
{
    public string Encounter { get; set; } = "";
    public SituationRules? Situation { get; set; }
    public required string Action { get; set; }
    public required string Text { get; set; }
    public string[] Layout { get; set; } = [".....", ".....", ".....", ".....", "....."];
    public Dictionary<string, int> Supplies { get; set; } = [];
    public string[] Rewards { get; set; } = [];
    public string[] Enemies { get; set; } = [];
}
public sealed class SituationRules
{
    public required string Kind { get; set; }
    public required string ResolvedSprite { get; set; }
    public required DiscoveryApproach[] Approaches { get; set; }
    public required Cell[] TunnelWalls { get; set; }
    public Cell[] BridgeCells { get; set; } = [];
}
public sealed class DiscoveryApproach
{
    public required string Action { get; set; }
    public required string Name { get; set; }
    public required string Sprite { get; set; }
    public string Description { get; set; } = "";
    public int Work { get; set; }
    public Dictionary<string, int> Cost { get; set; } = [];
}
public sealed class CampDefinition : ContentDefinition
{
    public required string Role { get; set; }
    public required string Effect { get; set; }
    public int Power { get; set; } = 2;
    public int Duration { get; set; } = 1800;
    public int Work { get; set; } = 60;
    public Dictionary<string, int> Cost { get; set; } = [];
}
public sealed class ResourceDefinition : ContentDefinition
{
    public required Material Terrain { get; set; }
    public required Biome[] Biomes { get; set; }
    public bool Underground { get; set; }
    public int ChancePerThousand { get; set; } = 80;
    public int Capacity { get; set; } = 3;
    public int Work { get; set; } = 20;
    public int RegrowTicks { get; set; } = 1800;
    public string Activity { get; set; } = "harvest";
    public required string DepletedSprite { get; set; }
    public required Dictionary<string, int> Yield { get; set; }
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
    public int PreparationTurns { get; set; }
    public CooperationRules? Cooperation { get; set; }
    public int Mana { get; set; }
    public TargetKind Target { get; set; }
    public int Power { get; set; }
    public required string Effect { get; set; }
    public int Duration { get; set; } = 3;
    public Skill AsSkill() => new(Name, Sprite ?? "sword", Mana, Target, Power, Effect, Description, Duration,
        Cooperation != null, Cooperation?.PartnerMana ?? 0, Cooperation?.PartnerPowerPercent ?? 0, Id, PreparationTurns);
}
public sealed class CooperationRules
{
    public int PartnerMana { get; set; } = 3;
    public int PartnerPowerPercent { get; set; } = 100;
}
public sealed class BehaviorRule
{
    public string Condition { get; set; } = "always";
    public bool NoRepeat { get; set; }
    public required string Ability { get; set; }
    public int Weight { get; set; } = 1;
    public int Every { get; set; } = 1;
    public int FromRound { get; set; } = 1;
    public int BelowHealthPercent { get; set; } = 100;
    public string Target { get; set; } = "random";
}
public class CharacterDefinition : ContentDefinition
{
    public string Reward { get; set; } = "";
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
    public EcologyRules? Ecology { get; set; }
}
public sealed class EcologyRules
{
    public required string Behavior { get; set; }
    public required Biome[] Biomes { get; set; }
    public bool Underground { get; set; }
    public string? Resource { get; set; }
    public int Notice { get; set; } = 4;
    public int Leash { get; set; } = 8;
    public required Dictionary<string, int> Drops { get; set; }
}
public sealed class BossDefinition : ContentDefinition
{
    public string Reward { get; set; } = "";
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
public sealed class AffixDefinition : ContentDefinition
{
    public required string Effect { get; set; }
    public required int Power { get; set; }
    public required string[] Slots { get; set; }
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
    public Dictionary<string, int> WorkBonuses { get; set; } = [];
    public string[] Effects { get; set; } = [];
    public string[] AffixPool { get; set; } = [];
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
