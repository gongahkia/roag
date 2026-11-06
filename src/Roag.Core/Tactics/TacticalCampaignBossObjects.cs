using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    public static string CampaignBossFixtureName(CampaignBossFixtureKind kind) => kind switch
    {
        CampaignBossFixtureKind.Resonator => "Funeral resonator", CampaignBossFixtureKind.Valve => "Cistern valve",
        CampaignBossFixtureKind.WebAnchor => "Web anchor", CampaignBossFixtureKind.WebStrand => "Cuttable partition web",
        CampaignBossFixtureKind.ReelLine => "Reeling line", CampaignBossFixtureKind.BoneRampart => "Bone rampart",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public string CampaignBossFixtureDescription(WorldPosition p)
    {
        var arena = State.CampaignBosses?.Arenas.FirstOrDefault(a => a.Contains(p));
        var fixture = arena?.Fixtures.FirstOrDefault(f => f.Position == p && f.Health > 0);
        if (fixture == null) return "";
        return fixture.Kind switch
        {
            CampaignBossFixtureKind.Resonator => $"{fixture.Health} HP. Blocks movement and shots. Attack it to cancel its expanding rings and expose the Bellkeeper during a long recovery.",
            CampaignBossFixtureKind.Valve => arena!.Channels[fixture.Channel].Drained ? $"Channel #{fixture.Channel + 1}: permanently drained. This channel is dry and cannot feed Sluice-Maw."
                : $"Channel #{fixture.Channel + 1}, flow {arena.Channels[fixture.Channel].Flow}. Hover to trace its visible channel and pool. Operate from beside it for one action. Permanently drains its marked channel, opens a dry route and exposes Sluice-Maw. Drain the announced feast pool before impact to deny healing.",
            CampaignBossFixtureKind.WebAnchor => $"{fixture.Health} HP. Cut this anchor to clear its entire connected partition. The central walking gap remains open. Fire schedules visible spreading burns that can hit allies.",
            CampaignBossFixtureKind.WebStrand => $"{fixture.Health} HP. Blocks movement and shots; any damaging attack can cut it. Fire clears connected strands with delayed impacts that can hit nearby allies. Walk through the marked gap or cut an anchor.",
            CampaignBossFixtureKind.ReelLine => "1 HP. Any damaging attack cuts the line and stops the locked pull. Moving away from its marked landing also evades it.",
            _ => $"{fixture.Health} HP. Breakable lane barricade. The open ends and central gap preserve escape routes."
        };
    }
    private TacticalPreview PreviewCampaignBossInteraction(Actor actor, TacticalIntent intent)
    {
        var arena = State.CampaignBosses?.Arenas.FirstOrDefault(a => a.Id == intent.Target);
        var valve = arena?.Fixtures.FirstOrDefault(f => f.Id == intent.Choice && f.Kind == CampaignBossFixtureKind.Valve && f.Health > 0);
        if (!Friendly(actor) || arena == null || valve == null || actor.Position.Distance(valve.Position) > 1
            || !_world.VisionFrom(actor.Position).Contains(valve.Position)) return Invalid("Stand beside a visible cistern valve.");
        if (arena.Channels[valve.Channel].Drained) return Invalid("This channel is already drained.");
        return new(true, "", ActionTiming.Recovery(100, EffectiveSpeed(actor)), arena.Channels[valve.Channel].Cells);
    }
    private void CommitCampaignBossInteraction(Actor actor, TacticalIntent intent, long action)
    {
        var arena = BossStore.Arenas.Single(a => a.Id == intent.Target);
        var valve = arena.Fixtures.Single(f => f.Id == intent.Choice);
        var channel = arena.Channels[valve.Channel]; channel.Drained = true;
        var stillWet = arena.Channels.Where(c => !c.Drained).SelectMany(c => c.Cells).ToHashSet();
        foreach (var p in channel.Cells)
            if (!stillWet.Contains(p) && _world.Chunks.At(p) == Material.Water) _world.Chunks.Edit(p, Material.Floor);
        if (Actor(arena.Boss).Life == LifeState.Alive) ExposeCampaignBoss(Actor(arena.Boss), action, arena.FinalRole ? 350 : 450);
        Emit("BossValveDrained", actor.Id, arena.Boss, action, valve.Position, valve.Channel, ability: CampaignBossCatalog.Id(arena.Kind));
    }

    private void ObserveCampaignBossNoise(TacticalEvent e)
    {
        if (State.CampaignBosses is not { Arenas.Count: > 0 } store || e.Source == 0 || !_world.State.Party.Contains(e.Source)
            || e.Kind is not ("AttackPerformed" or "CampAlarm" or "DoorOpened" or "TerrainBroken" or "CampFireLit" or "BellRung" or "BossValveDrained")) return;
        foreach (var arena in store.Arenas)
        {
            if (arena.Kind != CampaignBossKind.Bellkeeper || !arena.Contains(e.Position)) continue;
            var boss = Actor(arena.Boss);
            if (boss.Life != LifeState.Alive || !CanHear(e.Position, boss.Position, arena.FinalRole ? 20 : 16)) continue;
            var memory = boss.Tactics!.Enemy!.CampaignBoss!;
            memory.LastNoise = e.Position; memory.NoiseAt = Clock; memory.NoiseAction = e.Action;
        }
    }

    // Called after canonical actor damage, preserving the pre-break obstruction of that same attack.
    private void ResolveCampaignBossObjects(LockedAttack attack)
    {
        if (attack.Damage <= 0 || State.CampaignBosses is not { Arenas.Count: > 0 }) return;
        var cuts = new List<(CampaignBossArena Arena, CampaignBossFixture Fixture)>();
        foreach (var p in attack.Cells)
        {
            var arena = State.CampaignBosses?.Arenas.FirstOrDefault(a => a.Contains(p));
            var fixture = arena?.Fixtures.FirstOrDefault(f => f.Position == p && f.Health > 0 && f.Kind != CampaignBossFixtureKind.Valve);
            if (fixture != null && (attack.Source != arena!.Boss || attack.Element == DamageElement.Fire)) cuts.Add((arena!, fixture));
            bool blocked = !ProjectileOpen(p);
            if (attack.Delivery is AttackDelivery.Projectile or AttackDelivery.PiercingLine && blocked) break;
            if (attack.Delivery == AttackDelivery.Projectile && _world.OccupantAt(p, attack.Source) != null) break;
        }
        foreach (var (arena, fixture) in cuts)
        {
            if (fixture.Health <= 0) continue;
            if (attack.Element == DamageElement.Fire && fixture.Kind is CampaignBossFixtureKind.WebAnchor or CampaignBossFixtureKind.WebStrand)
                IgniteCampaignWeb(arena, fixture, attack);
            fixture.Health = Math.Max(0, fixture.Health - attack.Damage);
            Emit("BossFixtureDamaged", attack.Source, arena.Boss, attack.Id, fixture.Position, attack.Damage, ability: fixture.Kind.ToString());
            if (fixture.Health > 0) continue;
            BreakCampaignBossFixture(arena, fixture, attack.Source, attack.Id);
        }
        foreach (var arena in cuts.Select(c => c.Arena).Distinct()) CompactBossFixtures(arena);
    }
    private void BreakCampaignBossFixture(CampaignBossArena arena, CampaignBossFixture fixture, long source, long action)
    {
        Emit("BossFixtureBroken", source, arena.Boss, action, fixture.Position, ability: fixture.Kind.ToString());
        if (fixture.Kind == CampaignBossFixtureKind.WebAnchor)
            foreach (var strand in arena.Fixtures.Where(f => f.Group == fixture.Group)) strand.Health = 0;
        if (fixture.Kind == CampaignBossFixtureKind.ReelLine
            && Actor(arena.Boss) is { Life: LifeState.Alive } reeler
            && State.Attacks.Any(a => a.Source == reeler.Id && a.Id == fixture.Group && a.Boss?.Move == CampaignBossMove.ReelIn))
            RecordCombatControl(source, reeler, ActionTiming.Recovery(reeler.Tactics!.MoveCost, EffectiveSpeed(reeler)));
        if (fixture.Kind == CampaignBossFixtureKind.ReelLine)
            foreach (var line in arena.Fixtures.Where(f => f.Group == fixture.Group && f.Kind == CampaignBossFixtureKind.ReelLine)) line.Health = 0;
        if (fixture.Kind != CampaignBossFixtureKind.Resonator) return;
        CancelCampaignPulses(p => p.Arena == arena.Id && p.Kind == CampaignBossPulseKind.Chime && p.Fixture == fixture.Id);
        var boss = Actor(arena.Boss); if (boss.Life != LifeState.Alive) return;
        int duration = arena.FinalRole ? 350 : 450;
        long before = CampaignBossReadyAt(boss);
        bool interrupted = State.Attacks.Any(a => a.Source == boss.Id && a.Boss?.Move == CampaignBossMove.FuneralChime);
        if (interrupted) CancelAttacks(boss.Id, source, action, "broken chorus");
        var ready = State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == boss.Id && w.Kind == ScheduledKind.ActorReady);
        if (ready != null) State.Queue.Delay(ready.Sequence, duration);
        else if (State.Attacks.FindIndex(a => a.Source == boss.Id) is >= 0 and var index)
        {
            var pending = State.Attacks[index]; State.Attacks[index] = pending with { RecoveryCost = checked(pending.RecoveryCost!.Value + duration) };
        }
        else if (boss.Id != State.ActiveActor) ScheduleReady(boss, duration);
        RecordCombatControl(source, boss, Math.Max(CampaignBossReadyAt(boss) - before,
            interrupted ? ActionTiming.Recovery(boss.Tactics!.MoveCost, EffectiveSpeed(boss)) : 0));
        ExposeCampaignBoss(boss, action, duration);
        Emit("BossChorusBroken", source, boss.Id, action, fixture.Position, duration);
    }
    private long CampaignBossReadyAt(Actor boss)
    {
        if (State.Attacks.FirstOrDefault(a => a.Source == boss.Id && a.RecoveryCost != null) is { } attack)
            return checked(attack.Due + ActionTiming.Recovery(attack.RecoveryCost!.Value, EffectiveSpeed(boss)));
        return State.Queue.Snapshot().Pending.FirstOrDefault(w => w.ActorId == boss.Id && w.Kind == ScheduledKind.ActorReady)?.Due ?? Clock;
    }

    private void IgniteCampaignWeb(CampaignBossArena arena, CampaignBossFixture start, LockedAttack attack)
    {
        var network = arena.Fixtures.Where(f => f.Health > 0 && f.Kind is CampaignBossFixtureKind.WebAnchor or CampaignBossFixtureKind.WebStrand).ToArray();
        var distance = new Dictionary<long, int> { [start.Id] = 0 }; var queue = new Queue<CampaignBossFixture>(); queue.Enqueue(start);
        while (queue.TryDequeue(out var fixture))
        {
            if (!BossStore.Pulses.Any(p => p.Arena == arena.Id && p.Kind == CampaignBossPulseKind.BurningWeb && p.Fixture == fixture.Id))
            {
                var cells = new[] { fixture.Position }.Concat(new[] { Facing.North, Facing.East, Facing.South, Facing.West }.Select(f =>
                { var (x, y) = AttackGeometry.Offset(f); return fixture.Position.Offset(x, y); })).Where(p => BossCell(arena, p)).ToArray();
                QueueCampaignBossPulse(arena, CampaignBossPulseKind.BurningWeb, fixture.Position, cells,
                    checked(Clock + 100 + distance[fixture.Id] * 75), arena.FinalRole ? 4 : 2, fixture.Id, attack.Source,
                    _cause ?? attack.Causality ?? RootCause(attack.Id, attack.ClassGenerated));
            }
            foreach (var next in network)
                if (!distance.ContainsKey(next.Id) && (fixture.Position.Distance(next.Position) == 1 || next.Group == fixture.Group && fixture.Kind == CampaignBossFixtureKind.WebAnchor))
                { distance.Add(next.Id, distance[fixture.Id] + 1); queue.Enqueue(next); }
        }
        Emit("BossWebIgnited", attack.Source, arena.Boss, attack.Id, start.Position, distance.Count);
    }

    private void QueueCampaignBossPulse(CampaignBossArena arena, CampaignBossPulseKind kind, WorldPosition origin,
        WorldPosition[] cells, long due, int damage, long fixture, long? source = null, BoonCause? causality = null)
    {
        if (cells.Length == 0 || BossStore.Pulses.Count >= 2048) return;
        long id = State.NextAction++, owner = source ?? arena.Boss;
        long schedule = State.Queue.Schedule(ScheduledKind.CampaignBossPulse, due, owner, referenceId: id).Sequence;
        BossStore.Pulses.Add(new(id, arena.Id, owner, kind, origin, cells, due, schedule, damage, fixture, causality));
    }
    private void CancelCampaignPulses(Func<CampaignBossPulse, bool> predicate)
    {
        if (State.CampaignBosses is not { } store) return;
        foreach (var pulse in store.Pulses.Where(predicate).ToArray())
        { State.Queue.Cancel(pulse.Schedule); BossStore.Pulses.Remove(pulse); }
    }
    private void PruneCampaignBosses()
    {
        if (State.CampaignBosses is not { Arenas.Count: > 0 } store) return;
        CancelCampaignPulses(p => p.Kind == CampaignBossPulseKind.Chime &&
            (Actor(p.Source).Life != LifeState.Alive || BossStore.Arenas.Single(a => a.Id == p.Arena).Fixtures.All(f => f.Id != p.Fixture || f.Health <= 0)));
        foreach (var arena in BossStore.Arenas)
        {
            foreach (var line in arena.Fixtures.Where(f => f.Kind == CampaignBossFixtureKind.ReelLine && f.Health > 0
                && !State.Attacks.Any(a => a.Source == arena.Boss && a.Id == f.Group))) line.Health = 0;
            CompactBossFixtures(arena);
        }
    }
    private void ResolveCampaignBossPulse(ScheduledAction work)
    {
        var pulse = BossStore.Pulses.Single(p => p.Schedule == work.Sequence);
        BossStore.Pulses.Remove(pulse);
        var arena = BossStore.Arenas.Single(a => a.Id == pulse.Arena);
        ResolveAtomically(pulse.Causality ?? RootCause(pulse.Id), () =>
        {
            if (pulse.Kind == CampaignBossPulseKind.BurningWeb)
            {
                var fixture = arena.Fixtures.FirstOrDefault(f => f.Id == pulse.Fixture);
                if (fixture is { Health: > 0 }) { fixture.Health = 0; BreakCampaignBossFixture(arena, fixture, pulse.Source, pulse.Id); }
            }
            ResolveDamageBatch(new(pulse.Id, pulse.Source, pulse.Origin, pulse.Cells, pulse.Kind == CampaignBossPulseKind.Chime ? "core:ability/boss_chime_ring" : "core:ability/boss_burning_network",
                AttackDelivery.Area, pulse.Kind == CampaignBossPulseKind.Chime ? DamageElement.Resonance : DamageElement.Fire,
                pulse.Damage, pulse.Due, Cause: DamageCause.GroundHazard, ClassGenerated: pulse.Causality?.ClassGenerated == true,
                HitsOwner: pulse.Kind == CampaignBossPulseKind.BurningWeb, PersistsWithoutOwner: pulse.Kind == CampaignBossPulseKind.BurningWeb));
            Emit(pulse.Kind == CampaignBossPulseKind.Chime ? "BossChimeRing" : "BossWebBurned", pulse.Source, 0, pulse.Id, pulse.Origin, pulse.Damage);
            CompactBossFixtures(arena);
        });
    }
    private IEnumerable<LockedAttack> CampaignBossWarnings() => State.CampaignBosses is not { } store ? [] : store.Pulses.Select(p =>
        new LockedAttack(p.Id, p.Source, p.Origin, p.Cells.ToArray(), p.Kind == CampaignBossPulseKind.Chime ? "core:ability/boss_chime_ring" : "core:ability/boss_burning_network",
            AttackDelivery.Area, p.Kind == CampaignBossPulseKind.Chime ? DamageElement.Resonance : DamageElement.Fire, p.Damage, p.Due, Cause: DamageCause.GroundHazard));

    public string CampaignBossDescription(long bossId)
    {
        var boss = Actor(bossId); if (boss.Tactics?.Enemy?.CampaignBoss is not { } memory) return "";
        var arena = BossArena(boss);
        string variation = (arena.Kind, arena.Variation) switch
        {
            (CampaignBossKind.Bellkeeper, 0) => "Eastern choir", (CampaignBossKind.Bellkeeper, 1) => "Southern choir", (CampaignBossKind.Bellkeeper, _) => "Western choir",
            (CampaignBossKind.SluiceMaw, 0) => "Eastflow channels", (CampaignBossKind.SluiceMaw, 1) => "Southflow channels", (CampaignBossKind.SluiceMaw, _) => "Westflow channels",
            (CampaignBossKind.Threadmother, 0) => "Eastern partitions", (CampaignBossKind.Threadmother, 1) => "Northern partitions", (CampaignBossKind.Threadmother, _) => "Western partitions",
            (_, 0) => "Southern shield", (_, 1) => "Western shield", _ => "Northern shield"
        };
        string mechanics = arena.Kind switch
        {
            CampaignBossKind.Bellkeeper => "Hears recent loud party attacks, doors and alarms through attenuating walls; marks that exact noise tile for Soundseek. Breaking resonators cancels their expanding chime rings and grants an extended exposure window.",
            CampaignBossKind.SluiceMaw => "Surges lock marked channel tiles and push with the visible flow. Operate nearby valves for one action to open dry routes and deny the announced connected-pool feast. Draining exposes it to 50% extra damage.",
            CampaignBossKind.Threadmother => "Hunts downed or weakened prey. Partition webs block movement and shots but retain a walking gap; cut an anchor to clear its partition. Evade the marked Reel In tile or cut its visible line. Fire spreads as delayed visible hazards and can hit allies.",
            _ => $"Shield faces {memory.ShieldFacing}; front and front diagonals reduce damage by {(arena.FinalRole ? 90 : 80)}%. Turns and the long recovery after a locked forward charge expose flanks. Break bone ramparts or use their central gap and open ends."
        };
        var pending = State.Attacks.FirstOrDefault(a => a.Source == bossId && a.Boss != null);
        string warning = pending?.Boss is { } cast ? CampaignBossCatalog.MoveName(cast.Move)
            + (cast.Feature >= 0 ? " / channel #" + (cast.Feature + 1) : "") + $" in {Math.Max(0, pending.Due - Clock) / 100d:0.00}s. " : "";
        if (pending?.Boss?.Move == CampaignBossMove.ReservoirFeast) warning += "Drain that pool's numbered valve before impact to deny healing. ";
        string priority = arena.Kind == CampaignBossKind.Threadmother
            ? "Downed prey first, then lowest HP percentage; nearer prey breaks ties. "
            : arena.Kind == CampaignBossKind.Bellkeeper ? "Recent sound first; otherwise nearby standing threats before downed prey. "
            : "Nearby standing threats before downed prey. ";
        return warning + priority + mechanics + (arena.FinalRole ? " Final guardian: greater health, power and denser mechanics. " : " Major guardian: fixed world role. ")
            + variation + "." + (memory.ExposedUntil > Clock ? $" Exposed for {(memory.ExposedUntil - Clock) / 100d:0.00}s: takes 50% extra damage." : "");
    }
}
