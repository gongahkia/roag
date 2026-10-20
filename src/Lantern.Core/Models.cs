namespace Lantern.Core;

public enum Ground : byte
{
    Grass, Path, Water, Tree, Stone, Wall, Wood, Bridge, Void
}
public enum ObjectKind
{
    Scenery, Talk, Inn, Merchant, Chest, Shrine, Camp, Portal, Gate, Encounter, Relic
}
public enum TargetKind
{
    Enemy, Ally, AllEnemies, AllAllies, Self
}
public enum IntentKind
{
    Strike, Bleed, Dread, Sweep, Heal, Guard
}
public readonly record struct Cell(int X, int Y)
{
    public int Distance(Cell other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    public static Cell operator +(Cell a, Cell b) => new(a.X + b.X, a.Y + b.Y);
}
public readonly record struct ActionResult(bool Success, string Message, string? Title = null);
public sealed record Skill(string Name, string Icon, int Mana, TargetKind Target, int Power, string Effect, string Description);
public static class Skills
{
    public static readonly Skill[][] ByRole = [
        [new("Longsword","sword",0,TargetKind.Enemy,9,"","A reliable strike against one enemy."),new("Shield bash","shield",3,TargetKind.Enemy,6,"stun","Damage and stun for one turn."),new("Stand fast","banner",3,TargetKind.AllAllies,0,"rally","Guard all allies; restore 8 resolve (reduce stress).")],
        [new("Arrow","bow",0,TargetKind.Enemy,8,"","One precise shot."),new("Hunter's mark","mark",2,TargetKind.Enemy,4,"mark","Mark for 3 turns. Attacks deal +4 damage."),new("Volley","bow",4,TargetKind.AllEnemies,6,"","Hit every living enemy.")],
        [new("Wand","staff",0,TargetKind.Enemy,7,"","A spark that costs no mana."),new("Ember","ember",3,TargetKind.Enemy,11,"burn","Fire damage and 3 burning turns."),new("Chain arc","arc",5,TargetKind.AllEnemies,8,"","Lightning strikes every living enemy.")],
        [new("Smite","cross",0,TargetKind.Enemy,7,"","A steady strike of pale light."),new("Mend","potion",3,TargetKind.Ally,18,"heal","Restore health or revive a fallen ally."),new("Absolution","resolve",4,TargetKind.AllAllies,20,"calm","Reduce everyone's stress by 20.")]
    ];
    public static readonly Skill Guard = new("Guard", "shield", 0, TargetKind.Self, 0, "guard", "Halve incoming damage until your next turn.");
    public static readonly Skill Potion = new("Potion", "potion", 0, TargetKind.Ally, 24, "potion", "Spend a shared potion to heal or revive an ally.");
    public static readonly Skill Flee = new("Retreat", "exit", 0, TargetKind.Self, 0, "flee", "Leave the encounter. Everyone gains 5 stress.");
    public static Skill Get(int role, int index) => index switch { 3 => Guard, 4 => Potion, 5 => Flee, _ when index >= 0 && index < 3 => ByRole[role][index], _ => throw new ArgumentOutOfRangeException(nameof(index)) };
}
public sealed class Hero
{
    public int Id
    {
        get; set;
    }
    public string Name { get; set; } = "";
    public int Role
    {
        get; set;
    }
    public int Level { get; set; } = 1;
    public int Experience
    {
        get; set;
    }
    public int Health
    {
        get; set;
    }
    public int Mana
    {
        get; set;
    }
    public int Stress
    {
        get; set;
    }
    public int MaxHealth => (Role switch { 0 => 52, 1 => 38, 2 => 34, _ => 42 }) + (Level - 1) * 6;
    public int MaxMana => (Role switch { 0 => 12, 1 => 12, 2 => 18, _ => 20 }) + (Level - 1) * 3;
    public int Armor => Role == 0 ? 3 : 1;
    public int Speed => Role switch { 0 => 5, 1 => 9, 2 => 7, _ => 6 };
    public string RoleName => Role switch { 0 => "Warden", 1 => "Ranger", 2 => "Arcanist", _ => "Vestal" };
    public string Sprite => Role switch { 0 => "warden", 1 => "ranger", 2 => "mage", _ => "healer" };
}
public sealed record WorldObject(string Id, string Name, ObjectKind Kind, Cell Position, string Sprite, bool Blocks = false, string Detail = "", string Destination = "", Cell Arrival = default);
public sealed class CombatUnit
{
    public int Id
    {
        get; set;
    }
    public int HeroId { get; set; } = -1;
    public string Name { get; set; } = "";
    public string Sprite { get; set; } = "";
    public Cell Position
    {
        get; set;
    }
    public int Health
    {
        get; set;
    }
    public int MaxHealth
    {
        get; set;
    }
    public int Mana
    {
        get; set;
    }
    public int MaxMana
    {
        get; set;
    }
    public int Stress
    {
        get; set;
    }
    public int Armor
    {
        get; set;
    }
    public int Speed
    {
        get; set;
    }
    public int Power
    {
        get; set;
    }
    public bool Guarding
    {
        get; set;
    }
    public int Burn
    {
        get; set;
    }
    public int Bleed
    {
        get; set;
    }
    public int Mark
    {
        get; set;
    }
    public int Stun
    {
        get; set;
    }
    public IntentKind Intent
    {
        get; set;
    }
    public int IntentTarget
    {
        get; set;
    }
    public bool Alive => Health > 0;
    public bool Friendly => HeroId >= 0;
    public string Conditions => string.Join(" / ", new[] { Guarding ? "GUARD" : null, Burn > 0 ? "BURN " + Burn : null, Bleed > 0 ? "BLEED " + Bleed : null, Mark > 0 ? "MARK " + Mark : null, Stun > 0 ? "STUN" : null, Stress >= 80 ? "SHAKEN" : null }.Where(x => x is not null));
}
public sealed class Battle
{
    public string EncounterId { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Boss
    {
        get; set;
    }
    public List<CombatUnit> Units { get; set; } = [];
    public List<int> Order { get; set; } = [];
    public int TurnIndex
    {
        get; set;
    }
    public int Round { get; set; } = 1;
    public CombatUnit Active => Units[Order[TurnIndex]];
}
public sealed record JournalEntry(int Sequence, string Text);
public sealed record CombatEvent(int Sequence, int Target, int Amount, string Kind, string Text);
public sealed class RandomStream(uint seed)
{
    public uint State { get; set; } = seed == 0 ? 1 : seed;
    public int Next(int max)
    {
        uint x = State;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        State = x;
        return (int)(x % (uint)max);
    }
}
