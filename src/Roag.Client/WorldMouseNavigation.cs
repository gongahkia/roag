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
    // Inspection never searches for a route or moves the party.
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
            string blocked = session.MovementBlockReason(cell, session.State.Party[0], observedOnly: true);
            return new([], null, false, blocked.Length > 0 ? blocked : "Move with WASD or arrow keys, one tile per press.");
        }
        int range = intent.Kind is "person" or "actor" ? 2 : 1;
        if (session.State.Position.Distance(cell) <= range) return new([], intent, true);
        return new([], intent, false, "Move closer with WASD or arrow keys to interact.");
    }
}
