using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewNativeWork(Actor actor, WorldCommand? command)
    {
        if (!Friendly(actor) || !Tuning.EnableSurvival || IsLocallyEngaged(actor.Id)) return Invalid("Leave immediate combat before assigning expedition work.");
        string reason = _world.NativeWorkReason(actor, command);
        return reason.Length > 0 ? Invalid(reason) : new(true, "", ActionTiming.Recovery(Tuning.WaitCost, EffectiveSpeed(actor)), []);
    }

    private void BeginNativeWork(Actor actor, WorldCommand command, long action)
    {
        if (command is { Kind: WorldCommandKind.AssignWork, ActorId: > 0 } && command.ActorId != actor.Id)
        {
            _world.AssignNativeResidentWork(actor, command);
            Emit("WorkAssigned", actor.Id, command.ActorId, action, actor.Position, ability: command.WorkId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
        var order = _world.BeginNativeWork(actor, command);
        actor.Tactics!.Activity = new(SustainedActivityKind.Work, 0, order.Target, order.Id);
        actor.Body.RestUntil = 0; _world.PartyFollowing?.Remove(actor.Id);
        Emit("ActivityStarted", actor.Id, actor.Id, action, actor.Position, ability: "Work:" + order.Kind);
    }

    private bool WorkActivityTurn(Actor actor, SustainedActivity activity)
    {
        var intent = _world.NativeWorkStep(actor, activity.Work);
        if (intent == null) { StopActivity(actor, "work-stopped"); return false; }
        _continuingActivity = true;
        try
        {
            var result = Commit(intent);
            if (result.Success) return true;
            StopActivity(actor, "work-blocked"); return false;
        }
        finally { _continuingActivity = false; }
    }

    private void ContinueActivity(Actor actor, long recovery, long action)
    {
        var activity = actor.Tactics!.Activity!;
        if (activity.Kind == SustainedActivityKind.Work && !_world.PerformNativeWork(actor, activity.Work, recovery, this, action))
            StopActivity(actor, _world.State.Work.Any(w => w.Id == activity.Work) ? "work-stopped" : "completed");
        Emit("ActivityContinued", actor.Id, actor.Id, action, actor.Position);
    }
}
