using System.Collections.Immutable;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum EnemyIntentKind { Wait, Move, Windup }
public sealed record EnemyIntent(EnemyIntentKind Kind, Facing Facing = Facing.South, string Ability = "",
    AttackDelivery Delivery = AttackDelivery.Melee, ImmutableArray<WorldPosition> Cells = default,
    int DamageBonus = 0, bool Alert = false, bool ClearRetaliation = false,
    EnemySupport? Support = null, EnemyAdvance? Advance = null, Facing? ShieldFacing = null, EnemyShove? Shove = null);

public enum EnemySupportKind { Heal, Summon, Ward, Snare, Phase, Gulp, Revive, Alarm }
public sealed record EnemySupport(EnemySupportKind Kind, int Power = 0, string Definition = "", int MaximumActive = 0,
    long Target = 0, long Drop = 0, long Corpse = 0);
public sealed record EnemyAdvance(Facing Facing, int Steps);
public sealed record EnemyShove(WorldPosition Cell, Facing Facing);
public sealed record EnemyNeighbor(long Id, WorldPosition Position, int Health, int MaximumHealth, bool Ally,
    string Definition = "", EnemySquad? Squad = null);

/// <summary>Read-only terrain and navigation queries, independent of camera, floor windows and selected party member.</summary>
public sealed record EnemyPlanningContext(WorldPosition Source, WorldPosition Target,
    Func<WorldPosition, bool> Free, Func<WorldPosition, bool> ProjectileOpen, Func<WorldPosition, bool> MistBlocks,
    Func<IReadOnlySet<WorldPosition>, Facing?> NextStep,
    Func<WorldPosition, bool>? Barricade = null, IReadOnlyList<EnemyNeighbor>? Neighbors = null,
    EnemyNeighbor? Self = null, IReadOnlyList<EnemySnare>? Snares = null, Func<WorldPosition, bool>? TargetFree = null,
    Func<WorldPosition, bool>? SolidWall = null, Func<WorldPosition, bool>? ProtectedLanding = null,
    IReadOnlyList<GroundLoot>? Loot = null, IReadOnlyList<RevivalCandidate>? Corpses = null);

public static partial class EnemyPlanning
{
    private static readonly (int X, int Y)[] Cardinals = [(0, -1), (1, 0), (0, 1), (-1, 0)];
    private static readonly (int X, int Y)[] Diagonals = [(-1, -1), (1, -1), (1, 1), (-1, 1)];
    private static readonly IReadOnlyDictionary<string, (string First, string Second)> ElitePatterns =
        new Dictionary<string, (string, string)>
        {
            ["core:enemy/mycelial_matriarch"] = ("target_diagonals", "source_ring"),
            ["core:enemy/canal_lockmaster"] = ("source_lane", "target_sidebars"),
            ["core:enemy/ash_marshal"] = ("source_fan", "target_cross"),
            ["core:enemy/prism_curator"] = ("target_star", "target_ring"),
            ["core:enemy/index_keeper"] = ("target_row", "target_columns"),
            ["core:enemy/ember_crucible"] = ("source_spokes", "target_box")
        };

    public static bool Supported(EnemyBrainKind brain) => brain is EnemyBrainKind.Guard or EnemyBrainKind.Spitter
        or EnemyBrainKind.Skitter or EnemyBrainKind.Warden or EnemyBrainKind.Pursuer or EnemyBrainKind.SporeHerald
        or EnemyBrainKind.CanalLancer or EnemyBrainKind.CinderBellkeeper or EnemyBrainKind.HollowRegent or EnemyBrainKind.BiomeElite
        or EnemyBrainKind.ExpansionRegular or EnemyBrainKind.Squad;

    public static bool Supported(EnemyDefinition definition) => Supported(definition.Ai)
        || definition.Ai == EnemyBrainKind.IdeaRegular && definition.Id is "core:enemy/heavy_rammer" or "core:enemy/hollow_wraith"
            or "core:enemy/gulp_toad" or "core:enemy/bone_splitter" or "core:enemy/bone_fragment" or "core:enemy/haunted_cache"
            or "core:enemy/grave_cantor" or "core:enemy/alarm_keeper";

