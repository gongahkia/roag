using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Core.Dungeons;

public enum SpecialDungeonAftermath { LethalCollapse, PartialCollapse }

/// <summary>Explicitly activated extraction pressure on one persistent physical complex.</summary>
public sealed class SpecialDungeonState
{
    public SpecialDungeonAftermath Aftermath { get; set; }
    public SpecialDungeonTuning Tuning { get; set; } = new();
    public long? ActivatedAt { get; set; }
    public long ExpiresAt { get; set; }
    public long ExpiredAt { get; set; }
    public long NextPressureAt { get; set; }
    public long PressureSchedule { get; set; }
    public long PursuerAt { get; set; }
    public long PursuerSchedule { get; set; }
    public long DeadlineSchedule { get; set; }
    public long Pursuer { get; set; }
    public bool PursuerFailed { get; set; }
    public int RandomState { get; set; } = 1;
    public List<long> Reinforcements { get; set; } = [];
    public List<WorldPosition> Rubble { get; set; } = [];
    public List<long> Casualties { get; set; } = [];
}

/// <summary>Original pressure cadence and distances; a delegated world-expedition deadline.</summary>
public sealed record SpecialDungeonTuning(int DurationSeconds = 1800, int FirstPressure = 1200,
    int PursuerDelay = 1500, int FirstPeriod = 1200, int SecondPeriod = 900, int ThirdPeriod = 650,
    int ReinforcementLimit = 18, int ActiveLimit = 18, int SafeRadius = 8, int PursuerDistance = 14)
{
    internal void Validate()
    {
        if (DurationSeconds is < 1 or > 7200 || FirstPressure is < 1 or > 120000 || PursuerDelay is < 1 or > 120000
            || FirstPeriod is < 1 or > 120000 || SecondPeriod is < 1 or > 120000 || ThirdPeriod is < 1 or > 120000
            || ReinforcementLimit is < 0 or > 18 || ActiveLimit is < 1 or > 18 || SafeRadius is < 1 or > 30 || PursuerDistance is < 1 or > 60)
            throw new InvalidDataException("Invalid special-dungeon pressure tuning.");
    }
}

public static class SpecialDungeonRules
{
    public static EnemyPursuitBoundary Boundary(DungeonComplexState complex) => new(complex.Parcel.X, complex.Parcel.Y,
        complex.Parcel.X + complex.Parcel.Width - 1, complex.Parcel.Y + complex.Parcel.Height - 1, 1, 7);

