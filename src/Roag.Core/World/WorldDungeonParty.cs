using Roag.Core.Expeditions;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    /// <summary>The inspected member; readiness remains owned by the shared tactical timeline.</summary>
    public Actor DungeonMember => State.Dungeon != null && State.Tactics is { } tactics
        ? Party.FirstOrDefault(a => a.Id == tactics.ControlledActor)
            ?? throw new InvalidOperationException("No dungeon party member is selected.")
        : throw new InvalidOperationException("No native dungeon run is loaded.");

    public ActionResult SelectDungeonMember(long memberId)
    {
        if (State.Dungeon == null || State.Tactics == null || State.AwaitingContinuation)
            return new(false, "This run has ended or no dungeon is loaded.");
        var member = Party.FirstOrDefault(a => a.Id == memberId);
        if (member is not { Life: not LifeState.Dead, Tactics.Build: not null })
            return new(false, "Choose a surviving party member.");
        State.Tactics.ControlledActor = member.Id;
        State.Position = member.Position;
        Chunks.Prefetch(member.Position);
        Reveal();
        return new(true, member.Name + " selected.");
    }

    private bool SelectedDungeonMemberReady => State.Tactics is { ActiveActor: > 0 } tactics
        && tactics.ControlledActor == tactics.ActiveActor;
}
