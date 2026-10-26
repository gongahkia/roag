using System.Collections.Immutable;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public static partial class EnemyPlanning
{
    private static EnemyIntent Squad(EnemyDefinition definition, EnemyMemory memory, EnemyPlanningContext world)
    {
        var self = world.Self ?? throw new ArgumentException("Squad decisions need the acting member's identity.");
        var neighbors = world.Neighbors ?? throw new ArgumentException("Squad decisions need actor facts.");
        var snares = world.Snares ?? throw new ArgumentException("Squad decisions need placed trap facts.");
        var targetFree = world.TargetFree ?? throw new ArgumentException("Squad decisions need target-relative occupancy.");
        var source = world.Source; var target = world.Target;
        int dx = target.X - source.X, dy = target.Y - source.Y, distance = source.Distance(target);
        var leader = memory.Squad is { } group ? neighbors.FirstOrDefault(n => n.Id == group.Leader && n.Health > 0
            && n.Position.Floor == source.Floor && n.Definition == EnemySquads.Roster(group.Kind).Leader && n.Squad?.Id == group.Id) : null;
        EnemyIntent Toward(IReadOnlySet<WorldPosition> goals) => world.NextStep(goals) is { } facing
            ? new(EnemyIntentKind.Move, facing) : new(EnemyIntentKind.Wait);
        EnemyIntent Close(WorldPosition? aim = null) => Toward(Cardinals.Select(d => (aim ?? target).Offset(d.X, d.Y)).Where(world.Free).ToHashSet());
        EnemyIntent Wind(string name, IEnumerable<WorldPosition> cells, AttackDelivery delivery = AttackDelivery.Melee)
            => new(EnemyIntentKind.Windup, Ability: "core:ability/" + name, Cells: cells.ToImmutableArray(), Delivery: delivery);
        EnemyIntent Tap(string name) => distance == 1 ? Wind(name, [target]) : Close();
        EnemyIntent? Retreat()
        {
            foreach (var (x, y) in Cardinals)
                if (world.Free(source.Offset(x, y)) && source.Offset(x, y).Distance(target) > distance)
                { AttackGeometry.TryFacing(x, y, out var facing); return new(EnemyIntentKind.Move, facing); }
            return null;
        }
        bool line = distance <= definition.Range && ClearLine(world);
        switch (definition.Id)
        {
            case "core:enemy/ward_weaver":
                if (!memory.WardReady && distance <= 8) return Wind("ward_channel", [source]) with { Support = new(EnemySupportKind.Ward) };
                return distance <= 2 ? Retreat() ?? Tap("ward_tap") : new(EnemyIntentKind.Wait);
            case "core:enemy/bodyguard_brute":
                if (leader != null && source.Distance(leader.Position) > EnemySquads.GuardLeash) return Close(leader.Position);
                if (distance == 1)
                    return Wind("brute_sweep", new[] { target }.Concat(new[] { -1, 1 }
                        .Select(side => target.Offset(-dy * side, dx * side)).Where(world.ProjectileOpen)));
                var move = Close();
                if (leader != null && move.Kind == EnemyIntentKind.Move)
                {
                    var (mx, my) = AttackGeometry.Offset(move.Facing);
                    if (source.Offset(mx, my).Distance(leader.Position) > EnemySquads.GuardLeash) return new(EnemyIntentKind.Wait);
                }
                return move;
            case "core:enemy/mend_acolyte":
                EnemyNeighbor? wounded = null;
                foreach (var ally in neighbors.OrderBy(n => n.Id))
                    if (ally.Squad != null && ally.Squad.Id == memory.Squad?.Id && ally.Health > 0 && ally.Health < ally.MaximumHealth
                        && ally.Position.Distance(source) <= 4
                        && EnemySquads.Clear(source, ally.Position, p => world.ProjectileOpen(p) && !world.MistBlocks(p))
                        && (wounded == null || (long)ally.Health * wounded.MaximumHealth < (long)wounded.Health * ally.MaximumHealth)) wounded = ally;
                if (wounded != null) return Wind("acolyte_mend", [wounded.Position]) with { Support = new(EnemySupportKind.Heal, 2, Target: wounded.Id) };
                if (distance <= 2) return Retreat() ?? Tap("acolyte_tap");
                return distance > 5 ? Close() : new(EnemyIntentKind.Wait);
            case "core:enemy/rushwing":
            case "core:enemy/banner_follower":
                bool rushwing = definition.Id == "core:enemy/rushwing";
                if (distance == 1) return Wind(rushwing ? "rushwing_bite" : "follower_strike", [target]);
                if (rushwing && leader != null && self.Health < self.MaximumHealth && distance > 2 && source.Distance(leader.Position) > 2) return Close(leader.Position);
                var preferred = Cardinals[self.Id % 4]; var goal = target.Offset(preferred.X, preferred.Y);
                if (world.Free(goal) && world.NextStep(new HashSet<WorldPosition> { goal }) is { } step) return new(EnemyIntentKind.Move, step);
                return Close();
            case "core:enemy/snare_scribe":
                var escapes = Cardinals.Select(d => target.Offset(d.X, d.Y)).Where(p => targetFree(p) && !snares.Any(t => t.Position == p)).ToArray();
                if (line && snares.Count(t => t.Source == self.Id) < EnemySquads.SnareCap && escapes.Length >= 2)
                    return Wind("scribe_snare", [escapes[self.Id % escapes.Length]]) with { Support = new(EnemySupportKind.Snare) };
                return distance <= 2 ? Retreat() ?? Tap("scribe_tap") : new(EnemyIntentKind.Wait);
            case "core:enemy/siege_wisp":
                return line ? Wind("siege_square", new[] { (0, 0), (1, 0), (0, 1), (1, 1) }
                    .Select(d => target.Offset(d.Item1, d.Item2)).Where(world.ProjectileOpen), AttackDelivery.Area) : Close();
            case "core:enemy/shield_bearer":
                var (fx, fy) = Axis(dx, dy); AttackGeometry.TryFacing(fx, fy, out var shield);
                return Tap("shield_bash") with { ShieldFacing = shield };
            case "core:enemy/pin_archer":
                if ((dx == 0 || dy == 0) && distance >= 2 && line)
                {
                    var cells = Enumerable.Range(1, distance).Select(i => source.Offset(Math.Sign(dx) * i, Math.Sign(dy) * i)).ToArray();
                    if (!neighbors.Any(n => n.Ally && n.Health > 0 && cells.Contains(n.Position))) return Wind("pin_shot", cells, AttackDelivery.Projectile);
                }
                if (distance <= 2) return Retreat() ?? Tap("pin_tap");
                var goals = new HashSet<WorldPosition>();
                foreach (var d in Cardinals) for (int i = 2; i <= definition.Range; i++)
                {
                    var p = target.Offset(d.X * i, d.Y * i);
                    if (world.Free(p) && ClearLine(world with { Source = p })) goals.Add(p);
                }
                return Toward(goals);
            case "core:enemy/banner_captain": return Tap("captain_cut");
            default: throw new InvalidDataException("Unknown squad controller.");
        }
    }
}
