using Roag.Core.World;

namespace Roag.Core.Tactics;

public static partial class EnemyPlanning
{
    private static EnemyIntent Idea(EnemyDefinition definition, EnemyMemory memory, EnemyPlanningContext world)
    {
        if (definition.Id == EnemyMirrors.Definition)
        {
            int distance = world.Source.Distance(world.Target);
            if (distance is >= 1 and <= 4 && (world.Source.X == world.Target.X || world.Source.Y == world.Target.Y) && ClearLine(world))
            {
                var mirrors = world.Mirrors ?? throw new ArgumentException("Hexer decisions need active mirror facts.");
                var self = world.Self ?? throw new ArgumentException("Hexer decisions need their own identity.");
                int dx = Math.Sign(world.Target.X - world.Source.X), dy = Math.Sign(world.Target.Y - world.Source.Y);
                var cell = world.Source.Offset(dx, dy);
                if (distance >= 2 && memory.MirrorCasts < 2 && mirrors.Count < EnemyMirrors.Cap
                    && !mirrors.Any(m => m.Source == self.Id) && world.Free(cell)
                    && (world.PlainGround ?? throw new ArgumentException("Hexer decisions need plain-ground facts."))(cell))
                {
                    AttackGeometry.TryFacing(-dx, -dy, out var incoming);
                    return new(EnemyIntentKind.Windup, Ability: EnemyMirrors.Cast, Cells: [cell],
                        Support: new(EnemySupportKind.Mirror, MirrorIncoming: incoming));
                }
                return new(EnemyIntentKind.Windup, Ability: "core:ability/hexer_bolt", Delivery: AttackDelivery.Projectile,
                    Cells: [.. Enumerable.Range(1, distance).Select(i => world.Source.Offset(dx * i, dy * i))]);
            }
            var adjacent = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
            return world.NextStep(adjacent) is { } move ? new(EnemyIntentKind.Move, move) : new(EnemyIntentKind.Wait);
        }
        if (definition.Id == EnemyEmbers.Definition)
        {
            int distance = world.Source.Distance(world.Target);
            if (memory.EmberDives < 2 && distance is >= 2 and <= 3 && ClearLine(world))
            {
                var plain = world.PlainGround ?? throw new ArgumentException("Moth decisions need physical plain-ground facts.");
                if (plain(world.Target))
                    return new(EnemyIntentKind.Windup, Ability: EnemyEmbers.Dive, Delivery: AttackDelivery.Area,
                        Cells: [world.Target], EmberLanding: world.Target);
            }
            if (distance == 1) return new(EnemyIntentKind.Windup, Ability: "core:ability/moth_touch", Cells: [world.Target]);
            var adjacent = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
            return world.NextStep(adjacent) is { } move ? new(EnemyIntentKind.Move, move) : new(EnemyIntentKind.Wait);
        }
        if (definition.Id == EnemySilt.Definition)
        {
            int distance = world.Source.Distance(world.Target);
            if (memory.SiltCharges < 2 && distance is 2 or 3
                && (world.Source.X == world.Target.X || world.Source.Y == world.Target.Y) && ClearLine(world))
            {
                var ground = world.GroundOpen ?? throw new ArgumentException("Silt decisions need physical ground facts.");
                var reserved = world.ProtectedLanding ?? throw new ArgumentException("Silt decisions need protected landing facts.");
                int dx = Math.Sign(world.Target.X - world.Source.X), dy = Math.Sign(world.Target.Y - world.Source.Y);
                AttackGeometry.TryFacing(dx, dy, out var facing);
                var cells = Enumerable.Range(1, distance).Select(i => world.Source.Offset(dx * i, dy * i)).ToArray();
                if (cells.All(p => ground(p) && !reserved(p)))
                    return new(EnemyIntentKind.Windup, Ability: EnemySilt.Charge, Delivery: AttackDelivery.Projectile,
                        Cells: [.. cells], Advance: new(facing, 2));
            }
            if (distance == 1) return new(EnemyIntentKind.Windup, Ability: "core:ability/worm_touch", Cells: [world.Target]);
            var adjacent = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
            return world.NextStep(adjacent) is { } move ? new(EnemyIntentKind.Move, move) : new(EnemyIntentKind.Wait);
        }
        if (definition.Id == EnemySentinel.Definition)
        {
            if (!memory.SentinelFinished && (memory.SentinelStrikes > 0
                || world.Source.Distance(world.Target) <= 4 && ClearLine(world)))
            {
                var open = world.SentinelArcOpen ?? throw new ArgumentException("Sentinel decisions need bounded terrain facts.");
                var (dx, dy) = Axis(world.Target.X - world.Source.X, world.Target.Y - world.Source.Y);
                AttackGeometry.TryFacing(dx, dy, out var facing);
                if (memory.SentinelStrikes > 0) facing = EnemySentinel.Clockwise(memory.SentinelFacing!.Value);
                return new(EnemyIntentKind.Windup, Ability: EnemySentinel.Quarter, Delivery: AttackDelivery.Area,
                    Cells: [.. EnemySentinel.Arc(world.Source, facing).Where(open)], SentinelFacing: facing);
            }
            if (world.Source.Distance(world.Target) == 1)
                return new(EnemyIntentKind.Windup, Ability: "core:ability/sentinel_touch", Cells: [world.Target]);
            var adjacent = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
            return world.NextStep(adjacent) is { } move ? new(EnemyIntentKind.Move, move) : new(EnemyIntentKind.Wait);
        }
        if (definition.Id == "core:enemy/gulp_toad") return Toad(world);
        if (definition.Id == "core:enemy/web_weaver") return Weaver(memory, world);
        if (EnemyOaths.IsTwin(definition.Id))
        {
            if (memory.OathPartner == 0) throw new InvalidOperationException("Oath Twins require an assigned partner.");
            if (world.Source.Distance(world.Target) == 1)
                return new(EnemyIntentKind.Windup, Ability: definition.Id == EnemyOaths.A ? "core:ability/oath_pike" : "core:ability/oath_blade", Cells: [world.Target]);
            var adjacent = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
            return world.NextStep(adjacent) is { } move ? new(EnemyIntentKind.Move, move) : new(EnemyIntentKind.Wait);
        }
        if (definition.Id == "core:enemy/alarm_keeper")
        {
            var reserved = world.ProtectedLanding ?? throw new ArgumentException("Alarm decisions need protected landing facts.");
            if (!memory.AlarmSpent)
            {
                var cells = Cardinals.Select(d => world.Source.Offset(d.X, d.Y))
                    .Where(p => world.Free(p) && !reserved(p)).Take(2).ToArray();
                if (cells.Length > 0) return new(EnemyIntentKind.Windup, Ability: "core:ability/keeper_alarm",
                    Cells: [.. cells], Support: new(EnemySupportKind.Alarm));
            }
            if (world.Source.Distance(world.Target) == 1)
                return new(EnemyIntentKind.Windup, Ability: "core:ability/keeper_touch", Cells: [world.Target]);
            var adjacent = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
            return world.NextStep(adjacent) is { } move ? new(EnemyIntentKind.Move, move) : new(EnemyIntentKind.Wait);
        }
        if (definition.Id == "core:enemy/grave_cantor")
        {
            var corpses = world.Corpses ?? throw new ArgumentException("Cantor decisions need eligible corpse facts.");
            RevivalCandidate? best = null; int distance = int.MaxValue;
            if (!memory.RevivalSpent)
                foreach (var corpse in corpses)
                {
                    if (corpse.ClaimedBy != 0 || corpse.Position.Floor != world.Source.Floor) continue;
                    int score = world.Source.Distance(corpse.Position);
                    if (score <= EnemyRevivalRules.Range && score < distance && world.Free(corpse.Position)
                        && ClearLine(world with { Target = corpse.Position }))
                    { best = corpse; distance = score; }
                }
            if (best != null) return new(EnemyIntentKind.Windup, Ability: "core:ability/cantor_revive", Cells: [best.Position],
                Support: new(EnemySupportKind.Revive, Corpse: best.Actor));
            if (world.Source.Distance(world.Target) == 1)
                return new(EnemyIntentKind.Windup, Ability: "core:ability/cantor_touch", Cells: [world.Target]);
            var adjacent = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
            return world.NextStep(adjacent) is { } move ? new(EnemyIntentKind.Move, move) : new(EnemyIntentKind.Wait);
        }
        if (definition.Id == "core:enemy/haunted_cache")
        {
            if (world.Source.Distance(world.Target) == 1)
                return new(EnemyIntentKind.Windup, Ability: "core:ability/cache_bite", Cells: [world.Target], Alert: true);
            var self = world.Self ?? throw new ArgumentException("Cache decisions need current health facts.");
            if (!memory.Alerted && self.Health == self.MaximumHealth) return new(EnemyIntentKind.Wait);
            var adjacent = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
            return world.NextStep(adjacent) is { } move ? new(EnemyIntentKind.Move, move, Alert: true) : new(EnemyIntentKind.Wait, Alert: true);
        }
        if (definition.Id is "core:enemy/bone_splitter" or "core:enemy/bone_fragment")
        {
            if (world.Source.Distance(world.Target) == 1)
                return new(EnemyIntentKind.Windup, Ability: definition.Id == "core:enemy/bone_splitter" ? "core:ability/bone_strike" : "core:ability/fragment_stab",
                    Cells: [world.Target]);
            var adjacent = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
            return world.NextStep(adjacent) is { } move ? new(EnemyIntentKind.Move, move) : new(EnemyIntentKind.Wait);
        }
        if (definition.Id is not ("core:enemy/heavy_rammer" or "core:enemy/hollow_wraith")) throw new NotSupportedException("Unported idea controller: " + definition.Id);
        bool wraith = definition.Id == "core:enemy/hollow_wraith";
        if (world.Source.Distance(world.Target) == 1)
        {
            AttackGeometry.TryFacing(world.Target.X - world.Source.X, world.Target.Y - world.Source.Y, out var facing);
            return new(EnemyIntentKind.Windup, Ability: wraith ? "core:ability/wraith_touch" : "core:ability/rammer_shove",
                Cells: [world.Target], Shove: wraith ? null : new(world.Target, facing));
        }
        if (wraith)
        {
            var wall = world.SolidWall ?? throw new ArgumentException("Wraith decisions need wall material facts.");
            var reserved = world.ProtectedLanding ?? throw new ArgumentException("Wraith decisions need protected landing facts.");
            WorldPosition? best = null; int distance = world.Source.Distance(world.Target);
            foreach (var (x, y) in Cardinals)
            {
                var candidate = world.Source.Offset(x * 2, y * 2); int score = candidate.Distance(world.Target);
                if (wall(world.Source.Offset(x, y)) && score < distance && world.Free(candidate) && !reserved(candidate)
                    && (best == null || score < best.Value.Distance(world.Target))) best = candidate;
            }
            if (best is { } landing) return new(EnemyIntentKind.Windup, Ability: "core:ability/wraith_phase", Cells: [landing], Support: new(EnemySupportKind.Phase));
        }
        var goals = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
        return world.NextStep(goals) is { } step ? new(EnemyIntentKind.Move, step) : new(EnemyIntentKind.Wait);
    }

