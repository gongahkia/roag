using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewRecruit(Actor actor, long targetId)
    {
        var recruit = _world.NativeAdventurers.FirstOrDefault(r => r.Actor == targetId);
        var candidate = _world.State.Actors.FirstOrDefault(a => a.Id == targetId);
        if (!Friendly(actor) || recruit == null || candidate is not { Life: LifeState.Alive, Recruited: false }
            || candidate.Tactics != null && candidate.Tactics.Resident == null
            || actor.Position.Distance(candidate.Position) != 1 || !_world.VisionFrom(actor.Position).Contains(candidate.Position))
            return Invalid("Stand beside an available adventurer to recruit them.");
        if (_world.Party.Count(a => a.Life != LifeState.Dead) >= 4)
            return Invalid("Four surviving members already fill the party.");
        return new(true, "", ActionTiming.Recovery(100, EffectiveSpeed(actor)), [candidate.Position]);
    }

    private void RecruitDungeonMember(Actor actor, long targetId, long action)
    {
        var recruit = _world.NativeAdventurers.Single(r => r.Actor == targetId);
        var candidate = Actor(targetId);
        candidate.Recruited = true;
        _world.State.Party.Add(candidate.Id);
        int seed = RoagRandom.Derive(_world.DungeonSeed, "recruit-build:" + candidate.Id);
        // Joining pays its own full recovery too; recruitment never manufactures an immediate extra turn.
        long recovery = ActionTiming.Recovery(100, ClassCatalog.Get(recruit.Class).Speed);
        if (candidate.Tactics == null)
            Register(candidate, new() { Build = CharacterBuild.Create(recruit.Class, seed), AutoEat = Tuning.EnableSurvival }, checked(Clock + recovery));
        else
        {
            // An already acting rescuer retains their build and previously paid
            // recovery, then pays the joining recovery as a new party member.
            long due = State.Queue.Snapshot().Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == candidate.Id).Due;
            candidate.Tactics.Resident = null;
            candidate.Tactics.AutoEat = Tuning.EnableSurvival;
            State.Queue.CancelActor(candidate.Id, ScheduledKind.ActorReady);
            State.Queue.Schedule(ScheduledKind.ActorReady, checked(due + recovery), candidate.Id, playerControlled: true);
        }
        _world.ShareRecruitSupplies(candidate);
        _world.FollowDungeonRecruit(candidate, actor);
        Emit("Recruited", actor.Id, candidate.Id, action, candidate.Position);
    }
}
