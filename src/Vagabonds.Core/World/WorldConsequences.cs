using System.Text.Json.Serialization;

namespace Vagabonds.Core.World;

public enum ConsequenceKind { SuppliedFood, InterruptedWork, AidInDanger, DamagedLivelihood }
public sealed class ConsequenceReceipt
{
    [JsonRequired] public long Other { get; set; }
    [JsonRequired] public long Tick { get; set; }
    [JsonRequired] public long After { get; set; }
    [JsonRequired] public string Event { get; set; } = "";
}

public sealed partial class WorldSession
{
    public const int ConsequenceDuration = TicksPerHour * 24, GrievanceTrust = -5;
    private static (string Key, int Mood, int Trust, string Reply, string Fact) ConsequenceRule(ConsequenceKind kind) => kind switch
    {
        ConsequenceKind.SuppliedFood => ("consequence.food", 6, 8, "You brought food when our supplies were short. I remember that.", "brought food when our supplies were short."),
        ConsequenceKind.InterruptedWork => ("consequence.interrupted", -4, -3, "You interrupted work I had already begun. Please give me time to finish what matters here.", "interrupted work I had already begun."),
        ConsequenceKind.AidInDanger => ("consequence.aid", 8, 12, "You helped drive danger away while I was taking cover. I will not forget your help.", "helped drive danger away while I was taking cover."),
        ConsequenceKind.DamagedLivelihood => ("consequence.livelihood", -10, -12, "You damaged the source or workplace I depended on. I need to put my livelihood back in order.", "damaged the source or workplace I depended on."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private Actor? ConsequenceLeader()
    { foreach (long id in State.Party) if (ActorById(id) is { Life: LifeState.Alive } actor) return actor; return null; }
    private bool ApplyConsequence(Actor actor, Actor? other, ConsequenceKind kind, string eventId)
    {
        if (actor.Kind != ActorKind.Person || actor.Life != LifeState.Alive || other == null || other.Kind != ActorKind.Person || actor.Id == other.Id) return false;
        if (actor.Consequences.TryGetValue(kind, out var previous) && (previous.After > State.Tick || previous.Event == eventId)) return false;
        var rule = ConsequenceRule(kind); string name = other.Name.Length <= 64 ? other.Name : other.Name[..64];
        if (!Remember(actor, rule.Key, name + " " + rule.Fact, rule.Mood, other.Id, ConsequenceDuration)) return false;
        actor.Consequences[kind] = new() { Other = other.Id, Tick = State.Tick, After = State.Tick + ConsequenceDuration, Event = eventId };
        ChangeTrust(actor, other, rule.Trust);
        Diagnose("social-consequence", new { actor = actor.Id, other = other.Id, kind = kind.ToString(), eventId, trust = actor.Trust.GetValueOrDefault(other.Id) });
        return true;
    }
    public string ConsequenceDialogue(Actor actor, Actor other)
    {
        ConsequenceKind? newest = null; long tick = -1;
        foreach (var (kind, receipt) in actor.Consequences)
            if (receipt.Other == other.Id && receipt.After > State.Tick && receipt.Tick >= tick) { newest = kind; tick = receipt.Tick; }
        return newest is { } found ? ConsequenceRule(found).Reply : "";
    }
    public string ResidentGrievance(Actor actor)
    {
        if (actor.Consequences.Count == 0) return "";
        var leader = ConsequenceLeader();
        if (actor.Recruited || leader == null) return "";
        return HasGrievanceToward(actor, leader.Id)
            ? actor.Name + " does not trust this request after recent harm or disrupted work. Rebuild trust or give the grievance time to fade." : "";
    }
    private bool HasGrievanceToward(Actor actor, long other)
    {
        if (actor.Consequences.Count == 0 || actor.Trust.GetValueOrDefault(other) > GrievanceTrust) return false;
        foreach (var (kind, receipt) in actor.Consequences)
            if (receipt.Other == other && receipt.After > State.Tick && kind is ConsequenceKind.DamagedLivelihood or ConsequenceKind.InterruptedWork) return true;
        return false;
    }
    private void NoteFoodSupply(int townId, string eventId, bool needed)
    {
        var leader = ConsequenceLeader();
        foreach (var actor in State.Actors)
            if (actor.Kind == ActorKind.Person && !actor.Recruited && actor.HomeTown == townId && (needed || actor.Body.Hunger >= Simulation.MealAt))
                ApplyConsequence(actor, leader, ConsequenceKind.SuppliedFood, eventId);
    }
    private void NoteInterruptedWork(Actor actor, string eventId)
    { if (!actor.Recruited) ApplyConsequence(actor, ConsequenceLeader(), ConsequenceKind.InterruptedWork, eventId); }
    private void CaptureLivelihoodStakeholders(WorkOrder order)
    {
        if (order.RequestedBy == 0 || order.Kind is not (WorkKind.Dig or WorkKind.Build or WorkKind.Harvest)) return;
        order.LivelihoodStakeholders = State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited && a.Life == LifeState.Alive
            && (a.OccupationWorkplace == order.Target || a.Occupation is { Phase: OccupationPhase.Working or OccupationPhase.Collecting } job && job.Target == order.Target))
            .OrderBy(a => a.Id).Take(16).Select(a => a.Id).ToArray();
    }
    private void NoteLivelihoodDamage(WorkOrder order)
    {
        if (order.RequestedBy == 0 || order.LivelihoodStakeholders.Length == 0 || order.Kind == WorkKind.Harvest && ResourceAt(order.Target)?.Available > 0) return;
        var requester = ActorById(order.RequestedBy);
        foreach (long id in order.LivelihoodStakeholders) ApplyConsequence(ActorById(id), requester, ConsequenceKind.DamagedLivelihood, "work:" + order.Id);
    }
    private void NoteDangerAid(WorldBattle battle)
    {
        var defeated = battle.Units.Where(u => !u.Friendly && !u.Alive).Select(u => battle.ActorIds[u.Id]).ToHashSet();
        var leader = ConsequenceLeader();
        foreach (var actor in State.Actors)
            if (!actor.Recruited && actor.Kind == ActorKind.Person && actor.Routine is { Kind: RoutineKind.Danger } routine
                && defeated.Contains(routine.Other) && actor.Position.Distance(State.Position) <= 100)
                ApplyConsequence(actor, leader, ConsequenceKind.AidInDanger, "danger:" + routine.Other);
    }
    private static void ValidateConsequences(WorldState state)
    {
        var people = state.Actors.Where(a => a.Kind == ActorKind.Person).Select(a => a.Id).ToHashSet();
        foreach (var actor in state.Actors)
        {
            if (actor.Consequences == null || actor.Consequences.Count > 4 || actor.Kind != ActorKind.Person && actor.Consequences.Count != 0
                || actor.Consequences.Any(p => !Enum.IsDefined(p.Key) || p.Value == null || !people.Contains(p.Value.Other) || p.Value.Other == actor.Id
                    || p.Value.Tick < 0 || p.Value.Tick > state.Tick || p.Value.After != p.Value.Tick + ConsequenceDuration
                    || string.IsNullOrWhiteSpace(p.Value.Event) || p.Value.Event.Length > 128)) throw new InvalidDataException("Invalid social consequence receipts.");
            foreach (var memory in actor.Memories.Where(m => m.Until > state.Tick))
                foreach (var kind in Enum.GetValues<ConsequenceKind>())
                {
                    var rule = ConsequenceRule(kind); if (memory.Key != rule.Key) continue;
                    if (!actor.Consequences.TryGetValue(kind, out var receipt) || memory.Other != receipt.Other || memory.Tick != receipt.Tick
                        || memory.Until != receipt.After || memory.Mood != rule.Mood) throw new InvalidDataException("Social consequence memory differs from its receipt.");
                }
        }
        foreach (var order in state.Work)
            if (order.RequestedBy != 0 && !people.Contains(order.RequestedBy) || order.LivelihoodStakeholders == null || order.LivelihoodStakeholders.Length > 16
                || order.LivelihoodStakeholders.Distinct().Count() != order.LivelihoodStakeholders.Length || order.LivelihoodStakeholders.Any(id => !people.Contains(id) || id == order.RequestedBy)
                || order.RequestedBy == 0 && order.LivelihoodStakeholders.Length > 0 || order.LivelihoodStakeholders.Length > 0 && order.Kind is not (WorkKind.Dig or WorkKind.Build or WorkKind.Harvest))
                throw new InvalidDataException("Invalid work requester or livelihood attribution.");
    }
}
