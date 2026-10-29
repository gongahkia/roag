namespace Roag.Core.World;

/// <summary>Physical paired stairs used by the imported landscape generator.</summary>
public static class WorldStairs
{
    public static WorldPosition? Destination(WorldPosition from, Material material) => material switch
    {
        Material.StairsDown => from.Offset(-4, 0) with { Floor = from.Floor + 1 },
        Material.StairsUp => from.Offset(4, 0) with { Floor = from.Floor - 1 },
        _ => null
    };

    public static bool Connects(WorldPosition from, WorldPosition to) => from.Y == to.Y
        && ((long)to.Floor - from.Floor == 1 && (long)to.X - from.X == -4
            || (long)to.Floor - from.Floor == -1 && (long)to.X - from.X == 4);
}

public sealed partial class WorldSession
{
    /// <summary>Checks both persistent endpoints, including edits and solid objects, without moving anyone.</summary>
    public WorldPosition? StairLanding(WorldPosition from)
    {
        if (!Plan.Contains(from) || !GroundOpen(from)) return null;
        var material = Chunks.At(from);
        if (WorldStairs.Destination(from, material) is not { } to || !Plan.Contains(to) || !GroundOpen(to)) return null;
        var counterpart = material == Material.StairsDown ? Material.StairsUp : Material.StairsDown;
        return Chunks.At(to) == counterpart ? to : null;
    }
}
