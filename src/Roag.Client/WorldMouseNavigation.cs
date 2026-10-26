using Microsoft.Xna.Framework;
using Roag.Core.World;
using Roag.Graphics;

namespace Roag.Client;

internal static class WorldCameraScale
{
    public static float Minimum(Rectangle viewport) => Math.Max(24, Math.Max(viewport.Width / 48f, viewport.Height / 32f));
    public static float Default(Rectangle viewport) => Math.Clamp(viewport.Width / 28f, 36, 64);
    public static void Apply(MapCamera camera, bool reset = false)
    {
        float minimum = Minimum(camera.Viewport);
        camera.ConstrainZoom(minimum, Math.Max(96, minimum));
        if (reset) camera.Reset(Default(camera.Viewport));
    }
}

internal sealed record WorldClickIntent(string Kind, WorldPosition Cell, long Actor = 0);
internal sealed record WorldClickPlan(IReadOnlyList<WorldPosition> Path, WorldClickIntent? Interaction, bool Reachable, string Reason = "");

internal static class WorldMouseNavigation
{
    // Hover checks only this cell. Connected routes and approach searches are deferred until a click.
    public static string HoverBlockReason(WorldSession session, WorldPosition cell)
    {
        if (!session.Plan.Contains(cell) || cell.Floor != session.State.Position.Floor) return "Choose ground on the current floor.";
        if (!session.Chunks.Seen(cell)) return "Choose observed ground first.";
        if (Intent(session, cell) != null) return "";
        return session.MovementBlockReason(cell, session.State.Party[0], observedOnly: true);
    }

    public static WorldClickIntent? Intent(WorldSession session, WorldPosition cell)
    {
        if (!session.Chunks.Seen(cell)) return null;
        var actor = session.State.Actors.FirstOrDefault(a => !a.Recruited && a.Life != LifeState.Dead && a.Position == cell && session.Visible.Contains(cell));
        if (actor != null && session.SituationForActor(actor) is { } situation) return new("discovery", situation.Position);
        if (actor != null) return new(actor.Kind == ActorKind.Person ? "person" : actor.Kind is ActorKind.Beast or ActorKind.Raider ? "enemy" : "actor", cell, actor.Id);
        if (session.State.Discoveries.Any(d => d.Position == cell)) return new("discovery", cell);
        if (session.State.Loot.Any(c => !c.Taken && c.Position == cell && session.Visible.Contains(cell))) return new("loot", cell);
        return session.Chunks.At(cell) switch
        {
            Material.StairsDown or Material.StairsUp => new("stairs", cell),
            Material.Storage => new("storage", cell),
            _ => session.OccupantAt(cell, observedOnly: true) is { Recruited: true } member ? new("party", cell, member.Id) : null
        };
    }
    public static WorldClickPlan Plan(WorldSession session, WorldPosition cell)
    {
        if (!session.Plan.Contains(cell) || cell.Floor != session.State.Position.Floor) return new([], null, false, "Choose ground on the current floor.");
        if (!session.Chunks.Seen(cell)) return new([], null, false, "Unexplored ground. Move toward the visible edge first.");
        var intent = Intent(session, cell);
        if (intent != null && intent.Cell != cell) return Plan(session, intent.Cell);
        if (intent?.Kind == "party") return new([], intent, true);
        if (intent == null)
        {
            var path = session.Route(cell); bool reachable = path.Count > 0 || cell == session.State.Position;
            string blocked = session.MovementBlockReason(cell, session.State.Party[0], observedOnly: true);
            if (blocked.Length == 0) blocked = "No connected route through observed free ground. Move a blocking character or open a passage.";
            return new(path, null, reachable, reachable ? "" : blocked);
        }
        int range = intent.Kind is "person" or "actor" ? 2 : 1;
        if (session.State.Position.Distance(cell) <= range) return new([], intent, true);
        var candidates = new List<(WorldPosition Cell, int Score)>();
        for (int y = -range; y <= range; y++) for (int x = -range; x <= range; x++)
        {
            if (Math.Abs(x) + Math.Abs(y) > range) continue;
            var p = cell.Offset(x, y);
            if (session.Chunks.Seen(p) && session.CanEnter(p, session.State.Party[0], observedOnly: true)) candidates.Add((p, p.Distance(session.State.Position)));
        }
        // Try the nearest observed approach cells first; no hidden terrain enters route selection.
        foreach (var candidate in candidates.OrderBy(c => c.Score).ThenBy(c => c.Cell.Y).ThenBy(c => c.Cell.X))
        {
            var path = session.Route(candidate.Cell);
            if (path.Count > 0) return new(path, intent, true);
        }
        return new([], intent, false, "No observed approach reaches this object. Explore nearer ground or open a passage.");
    }
}
