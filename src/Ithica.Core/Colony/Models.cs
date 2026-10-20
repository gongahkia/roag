using System.Text.Json.Serialization;

namespace Ithica.Core.Colony;

public enum Terrain : byte
{
    Vacuum, Rock, Ice, Ore, Crystal, Floor, Wall, Airlock
}
public enum ModuleKind : byte
{
    Wall, Floor, Airlock, Solar, Battery, Oxygen, Farm, Bunk, Medbay, Beacon, Rtg, SuitRack
}
public enum ResidentLocation : byte { Home, Field }

public enum WorkKind : byte
{
    Dig, Build, Salvage, Survey
}
public enum Tool : byte
{
    Inspect, Dig, Build, Survey, Cancel
}
public enum ContactState : byte
{
    Stranded, Escorted, Rescued, Lost
}
public enum RescuePhase : byte
{
    Outbound, Returning, Complete, Failed
}
public enum NoticeLevel : byte
{
    Info, Good, Warning, Danger
}

public readonly record struct Result(bool Success, string Message);
public readonly record struct Stock(float Metal, float Water, float Food, float Crystal, float Components = 0)
{
    public static Stock operator +(Stock a, Stock b) => new(a.Metal + b.Metal, a.Water + b.Water, a.Food + b.Food, a.Crystal + b.Crystal, a.Components + b.Components);
    public static Stock operator -(Stock a, Stock b) => new(a.Metal - b.Metal, a.Water - b.Water, a.Food - b.Food, a.Crystal - b.Crystal, a.Components - b.Components);
    public static Stock operator *(Stock a, float n) => new(a.Metal * n, a.Water * n, a.Food * n, a.Crystal * n, a.Components * n);
    public bool Covers(Stock cost) => Metal >= cost.Metal && Water >= cost.Water && Food >= cost.Food && Crystal >= cost.Crystal && Components >= cost.Components;
    [JsonIgnore]
    public bool Valid => float.IsFinite(Metal) && float.IsFinite(Water) && float.IsFinite(Food) && float.IsFinite(Crystal)
                                  && float.IsFinite(Components) && Metal >= 0 && Water >= 0 && Food >= 0 && Crystal >= 0 && Components >= 0;
}

public sealed record Blueprint(ModuleKind Kind, string Name, Stock Cost, float Work, float Power, string Description, string Sprite)
{
    public static readonly Blueprint[] All = [
        new(ModuleKind.Wall, "Pressure hull", new(2,0,0,0), 3, 0, "Seals atmosphere. Complete the enclosure around a room.", "wall"),
        new(ModuleKind.Floor, "Habitat deck", new(1,0,0,0), 2, 0, "Walkable deck carries power between adjacent modules.", "floor"),
        new(ModuleKind.Airlock, "Airlock", new(8,0,0,0), 6, 0, "Walkable pressure seal. Can replace a hull tile.", "airlock"),
        new(ModuleKind.Solar, "Solar array", new(14,0,0,2), 10, -12, "Up to 12 kW in daylight. Connect it to the habitat deck.", "solar"),
        new(ModuleKind.Battery, "Battery bank", new(16,0,0,2), 9, 0, "Stores 240 kWh on its own connected power network.", "battery"),
        new(ModuleKind.Oxygen, "Life support", new(20,0,0,4), 12, 3, "Uses water and 3 kW to release oxygen and warmth.", "oxygen"),
        new(ModuleKind.Farm, "Hydroponics", new(24,0,0,4), 14, 4, "Grows food using water, pressure, warmth, and 4 kW.", "farm"),
        new(ModuleKind.Bunk, "Sleeping pod", new(6,0,0,0), 5, 0, "A proper bed restores energy, health, and morale.", "bunk"),
        new(ModuleKind.Medbay, "Med station", new(20,0,0,6), 12, 2, "Treats nearby resting colonists when supplied with 2 kW.", "medbay"),
        new(ModuleKind.Beacon, "Earth uplink", new(60,0,0,20), 24, 5, "Send the founding charter once your colony is ready.", "beacon"),
        new(ModuleKind.Rtg, "Radioisotope generator", default, 0, -12, "The outpost's reliable 12 kW starter generator.", "rtg"),
        new(ModuleKind.SuitRack, "Suit service", new(24,0,0,4,1), 16, 2, "A recovered component equips suits to use 35% less oxygen on their next departure. Opens a deeper signal.", "medbay")
    ];
    public static Blueprint Get(ModuleKind kind) => All[(int)kind];
}

