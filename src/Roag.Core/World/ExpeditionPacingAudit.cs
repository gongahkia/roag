using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record PacingBody(long Id, string Name, string Class, string Life, int Health, int MaximumHealth, int Mana, int Wounds, double Hunger, double Fatigue);
public sealed record PacingPoint(int X, int Y, int Floor);
public sealed record PacingSnapshot(long Tick, int Steps, PacingPoint Position, int Food, int Medicine, int Gold, PacingBody[] Party);
public sealed record PacingLeg(string Goal, string Subject, int InitialDistance, int RemainingDistance, int Steps, string Outcome, string Detail, PacingSnapshot Before, PacingSnapshot After);
public sealed record PacingFight(string[] Enemies, int Actions, int Rounds, string Outcome, EncounterReward? Reward, PacingSnapshot Before, PacingSnapshot After);
public sealed record PacingRecovery(string Action, bool Success, string Detail, PacingSnapshot Before, PacingSnapshot After);
public sealed record PacingWorld(int Seed, string StartingClass, int RecruitmentOffersAtStart, int NearestDiscoveryDistance, int NearestDungeonDistance, int LeadershipReversals,
    PacingLeg[] Legs, PacingFight[] Fights, PacingRecovery[] Recovery, PacingSnapshot Final, string[] CoverageGaps, string[] InvariantErrors);
public sealed record ExpeditionPacingReport(bool Passed, string ContentHash, int StepBudget, string Scope, string[] Assumptions, PacingWorld[] Worlds, string[] Findings);

/// <summary>Bounded, reproducible observations of untouched new worlds, not an autonomous playtest or balance verdict.</summary>
public static class ExpeditionPacingAudit
{
    public static ExpeditionPacingReport Run(ContentCatalog catalog, IReadOnlyList<int> seeds, int stepBudget = 900)
    {
        ArgumentNullException.ThrowIfNull(catalog); ArgumentNullException.ThrowIfNull(seeds);
        if (seeds.Count is < 1 or > 16 || seeds.Distinct().Count() != seeds.Count) throw new ArgumentException("Audit 1..16 distinct explicit seeds.", nameof(seeds));
        if (stepBudget is < 1 or > 5000) throw new ArgumentOutOfRangeException(nameof(stepBudget), "Use a movement budget of 1..5000 tiles per world.");
        var worlds = seeds.Select(seed => new Pilot(WorldSession.Create(seed, catalog), stepBudget).Run()).ToArray();
        var findings = new List<string>();
        foreach (var world in worlds)
            foreach (string gap in world.CoverageGaps) findings.Add($"Seed {world.Seed}: {gap}");
        var rewards = worlds.SelectMany(w => w.Fights).Where(f => f.Reward != null).Select(f => f.Reward!).ToArray();
        if (rewards.Length > 1 && rewards.Select(r => (r.Gold, r.Food, r.Experience)).Distinct().Count() == 1)
            findings.Add($"All {rewards.Length} observed victories awarded the same gold/food/experience tuple. This sample does not establish that all encounter rewards are identical.");
        return new(worlds.All(w => w.InvariantErrors.Length == 0), catalog.Hash, stepBudget,
            "Fresh-world command audit. Passed means observed state invariants held; separate coverage gaps show unmet goals. No player saves, teleportation, stat edits, terrain edits or granted supplies. This is not human play, perceived pacing or proof of global reachability.",
            ["Coordinates select nearest generated goals, including undiscovered goals; local routing uses only observed terrain and actors.",
             "A 25 by 19 surface view follows the leader. Movement advances the real simulation by 140 milliseconds per successful tile; decisions and menus take no simulated time.",
             "Recruit up to three nearby inhabitants, visit a local landmark, seek an ordinary local encounter, then approach the nearest dungeon stairs. Failed navigation is a bounded controller observation, not a proof that the player is trapped.",
             "Combat uses the first class ability against the lowest-health living enemy, without equipment changes, development choices or tactical optimisation. At most four encounters and 200 actions per encounter.",
             "Blocked party corridors may use the real LeadParty command. Recovery uses owned medicine and returns to an observed camp where available. No automatic shopping, harvesting, construction or tunnelling."],
            worlds, findings.ToArray());
    }

