using Roag.Core.World;

namespace Roag.Core.Tactics;

internal static class CampaignBossValidation
{
    internal static bool MoveFits(CampaignBossKind kind, CampaignBossMove move) => kind switch
    {
        CampaignBossKind.Bellkeeper => move is CampaignBossMove.Soundseek or CampaignBossMove.FuneralChime,
        CampaignBossKind.SluiceMaw => move is CampaignBossMove.ChannelSurge or CampaignBossMove.ReservoirFeast,
        CampaignBossKind.Threadmother => move is CampaignBossMove.PartitionWeb or CampaignBossMove.ReelIn,
        CampaignBossKind.OssuaryCaptain => move is CampaignBossMove.ShieldAdvance or CampaignBossMove.BoneRampart,
        _ => false
    };
    internal static bool GeometryFits(CampaignBossArena arena, CampaignBossCast cast, WorldPosition origin, IReadOnlyList<WorldPosition> cells)
    {
        if (!MoveFits(arena.Kind, cast.Move) || !Enum.IsDefined(cast.Facing) || !AttackGeometry.Cardinal(cast.Facing)
            || cells.Any(p => !arena.Contains(p)) || cast.Feature < -1
            || arena.Kind == CampaignBossKind.SluiceMaw && (cast.Feature < 0 || cast.Feature >= arena.Channels.Count)
            || arena.Kind != CampaignBossKind.SluiceMaw && cast.Feature != -1) return false;
        return cast.Move switch
        {
            CampaignBossMove.Soundseek => cells.Count is >= 1 and <= 9 && (arena.FinalRole || cells.Count == 1)
                && cells.All(p => Math.Max(Math.Abs(p.X - cells[0].X), Math.Abs(p.Y - cells[0].Y)) <= 1),
            CampaignBossMove.ReservoirFeast => cells.SequenceEqual(new[] { arena.Channels[cast.Feature].Pool }),
            CampaignBossMove.ChannelSurge => cells.SequenceEqual(arena.Channels[cast.Feature].Cells),
            CampaignBossMove.ReelIn => cells.Count == 1,
            CampaignBossMove.ShieldAdvance => cells.Count >= 1 && cells.Count <= (arena.FinalRole ? 5 : 4)
                && cells.Select((p, i) => { var (x,y) = AttackGeometry.Offset(cast.Facing); return p == origin.Offset(x * (i + 1), y * (i + 1)); }).All(valid => valid),
            CampaignBossMove.PartitionWeb => cells.Count <= (arena.FinalRole ? 16 : 8)
                && cells.All(p => Math.Max(Math.Abs(p.X - arena.Center.X), Math.Abs(p.Y - arena.Center.Y)) <= 4),
            CampaignBossMove.BoneRampart => cells.Count <= 4
                && cells.All(p => Math.Max(Math.Abs(p.X - arena.Center.X), Math.Abs(p.Y - arena.Center.Y)) <= 2),
            _ => cells.Count <= (arena.FinalRole ? 24 : 16)
        };
    }

