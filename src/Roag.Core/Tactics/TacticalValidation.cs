using Roag.Core.World;
using Roag.Core.Content;

namespace Roag.Core.Tactics;

public static class TacticalValidation
{
    public static void Validate(WorldPlan plan, WorldState world, ContentCatalog? content = null)
    {
        foreach (var actor in world.Actors) actor.Tactics?.Validate();
        if (world.Tactics is not { } state)
        {
            if (world.Actors.Any(a => a.Tactics != null)) throw new InvalidDataException("Tactical actors lack a world timeline.");
            return;
        }
        if (state.NextAction < 1 || state.NextEvent < 1 || state.RandomState <= 0 || state.RandomState >= RoagRandom.Modulus
            || state.Participants is null || state.Attacks is null || state.Events is null || state.Events.Count > 512)
            throw new InvalidDataException("Invalid tactical world header.");
        var queue = ActionTimeline.Restore(state.Timeline).Snapshot();
        if (state.Tuning is null) throw new InvalidDataException("Missing tactical tuning.");
        try { state.Tuning.Validate(); }
        catch (ArgumentException e) { throw new InvalidDataException("Invalid saved tactical tuning.", e); }
        if (state.DrivesWorldClock && world.Tick != queue.Clock / state.Tuning.UnitsPerWorldTick)
            throw new InvalidDataException("Tactical and world clocks disagree.");
        var actors = world.Actors.ToDictionary(a => a.Id);
        EnemyRevivalRules.Validate(state, actors);
        var fragments = new Dictionary<long, int>();
        foreach (var actor in world.Actors)
            if (actor.Tactics is { DefinitionId: "core:enemy/bone_fragment" } fragment)
            {
                long parentId = fragment.Enemy?.FragmentOf ?? 0;
                if (parentId <= 0 || parentId >= actor.Id || !fragment.RewardExempt
                    || !actors.TryGetValue(parentId, out var parent)
                    || parent is not { Life: LifeState.Dead, Tactics: { DefinitionId: "core:enemy/bone_splitter", RewardExempt: false } }
                    || fragments.GetValueOrDefault(parentId) >= 2)
                    throw new InvalidDataException("Invalid fragment ancestry or population.");
                fragments[parentId] = fragments.GetValueOrDefault(parentId) + 1;
            }
        foreach (var actor in world.Actors)
            if (actor.Tactics?.Enemy is { SummonedBy: > 0 } summoned
                && (summoned.SummonedBy == actor.Id || !actors.TryGetValue(summoned.SummonedBy, out var summoner) || summoner.Tactics?.Enemy == null))
                throw new InvalidDataException("Missing native summoner identity.");
        foreach (var actor in world.Actors)
            if (actor.Tactics?.Enemy?.Squad is { } squad)
            {
                if (!plan.Contains(squad.Home) || !actors.TryGetValue(squad.Leader, out var leader)
                    || leader.Tactics?.DefinitionId != EnemySquads.Roster(squad.Kind).Leader
                    || leader.Tactics.Enemy?.Squad != squad)
                    throw new InvalidDataException("Missing or inconsistent squad leader.");
            }
        if (state.EnemySnares is null || state.EnemySnares.Any(s => s == null || !plan.Contains(s.Position)
            || !actors.TryGetValue(s.Source, out var owner) || owner.Life != LifeState.Alive
            || owner.Tactics?.DefinitionId != "core:enemy/snare_scribe")
            || state.EnemySnares.Select(s => s.Position).Distinct().Count() != state.EnemySnares.Count
            || state.EnemySnares.GroupBy(s => s.Source).Any(g => g.Count() > EnemySquads.SnareCap))
            throw new InvalidDataException("Invalid owned enemy traps.");
        if (state.Rewards is null) throw new InvalidDataException("Missing tactical rewards.");
        state.Rewards.Validate(actors);
        if (state.Loot is null) throw new InvalidDataException("Missing physical loot.");
        state.Loot.Validate(plan, actors);
        ValidateBoonWork(plan, state, actors, queue);
        ChunkGenerator? rooms = null;
        if (state.ControlledActor != 0 && (!world.Party.Contains(state.ControlledActor)
            || !state.Participants.Contains(state.ControlledActor)))
            throw new InvalidDataException("Invalid controlled party member.");
        foreach (long id in state.Participants)
            if (!actors.TryGetValue(id, out var actor) || actor.Tactics is null)
                throw new InvalidDataException("Missing tactical participant.");
        if (world.Actors.Any(a => a.Tactics != null && !state.Participants.Contains(a.Id)))
            throw new InvalidDataException("Unregistered tactical actor.");
        if (state.ActiveActor != 0 && (!state.Participants.Contains(state.ActiveActor)
            || actors[state.ActiveActor].Life != LifeState.Alive))
            throw new InvalidDataException("Invalid active tactical actor.");
        foreach (long id in state.Participants)
        {
            var actor = actors[id];
            actor.Tactics!.Boons.Validate(actor.Tactics.Build?.Class, state.NextAction);
            var resources = actor.Tactics!.Resources;
            if (actor.Tactics.SnaredBy != 0 && !actors.ContainsKey(actor.Tactics.SnaredBy)
                || resources.StormLastAction >= state.NextAction
                || new[] { resources.Decoy, resources.Snare, resources.Spirit, resources.Sentry?.Position,
                    resources.Parry?.Origin, resources.Bulwark?.Position, resources.Beacon?.Position }
                    .Any(p => p is { } cell && !plan.Contains(cell)))
                throw new InvalidDataException("Invalid owned class resource.");
            if (resources.Beacon is { } beacon && ((rooms ??= new(plan, content)).RoomAt(beacon.Position) != beacon.Room
                || !world.Chunks.Any(c => c.Key == beacon.Position.Chunk && c.Discovered.Contains(beacon.Position.LocalIndex))))
                throw new InvalidDataException("Invalid or unexplored return beacon room.");
            if (resources.Bulwark is { } block && (block.Action >= state.NextAction || block.Expires < queue.Clock
                || block.Expires - queue.Clock > 300
                || queue.Pending.Count(e => MatchesBulwark(e, block, id)) != 1
                || world.Actors.Count(a => a.Tactics?.Resources.Bulwark?.Position == block.Position) != 1))
                throw new InvalidDataException("Invalid owned bulwark or missing expiry.");
            if (resources.Sentry is { } sentry)
            {
                if (sentry.Action >= state.NextAction || sentry.Phase != SentryPhase.Spent && (sentry.Due < queue.Clock
                    || queue.Pending.Count(e => e.Kind == ScheduledKind.Effect && e.ActorId == id
                        && e.ReferenceId == sentry.Action && e.Sequence == sentry.Schedule && e.Due == sentry.Due) != 1))
                    throw new InvalidDataException("Missing sentry work.");
                for (int i = 0; i < sentry.Cells.Length; i++)
                {
                    var first = sentry.Cells[0];
                    int dx = first.X - sentry.Position.X, dy = first.Y - sentry.Position.Y;
                    if (Math.Abs(dx) + Math.Abs(dy) != 1 || sentry.Cells[i] != sentry.Position.Offset(dx * (i + 1), dy * (i + 1))
                        || !plan.Contains(sentry.Cells[i])) throw new InvalidDataException("Invalid locked sentry lane.");
                }
            }
            int readiness = queue.Pending.Count(e => e.Kind == ScheduledKind.ActorReady && e.ActorId == id);
            int windingUp = state.Attacks.Count(a => a != null && a.Source == id && a.RecoveryCost != null);
            if (readiness != (actor.Life == LifeState.Dead || id == state.ActiveActor || windingUp > 0 ? 0 : 1)
                || windingUp > 1 || windingUp > 0 && id == state.ActiveActor
                || actor.Health < 0 || actor.Health > actor.MaxHealth
                || (actor.Life == LifeState.Alive) != (actor.Health > 0)
                || (actor.Life == LifeState.Downed) != (actor.Tactics!.BleedTurns > 0)
                || actor.Life != LifeState.Alive && actor.Tactics!.Guarding
                || id == state.ActiveActor && actor.Tactics!.StunnedUntil > queue.Clock
                || queue.Pending.Any(e => e.Kind == ScheduledKind.ActorReady && e.ActorId == id && e.Due < actor.Tactics!.StunnedUntil))
                throw new InvalidDataException("Actor life/recovery state is inconsistent.");
        }
        if (state.Attacks.Any(a => a is null) || state.Attacks.Select(a => a.Id).Distinct().Count() != state.Attacks.Count)
            throw new InvalidDataException("Duplicate tactical attack identities.");
        foreach (var attack in state.Attacks)
        {
            EnemyAttackRules.Validate(attack.Support, attack.Advance, attack.Shove);
            if (!EnemyAttackRules.SpecialIdentityValid(actors.GetValueOrDefault(attack.Source)?.Tactics?.DefinitionId ?? "",
                attack.Ability, attack.Origin, attack.Cells, attack.Support, attack.Shove))
                throw new InvalidDataException("Special enemy attack identity or geometry is inconsistent.");
            if (attack.Shove is { } shove && (attack.Cells is null || !attack.Cells.Contains(shove.Cell)))
                throw new InvalidDataException("Shove lacks a locked cell.");
            if (attack.Support is { Target: > 0 } support && !actors.ContainsKey(support.Target)
                || attack.Support is { Drop: > 0 } cargo && cargo.Drop >= state.Loot.NextDrop
                || attack.Support?.Kind == EnemySupportKind.Ward && actors.GetValueOrDefault(attack.Source)?.Tactics?.DefinitionId != "core:enemy/ward_weaver"
                || attack.Support?.Kind == EnemySupportKind.Snare && actors.GetValueOrDefault(attack.Source)?.Tactics?.DefinitionId != "core:enemy/snare_scribe"
                || attack.Support?.Kind == EnemySupportKind.Phase && (actors.GetValueOrDefault(attack.Source)?.Tactics?.DefinitionId != "core:enemy/hollow_wraith"
                    || attack.Cells == null || !EnemyAttackRules.PhaseGeometry(attack.Origin, attack.Cells)))
                throw new InvalidDataException("Invalid support ownership or healing target.");
            if ((attack.Support != null || attack.Advance != null || attack.Shove != null) && attack.RecoveryCost == null)
                throw new InvalidDataException("Enemy effects lack their windup recovery.");
            if (attack.RecoveryCost is <= 0
                || attack.RecoveryCost != null && (attack.ClassGenerated || !actors.TryGetValue(attack.Source, out var recovering) || recovering.Tactics?.Enemy == null)
                || actors.TryGetValue(attack.Source, out var enemy) && enemy.Tactics?.Enemy != null && !attack.ClassGenerated && attack.RecoveryCost == null)
                throw new InvalidDataException("Invalid enemy windup recovery contract.");
            if (attack.IntendedTarget < 0 || attack.IntendedTarget != 0 && !actors.ContainsKey(attack.IntendedTarget))
                throw new InvalidDataException("Unknown intended enemy target.");
            if (attack.Causality is { } cause)
            {
                if (cause.ChainId <= 0 || cause.ChainId >= state.NextAction || cause.RootActionId <= 0 || cause.RootActionId >= state.NextAction
                    || !double.IsFinite(cause.ProcCoefficient) || cause.ProcCoefficient is < 0 or > 10
                    || cause.ClassGenerated != attack.ClassGenerated || cause.Ancestry.IsDefault
                    || cause.Ancestry.Any(k => k is null || !actors.ContainsKey(k.Owner) || !BoonCatalog.Default.Contains(k.Boon))
                    || cause.Ancestry.Distinct().Count() != cause.Ancestry.Length)
                    throw new InvalidDataException("Invalid pending attack causality.");
            }
            if (attack.Id < 1 || attack.Id >= state.NextAction || !state.Participants.Contains(attack.Source)
                || !attack.ClassGenerated && attack.Causality is null
                || !attack.ClassGenerated && actors[attack.Source].Life != LifeState.Alive || attack.Due < queue.Clock
                || attack.Damage < 0 || attack.IgnoresTerrain || attack.FateFace != null || string.IsNullOrWhiteSpace(attack.Ability) || !plan.Contains(attack.Origin)
                || attack.Cells is null || attack.Cells.Length == 0 && (attack.RecoveryCost == null || attack.Delivery != AttackDelivery.Area)
                || attack.Cells.Distinct().Count() != attack.Cells.Length
                || attack.Cells.Any(c => !plan.Contains(c) || c.Floor != attack.Origin.Floor)
                || !Enum.IsDefined(attack.Delivery) || !Enum.IsDefined(attack.Element) || !Enum.IsDefined(attack.Cause)
                || queue.Pending.Count(e => e.Kind == ScheduledKind.Impact && e.ActorId == attack.Source
                    && e.ReferenceId == attack.Id && e.Due == attack.Due) != 1)
                throw new InvalidDataException("Invalid pending tactical attack.");
            if (attack.ClassGenerated && (actors[attack.Source].Tactics!.Build?.Class != ClassKind.Stormcaller
                || attack.Ability != ClassCatalog.Get(ClassKind.Stormcaller).SpecialAbilityId
                || attack.Delivery != AttackDelivery.Area || attack.Element != DamageElement.Lightning
                || attack.Critical || attack.HitsOwner || attack.PersistsWithoutOwner
                || StateAttackCount(attack.Source) != 1))
                throw new InvalidDataException("Invalid pending class discharge.");
        }
        foreach (var work in queue.Pending)
        {
            if (!state.Participants.Contains(work.ActorId)
                || work.Kind == ScheduledKind.ActorReady && work.PlayerControlled != world.Party.Contains(work.ActorId)
                || work.Kind == ScheduledKind.Impact && !state.Attacks.Any(a => a.Id == work.ReferenceId)
                || work.Kind == ScheduledKind.Effect && !MatchesOwnedEffect(work, actors[work.ActorId].Tactics!.Resources)
                    && !MatchesBoonWork(work, state, actors)
                || work.Kind is not (ScheduledKind.ActorReady or ScheduledKind.Impact or ScheduledKind.Effect))
                throw new InvalidDataException("Orphaned tactical work.");
        }
        long previous = 0;
        foreach (var entry in state.Events)
        {
            if (entry?.Protection is { } protection && (!actors.ContainsKey(protection.Source)
                || protection.Reason is not ("forcefield" or "directional shield")))
                throw new InvalidDataException("Invalid squad protection event.");
            if (entry?.Loot is { } loot && (loot.Drop <= 0 || loot.Drop >= state.Loot.NextDrop
                || !Enum.IsDefined(loot.Kind) || loot.Amount <= 0 || loot.ActualGain < 0 || loot.ActualGain > loot.Amount || loot.Balance < 0))
                throw new InvalidDataException("Invalid loot event.");
            if (entry is null || entry.Sequence <= previous || entry.Sequence >= state.NextEvent
                || entry.Time < 0 || entry.Time > queue.Clock || entry.Action < 0 || entry.Action >= state.NextAction
                || entry.Source != 0 && !actors.ContainsKey(entry.Source) || entry.Target != 0 && !actors.ContainsKey(entry.Target)
                || !plan.Contains(entry.Position) || entry.From is { } from && (!plan.Contains(from) || from.Floor != entry.Position.Floor)
                || entry.Forced && entry.From == null || entry.Amount < 0 || entry.Prevented < 0 || string.IsNullOrWhiteSpace(entry.Kind))
                throw new InvalidDataException("Invalid tactical event.");
            previous = entry.Sequence;
        }
        int StateAttackCount(long owner) => state.Attacks.Count(a => a.Source == owner && a.ClassGenerated);
    }

