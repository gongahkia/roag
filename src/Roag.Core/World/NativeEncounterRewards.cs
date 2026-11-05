using Roag.Core.Content;
using Roag.Core.Tactics;

namespace Roag.Core.World;

/// <summary>Contributions from defeated group members survive retreat until their source settles.</summary>
public sealed class NativeEncounterRewards
{
    public Dictionary<long, EnemyContributions> Pending { get; set; } = [];

    internal void Validate(WorldState world, IReadOnlyDictionary<long, Actor> actors)
    {
        if (Pending == null || Pending.Count > 10000 || world.RewardedEncounterIds == null || world.RewardedEncounterIds.Count > 10000)
            throw new InvalidDataException("Invalid native encounter reward records.");
        foreach (var (id, ledger) in Pending)
        {
            if (!actors.TryGetValue(id, out var source) || Source(source) != id || source.Tactics is not { RewardExempt: false, Enemy.Retained: not null }
                || world.RewardedEncounterIds.Contains(id)
                || !actors.Values.Any(a => Source(a) == id && a.Life == LifeState.Alive)
                || !actors.Values.Any(a => Source(a) == id && a is { Life: LifeState.Dead, Tactics.RewardsClaimed: true })
                || ledger == null || ledger.Members == null || ledger.Members.Count == 0
                || ledger.UnhealedDamage == null || ledger.UnhealedDamage.Count != 0
                || ledger.Members.Any(p => !world.Party.Contains(p.Key) || !actors.TryGetValue(p.Key, out var member)
                    || member.Tactics?.Build == null || p.Value == null || p.Value.Damage < 0 || p.Value.Healing < 0
                    || p.Value.Protection < 0 || p.Value.Control < 0 || p.Value.Weight(world.Tactics!.Rewards.Tuning) <= 0))
                throw new InvalidDataException("Invalid pending native encounter contribution.");
        }
    }

    internal static long Source(Actor actor) => actor.EncounterLeader > 0 ? actor.EncounterLeader : actor.Id;
}

internal sealed record NativeEncounterSettlement(long Source, string Reward, int Experience, EnemyContributions Contributions);

public sealed partial class WorldSession
{
    public static bool IsEncounterRewardCache(LootCache cache) => cache.SourceActor > 0 && cache.Id == "native-encounter-" + cache.SourceActor;

    internal NativeEncounterSettlement? SettleNativeEncounterDeath(Actor enemy, EnemyContributions? contribution)
    {
        long id = NativeEncounterRewards.Source(enemy);
        if (State.RewardedEncounterIds.Contains(id)) return null;
        var pending = State.NativeCampaign!.EncounterRewards.Pending;
        var source = ActorById(id);
        if (source.Tactics is not { RewardExempt: false, Enemy.Retained: not null }) { pending.Remove(id); return null; }
        var aggregate = pending.GetValueOrDefault(id) ?? new EnemyContributions();
        Merge(contribution);
        if (source.Life != LifeState.Dead || Content.Entries[source.Archetype] is not BossDefinition
            && State.Actors.Any(a => NativeEncounterRewards.Source(a) == id && a.Life != LifeState.Dead))
        {
            if (aggregate.Members.Count > 0) pending[id] = aggregate;
            return null;
        }

        // A simultaneous batch may have killed other members whose queued kill
        // events have not dispatched yet. Living boss attendants also helped
        // define this encounter's work; include their actual current credit.
        foreach (var member in State.Actors.Where(a => a.Id != enemy.Id && NativeEncounterRewards.Source(a) == id))
            if (State.Tactics!.Rewards.Enemies.Remove(member.Id, out var ledger)) Merge(ledger);
        pending.Remove(id);
        var reward = RewardFor(source);
        var contents = new Dictionary<string, int>(StringComparer.Ordinal);
        AddStack("gold", reward.Gold); AddStack("food", reward.Food);
        foreach (var (item, count) in reward.Supplies) AddStack(Content.Get<ItemDefinition>(item).Stack, count);
        if (reward.Signature is { } signature) AddEquipment(signature, 0);
        var random = WorldRandom.Stream(Seed, unchecked((int)id), unchecked((int)(id >> 32)), 7201);
        if (reward.EquipmentPool.Length > 0 && random.Next(100) < reward.EquipmentChancePercent)
            AddEquipment(reward.EquipmentPool[random.Next(reward.EquipmentPool.Length)], 1);
        if (contents.Count > 0)
            State.Loot.Add(new() { Id = "native-encounter-" + id, SourceActor = id, Position = source.Position, Contents = contents });
        State.RewardedEncounterIds.Add(id);
        Diagnose("native-encounter-reward", new { source = id, reward = reward.Id, reward.Experience, contents });
        return new(id, reward.Id, reward.Experience, aggregate);

        void Merge(EnemyContributions? ledger)
        {
            if (ledger == null) return;
            foreach (var (member, value) in ledger.Members)
            {
                if (!aggregate.Members.TryGetValue(member, out var total)) aggregate.Members.Add(member, total = new());
                total.Damage = checked(total.Damage + value.Damage); total.Healing = checked(total.Healing + value.Healing);
                total.Protection = checked(total.Protection + value.Protection); total.Control = checked(total.Control + value.Control);
            }
        }
        void AddStack(string stack, int count)
        { if (count > 0) contents[stack] = checked(contents.GetValueOrDefault(stack) + count); }
        void AddEquipment(string basis, int choice)
        { string item = EncounterEquipment(basis, id, choice); AddStack(Item(item).Stack, 1); }
    }

