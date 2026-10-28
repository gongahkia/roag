using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>Eligibility and claim only; the dead world actor owns location, definition and maximum HP.</summary>
public sealed record RevivalCorpse(long Actor, long ClaimedBy = 0);
/// <summary>Transient read-only planning facts derived from the world actor.</summary>
public sealed record RevivalCandidate(long Actor, WorldPosition Position, long ClaimedBy = 0);

internal static class EnemyRevivalRules
{
    public const int Range = 4;
    public const int FloorCap = 16;
    public const int TotalCap = 32;

    public static bool Revivable(string definition)
    {
        if (!EnemyCatalog.Default.Contains(definition)) return false;
        return EnemyCatalog.Default.Get(definition).Ai is EnemyBrainKind.Guard or EnemyBrainKind.Spitter or EnemyBrainKind.Skitter
            or EnemyBrainKind.SporeHerald or EnemyBrainKind.CanalLancer or EnemyBrainKind.CinderBellkeeper or EnemyBrainKind.ExpansionRegular
            || definition is "core:enemy/heavy_rammer" or "core:enemy/hollow_wraith" or "core:enemy/gulp_toad";
    }

    public static bool Eligible(Actor actor) => actor is { Life: LifeState.Dead, Tactics: { Enemy: not null, RewardExempt: false } }
        && Revivable(actor.Tactics.DefinitionId);

    public static void Validate(TacticalWorldState state, IReadOnlyDictionary<long, Actor> actors)
    {
        if (state.Corpses == null || state.Corpses.Count > TotalCap || state.Corpses.Any(c => c == null)
            || state.Corpses.Select(c => c.Actor).Distinct().Count() != state.Corpses.Count)
            throw new InvalidDataException("Invalid revival corpse registry.");
        var floors = new Dictionary<int, int>();
        foreach (var corpse in state.Corpses)
        {
            if (!actors.TryGetValue(corpse.Actor, out var original) || !Eligible(original) || corpse.ClaimedBy < 0)
                throw new InvalidDataException("Invalid revival corpse identity.");
            int floor = original.Position.Floor;
            floors[floor] = floors.GetValueOrDefault(floor) + 1;
            if (floors[floor] > FloorCap) throw new InvalidDataException("Too many revival corpses on one floor.");
            if (corpse.ClaimedBy == 0) continue;
            if (!actors.TryGetValue(corpse.ClaimedBy, out var cantor)
                || cantor is not { Life: LifeState.Alive, Tactics: { DefinitionId: "core:enemy/grave_cantor", Enemy.RevivalSpent: true } }
                || cantor.Position.Floor != floor || !state.Attacks.Any(a => a.Source == cantor.Id
                    && a.Support is { Kind: EnemySupportKind.Revive } s && s.Corpse == corpse.Actor
                    && a.Cells is { Length: 1 } && a.Cells[0] == original.Position))
                throw new InvalidDataException("Invalid committed corpse claim.");
        }
        foreach (var attack in state.Attacks)
            if (attack?.Support is { Kind: EnemySupportKind.Revive } support
                && !state.Corpses.Any(c => c.Actor == support.Corpse && c.ClaimedBy == attack.Source))
                throw new InvalidDataException("Revival attack has no matching claim.");
        var usedCorpses = new HashSet<long>(); var usedCasters = new HashSet<long>();
        foreach (var actor in actors.Values)
            if (actor.Tactics?.Enemy is { RevivalOf: > 0 } memory)
            {
                if (memory.RevivalOf >= actor.Id || memory.RevivedBy >= actor.Id
                    || !actors.TryGetValue(memory.RevivalOf, out var original) || !Eligible(original)
                    || original.Tactics!.DefinitionId != actor.Tactics.DefinitionId
                    || actor.MaxHealth != (int)(((long)original.MaxHealth + 1) / 2)
                    || !actors.TryGetValue(memory.RevivedBy, out var caster)
                    || caster.Tactics is not { DefinitionId: "core:enemy/grave_cantor", Enemy.RevivalSpent: true }
                    || !usedCorpses.Add(memory.RevivalOf) || !usedCasters.Add(memory.RevivedBy)
                    || state.Corpses.Any(c => c.Actor == memory.RevivalOf))
                    throw new InvalidDataException("Invalid revived actor ancestry.");
            }
    }
}

