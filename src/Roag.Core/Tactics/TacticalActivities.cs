using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum SustainedActivityKind { Rest, Watch }

/// <summary>An explicit continuing order, paid one native turn at a time.</summary>
public sealed record SustainedActivity(SustainedActivityKind Kind, long Until, WorldPosition Anchor);

public sealed record CampTuning(int RestNoticeRange = 2, int WatchNoticeRange = 11, int AlarmHearing = 16, int WallSoundCost = 5, int FireMinutes = 10, int FireWarmthRange = 3, int FireDeterrenceRange = 3, int FireAttractionRange = 11, int FireWarmthPercent = 85)
{
    internal void Validate()
    {
        if (RestNoticeRange is < 0 or > 11 || WatchNoticeRange is < 1 or > 11 || AlarmHearing is < 1 or > 64 || WallSoundCost is < 1 or > 64
            || FireMinutes is < 1 or > 240 || FireWarmthRange is < 1 or > 11 || FireDeterrenceRange is < 1 or > 11
            || FireAttractionRange is < 1 or > 11 || FireWarmthPercent is < 0 or > 100)
            throw new InvalidDataException("Invalid native camp tuning.");
    }
}

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewActivity(Actor actor, TacticalIntent intent)
    {
        if (!Friendly(actor) || !Tuning.EnableSurvival) return Invalid("Survival activities require a living party member.");
        if (intent.Action == TacticalAction.ContinueActivity)
            return actor.Tactics!.Activity != null ? new(true, "", ActionTiming.Recovery(Tuning.WaitCost, EffectiveSpeed(actor)), [])
                : Invalid("This member has no continuing activity.");
        if (intent.Choice is < 1 or > 240) return Invalid("Choose an activity duration of 1 to 240 minutes.");
        if (IsLocallyEngaged(actor.Id)) return Invalid("Leave immediate combat before resting or standing watch.");
        return new(true, "", ActionTiming.Recovery(Tuning.WaitCost, EffectiveSpeed(actor)), []);
    }

    private void BeginActivity(Actor actor, TacticalIntent intent, long action)
    {
        var kind = intent.Action == TacticalAction.Rest ? SustainedActivityKind.Rest : SustainedActivityKind.Watch;
        actor.Tactics!.Activity = new(kind, checked(Clock + (long)intent.Choice * 60 * Tuning.UnitsPerSecond), actor.Position);
        // Rest is physical and personal. Watch never grants a rest interval.
        actor.Body.RestUntil = kind == SustainedActivityKind.Rest
            ? actor.Tactics.Activity.Until / Tuning.UnitsPerWorldTick + 1 : 0;
        _world.State.Dungeon?.Following.Remove(actor.Id);
        Emit("ActivityStarted", actor.Id, actor.Id, action, actor.Position, intent.Choice, ability: kind.ToString());
    }

    public bool CancelActivity(long memberId)
    {
        if (!_world.State.Party.Contains(memberId) || Actor(memberId).Tactics?.Activity == null) return false;
        StopActivity(Actor(memberId), "cancelled");
        return true;
    }

    private void StopActivity(Actor actor, string reason)
    {
        if (actor.Tactics?.Activity is not { } activity) return;
        actor.Tactics.Activity = null; actor.Body.RestUntil = 0;
        Emit("ActivityStopped", actor.Id, actor.Id, 0, actor.Position, ability: reason + ":" + activity.Kind);
    }

    private bool ActivityTurn(Actor actor)
    {
        if (actor.Tactics!.Activity is not { } activity) return false;
        if (Clock >= activity.Until) { StopActivity(actor, "completed"); return false; }
        if (actor.Position != activity.Anchor) { StopActivity(actor, "displaced"); return false; }
        var result = Commit(new(TacticalAction.ContinueActivity));
        if (!result.Success) throw new InvalidOperationException(result.Message);
        return true;
    }

    /// <summary>Attenuated physical sound; walls cost distance, floors do not magically share hearing.</summary>
    public bool CanHear(WorldPosition source, WorldPosition listener, int budget)
    {
        if (source.Floor != listener.Floor || source.Distance(listener) > budget) return false;
        var frontier = new PriorityQueue<WorldPosition, int>(); var best = new Dictionary<WorldPosition, int> { [source] = 0 };
        frontier.Enqueue(source, 0);
        while (frontier.TryDequeue(out var p, out int spent))
        {
            if (spent != best[p]) continue;
            if (p == listener) return true;
            foreach (var direction in new[] { Facing.North, Facing.East, Facing.South, Facing.West })
            {
                var (dx, dy) = AttackGeometry.Offset(direction); var next = p.Offset(dx, dy);
                if (!_world.Plan.Contains(next)) continue;
                int cost = spent + (Materials.Opaque(_world.Chunks.At(next)) || _world.TacticalBlockAt(next) ? State.Camp.WallSoundCost : 1);
                if (cost > budget || best.TryGetValue(next, out int prior) && prior <= cost) continue;
                best[next] = cost; frontier.Enqueue(next, cost);
            }
        }
        return false;
    }

    private void ObserveActivities()
    {
        var busy = _world.Party.Where(a => a.Life == LifeState.Alive && a.Tactics?.Activity != null).OrderBy(a => a.Id).ToArray();
        foreach (var member in busy)
        {
            if (member.Tactics!.Activity is not { } activity) continue;
            if (member.Position != activity.Anchor) { StopActivity(member, "displaced"); continue; }
            int range = activity.Kind == SustainedActivityKind.Watch ? State.Camp.WatchNoticeRange : State.Camp.RestNoticeRange;
            var threats = _world.State.Actors.Where(a => a.Life != LifeState.Dead && a.Tactics?.Enemy is { Dormant: false }
                && _world.IsHostile(a) && a.Position.Distance(member.Position) <= range).ToArray();
            if (threats.Length == 0 || !threats.Any(a => _world.VisionFrom(member.Position).Contains(a.Position))) continue;
            StopActivity(member, "danger");
            if (activity.Kind == SustainedActivityKind.Watch) SoundAlarm(member);
        }
    }

    private void SoundAlarm(Actor watcher)
    {
        Emit("CampAlarm", watcher.Id, watcher.Id, 0, watcher.Position, State.Camp.AlarmHearing);
        foreach (var member in _world.Party.Where(a => a.Life == LifeState.Alive && a.Tactics?.Activity != null).ToArray())
            if (CanHear(watcher.Position, member.Position, State.Camp.AlarmHearing)) StopActivity(member, "alarm");
        foreach (var enemy in _world.State.Actors.Where(a => a.Life == LifeState.Alive && a.Tactics?.Enemy is { Dormant: false }
                     && _world.IsHostile(a) && PursuitAllowed(a, watcher.Position)))
            if (CanHear(watcher.Position, enemy.Position, State.Camp.AlarmHearing))
                enemy.Tactics!.Enemy!.Awareness = new(watcher.Id, watcher.Position, Clock);
    }
}
