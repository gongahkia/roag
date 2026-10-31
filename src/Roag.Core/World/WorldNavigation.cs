using Roag.Core.Tactics;

namespace Roag.Core.World;

internal sealed record WorldNavigationStep(TacticalAction Action, Facing Facing, WorldPosition Destination);

public sealed partial class WorldSession
{
    private static readonly Facing[] NavigationFacings = [Facing.North, Facing.East, Facing.South, Facing.West];
    // Search actual movement edges: entering a stair and arriving at its paired
    // endpoint are one paid step. There are no camera or explored-map gates.
    internal WorldNavigationStep? Navigate(Actor actor, IReadOnlySet<WorldPosition> goals,
        Func<WorldPosition, bool> allowed)
    {
        const int nodeLimit = 32768;
        var destinations = goals.Where(p => Plan.Contains(p) && allowed(p)).ToArray();
        if (destinations.Length == 0 || destinations.Contains(actor.Position)) return null;
        int Estimate(WorldPosition p) => destinations.Min(g => Math.Max(
            Math.Abs(p.X + 4 * p.Floor - g.X - 4 * g.Floor) + Math.Abs(p.Y - g.Y), Math.Abs(p.Floor - g.Floor)));
        var open = new PriorityQueue<(WorldPosition Position, int Cost), (int Score, int Tie)>();
        var nodes = new Dictionary<WorldPosition, (int Cost, WorldNavigationStep? First)> { [actor.Position] = (0, null) };
        int tie = 0;
        open.Enqueue((actor.Position, 0), (Estimate(actor.Position), tie++));
        while (open.TryDequeue(out var entry, out _))
        {
            var (current, cost) = entry;
            if (nodes[current].Cost != cost) continue;
            if (goals.Contains(current)) return nodes[current].First;
            if (nodes.Count >= nodeLimit) return null;
            foreach (var facing in NavigationFacings)
            {
                var (dx, dy) = AttackGeometry.Offset(facing);
                var next = current.Offset(dx, dy);
                if (!allowed(next) || !CanEnter(next, actor.Id)) continue;
                if (Chunks.At(next) is Material.StairsDown or Material.StairsUp)
                {
                    if (StairLanding(next) is not { } landing || !allowed(landing) || !CanEnter(landing, actor.Id)) continue;
                    next = landing;
                }
                Visit(new(TacticalAction.Move, facing, next));
            }
            if (StairLanding(current) is { } endpoint && allowed(endpoint) && CanEnter(endpoint, actor.Id))
                Visit(new(TacticalAction.UseStairs, actor.Tactics!.Facing, endpoint));

            void Visit(WorldNavigationStep step)
            {
                int nextCost = cost + 1;
                if (nodes.TryGetValue(step.Destination, out var previous) && previous.Cost <= nextCost) return;
                nodes[step.Destination] = (nextCost, nodes[current].First ?? step);
                open.Enqueue((step.Destination, nextCost), (nextCost + Estimate(step.Destination), tie++));
            }
        }
        return null;
    }
}