public sealed class Machine
{
    public int Id
    {
        get; set;
    }
    public ModuleKind Kind
    {
        get; set;
    }
    public int Cell
    {
        get; set;
    }
    public bool Powered
    {
        get; set;
    }
    public float Progress
    {
        get; set;
    }
    public float Health { get; set; } = 100;
    public float Energy
    {
        get; set;
    }
}

public sealed class WorkOrder
{
    public int Id
    {
        get; set;
    }
    public int Cell
    {
        get; set;
    }
    public WorkKind Kind
    {
        get; set;
    }
    public ModuleKind Blueprint
    {
        get; set;
    }
    public float Progress
    {
        get; set;
    }
    public float Required
    {
        get; set;
    }
    public int? Assigned
    {
        get; set;
    }
    public bool Reserved
    {
        get; set;
    }
    public long RetryAfter
    {
        get; set;
    }
}

public sealed class Colonist
{
    public ResidentLocation Location { get; set; }
    public string? SiteId { get; set; }
    public int FieldRole { get; set; }
    public int Charge { get; set; } = 12;
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public float SuitEfficiency { get; set; } = 1;

    public int Id
    {
        get; set;
    }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Engineer";
    public string Trait { get; set; } = "Patient";
    public int Cell
    {
        get; set;
    }
    public float Health { get; set; } = 100;
    public float Hunger { get; set; } = 10;
    public float Energy { get; set; } = 90;
    public float Morale { get; set; } = 80;
    public float Suit { get; set; } = 100;
    public int? Job
    {
        get; set;
    }
    public int? Expedition
    {
        get; set;
    }
    public string Activity { get; set; } = "On standby";
    public List<int> Path { get; set; } = [];
    public int PathCursor
    {
        get; set;
    }
    public bool Rest
    {
        get; set;
    }
    public bool Alive { get; set; } = true;
    public int CompletedWork
    {
        get; set;
    }
}

public sealed class Survivor
{
    public bool FieldEscort { get; set; }
    public int? RecruitedResidentId { get; set; }

    public int Id
    {
        get; set;
    }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Miner";
    public string Trait { get; set; } = "Restless";
    public int Cell
    {
        get; set;
    }
    public float Health { get; set; } = 62;
    public float Suit { get; set; } = 68;
    public ContactState State
    {
        get; set;
    }
    public bool LowOxygenReported
    {
        get; set;
    }
    [JsonIgnore] public bool Active => State is ContactState.Stranded or ContactState.Escorted;
    [JsonIgnore] public int TimeLeft => Math.Max(0, (int)Math.Ceiling(Suit / .16f + Health / .6f));
}

public sealed class Expedition
{
    public int Id
    {
        get; set;
    }
    public int CrewId
    {
        get; set;
    }
    public int SurvivorId
    {
        get; set;
    }
    public RescuePhase Phase
    {
        get; set;
    }
    [JsonIgnore] public bool Active => Phase is RescuePhase.Outbound or RescuePhase.Returning;
}

public sealed record Notice(long Tick, string Text, NoticeLevel Level);

/// <summary>Explicit state makes saving and seeded continuation independent of System.Random internals.</summary>
public sealed class RandomStream
{
    public ulong State
    {
        get; set;
    }
    public RandomStream(ulong seed) => State = seed + 0x9e3779b97f4a7c15UL;
    public uint NextUInt()
    {
        ulong old = State;
        State = unchecked(old * 6364136223846793005UL + 1442695040888963407UL);
        uint value = (uint)(((old >> 18) ^ old) >> 27);
        int rotation = (int)(old >> 59);
        return (value >> rotation) | (value << ((-rotation) & 31));
    }
    public double NextDouble() => NextUInt() / 4294967296.0;
    public int Next(int minimum, int maximum) => minimum + (int)(NextUInt() % (uint)(maximum - minimum));
}
