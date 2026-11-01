using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record RescueTuning(int NoticeRange = 8, int DangerRange = 5, int ReactionCost = 100,
    int MedicineCarrierPercent = 35)
{
    public void Validate()
    {
        if (NoticeRange is < 1 or > 20 || DangerRange is < 1 or > 12 || ReactionCost <= 0 || MedicineCarrierPercent is < 0 or > 100)
            throw new InvalidDataException("Invalid native rescue tuning.");
    }
}

/// <summary>An existing person's paid physical rescue activity, not a spawned rescuer.</summary>
public sealed class ResidentRescue
{
    public WorldPosition Home { get; set; }
    public long Target { get; set; }
    public WorldPosition LastKnown { get; set; }
}

public sealed partial class TacticalCombat
{
    private bool CanRescue(Actor actor) => Friendly(actor)
        || actor is { Kind: ActorKind.Person, Recruited: false, Tactics.Resident: not null };
    private int RescueMedicine(Actor actor) => Friendly(actor) ? _world.Count("medicine") : actor.Cargo.GetValueOrDefault("medicine");

    private bool ResidentSees(Actor resident, Actor target) => resident.Position.Distance(target.Position) <= State.Rescue.NoticeRange
        && _world.VisionFrom(resident.Position).Contains(target.Position);

    private void WakeResidentRescuers(Actor casualty)
    {
        foreach (var resident in _world.State.Actors.Where(a => a is { Kind: ActorKind.Person, Life: LifeState.Alive, Recruited: false }
                     && (a.Tactics == null || a.Tactics.Resident != null)).OrderBy(a => a.Id))
        {
            if (resident.Cargo.GetValueOrDefault("medicine") < 1 || !ResidentSees(resident, casualty)
                || !_world.WillingToRescue(resident, casualty, State.Rescue.DangerRange)) continue;
            if (resident.Tactics == null)
            {
                var recruit = _world.State.Dungeon?.Recruits.FirstOrDefault(r => r.Actor == resident.Id);
                var kind = recruit?.Class ?? resident.Role switch
                { 0 => ClassKind.Sentinel, 1 => ClassKind.Ranger, 2 => ClassKind.Arcanist, _ => ClassKind.Spiritbinder };
                int seed = recruit == null ? RoagRandom.Derive(_world.Seed, "resident-build:" + resident.Id)
                    : RoagRandom.Derive(_world.DungeonSeed, "recruit-build:" + resident.Id);
                resident.CombatTrait = ""; resident.Talents.Clear(); resident.SpriteId = NativeAppearance.ForClass(kind);
                Register(resident, new() { Build = CharacterBuild.Create(kind, seed), AutoEat = false,
                    Resident = new() { Home = resident.Position, Target = casualty.Id, LastKnown = casualty.Position } });
                var readiness = State.Queue.Snapshot().Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == resident.Id);
                State.Queue.Delay(readiness.Sequence, ActionTiming.Recovery(State.Rescue.ReactionCost, EffectiveSpeed(resident)));
            }
            else
            {
                resident.Tactics.Resident!.Target = casualty.Id;
                resident.Tactics.Resident.LastKnown = casualty.Position;
            }
            Emit("RescueNoticed", resident.Id, casualty.Id, 0, resident.Position);
        }
    }

    private void CommitResidentRescueTurn(Actor resident)
    {
        var memory = resident.Tactics!.Resident!;
        var detected = _world.Party.Where(a => a.Life == LifeState.Downed && RescueMedicine(resident) > 0
                && ResidentSees(resident, a) && _world.WillingToRescue(resident, a, State.Rescue.DangerRange))
            .OrderBy(a => a.Tactics!.BleedTurns).ThenBy(a => a.Position.Distance(resident.Position)).ThenBy(a => a.Id).FirstOrDefault();
        if (detected != null) { memory.Target = detected.Id; memory.LastKnown = detected.Position; }
        if (memory.Target > 0)
        {
            var target = Actor(memory.Target);
            bool seen = ResidentSees(resident, target);
            if (RescueMedicine(resident) < 1 || !_world.WillingToRescue(resident, target, State.Rescue.DangerRange)
                || seen && target.Life != LifeState.Downed
                || !seen && resident.Position.Distance(memory.LastKnown) <= 1)
                memory.Target = 0;
            else
            {
                if (seen) memory.LastKnown = target.Position;
                if (seen && resident.Position.Distance(target.Position) == 1)
                {
                    var result = Commit(new(TacticalAction.Revive, resident.Tactics.Facing, target.Id));
                    if (!result.Success) throw new InvalidOperationException("NPC rescue failed its own preview: " + result.Message);
                    memory.Target = 0;
                    return;
                }
            }
        }
        WorldPosition destination = memory.Target > 0 ? memory.LastKnown : memory.Home;
        var goals = new HashSet<WorldPosition>();
        if (memory.Target == 0) goals.Add(destination);
        else foreach (var facing in new[] { Facing.North, Facing.East, Facing.South, Facing.West })
        { var (dx, dy) = AttackGeometry.Offset(facing); goals.Add(destination.Offset(dx, dy)); }
        var step = _world.Navigate(resident, goals, p => _world.State.Dungeon?.Contains(p) ?? _world.Plan.Contains(p));
        var intent = step == null ? new TacticalIntent(TacticalAction.Wait, resident.Tactics.Facing) : new(step.Action, step.Facing);
        var committed = Commit(intent);
        if (!committed.Success) throw new InvalidOperationException("NPC rescue movement failed its own navigation: " + committed.Message);
    }
}
