using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record SiltTrail(long Action, long Source, WorldPosition Position, long Expires);

internal static class EnemySilt
{
    public const string Definition = "core:enemy/silt_worm", Charge = "core:ability/worm_charge";
    public const int Duration = 300, Cap = 12, WalkExtra = 40, DashExtra = 20;
    public static bool Plain(Material material) => material is Material.Floor or Material.Earth or Material.Grass or Material.Bridge;

    public static bool ValidAttack(ActorTactics? tactics, string ability, WorldPosition origin,
        IReadOnlyList<WorldPosition>? cells, AttackDelivery delivery, EnemyAdvance? advance, bool committing)
    {
        if (ability != Charge) return tactics?.DefinitionId != Definition || advance == null;
        if (tactics?.DefinitionId != Definition || tactics.Enemy is not { } memory
            || (committing ? memory.SiltCharges >= 2 : memory.SiltCharges == 0)
            || delivery != AttackDelivery.Projectile || advance is not { Steps: 2 } || cells?.Count is not (2 or 3)
            || !Enum.IsDefined(advance.Facing) || !AttackGeometry.Cardinal(advance.Facing)) return false;
        var (dx, dy) = AttackGeometry.Offset(advance.Facing);
        for (int i = 0; i < cells.Count; i++) if (cells[i] != origin.Offset(dx * (i + 1), dy * (i + 1))) return false;
        return true;
    }

    public static void Validate(WorldPlan plan, WorldState world, IReadOnlyDictionary<long, Actor> actors, ContentCatalog? content)
    {
        var state = world.Tactics!; long clock = state.Queue.Clock;
        if (state.Silt == null || state.Silt.Count > Cap || state.Silt.Any(s => s == null)
            || state.Silt.Select(s => s.Position).Distinct().Count() != state.Silt.Count)
            throw new InvalidDataException("Invalid silt trail store.");
        if (state.Silt.Count == 0) return;
        content ??= ContentCatalog.Default;
        using var terrain = new ChunkCache(plan, world, content);
        var actions = new Dictionary<long, (long Source, int Count)>(); var counts = new Dictionary<long, int>();
        foreach (var trail in state.Silt)
        {
            var owner = actors.GetValueOrDefault(trail.Source);
            if (trail.Action <= 0 || trail.Action >= state.NextAction || trail.Source <= 0 || trail.Source >= world.NextActorId
                || trail.Expires <= clock || trail.Expires - clock > Duration || !plan.Contains(trail.Position)
                || !Plain(terrain.At(trail.Position)) || !WorldSession.SnapshotProjectileOpen(plan, world, content, terrain, trail.Position)
                || owner != null && owner.Tactics is not { DefinitionId: Definition, Enemy: not null })
                throw new InvalidDataException("Invalid silt trail identity, terrain or expiry.");
            int count = counts.GetValueOrDefault(trail.Source) + 1;
            counts[trail.Source] = count;
            var action = actions.GetValueOrDefault(trail.Action, (Source: trail.Source, Count: 0));
            if (count > 4 || owner != null && count > owner.Tactics!.Enemy!.SiltCharges * 2
                || action.Source != trail.Source || action.Count >= 2)
                throw new InvalidDataException("Silt trail exceeds its committed budget.");
            actions[trail.Action] = (trail.Source, action.Count + 1);
        }
    }
}

public sealed partial class TacticalCombat
{
    private bool SiltGround(WorldPosition position) => _world.Plan.Contains(position)
        && EnemySilt.Plain(_world.Chunks.At(position)) && _world.GroundOpen(position);

    private void PruneSilt()
    {
        if (State.Silt.Count > 0) State.Silt.RemoveAll(s => s.Expires <= Clock || !SiltGround(s.Position));
    }

    private int SiltMovementExtra(IReadOnlyList<WorldPosition> cells, TacticalAction action)
        => cells.Count(p => State.Silt.Any(s => s.Position == p && s.Expires > Clock) && SiltGround(p))
            * (action == TacticalAction.Dash ? EnemySilt.DashExtra : EnemySilt.WalkExtra);

    private void LeaveSilt(LockedAttack attack, WorldPosition origin, IReadOnlyList<WorldPosition> path)
    {
        if (attack.Ability != EnemySilt.Charge) return;
        PruneSilt();
        var vacated = origin;
        for (int i = 0; i < Math.Min(2, path.Count); i++)
        {
            if (State.Silt.Count < EnemySilt.Cap && SiltGround(vacated) && !State.Silt.Any(s => s.Position == vacated))
            {
                State.Silt.Add(new(attack.Id, attack.Source, vacated, checked(Clock + EnemySilt.Duration)));
                Emit("SiltTrailCreated", attack.Source, 0, attack.Id, vacated);
            }
            vacated = path[i];
        }
    }
}