public sealed partial class TacticalCombat
{
    private void RememberRevivalCorpse(Actor actor)
    {
        if (!EnemyRevivalRules.Eligible(actor) || State.Corpses.Count >= EnemyRevivalRules.TotalCap
            || State.Corpses.Count(c => Actor(c.Actor).Position.Floor == actor.Position.Floor) >= EnemyRevivalRules.FloorCap
            || !_world.State.Actors.Any(a => a is { Life: LifeState.Alive, Health: > 0,
                Tactics: { DefinitionId: "core:enemy/grave_cantor", Enemy.RevivalSpent: false } } && a.Position.Floor == actor.Position.Floor)) return;
        State.Corpses.Add(new(actor.Id));
    }

    private void PruneRevivalCorpses()
    {
        State.Corpses.RemoveAll(c => !_world.State.Actors.Any(a => a is { Life: LifeState.Alive, Health: > 0,
            Tactics: { DefinitionId: "core:enemy/grave_cantor", Enemy: { } memory } }
            && a.Position.Floor == Actor(c.Actor).Position.Floor
            && (!memory.RevivalSpent || State.Attacks.Any(p => p.Source == a.Id && p.Support?.Kind == EnemySupportKind.Revive && p.Support.Corpse == c.Actor))));
        // A canceled attempt cannot leave another unspent Cantor looking at a claimed marker.
        State.Corpses.RemoveAll(c => c.ClaimedBy != 0 && !State.Attacks.Any(a => a.Source == c.ClaimedBy
            && a.Support?.Kind == EnemySupportKind.Revive && a.Support.Corpse == c.Actor));
    }

    private void ValidateRevivalCommit(Actor owner, long corpseId, WorldPosition cell)
    {
        if (owner.Tactics?.Enemy is not { RevivalSpent: false }
            || !State.Corpses.Any(c => c.Actor == corpseId && c.ClaimedBy == 0) || Actor(corpseId).Position != cell)
            throw new ArgumentException("Revival attempt is spent or its corpse is unavailable.");
    }

    private void ClaimRevival(Actor owner, long corpseId)
    {
        int index = State.Corpses.FindIndex(c => c.Actor == corpseId);
        State.Corpses[index] = State.Corpses[index] with { ClaimedBy = owner.Id };
        owner.Tactics!.Enemy!.RevivalSpent = true;
    }

    private void ResolveRevival(Actor owner, long corpseId, WorldPosition cell, long action)
    {
        var corpse = State.Corpses.FirstOrDefault(c => c.Actor == corpseId);
        var original = Actor(corpseId);
        if (corpse?.ClaimedBy != owner.Id || original.Position != cell || !_world.CanEnter(cell) || ProtectedForcedLanding(cell))
            Emit("RevivalBlocked", owner.Id, 0, action, cell);
        else
        {
            // Retain the original actor's approved atlas appearance without copying its ecology,
            // campaign role, loot receipt, status effects or spent ability memories.
            var revived = ActorFactory.Create(_world.Content, "character.roag-revived", _world.Seed,
                _world.State.NextActorId++, cell, original.HomeTown, original.Culture, original.FactionId);
            revived.Name = original.Name; revived.SpriteId = original.SpriteId;
            var tactics = EnemyCatalog.Default.Get(original.Tactics!.DefinitionId).CreateTactics(rewardExempt: true);
            tactics.InnateStats = tactics.InnateStats! with { MaxHealth = (int)(((long)original.MaxHealth + 1) / 2) };
            tactics.Enemy!.RevivalOf = original.Id; tactics.Enemy.RevivedBy = owner.Id;
            _world.State.Actors.Add(revived); Register(revived, tactics, checked(Clock + 1));
            Emit("EnemyRevived", owner.Id, revived.Id, action, cell, ability: tactics.DefinitionId);
        }
        State.Corpses.RemoveAll(c => c.Actor == corpseId);
    }
}
