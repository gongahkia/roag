using Microsoft.Xna.Framework;
using Vagabonds.Core.World;
using Vagabonds.Graphics;

namespace Vagabonds.Client;

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
internal sealed record WorldClickPlan(IReadOnlyList<WorldPosition> Path, WorldClickIntent? Interaction, bool Reachable);

internal static class WorldMouseNavigation
{
    public static WorldClickIntent? Intent(WorldSession session, WorldPosition cell)
    {
        if (!session.Chunks.Seen(cell)) return null;
        var actor = session.State.Actors.FirstOrDefault(a => !a.Recruited && a.Life == LifeState.Alive && a.Position == cell && session.Visible.Contains(cell));
        if (actor != null) return new(actor.Kind == ActorKind.Person ? "person" : actor.Kind is ActorKind.Beast or ActorKind.Raider ? "enemy" : "actor", cell, actor.Id);
        if (session.State.Loot.Any(c => !c.Taken && c.Position == cell && session.Visible.Contains(cell))) return new("loot", cell);
        return session.Chunks.At(cell) switch
        {
            Material.StairsDown or Material.StairsUp => new("stairs", cell),
            Material.Storage => new("storage", cell), _ => null
        };
    }
    public static WorldClickPlan Plan(WorldSession session, WorldPosition cell)
    {
        if (!session.Plan.Contains(cell) || cell.Floor != session.State.Position.Floor || !session.Chunks.Seen(cell)) return new([], null, false);
        var intent = Intent(session, cell);
        if (intent == null) { var path = session.Route(cell); return new(path, null, path.Count > 0 || cell == session.State.Position); }
        int range = intent.Kind is "person" or "actor" ? 2 : 1;
        if (session.State.Position.Distance(cell) <= range) return new([], intent, true);
        var candidates = new List<(WorldPosition Cell, int Score)>();
        for (int y = -range; y <= range; y++) for (int x = -range; x <= range; x++)
        {
            if (Math.Abs(x) + Math.Abs(y) > range) continue;
            var p = cell.Offset(x, y);
            if (session.Chunks.Seen(p) && Materials.Walkable(session.Chunks.At(p))) candidates.Add((p, p.Distance(session.State.Position)));
        }
        // Try the nearest observed approach cells first; no hidden terrain enters route selection.
        foreach (var candidate in candidates.OrderBy(c => c.Score).ThenBy(c => c.Cell.Y).ThenBy(c => c.Cell.X))
        {
            var path = session.Route(candidate.Cell);
            if (path.Count > 0) return new(path, intent, true);
        }
        return new([], intent, false);
    }
}
