using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record BellPiece(long Puzzle, int Number);
public sealed record BellPuzzle(long Plaque, long[] Bells, int[] Sequence, int Progress = 0, bool Completed = false);
public sealed record FixtureCommit(long Action, long Owner, long Due, long Schedule);
public sealed record FallingFixture(WorldPosition Center, WorldPosition[] Cells, FixtureCommit? Pending = null);
public sealed record DungeonRouteHint(Facing Direction, string Hint, WorldRoomId Target);

public sealed partial class DungeonServiceState
{
    private void ValidateAssemblies(WorldPlan plan, WorldState world)
    {
        var state = world.Tactics!; var queue = state.Queue.Snapshot();
        if (Puzzles == null || Puzzles.Any(p => p == null) || Puzzles.Select(p => p.Plaque).Distinct().Count() != Puzzles.Count)
            throw new InvalidDataException("Invalid bell puzzle registry.");
        foreach (var puzzle in Puzzles)
        {
            var plaque = Nodes.SingleOrDefault(n => n.Id == puzzle.Plaque);
            if (plaque?.Kind != DungeonServiceKind.BellClue || puzzle.Bells == null || puzzle.Bells.Length != 3
                || puzzle.Bells.Distinct().Count() != 3 || puzzle.Sequence == null || !puzzle.Sequence.Order().SequenceEqual(new[] { 1, 2, 3 })
                || puzzle.Progress is < 0 or > 3 || puzzle.Completed != (puzzle.Progress == 3)
                || plaque.Bell != new BellPiece(puzzle.Plaque, 0)) throw new InvalidDataException("Invalid bell clue or progress.");
            var offsets = new[] { (0, -1), (1, 0), (0, 1) };
            for (int i = 0; i < 3; i++)
            {
                var bell = Nodes.SingleOrDefault(n => n.Id == puzzle.Bells[i]);
                if (bell?.Kind != DungeonServiceKind.Bell || bell.Bell != new BellPiece(puzzle.Plaque, i + 1)
                    || bell.Position != plaque.Position.Offset(offsets[i].Item1, offsets[i].Item2))
                    throw new InvalidDataException("Invalid bell labels, geometry or group membership.");
            }
        }
        foreach (var node in Nodes)
        {
            if ((node.Kind is DungeonServiceKind.Bell or DungeonServiceKind.BellClue) != (node.Bell != null)
                || node.Bell != null && (!Puzzles.Any(p => p.Plaque == node.Bell.Puzzle
                    && (node.Kind == DungeonServiceKind.BellClue ? node.Id == p.Plaque : p.Bells.Contains(node.Id))) || node.UsedBy != 0)
                || (node.Kind == DungeonServiceKind.FallingFixture) != (node.Fixture != null)
                || (node.Kind == DungeonServiceKind.RouteSign) != (node.Sign != null))
                throw new InvalidDataException("Invalid fixture metadata or bell membership.");
            if (node.Sign is { } sign && (!Enum.IsDefined(sign.Direction) || !AttackGeometry.Cardinal(sign.Direction)
                || sign.Hint is not ("Treasure" or "Supplies" or "Rooms / danger") || node.UsedBy != 0
                || sign.Target.Area != RoomArea.Dungeon || sign.Target.Floor != node.Position.Floor
                || world.Dungeon?.Floors.FirstOrDefault(f => f.Origin.Floor == sign.Target.Floor)?.Rooms.Any(r => r.Index + 1 == sign.Target.Room) != true
                || world.Dungeon?.RoomAt(node.Position) == sign.Target)) throw new InvalidDataException("Invalid coarse route sign.");
            if (node.Fixture is not { } fixture) continue;
            if (fixture.Center != node.Position.Offset(1, 0) || fixture.Cells == null
                || !fixture.Cells.SequenceEqual(TacticalCombat.FixtureCross(fixture.Center))
                || fixture.Cells.Any(p => !plan.Contains(p) || world.Dungeon != null && !world.Dungeon.Contains(p)))
                throw new InvalidDataException("Invalid falling-fixture footprint.");
            if (fixture.Pending is { } pending && (pending.Action <= 0 || pending.Action >= state.NextAction
                || node.UsedBy != pending.Owner || !world.Party.Contains(pending.Owner)
                || pending.Due < queue.Clock || pending.Due - queue.Clock > 100 || pending.Schedule <= 0
                || queue.Pending.Count(w => FixtureMatches(w, node)) != 1))
                throw new InvalidDataException("Invalid committed falling-fixture work.");
        }
        if (queue.Pending.Any(w => w.Kind == ScheduledKind.FixtureImpact && !Nodes.Any(n => FixtureMatches(w, n))))
            throw new InvalidDataException("Orphaned falling-fixture work.");
    }

    internal static bool FixtureMatches(ScheduledAction work, DungeonService node) => node.Fixture?.Pending is { } pending
        && work.Kind == ScheduledKind.FixtureImpact && work.ActorId == 0 && work.ReferenceId == node.Id
        && work.Sequence == pending.Schedule && work.Due == pending.Due;
}

public sealed partial class TacticalCombat
{
    internal static WorldPosition[] FixtureCross(WorldPosition center)
        => [center, center.Offset(0, -1), center.Offset(1, 0), center.Offset(0, 1), center.Offset(-1, 0)];