    internal static void Validate(WorldPlan plan, WorldState world)
    {
        var queue = world.Tactics?.Queue.Snapshot();
        if (queue == null) return;
        var expected = new HashSet<long>();
        foreach (var complex in world.NativeCampaign?.Complexes ?? [])
        {
            if (complex.Special is not { } special) continue;
            special.Tuning?.Validate();
            if (!complex.Populated || !Enum.IsDefined(special.Aftermath) || special.Tuning == null
                || special.RandomState is < 1 or >= RoagRandom.Modulus || special.Reinforcements == null
                || special.Reinforcements.Count > special.Tuning.ReinforcementLimit || special.Reinforcements.Distinct().Count() != special.Reinforcements.Count
                || special.Rubble == null || special.Rubble.Count > 12 || special.Rubble.Distinct().Count() != special.Rubble.Count
                || special.Rubble.Any(p => !complex.Contains(p)) || special.Casualties == null
                || special.Casualties.Distinct().Count() != special.Casualties.Count || special.Pursuer < 0
                || special.ExpiredAt < 0 || special.ExpiresAt < 0 || special.NextPressureAt < 0 || special.PursuerAt < 0
                || special.PressureSchedule < 0 || special.PursuerSchedule < 0 || special.DeadlineSchedule < 0)
                throw new InvalidDataException("Invalid special-dungeon state.");
            if (special.ActivatedAt is not { } start)
            {
                if (special.ExpiresAt != 0 || special.ExpiredAt != 0 || special.NextPressureAt != 0 || special.PursuerAt != 0
                    || special.PressureSchedule != 0 || special.PursuerSchedule != 0 || special.DeadlineSchedule != 0
                    || special.Pursuer != 0 || special.PursuerFailed || special.Reinforcements.Count != 0 || special.Rubble.Count != 0 || special.Casualties.Count != 0)
                    throw new InvalidDataException("An inactive special dungeon has committed consequences.");
                continue;
            }
            if (start < 0 || start > queue.Clock || special.ExpiresAt != checked(start + (long)special.Tuning.DurationSeconds * world.Tactics!.Tuning.UnitsPerSecond)
                || special.PursuerAt != checked(start + special.Tuning.PursuerDelay)
                || special.ExpiredAt != 0 && special.ExpiredAt != special.ExpiresAt)
                throw new InvalidDataException("Invalid persistent special-dungeon deadline.");
            if (special.ExpiredAt == 0)
            {
                if (special.ExpiresAt < queue.Clock || special.Rubble.Count != 0 || special.Casualties.Count != 0)
                    throw new InvalidDataException("An active special dungeon has premature aftermath.");
                Match(ScheduledKind.StageDeadline, special.DeadlineSchedule, special.ExpiresAt);
                if (special.PressureSchedule > 0) Match(ScheduledKind.StagePressure, special.PressureSchedule, special.NextPressureAt);
                if (special.PursuerSchedule > 0) Match(ScheduledKind.SpecialPursuerArrival, special.PursuerSchedule, special.PursuerAt);
                if (special.PressureSchedule == 0 && (special.NextPressureAt != 0 || start + special.Tuning.FirstPressure < special.ExpiresAt)
                    || special.Pursuer == 0 && !special.PursuerFailed && special.PursuerSchedule == 0 && special.PursuerAt < special.ExpiresAt
                    || special.PursuerFailed && (special.Pursuer != 0 || special.PursuerSchedule != 0 || special.PursuerAt > queue.Clock))
                    throw new InvalidDataException("Missing active special-dungeon work.");
            }
            else if (special.ExpiredAt > queue.Clock || special.DeadlineSchedule != 0 || special.PressureSchedule != 0 || special.PursuerSchedule != 0 || special.NextPressureAt != 0)
                throw new InvalidDataException("Expired special-dungeon work was not settled.");
            if (special.Pursuer != 0)
            {
                var actor = world.Actors.FirstOrDefault(a => a.Id == special.Pursuer);
                if (actor?.Tactics?.DefinitionId != "core:enemy/extraction_pursuer" || actor.Tactics.Enemy?.PursuitBoundary != Boundary(complex))
                    throw new InvalidDataException("Special dungeon lost its confined pursuer.");
            }
            foreach (long id in special.Reinforcements)
                if (!world.Actors.Any(a => a.Id == id && a.Tactics?.Enemy != null && a.Tactics.DefinitionId != "core:enemy/extraction_pursuer"))
                    throw new InvalidDataException("Special dungeon lost a reinforcement identity.");
            if (special.Casualties.Any(id => !world.Actors.Any(a => a.Id == id && a.Life == LifeState.Dead)))
                throw new InvalidDataException("Special dungeon lost its aftermath casualties.");

            void Match(ScheduledKind kind, long sequence, long due)
            {
                if (sequence <= 0 || due < queue.Clock || queue.Pending.Count(w => w.Kind == kind && w.Sequence == sequence
                    && w.Due == due && w.ActorId == 0 && w.ReferenceId == complex.Parcel.Id) != 1 || !expected.Add(sequence))
                    throw new InvalidDataException("Invalid special-dungeon scheduled event.");
            }
        }
        if (queue.Pending.Any(w => w.Kind is ScheduledKind.StagePressure or ScheduledKind.StageDeadline or ScheduledKind.SpecialPursuerArrival
            && !expected.Contains(w.Sequence))) throw new InvalidDataException("Orphaned special-dungeon event.");
    }
}
