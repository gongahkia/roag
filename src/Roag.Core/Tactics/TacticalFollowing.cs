using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private bool TryFollowerTurn(Actor actor)
    {
        if (_world.PartyFollowing is not { } following || !following.TryGetValue(actor.Id, out long leaderId)) return false;
        var leader = Actor(leaderId);
        if (leader.Life != LifeState.Alive || leader.Position.Floor != actor.Position.Floor || IsLocallyEngaged(actor.Id)) return false;

        // Follow the nearer part of this group through narrow passages without
        // swapping/passing through bodies or borrowing the leader's action.
        int distance = actor.Position.Distance(leader.Position);
        var (forwardX, forwardY) = AttackGeometry.Offset(leader.Tactics!.Facing);
        bool Ahead(WorldPosition p) => (p.X - leader.Position.X) * forwardX + (p.Y - leader.Position.Y) * forwardY > 0;
        var anchor = _world.Party.Where(a => a.Id == leader.Id || following.GetValueOrDefault(a.Id) == leader.Id)
            .Where(a => a.Life == LifeState.Alive && a.Position.Distance(leader.Position) < distance)
            .OrderBy(a => actor.Position.Distance(a.Position)).ThenBy(a => a.Position.Distance(leader.Position)).ThenBy(a => a.Id)
            .FirstOrDefault() ?? leader;
        // Prefer rear/side landings so faster followers leave the leader's next step clear.
        if (Ahead(anchor.Position)) anchor = leader;
        TacticalIntent intent = new(TacticalAction.Wait);
        if (actor.Position.Distance(anchor.Position) > 1 || Ahead(actor.Position))
        {
            var goals = new[] { Facing.North, Facing.East, Facing.South, Facing.West }.Select(f =>
            {
                var (dx, dy) = AttackGeometry.Offset(f); return anchor.Position.Offset(dx, dy);
            }).Where(p => !Ahead(p)).ToHashSet();
            var step = _world.Navigate(actor, goals, p => p.Floor == actor.Position.Floor && _world.Chunks.Seen(p)
                && _world.Chunks.At(p) is not (Material.StairsUp or Material.StairsDown));
            if (step != null) intent = new(step.Action, step.Facing);
        }
        var result = Commit(intent);
        if (!result.Success) throw new InvalidOperationException("Invalid follower decision: " + result.Message);
        return true;
    }
}