    private static void ValidateNativeEncounterCaches(WorldPlan plan, WorldState world, ContentCatalog content)
    {
        foreach (var cache in world.Loot.Where(IsEncounterRewardCache))
        {
            var source = world.Actors.FirstOrDefault(a => a.Id == cache.SourceActor);
            if (world.NativeCampaign == null || source is not { Life: LifeState.Dead, Tactics: { RewardExempt: false, Enemy.Retained: not null } }
                || !world.RewardedEncounterIds.Contains(source.Id) || NativeEncounterRewards.Source(source) != source.Id
                || cache.Position != source.Position || cache.Choices.Length != 0)
                throw new InvalidDataException("Invalid native encounter spoils identity.");
        }
        if (world.NativeCampaign == null) return;
        foreach (long id in world.RewardedEncounterIds)
        {
            var source = world.Actors.FirstOrDefault(a => a.Id == id)
                ?? throw new InvalidDataException("Invalid native encounter reward source.");
            if (source.Tactics?.Enemy?.Retained == null) continue;
            var reward = RewardFor(content, source); var expected = new Dictionary<string, int>(StringComparer.Ordinal);
            Add("gold", reward.Gold); Add("food", reward.Food);
            foreach (var (item, count) in reward.Supplies) Add(content.Get<ItemDefinition>(item).Stack, count);
            if (reward.Signature is { } signature) Equipment(signature, 0);
            var random = WorldRandom.Stream(plan.Seed, unchecked((int)id), unchecked((int)(id >> 32)), 7201);
            if (reward.EquipmentPool.Length > 0 && random.Next(100) < reward.EquipmentChancePercent)
                Equipment(reward.EquipmentPool[random.Next(reward.EquipmentPool.Length)], 1);
            var cache = world.Loot.FirstOrDefault(c => c.Id == "native-encounter-" + id);
            if (expected.Count == 0 ? cache != null : cache == null || expected.Count != cache.Contents.Count
                || expected.Any(p => cache.Contents.GetValueOrDefault(p.Key) != p.Value))
                throw new InvalidDataException("Native encounter spoils differ from the authored reward.");

            void Add(string stack, int count)
            { if (count > 0) expected[stack] = checked(expected.GetValueOrDefault(stack) + count); }
            void Equipment(string basis, int choice)
            {
                var roll = RollEncounterEquipment(content, plan.Seed, basis, id, choice);
                if (roll == null) { Add(content.Get<ItemDefinition>(basis).Stack, 1); return; }
                string item = $"gear.7200.{id}.{choice}";
                if (!world.GeneratedEquipment.TryGetValue(item, out var saved) || saved.Base != roll.Base || !saved.Affixes.SequenceEqual(roll.Affixes))
                    throw new InvalidDataException("Native encounter equipment differs from its seeded source.");
                Add(item, 1);
            }
        }
    }
}
