using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewRecruit(Actor actor, long targetId)
    {
        var recruit = _world.State.Dungeon?.Recruits.FirstOrDefault(r => r.Actor == targetId);
        var candidate = _world.State.Actors.FirstOrDefault(a => a.Id == targetId);
        if (!Friendly(actor) || recruit == null || candidate is not { Life: LifeState.Alive, Recruited: false, Tactics: null }
            || actor.Position.Distance(candidate.Position) != 1 || !_world.VisionFrom(actor.Position).Contains(candidate.Position))
            return Invalid("Stand beside an available adventurer to recruit them.");
        if (_world.Party.Count(a => a.Life != LifeState.Dead) >= 4)
            return Invalid("Four surviving members already fill the party.");
        return new(true, "", ActionTiming.Recovery(100, EffectiveSpeed(actor)), [candidate.Position]);
    }

    private void RecruitDungeonMember(Actor actor, long targetId, long action)
    {
        var recruit = _world.State.Dungeon!.Recruits.Single(r => r.Actor == targetId);
        var candidate = Actor(targetId);
        candidate.Recruited = true;
        _world.State.Party.Add(candidate.Id);
        int seed = RoagRandom.Derive(_world.DungeonSeed, "recruit-build:" + candidate.Id);
        // Joining pays its own full recovery too; recruitment never manufactures an immediate extra turn.
        Register(candidate, new() { Build = CharacterBuild.Create(recruit.Class, seed), AutoEat = false },
            checked(Clock + ActionTiming.Recovery(100, ClassCatalog.Get(recruit.Class).Speed)));
        Emit("Recruited", actor.Id, candidate.Id, action, candidate.Position);
    }
}