    private sealed class Pilot(WorldSession world, int budget)
    {
        private readonly List<PacingLeg> _legs = [];
        private readonly List<PacingFight> _fights = [];
        private readonly List<PacingRecovery> _recovery = [];
        private readonly List<string> _errors = [];
        private readonly Dictionary<WorldPosition, int> _visits = [];
        private readonly HashSet<WorldPosition> _knownCamps = [];
        private readonly HashSet<(long Leader, WorldPosition Position)> _reversals = [];
        private int _steps;
        private int _leadershipReversals;
        private string _halt = "";
        private long _lastTick;
        private static readonly TimeSpan StepTime = TimeSpan.FromMilliseconds(140);

        public PacingWorld Run()
        {
            world.Paused = false; world.PartyTravelling = true; Observe();
            string startingClass = world.Class(world.Party.Single()).Id;
            var origin = world.State.Position;
            var recruits = world.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited && a.HomeTown == 0
                && world.Interactions(a.Id).Any(i => i.Action == "recruit")).OrderBy(a => a.Position.Distance(origin)).ThenBy(a => a.Id).ToArray();
            int discoveryDistance = world.State.Discoveries.Min(d => d.Position.Distance(origin));
            var site = world.Plan.Sites.OrderBy(s => s.Center.Distance(origin)).ThenBy(s => s.Id).First();
            foreach (var actor in recruits.Take(3))
            {
                Walk("recruit", actor.Name, () => actor.Position, 2, Math.Min(80, budget - _steps));
                if (world.State.Position.Distance(actor.Position) <= 2 && CanAct)
                {
                    var interaction = world.Interactions(actor.Id).First(i => i.Action == "recruit");
                    var result = world.PerformInteraction(actor.Id, interaction.Id);
                    RecordAction(result.Success ? "recruited" : "recruitment refused", result.Message);
                }
            }
            var discovery = world.State.Discoveries.OrderBy(d => d.Position.Distance(world.State.Position)).ThenBy(d => d.Id, StringComparer.Ordinal).First();
            Walk("discovery", discovery.Name, () => discovery.Position, 1, Math.Min(220, budget - _steps));
            if (CanAct && world.State.Position.Distance(discovery.Position) <= 1)
            {
                var result = world.Submit(new(WorldCommandKind.Discover, discovery.Position));
                RecordAction(result.Success ? "visited" : "discovery refused", result.Message);
                foreach (var camp in world.Visible.Where(p => world.Chunks.At(p) == Material.Camp)) _knownCamps.Add(camp);
            }
            var encounter = world.State.Discoveries.Where(d => d.Enemies.Length > 0 && d.TownId == 0)
                .OrderBy(d => d.Position.Distance(world.State.Position)).ThenBy(d => d.Id, StringComparer.Ordinal).FirstOrDefault();
            if (_fights.Count == 0 && encounter != null)
            {
                var enemy = world.State.Actors.First(a => a.Id == encounter.Enemies[0]);
                Walk("ordinary encounter", encounter.Name, () => enemy.Position, 2, Math.Min(260, budget - _steps));
                if (CanAct && enemy.Life == LifeState.Alive && world.Visible.Contains(enemy.Position) && world.State.Position.Distance(enemy.Position) <= 2)
                { world.Submit(new(WorldCommandKind.Interact, ActorId: enemy.Id)); Fight(); }
            }
            Recover();
            // Stairs are fixed at local (34,32) in a 64x64 site footprint.
            var stair = site.Center.Offset(2, 0);
            Walk("dungeon", site.Name, () => stair, 1, budget - _steps);
            if (CanAct && world.State.Position.Distance(stair) <= 1)
            {
                var result = world.Submit(new(WorldCommandKind.Interact, stair));
                RecordAction(result.Success && world.State.Position.Floor > 0 ? "entered dungeon" : "entrance refused", result.Message);
            }
            Validate();
            var gaps = new List<string>();
            if (world.State.Party.Count < 4) gaps.Add($"Only {world.State.Party.Count} of four party slots recruited.");
            if (!world.State.Discoveries.Any(d => d.Visited)) gaps.Add("No landmark visit completed.");
            if (_fights.Count == 0) gaps.Add("No encounter reached; fight/reward/recovery pacing remains unobserved.");
            if (_fights.Count > 0 && !_recovery.Any(r => r.Success)) gaps.Add("No recovery action completed after combat.");
            if (world.State.Position.Floor == 0) gaps.Add("No dungeon entry completed within the route policy and movement budget.");
            if (_halt.Length > 0) gaps.Add(_halt);
            foreach (var leg in _legs.Where(l => l.Outcome is "blocked" or "budget exhausted")) gaps.Add($"{leg.Goal}: {leg.Detail}");
            return new(world.Seed, startingClass, recruits.Length, discoveryDistance, site.Center.Distance(origin), _leadershipReversals, _legs.ToArray(), _fights.ToArray(),
                _recovery.ToArray(), Snapshot(), gaps.Distinct().ToArray(), _errors.Distinct().ToArray());
        }

