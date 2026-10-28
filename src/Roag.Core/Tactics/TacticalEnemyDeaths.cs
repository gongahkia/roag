using Roag.Core.Content;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    /// <summary>Runs in causal event order after the entire primary damage/removal batch.</summary>
    private void ResolveEnemyDeath(TacticalEvent death)
    {
        var parent = Actor(death.Target);
        if (_world.State.AwaitingContinuation
            || parent.Tactics is not { DefinitionId: "core:enemy/bone_splitter", RewardExempt: false }) return;
        int count = 0;
        foreach (var (dx, dy) in new[] { (0, 0), (0, -1), (1, 0), (0, 1), (-1, 0) })
        {
            var cell = death.Position.Offset(dx, dy);
            if (!_world.CanEnter(cell) || ProtectedForcedLanding(cell)) continue;
            var fragment = ActorFactory.Create(_world.Content, "character.roag-bone-fragment", _world.Seed,
                _world.State.NextActorId++, cell, parent.HomeTown, parent.Culture, parent.FactionId);
            var tactics = EnemyCatalog.Default.Get("core:enemy/bone_fragment").CreateTactics(rewardExempt: true);
            tactics.Enemy!.FragmentOf = parent.Id;
            _world.State.Actors.Add(fragment);
            Register(fragment, tactics, checked(Clock + 1));
            Emit("BoneFragmented", parent.Id, fragment.Id, death.Action, cell);
            if (++count == 2) break;
        }
    }
}