    private static bool MatchesBulwark(ScheduledAction work, BulwarkState block, long owner) =>
        work.Kind == ScheduledKind.Effect && work.ActorId == owner && work.ReferenceId == block.Action
        && work.Sequence == block.Schedule && work.Due == block.Expires;

    private static bool MatchesOwnedEffect(ScheduledAction work, ClassResources resources) =>
        resources.Bulwark is { } block && MatchesBulwark(work, block, work.ActorId)
        || resources.Sentry is { Phase: not SentryPhase.Spent } sentry && sentry.Schedule == work.Sequence
            && sentry.Action == work.ReferenceId && sentry.Due == work.Due;

    private static bool MatchesBoonWork(ScheduledAction work, TacticalWorldState state, IReadOnlyDictionary<long, Actor> actors) =>
        state.BoonEffects.Echoes.TryGetValue(work.Sequence, out var echo)
            && echo is { Activation: not null } && echo.Schedule == work.Sequence && echo.Due == work.Due && echo.Activation.OwnerId == work.ActorId
            && (echo.Activation.RootActionId ?? 0) == work.ReferenceId
        || actors.TryGetValue(work.ActorId, out var actor) && actor.Tactics?.Burning is { } burning && burning.Schedule == work.Sequence
            && burning.Due == work.Due && work.ReferenceId == 0;