    public static EnemyIntent Choose(EnemyDefinition definition, EnemyMemory memory, EnemyPlanningContext world)
    {
        definition.Validate(); memory.Validate();
        if (world.Source.Floor != world.Target.Floor) throw new ArgumentException("Route between floors before selecting a local attack.");
        if (!Supported(definition)) throw new NotSupportedException("This enemy controller still requires its native support/effect port: " + definition.Id);
        if (definition.Ai == EnemyBrainKind.ExpansionRegular) return Expansion(definition, memory, world);
        if (definition.Ai == EnemyBrainKind.Squad) return Squad(definition, memory, world);
        if (definition.Ai == EnemyBrainKind.IdeaRegular) return Idea(definition, memory, world);
        var source = world.Source; var target = world.Target;
        int dx = target.X - source.X, dy = target.Y - source.Y, distance = source.Distance(target);
        EnemyIntent Windup(string ability, IEnumerable<WorldPosition> cells, AttackDelivery delivery = AttackDelivery.Melee)
            => new(EnemyIntentKind.Windup, Ability: "core:ability/" + ability, Delivery: delivery, Cells: cells.ToImmutableArray());
        EnemyIntent Toward(IReadOnlySet<WorldPosition> goals) => world.NextStep(goals) is { } facing
            ? new(EnemyIntentKind.Move, facing) : new(EnemyIntentKind.Wait);
        HashSet<WorldPosition> Adjacent(bool diagonal = false) => (diagonal ? Diagonals : Cardinals)
            .Select(d => target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
        HashSet<WorldPosition> Lanes(int min, int max)
        {
            var goals = new HashSet<WorldPosition>();
            foreach (var d in Cardinals) for (int length = min; length <= max; length++)
            {
                var p = target.Offset(d.X * length, d.Y * length);
                if (world.Free(p) && Lane(world with { Source = p }, min, max) is not null) goals.Add(p);
            }
            return goals;
        }

        if (definition.Ai == EnemyBrainKind.BiomeElite)
        {
            var patterns = ElitePatterns[definition.Id];
            if (distance <= definition.Range && ClearLine(world))
            {
                string pattern = (memory.Attacks % 2 + (memory.PhaseTwo ? 1 : 0)) % 2 == 0 ? patterns.First : patterns.Second;
                var cells = EliteCells(pattern, world);
                if (cells.Length > 0) return Windup("elite_" + pattern, cells, AttackDelivery.Area);
            }
            return Toward(Adjacent());
        }
        if (definition.Ai == EnemyBrainKind.HollowRegent)
        {
            int pattern = (int)(memory.Attacks % 3);
            var line = Lane(world, 1, 5);
            if (pattern == 0 && line != null) return Windup("regent_line", line, AttackDelivery.PiercingLine);
            if (pattern == 1 && distance <= 2)
            {
                var (fx, fy) = Axis(dx, dy); int sx = -fy, sy = fx;
                var cells = new List<WorldPosition>();
                for (int step = 1; step <= 2; step++) for (int side = -1; side <= 1; side++)
                {
                    var p = source.Offset(fx * step + sx * side, fy * step + sy * side);
                    if (world.ProjectileOpen(p)) cells.Add(p);
                }
                return Windup("regent_crown", cells, AttackDelivery.Area);
            }
            if (distance <= definition.Range && ClearLine(world))
                return Windup("regent_sigil", EliteCells("target_box", world), AttackDelivery.Area);
            return Toward(Adjacent());
        }
        if (definition.Ai is EnemyBrainKind.Pursuer or EnemyBrainKind.Guard)
            return distance == 1 ? Windup(definition.Ai == EnemyBrainKind.Pursuer ? "pursuer_strike" : "guard_strike", [target]) : Toward(Adjacent());
        if (definition.Ai == EnemyBrainKind.Warden)
        {
            var line = Lane(world, 1, definition.Range);
            if (line != null && (distance != 1 || memory.Attacks % 2 == 1)) return Windup("warden_roots", line, AttackDelivery.Projectile);
            if (distance == 1) return Windup("warden_sweep", dx != 0
                ? [target, target.Offset(0, -1), target.Offset(0, 1)] : [target, target.Offset(-1, 0), target.Offset(1, 0)]);
            return Toward(Adjacent());
        }
        if (definition.Ai == EnemyBrainKind.SporeHerald)
        {
            if (distance <= 1)
                foreach (var (x, y) in Cardinals)
                {
                    var p = source.Offset(x, y);
                    if (world.Free(p) && p.Distance(target) > distance && ClearLine(world with { Source = p }))
                    { AttackGeometry.TryFacing(x, y, out var facing); return new(EnemyIntentKind.Move, facing); }
                }
            if (distance <= definition.Range && ClearLine(world))
            {
                (int X, int Y)[] offsets = Math.Abs(dx) >= Math.Abs(dy) ? [(0, -1), (0, 1)] : [(-1, 0), (1, 0)];
                return Windup("spore_burst", new[] { target }.Concat(offsets.Select(d => target.Offset(d.X, d.Y)).Where(world.ProjectileOpen)), AttackDelivery.Area);
            }
            return Toward(Lanes(2, definition.Range));
        }
        if (definition.Ai == EnemyBrainKind.CanalLancer)
        {
            var line = Lane(world, 1, 2);
            if (line != null)
            {
                var extra = source.Offset(2 * Math.Sign(dx), 2 * Math.Sign(dy));
                if (line.Count == 1 && world.ProjectileOpen(extra)) line.Add(extra);
                return Windup("canal_thrust", line, AttackDelivery.PiercingLine);
            }
            return Toward(Lanes(1, 2));
        }
        if (definition.Ai == EnemyBrainKind.CinderBellkeeper)
            return Lane(world, 1, definition.Range) != null
                ? Windup("bell_cross", new[] { target }.Concat(Cardinals.Select(d => target.Offset(d.X, d.Y)).Where(world.ProjectileOpen)), AttackDelivery.Area)
                : Toward(Lanes(2, definition.Range));
        if (definition.Ai == EnemyBrainKind.Spitter)
        {
            var line = Lane(world, definition.MinRange, definition.Range);
            return line != null ? Windup("thorn_lane", line, AttackDelivery.Projectile) : Toward(Lanes(definition.MinRange, definition.Range));
        }
        if (Math.Abs(dx) == 1 && Math.Abs(dy) == 1) return Windup("skitter_stab", [target]);
        var flank = world.NextStep(Adjacent(diagonal: true));
        if (flank is { } direction) return new(EnemyIntentKind.Move, direction);
        return Math.Max(Math.Abs(dx), Math.Abs(dy)) == 1 ? Windup("skitter_stab", [target]) : Toward(Adjacent());
    }

    private static (int X, int Y) Axis(int dx, int dy) => Math.Abs(dx) >= Math.Abs(dy) ? (dx >= 0 ? 1 : -1, 0) : (0, dy >= 0 ? 1 : -1);

    private static List<WorldPosition>? Lane(EnemyPlanningContext world, int minimum, int maximum)
    {
        int dx = world.Target.X - world.Source.X, dy = world.Target.Y - world.Source.Y;
        int distance = world.Source.Distance(world.Target);
        if (dx != 0 && dy != 0 || distance < minimum || distance > maximum) return null;
        var cells = new List<WorldPosition>();
        for (int step = 1; step <= distance; step++)
        {
            var p = world.Source.Offset(Math.Sign(dx) * step, Math.Sign(dy) * step);
            if (!world.ProjectileOpen(p) || world.MistBlocks(p)) return null;
            cells.Add(p);
        }
        return cells;
    }

    private static bool ClearLine(EnemyPlanningContext world)
    {
        var p = world.Source; var target = world.Target;
        int dx = Math.Abs(target.X - p.X), dy = Math.Abs(target.Y - p.Y);
        int sx = target.X > p.X ? 1 : -1, sy = target.Y > p.Y ? 1 : -1, error = dx - dy;
        while (p != target)
        {
            var old = p; int twice = 2 * error;
            if (twice > -dy) { error -= dy; p = p.Offset(sx, 0); }
            if (twice < dx) { error += dx; p = p.Offset(0, sy); }
            if (p.X != old.X && p.Y != old.Y && (!world.ProjectileOpen(new(p.X, old.Y, p.Floor)) || !world.ProjectileOpen(new(old.X, p.Y, p.Floor)))) return false;
            if (!world.ProjectileOpen(p) || world.MistBlocks(p)) return false;
        }
        return true;
    }

    private static ImmutableArray<WorldPosition> EliteCells(string pattern, EnemyPlanningContext world)
    {
        var cells = new List<WorldPosition>(); var seen = new HashSet<WorldPosition>();
        var s = world.Source; var t = world.Target; var (fx, fy) = Axis(t.X - s.X, t.Y - s.Y); int px = -fy, py = fx;
        void Add(WorldPosition p) { if (world.ProjectileOpen(p) && seen.Add(p)) cells.Add(p); }
        switch (pattern)
        {
            case "target_diagonals": Add(t); foreach (var d in Diagonals) Add(t.Offset(d.X, d.Y)); break;
            case "source_ring": case "target_ring":
                var center = pattern == "source_ring" ? s : t;
                for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++) if (Math.Max(Math.Abs(x), Math.Abs(y)) == 2) Add(center.Offset(x, y));
                break;
            case "source_lane":
                for (int i = 1; i <= 4; i++) { var p = s.Offset(fx * i, fy * i); if (!world.ProjectileOpen(p)) break; Add(p); } break;
            case "target_sidebars":
                for (int i = -1; i <= 1; i++) { Add(t.Offset(px + fx * i, py + fy * i)); Add(t.Offset(-px + fx * i, -py + fy * i)); } break;
            case "source_fan":
                for (int i = 1; i <= 2; i++) for (int side = -i; side <= i; side++) Add(s.Offset(fx * i + px * side, fy * i + py * side)); break;
            case "target_cross": Add(t); foreach (var d in Cardinals) Add(t.Offset(d.X, d.Y)); break;
            case "target_star": Add(t); for (int i = 1; i <= 2; i++) foreach (var d in Diagonals) Add(t.Offset(d.X * i, d.Y * i)); break;
            case "target_row": for (int i = -2; i <= 2; i++) Add(t.Offset(i, 0)); break;
            case "target_columns": for (int i = -1; i <= 1; i++) { Add(t.Offset(-1, i)); Add(t.Offset(1, i)); } break;
            case "source_spokes": for (int i = 1; i <= 3; i++) { Add(s.Offset(fx * i, fy * i)); Add(s.Offset(px * i, py * i)); Add(s.Offset(-px * i, -py * i)); } break;
            case "target_box": for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) Add(t.Offset(x, y)); break;
            default: throw new InvalidDataException("Unknown elite attack pattern.");
        }
        return cells.ToImmutableArray();
    }
}
