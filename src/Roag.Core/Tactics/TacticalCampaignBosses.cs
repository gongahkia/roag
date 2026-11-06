using System.Collections.Immutable;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    public void RegisterCampaignBoss(Actor actor, CampaignBossArena arena, long? readyAt = null)
    {
        if (State.CampaignBosses is not { } store || !store.Arenas.Contains(arena) || arena.Boss != actor.Id || !arena.Contains(actor.Position)
            || Friendly(actor) || actor.Tactics != null || State.Participants.Contains(actor.Id))
            throw new ArgumentException("Register an unacted persistent boss in its registered world arena.");
        var definition = CampaignBossCatalog.Definition(arena.Kind, arena.FinalRole, arena.Variation);
        var tactics = definition.CreateTactics();
        tactics.RegularEnemy = false; tactics.ImmuneToPull = true;
        tactics.Enemy!.CampaignBoss = new() { Arena = arena.Id, ShieldFacing = WorldSession.RotatedFacing(Facing.South, arena.Variation) };
        Register(actor, tactics, readyAt);
        actor.SpriteId = CampaignBossCatalog.Sprite(arena.Kind);
    }

    private void ValidateCampaignBossWindup(Actor actor, string ability, IReadOnlyList<WorldPosition> cells,
        AttackDelivery delivery, DamageElement element, int damage, int windup, int recovery, CampaignBossCast? cast)
    {
        if (actor.Tactics?.Enemy?.CampaignBoss == null)
        {
            if (cast != null || ability.StartsWith("core:ability/boss_", StringComparison.Ordinal))
                throw new ArgumentException("Campaign boss attacks require their owning controller.");
            return;
        }
        var arena = BossArena(actor); var definition = CampaignBossDefinition(actor);
        if (cast == null || !CampaignBossValidation.GeometryFits(arena, cast, actor.Position, cells)
            || ability != "core:ability/boss_" + cast.Move.ToString().ToLowerInvariant()
            || delivery != AttackDelivery.Area || element != DamageElement.Physical
            || damage != (cast.Move is CampaignBossMove.ReservoirFeast or CampaignBossMove.PartitionWeb or CampaignBossMove.BoneRampart or CampaignBossMove.FuneralChime ? 0 : actor.Power)
            || windup != (cast.Move == CampaignBossMove.ReservoirFeast ? 240 : definition.Windup)
            || recovery != (cast.Move == CampaignBossMove.ShieldAdvance ? 240 : definition.Recovery))
            throw new ArgumentException("Campaign boss commitment does not match its authored role or geometry.");
    }

    private CampaignBossWorldState BossStore => State.CampaignBosses ?? throw new InvalidDataException("Missing owning campaign boss registry.");
    private CampaignBossArena BossArena(Actor boss) => BossStore.Arenas.Single(a => a.Id == boss.Tactics!.Enemy!.CampaignBoss!.Arena);
    private EnemyDefinition CampaignBossDefinition(Actor boss)
    {
        var arena = BossArena(boss);
        return CampaignBossCatalog.Definition(arena.Kind, arena.FinalRole, arena.Variation);
    }
    private static Facing BossFacing(WorldPosition from, WorldPosition to)
        => Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y) ? to.X >= from.X ? Facing.East : Facing.West : to.Y >= from.Y ? Facing.South : Facing.North;
    private bool BossCell(CampaignBossArena arena, WorldPosition p) => arena.Contains(p) && !Materials.Opaque(_world.Chunks.At(p));
    private IEnumerable<WorldPosition> BossRing(CampaignBossArena arena, WorldPosition p, int radius)
    {
        for (int y = -radius; y <= radius; y++) for (int x = -radius; x <= radius; x++)
            if (Math.Max(Math.Abs(x), Math.Abs(y)) == radius && BossCell(arena, p.Offset(x, y))) yield return p.Offset(x, y);
    }
    private bool HasCampaignBossNoise(Actor boss) => BossArena(boss).Kind == CampaignBossKind.Bellkeeper
        && boss.Tactics!.Enemy!.CampaignBoss is { LastNoise: { } noise } memory
        && Clock - memory.NoiseAt <= 500 && BossArena(boss).Contains(noise);

    private EnemyIntent PlanCampaignBossReturn(Actor boss)
    {
        var arena = BossArena(boss);
        var goals = new HashSet<WorldPosition>();
        for (int y = -CampaignBossArena.Radius; y <= CampaignBossArena.Radius; y++)
            for (int x = -CampaignBossArena.Radius; x <= CampaignBossArena.Radius; x++)
            {
                var p = arena.Center.Offset(x, y);
                if (_world.CanEnter(p, boss.Id)) goals.Add(p);
            }
        // Forced movement remains ordinary physical counterplay. Returning spends
        // the same scheduled steps as pursuit and cannot cross blocked ground.
        var step = _world.Navigate(boss, goals, p => p.Floor == arena.Center.Floor && _world.Plan.Contains(p));
        return step == null ? new(EnemyIntentKind.Wait) : new(EnemyIntentKind.Move, step.Facing);
    }

    private EnemyIntent PlanCampaignBossTurn(Actor boss, Actor? target, Func<WorldPosition, IReadOnlySet<WorldPosition>, Facing?> nextStep)
    {
        var arena = BossArena(boss); var memory = boss.Tactics!.Enemy!.CampaignBoss!;
        if (!arena.Contains(boss.Position)) return PlanCampaignBossReturn(boss);
        bool heardNoise = HasCampaignBossNoise(boss);
        if (!heardNoise && (target == null || !arena.Contains(target.Position))) return new(EnemyIntentKind.Wait);
        var aim = target == null ? memory.LastNoise!.Value : EnemyAim(target, boss.Position.Floor);
        EnemyIntent Windup(CampaignBossMove move, IEnumerable<WorldPosition> cells, int feature = -1, Facing facing = Facing.South)
            => new(EnemyIntentKind.Windup, Ability: "core:ability/boss_" + move.ToString().ToLowerInvariant(), Delivery: AttackDelivery.Area,
                Cells: cells.Distinct().Where(arena.Contains).ToImmutableArray(), Boss: new(move, feature, facing));
        switch (arena.Kind)
        {
            case CampaignBossKind.Bellkeeper:
                if (memory.Moves % 2 == 1 && arena.Fixtures.Any(f => f.Kind == CampaignBossFixtureKind.Resonator && f.Health > 0))
                    return Windup(CampaignBossMove.FuneralChime, arena.Fixtures.Where(f => f.Kind == CampaignBossFixtureKind.Resonator && f.Health > 0)
                        .SelectMany(f => BossRing(arena, f.Position, 1)));
                var noise = memory.LastNoise is { } heard && Clock - memory.NoiseAt <= 500 && arena.Contains(heard) ? heard : aim;
                return Windup(CampaignBossMove.Soundseek, arena.FinalRole ? new[] { noise }.Concat(BossRing(arena, noise, 1)) : new[] { noise });
            case CampaignBossKind.SluiceMaw:
                int channel = (int)(memory.Moves / 2 % arena.Channels.Count);
                if (memory.Moves % 2 == 1)
                    return Windup(CampaignBossMove.ReservoirFeast, [arena.Channels[channel].Pool], channel);
                return Windup(CampaignBossMove.ChannelSurge, arena.Channels[channel].Cells, channel, arena.Channels[channel].Flow);
            case CampaignBossKind.Threadmother:
                if (memory.Moves % 2 == 0)
                {
                    int shift = arena.Variation == 1 ? -2 : 2;
                    var cells = new List<WorldPosition>();
                    foreach (int x in arena.FinalRole ? new[] { shift, -shift } : new[] { shift })
                        for (int y = -4; y <= 4; y++) if (y != 0) cells.Add(WorldSession.Rotate(arena.Center, x, y, arena.Variation));
                    return Windup(CampaignBossMove.PartitionWeb, cells);
                }
                return Windup(CampaignBossMove.ReelIn, [aim], facing: BossFacing(aim, arena.Center));
            case CampaignBossKind.OssuaryCaptain:
                var direction = BossFacing(boss.Position, aim); var (dx, dy) = AttackGeometry.Offset(direction);
                if (memory.Moves % 2 == 0)
                {
                    var cells = new List<WorldPosition>();
                    for (int i = 1; i <= (arena.FinalRole ? 5 : 4); i++)
                    {
                        var p = boss.Position.Offset(dx * i, dy * i);
                        if (!BossCell(arena, p) || _world.ObjectAt(p) is { Solid: true }) break;
                        cells.Add(p);
                    }
                    if (cells.Count > 0) return Windup(CampaignBossMove.ShieldAdvance, cells, facing: direction);
                }
                // Three-tile barricades leave both ends and the four arena exits open.
                int offset = memory.Moves % 4 == 1 ? 2 : -2;
                return Windup(CampaignBossMove.BoneRampart,
                    Enumerable.Range(-2, 5).Where(i => i != 0).Select(i => WorldSession.Rotate(arena.Center, i, offset, arena.Variation)), facing: direction);
            default: throw new InvalidDataException("Unknown campaign boss.");
        }
    }

    private void CommitCampaignBossTurn(Actor boss, EnemyIntent intent, long targetId)
    {
        var cast = intent.Boss ?? throw new InvalidOperationException("Missing campaign boss cast.");
        var arena = BossArena(boss); var memory = boss.Tactics!.Enemy!.CampaignBoss!;
        var definition = CampaignBossDefinition(boss);
        if (intent.Cells.IsDefaultOrEmpty && cast.Move is CampaignBossMove.Soundseek or CampaignBossMove.ReelIn)
            throw new InvalidOperationException("The locked boss attack needs a physical target tile.");
        var previousFacing = memory.ShieldFacing;
        if (arena.Kind == CampaignBossKind.OssuaryCaptain)
        {
            if (previousFacing != cast.Facing) memory.ExposedUntil = checked(Clock + 300);
            memory.ShieldFacing = cast.Facing; boss.Tactics.Facing = cast.Facing;
        }
        int windup = cast.Move == CampaignBossMove.ReservoirFeast ? 240 : definition.Windup;
        int recovery = cast.Move == CampaignBossMove.ShieldAdvance ? 240 : definition.Recovery;
        int damage = cast.Move is CampaignBossMove.ReservoirFeast or CampaignBossMove.PartitionWeb or CampaignBossMove.BoneRampart or CampaignBossMove.FuneralChime ? 0 : boss.Power;
        bool locationTarget = cast.Move is CampaignBossMove.FuneralChime or CampaignBossMove.ReservoirFeast
            or CampaignBossMove.PartitionWeb or CampaignBossMove.BoneRampart
            || cast.Move == CampaignBossMove.Soundseek && HasCampaignBossNoise(boss);
        var attack = CommitWindup(intent.Ability, intent.Cells, intent.Delivery, DamageElement.Physical,
            damage, windup, recovery, locationTarget ? 0 : targetId, boss: cast);
        memory.Moves = checked(memory.Moves + 1);
        if (cast.Move == CampaignBossMove.ReservoirFeast)
            Emit("BossFeastAnnounced", boss.Id, 0, attack.Id, arena.Channels[cast.Feature].Pool, cast.Feature + 1);
        if (cast.Move == CampaignBossMove.ChannelSurge)
            Emit("BossSurgeAnnounced", boss.Id, 0, attack.Id, boss.Position, cast.Feature + 1, ability: cast.Facing.ToString());
        if (cast.Move == CampaignBossMove.ReelIn)
        {
            // A cuttable line is present throughout the warning; breaking any segment cancels the pull.
            foreach (var p in BossLine(boss.Position, attack.Cells[0]).SkipLast(1))
                if (arena.Contains(p) && _world.CampaignBossFixtureAt(p) == null)
                    arena.AddFixture(CampaignBossFixtureKind.ReelLine, p, 1, attack.Id);
        }
    }

    private static IEnumerable<WorldPosition> BossLine(WorldPosition from, WorldPosition to)
    {
        int distance = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
        for (int i = 1; i <= distance; i++)
            yield return new(from.X + (int)Math.Round((to.X - from.X) * (double)i / distance),
                from.Y + (int)Math.Round((to.Y - from.Y) * (double)i / distance), from.Floor);
    }

    /// <returns>True when the cast owns its complete resolution; ordinary damaging casts continue through the canonical batch.</returns>
    private bool ResolveCampaignBossCast(LockedAttack attack)
    {
        if (attack.Boss == null) return false;
        var boss = Actor(attack.Source); var arena = BossArena(boss); var cast = attack.Boss;
        if (boss.Life != LifeState.Alive) return true;
        switch (cast.Move)
        {
            case CampaignBossMove.FuneralChime:
                foreach (var fixture in arena.Fixtures.Where(f => f.Kind == CampaignBossFixtureKind.Resonator && f.Health > 0))
                    for (int radius = 1; radius <= (arena.FinalRole ? 4 : 3); radius++)
                        QueueCampaignBossPulse(arena, CampaignBossPulseKind.Chime, fixture.Position,
                            (radius == 1 ? attack.Cells.Where(p => Math.Max(Math.Abs(p.X - fixture.Position.X), Math.Abs(p.Y - fixture.Position.Y)) == 1)
                                : BossRing(arena, fixture.Position, radius)).ToArray(), checked(Clock + (radius - 1) * (arena.FinalRole ? 90 : 120)), boss.Power, fixture.Id);
                Emit("BossChimeStarted", boss.Id, 0, attack.Id, boss.Position); return true;
            case CampaignBossMove.ReservoirFeast:
                var channel = arena.Channels[cast.Feature];
                bool connected = !channel.Drained && BossPoolConnected(arena, boss.Position, channel.Pool);
                if (connected && _world.OccupantAt(channel.Pool, boss.Id) == null && _world.ObjectAt(channel.Pool) is not { Solid: true })
                {
                    var from = boss.Position; _world.MoveTacticalActor(boss, channel.Pool);
                    Emit("Displaced", boss.Id, boss.Id, attack.Id, boss.Position, forced: true, from: from);
                    Emit("TileEntered", boss.Id, boss.Id, attack.Id, boss.Position, forced: true, from: from);
                    int heal = Math.Min(boss.MaxHealth - boss.Health, arena.FinalRole ? 24 : 14); boss.Health += heal;
                    if (heal > 0) Emit("Healed", boss.Id, boss.Id, attack.Id, boss.Position, heal, ability: attack.Ability);
                    Emit("BossReservoirFeast", boss.Id, boss.Id, attack.Id, boss.Position, heal);
                }
                else { ExposeCampaignBoss(boss, attack.Id, 300); Emit("BossFeastDenied", boss.Id, 0, attack.Id, channel.Pool); }
                return true;
            case CampaignBossMove.PartitionWeb:
            case CampaignBossMove.BoneRampart:
                var fixtureKind = cast.Move == CampaignBossMove.PartitionWeb ? CampaignBossFixtureKind.WebStrand : CampaignBossFixtureKind.BoneRampart;
                var partitions = new Dictionary<int, long>();
                foreach (var p in attack.Cells)
                {
                    if (arena.Fixtures.Count(f => f.Health > 0) >= 48 || !_world.CanEnter(p) || ProtectedForcedLanding(p)
                        || _world.ObjectAt(p) != null || !BossCell(arena, p) || p == arena.Center || Math.Abs(p.X - arena.Center.X) > 5 || Math.Abs(p.Y - arena.Center.Y) > 5) continue;
                    // Endpoints are anchors; all groups have a permanent central walking gap.
                    bool anchor = fixtureKind == CampaignBossFixtureKind.WebStrand && Math.Max(Math.Abs(p.X - arena.Center.X), Math.Abs(p.Y - arena.Center.Y)) == 4;
                    long group = attack.Id;
                    if (fixtureKind == CampaignBossFixtureKind.WebStrand)
                    {
                        int axis = arena.Variation == 1 ? p.Y : p.X;
                        if (!partitions.TryGetValue(axis, out group))
                        { group = partitions.Count == 0 ? attack.Id : State.NextAction++; partitions.Add(axis, group); }
                    }
                    arena.AddFixture(anchor ? CampaignBossFixtureKind.WebAnchor : fixtureKind, p, arena.FinalRole ? 5 : 3, group);
                }
                CompactBossFixtures(arena);
                Emit(cast.Move == CampaignBossMove.PartitionWeb ? "BossWebRaised" : "BossRampartRaised", boss.Id, 0, attack.Id, boss.Position); return true;
            case CampaignBossMove.ReelIn:
                bool intact = arena.Fixtures.Any(f => f.Kind == CampaignBossFixtureKind.ReelLine && f.Group == attack.Id && f.Health > 0);
                if (intact && _world.OccupantAt(attack.Cells[0], boss.Id) is { Life: not LifeState.Dead } victim
                    && EnemySquads.Clear(boss.Position, victim.Position, p => p == victim.Position || ProjectileOpen(p)))
                {
                    ResolveDamageBatch(attack);
                    for (int step = 0; step < (arena.FinalRole ? 3 : 2) && victim.Life == LifeState.Alive && victim.Position.Distance(arena.Center) > 1; step++)
                        if (!TryShove(victim, BossFacing(victim.Position, arena.Center), boss.Id, attack.Id)) break;
                }
                else Emit("BossReelEvaded", boss.Id, 0, attack.Id, attack.Cells[0]);
                foreach (var f in arena.Fixtures.Where(f => f.Kind == CampaignBossFixtureKind.ReelLine && f.Group == attack.Id)) f.Health = 0;
                CompactBossFixtures(arena); return true;
            case CampaignBossMove.ChannelSurge:
                if (arena.Channels[cast.Feature].Drained) { Emit("BossSurgeDry", boss.Id, 0, attack.Id, attack.Origin); return true; }
                ResolveDamageBatch(attack);
                // Front-most bodies move first; a single hit never pushes the same actor twice.
                var (dx, dy) = AttackGeometry.Offset(cast.Facing);
                var occupants = _world.State.Actors.Where(a => a.Id != boss.Id && a.Life == LifeState.Alive && attack.Cells.Contains(a.Position))
                    .OrderByDescending(a => a.Position.X * dx + a.Position.Y * dy).ToArray();
                foreach (var occupant in occupants)
                    for (int i = 0; i < (arena.FinalRole ? 2 : 1); i++)
                        if (!TryShove(occupant, cast.Facing, boss.Id, attack.Id, p =>
                            _world.GroundOpen(p) || arena.Channels[cast.Feature].Cells.Contains(p) && _world.Chunks.At(p) == Material.Water
                                && _world.ObjectAt(p) is not { Solid: true } && !_world.TacticalBlockAt(p))) break;
                return true;
            case CampaignBossMove.ShieldAdvance:
                // A newly built wall constrains this physical charge as well as its movement.
                var reachable = attack.Cells.TakeWhile(ProjectileOpen).ToArray();
                ResolveDamageBatch(attack with { Cells = reachable });
                foreach (var p in reachable)
                {
                    if (_world.OccupantAt(p, boss.Id) is { Life: LifeState.Alive } occupant) TryShove(occupant, cast.Facing, boss.Id, attack.Id);
                    if (!_world.CanEnter(p, boss.Id) || ProtectedForcedLanding(p)) break;
                    var from = boss.Position; _world.MoveTacticalActor(boss, p);
                    Emit("Displaced", boss.Id, boss.Id, attack.Id, p, forced: true, from: from);
                    Emit("TileEntered", boss.Id, boss.Id, attack.Id, p, forced: true, from: from);
                }
                ExposeCampaignBoss(boss, attack.Id, 300); return true;
            default: return false;
        }
    }

    private bool BossPoolConnected(CampaignBossArena arena, WorldPosition from, WorldPosition to)
    {
        var wet = arena.Channels.Where(c => !c.Drained).SelectMany(c => c.Cells).ToHashSet();
        var seen = new HashSet<WorldPosition> { from }; var queue = new Queue<WorldPosition>(); queue.Enqueue(from);
        while (queue.TryDequeue(out var p))
        {
            if (p == to) return true;
            foreach (var (dx, dy) in new[] { (0, -1), (1, 0), (0, 1), (-1, 0) })
            {
                var next = p.Offset(dx, dy);
                if (wet.Contains(next) && BossCell(arena, next) && _world.ObjectAt(next) is not { Solid: true }
                    && (_world.Chunks.At(next) is Material.Water or Material.Bridge or Material.Floor) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return false;
    }

    private void ExposeCampaignBoss(Actor boss, long action, int duration)
    {
        var memory = boss.Tactics!.Enemy!.CampaignBoss!;
        memory.ExposedUntil = Math.Max(memory.ExposedUntil, checked(Clock + duration));
        Emit("BossExposed", boss.Id, boss.Id, action, boss.Position, duration);
    }
    private int CampaignBossAdjustedDamage(Actor target, Actor? source, LockedAttack attack, int amount)
    {
        if (target.Tactics?.Enemy?.CampaignBoss is not { } memory) return amount;
        var arena = BossArena(target);
        if (memory.ExposedUntil > Clock || arena.Kind == CampaignBossKind.SluiceMaw && arena.Channels.All(c => c.Drained))
            return amount == 0 ? 0 : checked(amount + Math.Max(1, amount / 2));
        if (arena.Kind != CampaignBossKind.OssuaryCaptain || source == null || attack.Origin.Floor != target.Position.Floor
            || attack.Cause is not (DamageCause.DirectAttack or DamageCause.TriggeredEffect)
            || State.Attacks.Any(a => a.Source == target.Id)) return amount;
        int dx = Math.Sign(attack.Origin.X - target.Position.X), dy = Math.Sign(attack.Origin.Y - target.Position.Y);
        if (!AttackGeometry.TryFacing(dx, dy, out var incoming)) return amount;
        int difference = ((int)incoming - (int)memory.ShieldFacing + 8) % 8;
        if (difference is 0 or 1 or 7)
        {
            int result = (int)((long)amount * (arena.FinalRole ? 10 : 20) / 100);
            Emit("BossShieldBlocked", target.Id, source.Id, attack.Id, target.Position, amount - result);
            return result;
        }
        return amount;
    }
    private static void CompactBossFixtures(CampaignBossArena arena) => arena.Fixtures.RemoveAll(f => f.Health <= 0
        && f.Kind is not (CampaignBossFixtureKind.Resonator or CampaignBossFixtureKind.Valve));
}