    private static void ValidateBoonWork(WorldPlan plan, TacticalWorldState state, IReadOnlyDictionary<long, Actor> actors, TimelineSnapshot queue)
    {
        if (state.BoonEffects is null || state.BoonEffects.Dispatch is null || state.BoonEffects.Echoes is null)
            throw new InvalidDataException("Missing world boon state.");
        state.BoonEffects.Dispatch.Validate();
        foreach (var (chain, history) in state.BoonEffects.Dispatch.Chains)
        {
            if (chain >= state.NextAction
                || history.Damages.Any(d => !actors.ContainsKey(d.Target) || !plan.Contains(d.Position))
                || history.Pairs.Any(p => !actors.ContainsKey(p.Owner) || !actors.ContainsKey(p.First) || !actors.ContainsKey(p.Second))
                || history.Actions.Concat(history.Windups).Any(a => !actors.ContainsKey(a.Owner) || a.Sequence >= state.NextAction)
                || history.Groups.Any(g => !actors.ContainsKey(g.Owner) || g.Seen.Any(v => v.Target is { } target && !actors.ContainsKey(target))))
                throw new InvalidDataException("Invalid world boon history references.");
        }
        void Activation(BoonActivation a)
        {
            a.Validate(state.BoonEffects.Dispatch.NextActivationId);
            var e = a.Event;
            bool ActorExists(long? id) => id is null or 0 || actors.ContainsKey(id.Value);
            bool Inside(WorldPosition? p) => p is null || plan.Contains(p.Value);
            if (!state.Participants.Contains(a.OwnerId) || !ActorExists(a.PairTargetId) || !plan.Contains(a.SourcePosition)
                || a.ChainId >= state.NextAction || a.RootActionId >= state.NextAction || e.Id >= state.NextEvent || !ActorExists(e.SourceId) || !ActorExists(e.TargetId)
                || a.Ancestry.Any(k => !actors.ContainsKey(k.Owner)) || !Inside(e.Position) || !Inside(e.From) || !Inside(e.SourcePosition) || !Inside(e.PatternOrigin)
                || e.Path.Any(p => !plan.Contains(p)) || e.TargetCells.Any(p => !plan.Contains(p)))
                throw new InvalidDataException("Invalid world boon activation references.");
        }
        var scheduled = new HashSet<long>();
        foreach (var (sequence, echo) in state.BoonEffects.Echoes)
        {
            if (echo is null || echo.Activation is null || sequence != echo.Schedule || !scheduled.Add(sequence)
                || echo.Due < queue.Clock || !Enum.IsDefined(echo.Element) || !double.IsFinite(echo.ProcCoefficient) || echo.ProcCoefficient is < 0 or > 10
                || queue.Pending.Count(w => w.Kind == ScheduledKind.Effect && w.Sequence == sequence && MatchesBoonWork(w, state, actors)) != 1)
                throw new InvalidDataException("Invalid or unscheduled boon echo.");
            Activation(echo.Activation);
        }
        foreach (var actor in actors.Values)
        {
            if (actor.Tactics?.Burning is not { } burn) continue;
            if (actor.Life == LifeState.Dead || !scheduled.Add(burn.Schedule) || burn.Due < queue.Clock
                || queue.Pending.Count(w => w.Kind == ScheduledKind.Effect && w.ActorId == actor.Id && w.Sequence == burn.Schedule
                    && w.Due == burn.Due && w.ReferenceId == 0) != 1)
                throw new InvalidDataException("Invalid or unscheduled burning status.");
            Activation(burn.Activation);
        }
    }
}
