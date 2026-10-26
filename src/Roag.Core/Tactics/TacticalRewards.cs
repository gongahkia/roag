using System.Numerics;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private bool RewardEnemy(Actor enemy) => !Friendly(enemy) && _world.IsHostile(enemy)
        && enemy.Tactics is { RewardsClaimed: false, RewardExempt: false };
    private bool Contributor(Actor actor) => Friendly(actor) && actor.Tactics?.Build != null;

    private EnemyContributions Ledger(Actor enemy)
    {
        if (!State.Rewards.Enemies.TryGetValue(enemy.Id, out var ledger))
            State.Rewards.Enemies.Add(enemy.Id, ledger = new());
        return ledger;
    }

    private static CombatContribution Contribution(EnemyContributions ledger, long member)
    {
        if (!ledger.Members.TryGetValue(member, out var contribution))
            ledger.Members.Add(member, contribution = new());
        return contribution;
    }

    private void RecordCombatDamage(Actor source, Actor target, int loss, int prevented, int healthBefore)
    {
        if (loss > 0 && Contributor(source) && RewardEnemy(target))
        {
            var contribution = Contribution(Ledger(target), source.Id);
            contribution.Damage = checked(contribution.Damage + loss);
        }
        if (!Contributor(target) || !RewardEnemy(source) || healthBefore <= 0) return;
        int protection = Math.Min(prevented, healthBefore - loss);
        if (loss <= 0 && protection <= 0) return;
        var ledger = Ledger(source);
        if (loss > 0) ledger.UnhealedDamage[target.Id] = checked(ledger.UnhealedDamage.GetValueOrDefault(target.Id) + loss);
        if (protection > 0)
        {
            // Existing Guard, parry and boon wards are self-owned. Ally-owned shielding
            // must supply its actual owner when those effects are connected.
            var contribution = Contribution(ledger, target.Id);
            contribution.Protection = checked(contribution.Protection + protection);
        }
    }

    internal void RecordCombatHealing(long source, long recipient, int actualGain)
    {
        if (actualGain <= 0) return;
        var debts = State.Rewards.Enemies.Where(p => p.Value.UnhealedDamage.ContainsKey(recipient)).ToArray();
        BigInteger total = debts.Aggregate(BigInteger.Zero, (sum, p) => sum + p.Value.UnhealedDamage[recipient]);
        long restored = (long)BigInteger.Min(actualGain, total);
        var shares = ContributionRewards.Allocate(restored,
            debts.Select(p => (p.Key, (BigInteger)p.Value.UnhealedDamage[recipient])));
        bool credit = source != 0 && Contributor(Actor(source));
        foreach (var (enemy, amount) in shares)
        {
            if (amount == 0) continue;
            var ledger = State.Rewards.Enemies[enemy];
            ledger.UnhealedDamage[recipient] -= amount;
            if (ledger.UnhealedDamage[recipient] == 0) ledger.UnhealedDamage.Remove(recipient);
            if (!credit) continue;
            var contribution = Contribution(ledger, source);
            contribution.Healing = checked(contribution.Healing + amount);
        }
    }

    private void RecordCombatControl(long source, Actor enemy, long units)
    {
        if (units <= 0 || source == 0 || !Contributor(Actor(source)) || !RewardEnemy(enemy)) return;
        var contribution = Contribution(Ledger(enemy), source);
        contribution.Control = checked(contribution.Control + units);
    }

    private void ResolveKillRewards(BoonEvent e)
    {
        if (e.Kind != BoonTrigger.EntityKilled || e.TargetId is not { } id) return;
        var enemy = Actor(id);
        if (enemy.Tactics is not { } tactics || tactics.RewardsClaimed) return;
        bool eligibleEnemy = !Friendly(enemy) && !tactics.RewardExempt;
        tactics.RewardsClaimed = true;
        State.Rewards.Enemies.Remove(id, out var ledger);
        if (!eligibleEnemy) return;
        if (ledger != null && tactics.ExperienceReward > 0) AwardKillExperience(e, tactics, ledger, id);
        SpawnKillLoot(e, enemy);
    }

    private void AwardKillExperience(BoonEvent e, ActorTactics tactics, EnemyContributions ledger, long id)
    {
        // Removing dead members before normalizing redistributes their entitlement.
        // Downed members remain eligible, including for level-up stat growth.
        var shares = ContributionRewards.Allocate((long)tactics.ExperienceReward * ContributionRewards.UnitsPerExperience,
            ledger.Members.Where(p => Actor(p.Key).Life != LifeState.Dead)
                .Select(p => (p.Key, p.Value.Weight(State.Rewards.Tuning))));
        foreach (var (member, share) in shares)
        {
            if (share == 0) continue;
            var actor = Actor(member);
            var buildState = actor.Tactics!;
            long units = checked(share + buildState.ExperienceFraction);
            int whole = checked((int)(units / ContributionRewards.UnitsPerExperience));
            buildState.ExperienceFraction = (int)(units % ContributionRewards.UnitsPerExperience);
            Emit("ExperienceGained", member, id, e.RootActionId ?? 0, actor.Position, whole);
            if (whole > 0)
            {
                var advance = buildState.Build!.GainExperience(whole);
                buildState.Build = advance.Build;
                if (actor.Life == LifeState.Alive)
                    actor.Health = Math.Min(actor.MaxHealth, checked(actor.Health + advance.Growth.Sum(g => g.MaxHealthIncrease)));
                foreach (var growth in advance.Growth)
                {
                    Emit("LevelUp", member, member, e.RootActionId ?? 0, actor.Position, growth.Level, ability: growth.Stat.ToString());
                    int increase = growth.Stat switch
                    {
                        GrowthStat.MaxHealth => growth.MaxHealthIncrease,
                        GrowthStat.Damage => growth.After.Damage - growth.Before.Damage,
                        GrowthStat.Defense => growth.After.Defense - growth.Before.Defense,
                        GrowthStat.Speed => growth.After.Speed - growth.Before.Speed,
                        GrowthStat.CriticalChance => growth.After.CriticalChancePartsPerMillion - growth.Before.CriticalChancePartsPerMillion,
                        GrowthStat.CriticalDamage => growth.After.CriticalDamagePercent - growth.Before.CriticalDamagePercent,
                        _ => throw new InvalidOperationException("Unknown permanent stat growth.")
                    };
                    Emit("StatIncreased", member, member, e.RootActionId ?? 0, actor.Position, increase, ability: growth.Stat.ToString());
                }
            }
        }
    }
}
