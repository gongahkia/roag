using Roag.Core.World;

namespace Roag.Core.Tactics;

internal static class EnemySentinel
{
    public const string Definition = "core:enemy/rotating_sentinel";
    public const string Quarter = "core:ability/sentinel_quarter";

    public static Facing Clockwise(Facing facing) => (Facing)(((int)facing + 2) % 8);

    public static IEnumerable<WorldPosition> Arc(WorldPosition origin, Facing facing)
    {
        var (dx, dy) = AttackGeometry.Offset(facing);
        for (int side = -1; side <= 1; side++) yield return origin.Offset(dx - dy * side, dy + dx * side);
    }

    public static void ValidateMemory(EnemyMemory memory)
    {
        if (memory.SentinelStrikes is < 0 or > 4
            || memory.SentinelStrikes == 0 && (memory.SentinelFacing != null || memory.SentinelFinished)
            || memory.SentinelStrikes > 0 && (memory.SentinelFacing is not { } facing || !Enum.IsDefined(facing) || !AttackGeometry.Cardinal(facing))
            || memory.SentinelStrikes == 4 && !memory.SentinelFinished)
            throw new InvalidDataException("Invalid sentinel sequence.");
    }

    public static bool ValidAttack(ActorTactics? tactics, string ability, WorldPosition origin,
        IReadOnlyList<WorldPosition>? cells, Facing? facing, AttackDelivery delivery,
        EnemySupport? support, EnemyAdvance? advance, EnemyShove? shove, bool committing)
    {
        if (ability != Quarter && facing == null) return true;
        if (tactics?.DefinitionId != Definition || tactics.Enemy is not { } memory || ability != Quarter
            || facing is not { } direction || !Enum.IsDefined(direction) || !AttackGeometry.Cardinal(direction)
            || cells == null || delivery != AttackDelivery.Area || support != null || advance != null || shove != null) return false;
        if (committing)
        {
            if (memory.SentinelFinished || memory.SentinelStrikes >= 4
                || memory.SentinelStrikes > 0 && direction != Clockwise(memory.SentinelFacing!.Value)) return false;
        }
        else if (memory.SentinelStrikes == 0 || direction != memory.SentinelFacing
            || memory.SentinelFinished && memory.SentinelStrikes < 4) return false;
        // Terrain may have changed since commitment. Validate the locked ordered
        // subset, rather than recomputing an attack against today's terrain.
        var arc = Arc(origin, direction).ToArray(); int next = 0;
        foreach (var cell in cells)
        {
            while (next < arc.Length && arc[next] != cell) next++;
            if (next == arc.Length) return false;
            next++;
        }
        return true;
    }

    public static void Commit(EnemyMemory memory, Facing facing)
    {
        memory.SentinelFacing = facing;
        memory.SentinelStrikes++;
        if (memory.SentinelStrikes == 4) memory.SentinelFinished = true;
    }

    public static void Interrupt(ActorTactics? tactics)
    {
        if (tactics is { DefinitionId: Definition, Enemy.SentinelStrikes: > 0 }) tactics.Enemy.SentinelFinished = true;
    }
}
