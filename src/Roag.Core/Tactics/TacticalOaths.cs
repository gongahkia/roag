using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record OathDamage(int Given, int Received, bool SharedOnly);

public static class EnemyOaths
{
    public const string A = "core:enemy/oath_twin_a", B = "core:enemy/oath_twin_b";
    public const int Radius = 4;
    public static bool IsTwin(string definition) => definition is A or B;

    internal static bool Reaches(Actor first, Actor second, Func<WorldPosition, bool> open) => first.Life == LifeState.Alive
        && second.Life == LifeState.Alive && first.Health > 0 && second.Health > 0
        && first.Position.Floor == second.Position.Floor && first.Position.Distance(second.Position) <= Radius
        && EnemySquads.Clear(first.Position, second.Position, open) && EnemySquads.Clear(second.Position, first.Position, open);

    internal static void Validate(WorldPlan plan, WorldState world, IReadOnlyDictionary<long, Actor> actors, ContentCatalog? content)
    {
        var twins = actors.Values.Where(a => IsTwin(a.Tactics?.DefinitionId ?? "")).ToArray();
        if (twins.Length == 0) return;
        content ??= ContentCatalog.Default;
        using var terrain = new ChunkCache(plan, world, content);
        foreach (var twin in twins)
        {
            var memory = twin.Tactics!.Enemy;
            if (memory == null || memory.OathPartner <= 0 || memory.OathPartner >= world.NextActorId || memory.OathPartner == twin.Id)
                throw new InvalidDataException("Invalid oath owner or partner identity.");
            var partner = actors.GetValueOrDefault(memory.OathPartner);
            if (partner != null && (partner.Tactics?.Enemy == null || !IsTwin(partner.Tactics.DefinitionId)
                || partner.Tactics.DefinitionId == twin.Tactics.DefinitionId || partner.Tactics.Enemy.OathPartner != twin.Id
                || partner.Tactics.Enemy.OathBroken != memory.OathBroken))
                throw new InvalidDataException("Invalid mutual oath pair.");
            if (!memory.OathBroken && (partner == null || !Reaches(twin, partner,
                p => WorldSession.SnapshotProjectileOpen(plan, world, content, terrain, p) && !WorldSession.SnapshotDungeonMistBlocks(world, terrain, p))))
                throw new InvalidDataException("Invalid intact oath.");
        }
    }
}

public sealed partial class TacticalCombat
{
    /// <summary>Population setup links two registered, unpaired twins. A broken bond cannot be replaced.</summary>
    public void LinkOathTwins(long first, long second)
    {
        var a = Actor(first); var b = Actor(second);
        if (a.Id == b.Id || a.Tactics?.Enemy is not { OathPartner: 0, OathBroken: false } am
            || b.Tactics?.Enemy is not { OathPartner: 0, OathBroken: false } bm
            || !EnemyOaths.IsTwin(a.Tactics.DefinitionId) || !EnemyOaths.IsTwin(b.Tactics.DefinitionId)
            || a.Tactics.DefinitionId == b.Tactics.DefinitionId || !State.Participants.Contains(a.Id) || !State.Participants.Contains(b.Id)
            || !EnemyOaths.Reaches(a, b, SupportOpen))
            throw new ArgumentException("Link two unpaired opposite Oath Twins within a clear four-cell bond.");
        am.OathPartner = b.Id; bm.OathPartner = a.Id;
    }

    private Actor? OathPartner(Actor actor)
    {
        if (actor.Tactics is not { Enemy: { OathBroken: false } memory } || !EnemyOaths.IsTwin(actor.Tactics.DefinitionId)) return null;
        var partner = _world.State.Actors.FirstOrDefault(a => a.Id == memory.OathPartner);
        return partner?.Tactics?.Enemy is { OathBroken: false } other && other.OathPartner == actor.Id
            && EnemyOaths.IsTwin(partner.Tactics.DefinitionId) && partner.Tactics.DefinitionId != actor.Tactics.DefinitionId
            && EnemyOaths.Reaches(actor, partner, SupportOpen) ? partner : null;
    }

    private void PruneOaths()
    {
        foreach (var actor in _world.State.Actors)
            if (actor.Tactics?.Enemy is { OathBroken: false } memory && EnemyOaths.IsTwin(actor.Tactics.DefinitionId) && OathPartner(actor) == null)
            {
                memory.OathBroken = true;
                var partner = _world.State.Actors.FirstOrDefault(a => a.Id == memory.OathPartner);
                if (partner?.Tactics?.Enemy is { } other && EnemyOaths.IsTwin(partner.Tactics.DefinitionId)) other.OathBroken = true;
            }
    }

    private void RedistributeOathDamage(List<DamageOutcome> hits, LockedAttack attack)
    {
        if (!hits.Any(h => EnemyOaths.IsTwin(h.Target.Tactics?.DefinitionId ?? ""))) return;
        var received = new Dictionary<long, (Actor Actor, int Amount, BoonEvent Template)>();
        int originalCount = hits.Count;
        for (int i = 0; i < originalCount; i++)
        {
            var hit = hits[i];
            if (hit.Snapshot.OathSharedOnly || attack.FateFace == 7 && RegularBoonTarget(hit.Target)
                || OathPartner(hit.Target) is not { } partner) continue;
            int transfer = hit.Snapshot.ActualLoss!.Value / 2;
            if (transfer == 0) continue;
            hits[i] = hit with { Snapshot = hit.Snapshot with { ActualLoss = hit.Snapshot.ActualLoss - transfer, OathGiven = transfer } };
            if (received.TryGetValue(partner.Id, out var prior)) received[partner.Id] = prior with { Amount = checked(prior.Amount + transfer) };
            else received.Add(partner.Id, (partner, transfer, hit.Snapshot));
        }
        foreach (var (id, receipt) in received.OrderBy(p => p.Key))
        {
            int index = hits.FindIndex(h => h.Target.Id == id);
            if (index >= 0)
            {
                var hit = hits[index];
                hits[index] = hit with { Damage = Math.Max(hit.Damage, receipt.Amount), Snapshot = hit.Snapshot with
                { ActualLoss = Math.Min(hit.Snapshot.HpBefore!.Value, checked(hit.Snapshot.ActualLoss!.Value + receipt.Amount)), OathReceived = receipt.Amount } };
                continue;
            }
            var actor = receipt.Actor;
            var snapshot = receipt.Template with
            {
                TargetId = actor.Id, TargetDefinitionId = actor.Tactics!.DefinitionId, TargetFaction = BoonFaction(actor),
                TargetTags = [actor.Tactics.DefinitionId, BoonFaction(actor)], Position = actor.Position,
                HpBefore = actor.Health, TargetMaxHp = actor.MaxHealth, ActualLoss = Math.Min(actor.Health, receipt.Amount),
                Amount = receipt.Amount, DamageType = DamageElement.Oath, AbilityId = "core:ability/oath_share",
                DirectAttack = false, ProcCoefficient = 0, OathSharedOnly = true, OathGiven = 0, OathReceived = receipt.Amount,
                RewardExempt = actor.Tactics.RewardExempt, TargetBurning = actor.Tactics.Burning != null,
                AdjacentHostileCount = null, AdjacentBurningCount = null
            };
            hits.Add(new(actor, receipt.Amount, 0, snapshot, false, null));
        }
    }
}
