using Roag.Core.Content;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

public sealed record CounterplayPartyState(int Health, int MaxHealth, int Mana, int MaxMana, int Stress, int Conscious, int Downed, int Dead);
public sealed record CounterplayTrial(string Strategy, CounterplayPartyState Before, CounterplayPartyState After,
    int Commands, int Actions, int Rounds, string Outcome, int PreparedImpactDamage, int EnemyHealing,
    bool SuccessfulCommands, bool ReachedBoundary, string[] Decisions, CombatEvent[] Events)
{
    public bool Survived => After.Conscious > 0;
    public int HealthLost => Before.Health - After.Health;
    public int ManaSpent => Before.Mana - After.Mana;
    public int StressGained => After.Stress - Before.Stress;
}
public sealed record CounterplayComparison(string Question, string Setup, CounterplayTrial Baseline, CounterplayTrial Counterplay);
public sealed record EncounterCounterplayReport(int Seed, CounterplayComparison[] Comparisons, string[] Limitations)
{
    public bool Passed => Comparisons.Length == 3 && Comparisons.All(pair => pair.Baseline.SuccessfulCommands && pair.Counterplay.SuccessfulCommands
            && pair.Baseline.ReachedBoundary && pair.Counterplay.ReachedBoundary)
        && Comparisons[0].Baseline.PreparedImpactDamage > 0 && Comparisons[0].Counterplay.PreparedImpactDamage == 0
        && Comparisons[0].Counterplay.Events.Any(e => e.Label == "Preparation broken")
        && Comparisons[1].Baseline.PreparedImpactDamage > Comparisons[1].Counterplay.PreparedImpactDamage;
}

/// <summary>Matched actual-resolver experiments. Mechanism checks are distinct from observed policy outcomes.</summary>
public static class EncounterCounterplayAudit
{
    public static EncounterCounterplayReport Run(ContentCatalog catalog, int seed)
    {
        const string chargeSetup = "Four healthy companions with controlled initiative; channeler HP160 and bowman HP100 keep the first charge observable. Stop after the channeler's second-round slot. Same seed and setup within each pair.";
        return new(seed,
        [
            new("Does a timely stun prevent a prepared attack?", chargeSetup,
                Trial(catalog, seed, "allow-charge"), Trial(catalog, seed, "interrupt-charge")),
            new("How much does guarding reduce the prepared impact?", chargeSetup,
                Trial(catalog, seed, "allow-charge"), Trial(catalog, seed, "guard-charge")),
            new("What changes when the healer is targeted first?",
                "Road wardens use authored health and stats, with controlled initiative. The Ranger and Warden use basic attacks while both casters guard for at most eight rounds; only target priority changes.",
                Trial(catalog, seed, "bruiser-first"), Trial(catalog, seed, "healer-first"))
        ],
        [
            "These controlled fixtures exercise combat mechanics, not world travel, recruitment costs or a complete optimal strategy.",
            "The charge fixtures deliberately use durable enemies. The healer comparison fixes initiative and has two casters guard, so four attackers cannot erase the bruiser before support acts.",
            "Changed actions consume different combat rolls. Every trial starts from the same seed; variation after the decision is part of the resolver's actual continuation.",
            "Passed checks bounded successful commands, stun interruption and guard mitigation. Healer-priority results are measurements; neither policy is required to win or outperform the other."
        ]);
    }

    private static CounterplayTrial Trial(ContentCatalog catalog, int seed, string strategy)
    {
        bool support = strategy is "healer-first" or "bruiser-first";
        var world = WorldSession.Create(seed, catalog);
        var fixture = EncounterGroupPreview.Prepare(world, support ? "encounter.road-wardens" : "encounter.cinder-ambush");
        var battle = world.State.Battle!; int leader = battle.ActorIds.IndexOf(fixture.Leader);
        if (support)
        {
            foreach (var unit in battle.Units.Where(u => !u.Friendly))
            {
                var actor = world.State.Actors.Single(a => a.Id == battle.ActorIds[unit.Id]);
                var definition = catalog.Get<CharacterDefinition>(actor.Archetype);
                actor.BaseHealth = definition.Health; actor.Health = actor.MaxHealth;
                unit.MaxHealth = actor.MaxHealth; unit.Health = actor.Health;
            }
        }
        CounterplayPartyState Snapshot() => new(world.Party.Sum(a => a.Health), world.Party.Sum(a => a.MaxHealth),
            world.Party.Sum(a => a.Mana), world.Party.Sum(a => a.MaxMana), world.Party.Sum(a => a.Stress),
            world.Party.Count(a => a.Life == LifeState.Alive), world.Party.Count(a => a.Life == LifeState.Downed), world.Party.Count(a => a.Life == LifeState.Dead));
        var before = Snapshot(); var events = new List<CombatEvent>(world.State.CombatEvents); var decisions = new List<string>();
        int commands = 0, round = battle.Round; bool successful = true, boundary = false;
        long sequence = events.LastOrDefault()?.Sequence ?? 0;
        for (; commands < 96;)
        {
            if (world.State.Battle is not { } current) { boundary = true; break; }
            round = current.Round;
            if (support ? round > 8 : round > 2 || round == 2 && current.Order.IndexOf(leader) < current.TurnIndex)
            { round = Math.Min(round, support ? 8 : 2); boundary = true; break; }
            ActionResult result;
            if (!current.Active.Friendly) result = world.AdvanceEnemy();
            else
            {
                int target = current.Units.First(u => !u.Friendly && u.Alive).Id;
                if (support)
                {
                    bool prioritizeLeader = strategy == "healer-first";
                    target = current.Units.Where(u => !u.Friendly && u.Alive)
                        .OrderBy(u => (u.Id == leader) == prioritizeLeader ? 0 : 1).ThenBy(u => u.Id).First().Id;
                }
                int skill = 0;
                if (support && world.State.Actors.Single(a => a.Id == current.ActorIds[current.Active.Id]).Role is 2 or 3) skill = 3;
                if (strategy == "guard-charge") skill = 3;
                if (strategy == "interrupt-charge" && current.Prepared.ContainsKey(leader))
                {
                    int stun = world.ActiveSkills().Select((ability, index) => (ability, index)).Where(p => p.ability.Effect == "stun")
                        .Select(p => p.index).DefaultIfEmpty(-1).First();
                    if (stun >= 0 && world.ActionPreview(stun, leader).Reason.Length == 0) { skill = stun; target = leader; }
                }
                result = world.Act(skill, target);
            }
            commands++; decisions.Add(result.Message);
            var added = world.State.CombatEvents.Where(e => e.Sequence > sequence).ToArray(); events.AddRange(added);
            if (added.Length > 0) sequence = added[^1].Sequence;
            if (!result.Success) { successful = false; break; }
        }
        if (world.State.Battle == null) boundary = true;
        string outcome = world.State.AwaitingContinuation ? "party-wipe" : world.State.LastEncounterReward != null ? "victory" : boundary ? "bounded-observation" : "command-limit";
        var foes = fixture.Members.ToHashSet();
        return new(strategy, before, Snapshot(), commands, commands + events.Count(e => e.Kind == "skip" && e.Label == "Stunned"), round, outcome,
            events.Where(e => e.Kind == "damage" && e.Source == fixture.Leader && e.Label == "Cinder surge").Sum(e => e.Amount),
            events.Where(e => e.Kind == "heal" && foes.Contains(e.Source) && foes.Contains(e.Target)).Sum(e => e.Amount),
            successful, boundary, decisions.ToArray(), events.ToArray());
    }
}