    internal static void Validate(WorldPlan plan, WorldState world)
    {
        var state = world.Tactics!; var store = state.CampaignBosses;
        if (store == null)
        {
            if (world.Actors.Any(a => a.Tactics?.Enemy?.CampaignBoss != null) || state.Attacks.Any(a => a.Boss != null)
                || state.Queue.Snapshot().Pending.Any(w => w.Kind == ScheduledKind.CampaignBossPulse))
                throw new InvalidDataException("Campaign boss work has no owning registry.");
            return;
        }
        if (store.NextArena < 1 || store.NextArena > 1000 || store.Arenas == null || store.Arenas.Count > 32
            || store.Pulses == null || store.Pulses.Count > 2048 || store.Arenas.Any(a => a == null)
            || store.Arenas.Select(a => a.Id).Distinct().Count() != store.Arenas.Count
            || store.Arenas.Select(a => a.Boss).Distinct().Count() != store.Arenas.Count
            || store.Pulses.Any(p => p == null) || store.Pulses.Select(p => p.Id).Distinct().Count() != store.Pulses.Count)
            throw new InvalidDataException("Invalid campaign boss registry.");
        var actors = world.Actors.ToDictionary(a => a.Id); long clock = state.Queue.Clock;
        foreach (var arena in store.Arenas)
        {
            if (arena.Id <= 0 || arena.Id >= store.NextArena || !Enum.IsDefined(arena.Kind) || arena.Variation is < 0 or > 2
                || !plan.Contains(arena.Center.Offset(-7, -7)) || !plan.Contains(arena.Center.Offset(7, 7))
                || !actors.TryGetValue(arena.Boss, out var boss) || boss.Tactics?.Enemy?.CampaignBoss is not { } memory
                || memory.Arena != arena.Id || boss.Tactics.DefinitionId != CampaignBossCatalog.Id(arena.Kind)
                || boss.Tactics.RegularEnemy || !boss.Tactics.ImmuneToPull || boss.Tactics.Invulnerable || boss.Tactics.Enemy.Retained != null
                || boss.Tactics.Enemy.PhaseTwo || boss.Position.Floor != arena.Center.Floor || world.Party.Contains(boss.Id)
                || memory.LastNoise is { } noise && (!arena.Contains(noise) || memory.NoiseAt > clock || memory.NoiseAction >= state.NextAction)
                || memory.ExposedUntil > clock + 10_000
                || arena.Fixtures == null || arena.Fixtures.Count > 64 || arena.Channels == null || arena.NextFixture <= 0
                || arena.Fixtures.Any(f => f == null) || arena.Fixtures.Select(f => f.Id).Distinct().Count() != arena.Fixtures.Count
                || arena.Fixtures.Select(f => f.Position).Distinct().Count() != arena.Fixtures.Count)
                throw new InvalidDataException("Invalid campaign boss arena, identity or memory.");
            var definition = CampaignBossCatalog.Definition(arena.Kind, arena.FinalRole, arena.Variation);
            if (boss.Tactics.InnateStats != definition.CreateTactics().InnateStats || boss.Tactics.MoveCost != definition.MoveCost
                || boss.Tactics.ExperienceReward != definition.XpReward || boss.Tactics.CurrencyDrop != definition.CurrencyDrop
                || boss.Tactics.RewardExempt || boss.Tactics.HealthDrop || boss.Tactics.FoodDrop)
                throw new InvalidDataException("Campaign boss world-role stats or reward identity changed.");
            foreach (var fixture in arena.Fixtures)
            {
                bool validRole = arena.Kind switch
                {
                    CampaignBossKind.Bellkeeper => fixture.Kind == CampaignBossFixtureKind.Resonator,
                    CampaignBossKind.SluiceMaw => fixture.Kind == CampaignBossFixtureKind.Valve,
                    CampaignBossKind.Threadmother => fixture.Kind is CampaignBossFixtureKind.WebAnchor or CampaignBossFixtureKind.WebStrand or CampaignBossFixtureKind.ReelLine,
                    _ => fixture.Kind == CampaignBossFixtureKind.BoneRampart
                };
                if (fixture.Id <= 0 || fixture.Id >= arena.NextFixture || !validRole || !Enum.IsDefined(fixture.Kind)
                    || !arena.Contains(fixture.Position) || fixture.Health < 0 || fixture.Health > (arena.FinalRole ? 8 : 5)
                    || fixture.Group < 0 || fixture.Group >= state.NextAction
                    || fixture.Kind == CampaignBossFixtureKind.Valve && (fixture.Health != 1 || fixture.Channel < 0 || fixture.Channel >= arena.Channels.Count)
                    || fixture.Kind != CampaignBossFixtureKind.Valve && fixture.Channel != -1
                    || fixture.Kind is CampaignBossFixtureKind.Resonator or CampaignBossFixtureKind.Valve && fixture.Group != 0
                    || fixture.Kind is not (CampaignBossFixtureKind.Resonator or CampaignBossFixtureKind.Valve) && (fixture.Group == 0 || fixture.Health == 0)
                    || fixture.Kind == CampaignBossFixtureKind.ReelLine && !state.Attacks.Any(a => a.Source == boss.Id && a.Id == fixture.Group && a.Boss?.Move == CampaignBossMove.ReelIn))
                    throw new InvalidDataException("Invalid campaign boss physical fixture.");
            }
            if (arena.Kind == CampaignBossKind.Bellkeeper && arena.Fixtures.Count != (arena.FinalRole ? 3 : 2)
                || arena.Kind == CampaignBossKind.SluiceMaw && (arena.Channels.Count != (arena.FinalRole ? 3 : 2)
                    || arena.Fixtures.Count != arena.Channels.Count || arena.Fixtures.Select(f => f.Channel).Distinct().Count() != arena.Channels.Count)
                || arena.Kind != CampaignBossKind.SluiceMaw && arena.Channels.Count != 0)
                throw new InvalidDataException("Invalid campaign boss arena features.");
            foreach (var channel in arena.Channels)
                if (channel == null || channel.Cells == null || channel.Cells.Length is < 11 or > 25
                    || channel.Cells.Distinct().Count() != channel.Cells.Length || channel.Cells.Any(p => !arena.Contains(p))
                    || !channel.Cells.Contains(channel.Pool) || !Enum.IsDefined(channel.Flow) || !AttackGeometry.Cardinal(channel.Flow))
                    throw new InvalidDataException("Invalid local cistern channel geometry.");
        }
        foreach (var boss in actors.Values.Where(a => a.Tactics?.Enemy?.CampaignBoss != null))
            if (!store.Arenas.Any(a => a.Boss == boss.Id && a.Id == boss.Tactics!.Enemy!.CampaignBoss!.Arena))
                throw new InvalidDataException("Campaign boss has no owning arena.");
        var queue = state.Queue.Snapshot().Pending;
        foreach (var attack in state.Attacks)
        {
            bool owned = actors.GetValueOrDefault(attack.Source)?.Tactics?.Enemy?.CampaignBoss != null;
            if (owned != (attack.Boss != null)) throw new InvalidDataException("Missing or foreign campaign boss commitment.");
            if (attack.Boss is not { } cast) continue;
            var arena = store.Arenas.Single(a => a.Boss == attack.Source);
            if (!Enum.IsDefined(cast.Move) || attack.Cells == null || !GeometryFits(arena, cast, attack.Origin, attack.Cells)
                || attack.Ability != "core:ability/boss_" + cast.Move.ToString().ToLowerInvariant()
                || attack.Delivery != AttackDelivery.Area || attack.Element != DamageElement.Physical || attack.Cause != DamageCause.DirectAttack
                || attack.RecoveryCost == null || attack.Support != null || attack.Advance != null || attack.Shove != null
                || attack.Reflection != null || attack.SentinelFacing != null || attack.EmberLanding != null || attack.RetainedAbility
                || attack.ClassGenerated || attack.PersistsWithoutOwner || attack.HitsOwner || attack.Cells == null
                || attack.Cells.Any(p => !arena.Contains(p)) || cast.Feature < -1
                || arena.Kind == CampaignBossKind.SluiceMaw && (cast.Feature < 0 || cast.Feature >= arena.Channels.Count)
                || arena.Kind != CampaignBossKind.SluiceMaw && cast.Feature != -1
                || cast.Move is CampaignBossMove.Soundseek or CampaignBossMove.ReelIn && attack.Cells.Length == 0
                || cast.Move == CampaignBossMove.ReelIn && attack.Cells.Length != 1
                || cast.Move == CampaignBossMove.ReservoirFeast && !attack.Cells.SequenceEqual(new[] { arena.Channels[cast.Feature].Pool })
                || cast.Move == CampaignBossMove.ChannelSurge && !attack.Cells.SequenceEqual(arena.Channels[cast.Feature].Cells)
                || attack.Damage != (cast.Move is CampaignBossMove.ReservoirFeast or CampaignBossMove.PartitionWeb or CampaignBossMove.BoneRampart or CampaignBossMove.FuneralChime ? 0 : actors[attack.Source].Power))
                throw new InvalidDataException("Invalid saved campaign boss attack.");
        }
        foreach (var pulse in store.Pulses)
        {
            var arena = store.Arenas.FirstOrDefault(a => a.Id == pulse.Arena);
            if (arena == null || pulse.Id <= 0 || pulse.Id >= state.NextAction || !actors.ContainsKey(pulse.Source)
                || !state.Participants.Contains(pulse.Source) || !Enum.IsDefined(pulse.Kind)
                || !arena.Contains(pulse.Origin) || pulse.Cells == null || pulse.Cells.Length is < 1 or > 40
                || pulse.Cells.Distinct().Count() != pulse.Cells.Length || pulse.Cells.Any(p => !arena.Contains(p))
                || pulse.Due < clock || pulse.Due - clock > 5000 || pulse.Damage is < 1 or > 20 || pulse.Fixture <= 0 || pulse.Fixture >= arena.NextFixture
                || pulse.Kind == CampaignBossPulseKind.Chime && (arena.Kind != CampaignBossKind.Bellkeeper || pulse.Source != arena.Boss
                    || actors[pulse.Source].Life != LifeState.Alive || !arena.Fixtures.Any(f => f.Id == pulse.Fixture && f.Kind == CampaignBossFixtureKind.Resonator && f.Health > 0))
                || pulse.Kind == CampaignBossPulseKind.BurningWeb && arena.Kind != CampaignBossKind.Threadmother
                || queue.Count(w => Matches(w, pulse)) != 1 || state.Attacks.Any(a => a.Id == pulse.Id))
                throw new InvalidDataException("Invalid campaign boss pulse or schedule.");
        }
        if (queue.Any(w => w.Kind == ScheduledKind.CampaignBossPulse && !store.Pulses.Any(p => Matches(w, p))))
            throw new InvalidDataException("Orphaned campaign boss pulse.");
    }
    internal static bool Matches(ScheduledAction work, CampaignBossPulse pulse) => work.Kind == ScheduledKind.CampaignBossPulse
        && work.Sequence == pulse.Schedule && work.ActorId == pulse.Source && work.ReferenceId == pulse.Id && work.Due == pulse.Due && !work.PlayerControlled;
}