    private static EnemyIntent Toad(EnemyPlanningContext world)
    {
        var loot = world.Loot ?? throw new ArgumentException("Toad decisions need physical loot facts.");
        var self = world.Self ?? throw new ArgumentException("Toad decisions need their carrier identity.");
        GroundLoot? best = null; int distance = int.MaxValue;
        if (!loot.Any(d => d.CarriedBy == self.Id))
            foreach (var drop in loot)
            {
                if (drop.CarriedBy != 0 || drop.Position.Floor != world.Source.Floor) continue;
                int score = world.Source.Distance(drop.Position);
                if (score <= 4 && world.Free(drop.Position) && ClearLine(world with { Target = drop.Position })
                    && (score < distance || score == distance && drop.Id < best!.Id))
                { best = drop; distance = score; }
            }
        if (best != null)
        {
            if (distance <= 1) return new(EnemyIntentKind.Windup, Ability: "core:ability/toad_gulp",
                Cells: [best.Position], Support: new(EnemySupportKind.Gulp, Drop: best.Id));
            return world.NextStep(new HashSet<WorldPosition> { best.Position }) is { } towardLoot
                ? new(EnemyIntentKind.Move, towardLoot) : new(EnemyIntentKind.Wait);
        }
        if (world.Source.Distance(world.Target) == 1)
            return new(EnemyIntentKind.Windup, Ability: "core:ability/toad_bite", Cells: [world.Target]);
        var goals = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
        return world.NextStep(goals) is { } step ? new(EnemyIntentKind.Move, step) : new(EnemyIntentKind.Wait);
    }
}