    public BellPuzzle SpawnBellPuzzle(string marker, WorldPosition plaque, int seed)
    {
        if (seed <= 0 || seed >= RoagRandom.Modulus || !CanPlaceBellPuzzle(marker, plaque))
            throw new ArgumentException("A bell assembly needs a clear in-room cross without forcing terrain.");
        var points = FixtureCross(plaque);
        int[] sequence = [1, 2, 3]; var random = new RoagRandom(seed);
        for (int i = 2; i >= 1; i--) { int j = random.NextOneBased(i + 1) - 1; (sequence[i], sequence[j]) = (sequence[j], sequence[i]); }
        long id = State.Services.NextService;
        SpawnDungeonService(DungeonServiceKind.BellClue, marker + "/clue", plaque, new(id, 0));
        var bells = new long[3];
        for (int i = 0; i < 3; i++) bells[i] = SpawnDungeonService(DungeonServiceKind.Bell, marker + "/bell:" + (i + 1), points[i + 1], new(id, i + 1)).Id;
        var puzzle = new BellPuzzle(id, bells, sequence); State.Services.Puzzles.Add(puzzle); return puzzle;
    }

    public bool CanPlaceBellPuzzle(string marker, WorldPosition plaque) => !string.IsNullOrWhiteSpace(marker)
        && _world.RoomAt(plaque) is { Area: RoomArea.Dungeon } room
        && FixtureCross(plaque).All(p => _world.RoomAt(p) == room && _world.CanEnter(p) && !ProtectedForcedLanding(p)
            && !State.Services.Nodes.Any(n => n.Position == p) && !State.OptionalTerrain.Nodes.Any(n => n.Position == p)
            && !State.Secrets.Nodes.Any(n => !n.Revealed && n.Clue == p))
        && !State.Services.Nodes.Any(n => n.Marker.StartsWith(marker + "/", StringComparison.Ordinal));

    private void RingBell(DungeonService node, Actor owner, long action)
    {
        int index = State.Services.Puzzles.FindIndex(p => p.Plaque == node.Bell!.Puzzle); var puzzle = State.Services.Puzzles[index];
        bool correct = node.Bell!.Number == puzzle.Sequence[puzzle.Progress];
        int progress = correct ? puzzle.Progress + 1 : 0;
        State.Services.Puzzles[index] = puzzle with { Progress = progress, Completed = progress == 3 };
        Emit("BellRung", owner.Id, owner.Id, action, node.Position, progress, ability: correct ? "correct" : "reset");
        if (progress != 3) return;
        _world.State.Inventory["gold"] = checked(_world.Count("gold") + 5);
        var snapshot = CaptureBoonEvent(BoonTrigger.CurrencyCollected, owner.Id, 0, action, node.Position) with { Amount = 5 };
        Emit("CurrencyCollected", owner.Id, 0, action, node.Position, 5, snapshot: snapshot);
    }

    private void ResolveFallingFixture(ScheduledAction work)
    {
        int index = State.Services.Nodes.FindIndex(n => DungeonServiceState.FixtureMatches(work, n));
        if (index < 0) throw new InvalidDataException("Falling fixture lost its committed work.");
        var node = State.Services.Nodes[index]; var fixture = node.Fixture!; var pending = fixture.Pending!;
        State.Services.Nodes[index] = node with { Fixture = fixture with { Pending = null } };
        ResolveAtomically(RootCause(pending.Action, generated: true, coefficient: 0), () =>
        {
            Emit("FixtureFell", pending.Owner, 0, pending.Action, fixture.Center, 4, ability: "core:ability/falling_fixture");
            ResolveAttack(new(pending.Action, pending.Owner, fixture.Center, fixture.Cells, "core:ability/falling_fixture",
                AttackDelivery.Area, DamageElement.Physical, 4, Clock, DamageCause.GroundHazard,
                ClassGenerated: true, HitsOwner: true, PersistsWithoutOwner: true));
        });
    }

    private IEnumerable<LockedAttack> FixtureWarnings() => State.Services.Nodes.Where(n => n.Fixture?.Pending != null).Select(n =>
    {
        var fixture = n.Fixture!; var pending = fixture.Pending!;
        return new LockedAttack(pending.Action, pending.Owner, fixture.Center, fixture.Cells, "core:ability/falling_fixture",
            AttackDelivery.Area, DamageElement.Physical, 4, pending.Due, DamageCause.GroundHazard,
            ClassGenerated: true, HitsOwner: true, PersistsWithoutOwner: true);
    });

    public string DungeonServiceDescription(long serviceId, long observerId)
    {
        var node = State.Services.Nodes.SingleOrDefault(n => n.Id == serviceId);
        if (node == null || !_world.VisionFrom(Actor(observerId).Position).Contains(node.Position)) return "Observe this fixture to inspect its current state.";
        if (node.Bell is { } bell)
        {
            var puzzle = State.Services.Puzzles.Single(p => p.Plaque == bell.Puzzle);
            return "Ring bells " + string.Join(" -> ", puzzle.Sequence) + $". Progress {puzzle.Progress}/3. "
                + (puzzle.Completed ? "Completed; five-gold reward already collected." : "Wrong bells reset progress. Labels work without colour or audio.");
        }
        if (node.Sign is { } sign) return sign.Hint + " " + sign.Direction.ToString().ToLowerInvariant() + ". A coarse hint only; reading reveals no map and costs no time.";
        if (node.UsedBy != 0) return node.Fixture?.Pending != null ? "One use, already committed: four physical damage on the locked cross."
            : "Already used. This service does not refill on returning or Continue.";
        if (node.Fixture != null) return "One use. Locked five-tile cross: four physical damage to everyone, impact after 100 world time. No guaranteed reaction turn.";
        if (node.Contract is { } c) return $"Contract {c.Status}. Defeated {c.Defeated.Count}/{c.Targets.Length}. " + c.Failure;
        return "One use. Inspect saved choices freely; confirming pays one action and any listed price.";
    }
}