        private bool CanAct => _halt.Length == 0 && !world.IsInCombat && !world.State.AwaitingContinuation && !world.DeathPending && !world.State.Endgame.Victory;
        private void Observe()
        {
            var p = world.State.Position;
            if (p.Floor == 0) world.RevealSurface(p.X - 12, p.Y - 9, p.X + 13, p.Y + 10);
            else world.Reveal();
        }
        private PacingSnapshot Snapshot() => new(world.State.Tick, _steps, new(world.State.Position.X, world.State.Position.Y, world.State.Position.Floor), world.State.Inventory.GetValueOrDefault("food"),
            world.State.Inventory.GetValueOrDefault("medicine"), world.State.Inventory.GetValueOrDefault("gold"),
            world.Party.Select(a => new PacingBody(a.Id, a.Name, world.Class(a).Id, a.Life.ToString(), a.Health, a.MaxHealth, a.Mana, a.Body.Wounds,
                Math.Round(a.Body.Hunger, 3), Math.Round(a.Body.Fatigue, 3))).ToArray());
        private void RecordAction(string outcome, string detail)
        {
            var previous = _legs[^1]; _legs[^1] = previous with { Outcome = outcome, Detail = detail, After = Snapshot() };
        }
        private void Walk(string goal, string subject, Func<WorldPosition> target, int range, int allowance)
        {
            var before = Snapshot(); int distance = target().Distance(world.State.Position), limit = Math.Min(budget, _steps + Math.Max(0, allowance));
            int attempts = 0; string detail = ""; var terrain = new Dictionary<WorldPosition, bool>();
            while (CanAct && _steps < limit && target().Distance(world.State.Position) > range && attempts++ < Math.Max(24, allowance * 3))
            {
                Observe(); var route = Route(target(), range, terrain);
                if (route.Count == 0)
                {
                    if (world.State.Party.Count > 1 && _reversals.Count < 16 && _reversals.Add((world.State.Party[0], world.State.Position)))
                    {
                        var tail = world.Party.Last(a => a.Life == LifeState.Alive);
                        if (tail.Id != world.State.Party[0] && world.Submit(new(WorldCommandKind.LeadParty, ActorId: tail.Id)).Success) { _leadershipReversals++; Observe(); continue; }
                    }
                    detail = "No observed local route found after available leadership reversal; the controller does not clear terrain or move through companions."; break;
                }
                foreach (var next in route)
                {
                    if (_steps >= limit || !CanAct || target().Distance(world.State.Position) <= range) break;
                    var moved = world.Submit(new(WorldCommandKind.Move, next));
                    if (!moved.Success) { detail = moved.Message; terrain.Clear(); world.AdvanceFrame(StepTime); break; }
                    _steps++; _visits[next] = _visits.GetValueOrDefault(next) + 1;
                    world.AdvanceFrame(StepTime); Observe(); Validate();
                    if (world.IsInCombat) { Fight(); terrain.Clear(); break; }
                }
            }
            string outcome = target().Distance(world.State.Position) <= range ? "reached" : !CanAct ? "interrupted" : _steps >= limit ? "budget exhausted" : "blocked";
            if (detail.Length == 0) detail = outcome switch { "reached" => "Reached through real movement.", "budget exhausted" => "Movement allowance exhausted before reaching this goal.", "interrupted" => _halt.Length > 0 ? _halt : "The expedition cannot continue.", _ => "Bounded local navigation stopped without reaching the goal." };
            _legs.Add(new(goal, subject, distance, target().Distance(world.State.Position), _steps - before.Steps, outcome, detail, before, Snapshot()));
        }
        private List<WorldPosition> Route(WorldPosition target, int range, Dictionary<WorldPosition, bool> terrain)
        {
            var start = world.State.Position;
            var occupied = world.State.Actors.Where(a => a.Life != LifeState.Dead && a.Id != world.State.Party[0] && world.Visible.Contains(a.Position)).Select(a => a.Position).ToHashSet();
            var previous = new Dictionary<WorldPosition, WorldPosition> { [start] = start };
            var distance = new Dictionary<WorldPosition, int> { [start] = 0 };
            var queue = new Queue<WorldPosition>(); queue.Enqueue(start);
            WorldPosition? best = null; int bestScore = int.MaxValue;
            while (queue.TryDequeue(out var current) && previous.Count < 2400)
            {
                if (current != start)
                {
                    int score = current.Distance(target) * 5 + distance[current] + _visits.GetValueOrDefault(current) * 15;
                    if (current.Distance(target) <= range) { best = current; break; }
                    if (score < bestScore) { best = current; bestScore = score; }
                }
                foreach (var (x, y) in WorldSession.Directions)
                {
                    var next = current.Offset(x, y);
                    if (previous.ContainsKey(next) || next.Distance(start) > 30 || occupied.Contains(next) || !world.Chunks.Seen(next)) continue;
                    if (!terrain.TryGetValue(next, out bool open)) terrain[next] = open = world.GroundOpen(next);
                    if (!open) continue;
                    previous[next] = current; distance[next] = distance[current] + 1; queue.Enqueue(next);
                }
            }
            if (best == null) return [];
            var result = new List<WorldPosition>(); var p = best.Value;
            while (p != start) { result.Add(p); p = previous[p]; }
            result.Reverse(); return result;
        }
        private void Fight()
        {
            if (world.State.Battle is not { } battle) return;
            if (_fights.Count >= 4) { _halt = "Four-encounter audit limit reached; combat left unresolved."; return; }
            var before = Snapshot(); var enemies = battle.ActorIds.Where((_, i) => !battle.Units[i].Friendly).Select(id => world.State.Actors.Single(a => a.Id == id).Archetype).ToArray();
            int actions = 0, round = battle.Round; string refusal = "";
            while (world.State.Battle is { } current && actions < 200)
            {
                round = Math.Max(round, current.Round);
                var result = current.Active.Friendly ? world.Act(0, current.Units.Where(u => !u.Friendly && u.Alive).OrderBy(u => u.Health).ThenBy(u => u.Id).First().Id) : world.AdvanceEnemy();
                if (!result.Success) { refusal = result.Message; break; }
                actions++; Validate();
            }
            string outcome = world.IsInCombat ? "unresolved" : world.State.AwaitingContinuation ? "party wipe" : "victory";
            if (world.IsInCombat) _halt = refusal.Length > 0 ? "Baseline combat policy refused: " + refusal : "The 200-action combat bound was reached.";
            _fights.Add(new(enemies, actions, round, outcome, outcome == "victory" ? world.State.LastEncounterReward : null, before, Snapshot()));
        }
        private void Recover()
        {
            if (_fights.Count == 0 || !CanAct) return;
            foreach (var actor in world.Party.Where(a => a.Life == LifeState.Downed).ToArray())
            {
                var before = Snapshot(); var result = world.Submit(new(WorldCommandKind.Revive, ActorId: actor.Id));
                _recovery.Add(new("revive", result.Success, result.Message, before, Snapshot()));
            }
            Observe(); foreach (var observed in world.Visible.Where(p => world.Chunks.At(p) == Material.Camp)) _knownCamps.Add(observed);
            var camps = _knownCamps.Where(p => world.Chunks.Seen(p) && world.Chunks.At(p) == Material.Camp).OrderBy(p => p.Distance(world.State.Position)).ThenBy(p => p.X).ThenBy(p => p.Y).ToArray();
            if (camps.Length == 0)
            {
                var snapshot = Snapshot(); _recovery.Add(new("camp", false, "No serviceable camp has been observed; the audit does not teleport or grant a camp.", snapshot, snapshot)); return;
            }
            var camp = camps[0]; Walk("recovery camp", "Previously observed camp", () => camp, 1, Math.Min(160, budget - _steps));
            if (!CanAct) return;
            var campBefore = Snapshot(); var rested = world.Submit(new(WorldCommandKind.Camp));
            _recovery.Add(new("camp", rested.Success, rested.Message, campBefore, Snapshot()));
            if (world.IsInCombat) Fight();
        }
        private void Validate()
        {
            if (world.State.Tick < _lastTick) _errors.Add("World clock moved backwards."); _lastTick = world.State.Tick;
            if (world.State.Inventory.Values.Any(v => v < 0)) _errors.Add("Inventory became negative.");
            var party = world.Party.Where(a => a.Life != LifeState.Dead).ToArray();
            if (party.Select(a => a.Position).Distinct().Count() != party.Length) _errors.Add("Party members share a tile.");
            if (party.Any(a => !world.Plan.Contains(a.Position) || a.Health < 0 || a.Health > a.MaxHealth || a.Mana < 0 || a.Mana > a.MaxMana)) _errors.Add("A party member has invalid position or resources.");
        }
    }
}
