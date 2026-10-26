using System.Collections.Immutable;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public static partial class EnemyPlanning
{
    private static EnemyIntent Expansion(EnemyDefinition definition, EnemyMemory memory, EnemyPlanningContext world)
    {
        var source = world.Source; var target = world.Target;
        int dx = target.X - source.X, dy = target.Y - source.Y, distance = source.Distance(target);
        var neighbors = world.Neighbors ?? throw new ArgumentException("Expansion decisions need observed actor facts.");
        var barricade = world.Barricade ?? throw new ArgumentException("Expansion decisions need terrain facts.");
        EnemyNeighbor[] Allies(int radius) => neighbors.Where(n => n.Ally && n.Health > 0
            && n.Position.Floor == source.Floor && n.Position.Distance(source) <= radius).OrderBy(n => n.Id).ToArray();
        EnemyIntent Close()
        {
            var goals = Cardinals.Select(d => target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
            return world.NextStep(goals) is { } step ? new(EnemyIntentKind.Move, step) : new(EnemyIntentKind.Wait);
        }
        EnemyIntent Windup(string name, IEnumerable<WorldPosition> cells, AttackDelivery delivery = AttackDelivery.Melee)
            => new(EnemyIntentKind.Windup, Ability: "core:ability/" + name, Delivery: delivery, Cells: cells.ToImmutableArray());
        IEnumerable<WorldPosition> Area(params (int X, int Y)[] offsets)
            => offsets.Select(d => target.Offset(d.X, d.Y)).Where(world.ProjectileOpen);
        bool line = distance <= definition.Range && ClearLine(world);
        switch (definition.Id)
        {
            case "core:enemy/mossbreaker":
                var blocked = Cardinals.Select(d => source.Offset(d.X, d.Y)).Where(barricade)
                    .OrderBy(p => p.Distance(target)).ToArray(); // Stable N/E/S/W ties.
                if (blocked.Length > 0) return Windup("mossbreaker_crack", [blocked[0]]);
                return distance == 1 ? Windup("mossbreaker_cleave", [target]) : Close();
            case "core:enemy/gloom_strider":
                if (distance <= 2)
                    foreach (var d in Cardinals)
                        if (world.Free(source.Offset(d.X, d.Y)) && source.Offset(d.X, d.Y).Distance(target) > distance)
                        { AttackGeometry.TryFacing(d.X, d.Y, out var facing); return new(EnemyIntentKind.Move, facing); }
                return line ? Windup("gloom_fork", Math.Abs(dx) >= Math.Abs(dy)
                    ? Area((0, 0), (0, -1), (0, 1)) : Area((0, 0), (-1, 0), (1, 0)), AttackDelivery.Area) : Close();
            case "core:enemy/mire_binder":
                return line ? Windup("mire_ring", Area((-1, -1), (0, -1), (1, -1), (1, 0),
                    (1, 1), (0, 1), (-1, 1), (-1, 0)), AttackDelivery.Area) : Close();
            case "core:enemy/pack_chorister":
                return distance == 1 ? Windup("pack_chorus", [target]) with { DamageBonus = Math.Min(2, Allies(2).Length) } : Close();
            case "core:enemy/vault_lurker":
                if (distance > 5 && !memory.Alerted) return new(EnemyIntentKind.Wait);
                return (line ? Windup("vault_ambush", Area((0, 0), (-1, -1), (1, -1), (1, 1), (-1, 1)), AttackDelivery.Area) : Close()) with { Alert = true };
            case "core:enemy/crystal_bombardier":
                return line ? Windup("crystal_barrage", Area((0, 0), (0, -2), (2, 0), (0, 2), (-2, 0)), AttackDelivery.Area) : Close();
            case "core:enemy/cinder_mender":
                EnemyNeighbor? wounded = null;
                foreach (var ally in Allies(3))
                    if (ally.Health < ally.MaximumHealth && (wounded == null
                        || (long)ally.Health * wounded.MaximumHealth < (long)wounded.Health * ally.MaximumHealth)) wounded = ally;
                if (wounded != null) return Windup("cinder_mend", [wounded.Position]) with { Support = new(EnemySupportKind.Heal, Power: 3) };
                return distance == 1 ? Windup("cinder_tap", [target]) : Close();
            case "core:enemy/glass_charger":
                if (distance is >= 2 and <= 4 && (dx == 0 || dy == 0) && line)
                {
                    AttackGeometry.TryFacing(Math.Sign(dx), Math.Sign(dy), out var facing);
                    return Windup("glass_charge", Enumerable.Range(1, distance).Select(i => source.Offset(Math.Sign(dx) * i, Math.Sign(dy) * i)),
                        AttackDelivery.PiercingLine) with { Advance = new(facing, Math.Min(3, distance)) };
                }
                return distance == 1 ? Windup("glass_jab", [target]) : Close();
            case "core:enemy/forge_conjurer":
                if (distance <= 5 && Allies(5).Length < 3 && memory.Summons < 3)
                    foreach (var d in Cardinals)
                    {
                        var p = source.Offset(d.X, d.Y);
                        if (world.Free(p) && !neighbors.Any(n => n.Health > 0 && n.Position == p))
                            return Windup("forge_call", [p]) with { Support = new(EnemySupportKind.Summon, Definition: "core:enemy/ruin_skitter", MaximumActive: 3) };
                    }
                return distance == 1 ? Windup("forge_tap", [target]) : Close();
            case "core:enemy/bastion_bulwark":
                if (distance <= 2 && memory.Retaliate)
                    return Windup("bulwark_retort", Area((0, 0), (0, -1), (1, 0), (0, 1), (-1, 0)), AttackDelivery.Area) with { ClearRetaliation = true };
                return distance == 1 ? Windup("bulwark_bash", [target]) : Close();
            default: throw new InvalidDataException("Unknown expansion enemy.");
        }
    }
}
