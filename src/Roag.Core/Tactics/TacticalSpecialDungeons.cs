using Roag.Core.Dungeons;
using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewSpecialDungeon(Actor actor, TacticalIntent intent)
    {
        var complex = _world.State.NativeCampaign?.Complexes?.SingleOrDefault(c => c.Parcel.Id == intent.Target);
        if (!Friendly(actor) || complex?.Special is not { ActivatedAt: null }) return Invalid("This labyrinth has no unactivated extraction seal.");
        var activation = WorldSession.SpecialDungeonActivation(complex);
        if (intent.Choice != 1) return Invalid("Read and explicitly confirm the announced deadline and aftermath first.");
        if (actor.Position.Distance(activation) > 1 || !_world.VisionFrom(actor.Position).Contains(activation))
            return Invalid("Stand beside this labyrinth's observed entrance seal underground.");
        return new(true, "", ActionTiming.Recovery(Tuning.WaitCost, EffectiveSpeed(actor)), [activation]);
    }

    private void CommitSpecialDungeon(Actor actor, TacticalIntent intent, long action)
    {
        var complex = _world.State.NativeCampaign!.Complexes!.Single(c => c.Parcel.Id == intent.Target);
        var special = complex.Special!;
        special.ActivatedAt = Clock;
        special.ExpiresAt = checked(Clock + (long)special.Tuning.DurationSeconds * Tuning.UnitsPerSecond);
        special.PursuerAt = checked(Clock + special.Tuning.PursuerDelay);
        special.DeadlineSchedule = State.Queue.Schedule(ScheduledKind.StageDeadline, special.ExpiresAt, referenceId: complex.Parcel.Id).Sequence;
        if (special.PursuerAt < special.ExpiresAt)
            special.PursuerSchedule = State.Queue.Schedule(ScheduledKind.SpecialPursuerArrival, special.PursuerAt, referenceId: complex.Parcel.Id).Sequence;
        if (Clock + special.Tuning.FirstPressure < special.ExpiresAt)
            ScheduleSpecialPressure(complex, Clock + special.Tuning.FirstPressure);
        Emit("SpecialDungeonActivated", actor.Id, actor.Id, action, actor.Position, ability: special.Aftermath.ToString());
        _world.RecordSpecialDungeonMessage($"Extraction seal activated. Deadline {special.ExpiresAt}; {special.Aftermath}. Retreat does not reset it.");
    }

    private void ScheduleSpecialPressure(DungeonComplexState complex, long due)
    {
        var special = complex.Special!;
        special.NextPressureAt = due;
        special.PressureSchedule = State.Queue.Schedule(ScheduledKind.StagePressure, due, referenceId: complex.Parcel.Id).Sequence;
    }

    private void ResolveSpecialDungeonWork(ScheduledAction work)
    {
        var complex = _world.State.NativeCampaign?.Complexes?.SingleOrDefault(c => c.Parcel.Id == work.ReferenceId);
        if (complex?.Special is not { ActivatedAt: not null, ExpiredAt: 0 } special)
            throw new InvalidDataException("Special-dungeon work has no active owner.");
        ResolveAtomically(RootCause(0), () =>
        {
            if (work.Kind == ScheduledKind.StageDeadline)
            {
                if (special.DeadlineSchedule != work.Sequence || special.ExpiresAt != Clock) throw new InvalidDataException("Special-dungeon deadline differs from committed work.");
                special.DeadlineSchedule = 0; special.ExpiredAt = Clock;
                if (special.PressureSchedule != 0) State.Queue.Cancel(special.PressureSchedule);
                if (special.PursuerSchedule != 0) State.Queue.Cancel(special.PursuerSchedule);
                special.PressureSchedule = special.PursuerSchedule = special.NextPressureAt = 0;
                ResolveSpecialAftermath(complex);
                return;
            }
            if (work.Kind == ScheduledKind.SpecialPursuerArrival)
            {
                if (special.PursuerSchedule != work.Sequence || special.PursuerAt != Clock) throw new InvalidDataException("Special-dungeon arrival differs from committed work.");
                special.PursuerSchedule = 0;
                SpawnSpecialPursuer(complex);
                return;
            }
            if (work.Kind != ScheduledKind.StagePressure || special.PressureSchedule != work.Sequence || special.NextPressureAt != Clock)
                throw new InvalidDataException("Special-dungeon pressure differs from committed work.");
            special.PressureSchedule = 0; special.NextPressureAt = 0;
            SpawnSpecialReinforcement(complex);
            int tier = Math.Min(2, special.Reinforcements.Count / 3);
            int period = tier == 0 ? special.Tuning.FirstPeriod : tier == 1 ? special.Tuning.SecondPeriod : special.Tuning.ThirdPeriod;
            ScheduleSpecialPressure(complex, Math.Min(special.ExpiresAt, checked(Clock + period)));
        });
    }

    private DungeonFloorState SpecialPressureFloor(DungeonComplexState complex)
    {
        var member = _world.Party.Where(a => a.Life != LifeState.Dead && complex.Contains(a.Position)).OrderBy(a => a.Id).FirstOrDefault();
        return member == null ? complex.Run.Floors[0] : complex.Run.Floors.FirstOrDefault(f => f.Contains(member.Position)) ?? complex.Run.Floors[0];
    }

    internal WorldPosition[] SpecialSpawnCandidates(DungeonComplexState complex, DungeonFloorState floor, int minimum, bool pursuerFallback = false)
    {
        var members = _world.Party.Where(a => a.Life != LifeState.Dead && a.Position.Floor == floor.Origin.Floor).ToArray();
        var observed = members.SelectMany(m => _world.VisionFrom(m.Position)).ToHashSet();
        var origin = members.Where(m => floor.Contains(m.Position)).OrderBy(m => m.Id).FirstOrDefault()?.Position ?? floor.Entry;
        var reachable = new HashSet<WorldPosition>();
        var visited = new HashSet<WorldPosition>(); var queue = new Queue<WorldPosition>(); queue.Enqueue(origin);
        while (queue.TryDequeue(out var at))
        {
            if (!floor.Contains(at) || !visited.Add(at) || !_world.GroundOpen(at)) continue;
            if (at != origin && _world.OccupantAt(at) is { } occupant && occupant.Tactics?.Enemy == null) continue;
            reachable.Add(at);
            queue.Enqueue(at.Offset(0, -1)); queue.Enqueue(at.Offset(1, 0));
            queue.Enqueue(at.Offset(0, 1)); queue.Enqueue(at.Offset(-1, 0));
        }
        var cells = new List<WorldPosition>(); var added = new HashSet<WorldPosition>();
        (int X, int Y)[] offsets = [(0, 0), (0, -1), (1, 0), (0, 1), (-1, 0),
            (-1, -1), (1, -1), (1, 1), (-1, 1), (0, -2), (2, 0), (0, 2), (-2, 0)];
        // Preserve original room/enemy-marker/offset order and pressure RNG draws.
        // Adapt solo camera exclusion to all members' actual private observations.
        foreach (var room in floor.Rooms.Where(r => r.Role is not ("start" or "elite")))
            if (DungeonCatalog.Default.Rooms.TryGetValue(room.Definition, out var definition))
            {
                var shape = definition.Transform(room.Turns, room.Mirror);
                foreach (var marker in shape.Markers.GetValueOrDefault("enemy") ?? [])
                    foreach (var (dx, dy) in offsets) Add(room.Origin.Offset(marker.X - 1 + dx, marker.Y - 1 + dy));
            }
        if (pursuerFallback && cells.Count == 0)
            for (int y = 1; y < floor.Height; y++) for (int x = 1; x < floor.Width; x++) Add(floor.Origin.Offset(x, y));
        return cells.ToArray();

        void Add(WorldPosition p)
        {
            if (!reachable.Contains(p) || !complex.Contains(p) || !added.Add(p) || !_world.CanEnter(p)
                || observed.Contains(p) || members.Any(m => m.Position.Distance(p) < minimum) || p == floor.Entry || p == floor.Exit
                || State.Secrets.Nodes.Any(s => s.Position == p || s.Cache == p)) return;
            cells.Add(p);
        }
    }

    private Actor SpawnSpecialEnemy(string definition, WorldPosition at, EnemyPursuitBoundary? boundary = null)
    {
        var actor = ActorFactory.Create(_world.Content, _world.Content.Game.Raider, _world.Seed, _world.State.NextActorId++, at, 0);
        actor.Name = EnemyCatalog.Default.Get(definition).Name; actor.CombatTrait = "";
        _world.State.Actors.Add(actor);
        RegisterEnemy(actor, definition, Clock + 1, pursuitBoundary: boundary);
        actor.Health = actor.MaxHealth;
        return actor;
    }

    private void SpawnSpecialReinforcement(DungeonComplexState complex)
    {
        var special = complex.Special!;
        if (special.Reinforcements.Count >= special.Tuning.ReinforcementLimit) return;
        var floor = SpecialPressureFloor(complex);
        int active = _world.State.Actors.Count(a => a.Life == LifeState.Alive && floor.Contains(a.Position)
            && a.Tactics?.Enemy != null && a.Tactics.DefinitionId != "core:enemy/extraction_pursuer" && floor.Boss != a.Id);
        if (active >= special.Tuning.ActiveLimit) return;
        var candidates = SpecialSpawnCandidates(complex, floor, special.Tuning.SafeRadius);
        if (candidates.Length == 0) return;
        var rng = new RoagRandom(special.RandomState);
        var at = candidates[rng.NextOneBased(candidates.Length) - 1];
        var biome = DungeonCatalog.Default.Biomes.FirstOrDefault(b => b.Id == floor.Biome) ?? DungeonCatalog.Default.Biomes[0];
        string definition = biome.EnemyIds[rng.NextOneBased(biome.EnemyIds.Length) - 1];
        special.RandomState = rng.State;
        var enemy = SpawnSpecialEnemy(definition, at);
        special.Reinforcements.Add(enemy.Id);
        Emit("PressureSpawned", enemy.Id, 0, 0, at, ability: definition);
    }

    private void SpawnSpecialPursuer(DungeonComplexState complex)
    {
        var special = complex.Special!; var floor = SpecialPressureFloor(complex);
        var candidates = SpecialSpawnCandidates(complex, floor, special.Tuning.PursuerDistance, pursuerFallback: true);
        if (candidates.Length == 0)
        {
            special.PursuerFailed = true;
            Emit("PursuerSpawnFailed", 0, 0, 0, floor.Entry);
            return;
        }
        var pursuer = SpawnSpecialEnemy("core:enemy/extraction_pursuer", candidates[^1], SpecialDungeonRules.Boundary(complex));
        special.Pursuer = pursuer.Id;
        Emit("PursuerArrived", pursuer.Id, 0, 0, pursuer.Position);
        RecordPursuerArrival(pursuer.Id);
    }

    private void ResolveSpecialAftermath(DungeonComplexState complex)
    {
        var special = complex.Special!; var entry = WorldSession.SpecialDungeonActivation(complex);
        if (special.Aftermath == SpecialDungeonAftermath.LethalCollapse)
        {
            // One announced catastrophe, resolved through canonical death/gear/reward
            // accounting. Surface travellers and other complexes remain outside it.
            var victims = _world.State.Actors.Where(a => a.Life != LifeState.Dead && complex.Contains(a.Position))
                .OrderBy(a => Friendly(a)).ThenBy(a => a.Id).ToArray();
            foreach (var actor in victims)
                if (actor.Life != LifeState.Dead) { special.Casualties.Add(actor.Id); Die(actor, 0, 0); }
            // The paired stair remains an identifiable ruin; rubble beside it
            // affects real access and can later be excavated through ordinary work.
        }
        var outer = complex.Run.Floors.Single(f => f.Stage == 1 && f.Origin.Floor == 1);
        var protectedCells = _world.SpecialAftermathReservations();
        foreach (var p in Enumerable.Range(1, 12).SelectMany(r => new[] { entry.Offset(r, 0), entry.Offset(-r, 0), entry.Offset(0, r), entry.Offset(0, -r) }))
        {
            if (special.Rubble.Count >= (special.Aftermath == SpecialDungeonAftermath.LethalCollapse ? 8 : 4)) break;
            if (!outer.Contains(p) || !_world.CanAlterForSpecialAftermath(p, protectedCells)) continue;
            _world.Chunks.Edit(p, Material.Wall); special.Rubble.Add(p);
        }
        Emit("SpecialDungeonExpired", 0, 0, 0, entry, special.Casualties.Count, ability: special.Aftermath.ToString());
        _world.RecordSpecialDungeonMessage($"{special.Aftermath}: the activated labyrinth's deadline has expired. Its local aftermath is permanent; rewards and identities have not reset.");
    }
}
