using System.Runtime.InteropServices;

namespace Ithica.Core.Colony;

public sealed partial class Simulation
{
    private static readonly (string Name, string Role, string Trait)[] Profiles = [
        ("Mika","Miner","Restless"),("Sol","Botanist","Optimist"),("Inez","Medic","Steadfast"),
        ("Jae","Miner","Night owl"),("Noor","Engineer","Stargazer"),("Paz","Engineer","Resourceful"),
        ("Ren","Botanist","Patient"),("Val","Medic","Gentle"),("Ash","Miner","Tenacious"),("Uma","Engineer","Curious")];
    private static readonly string[] MiningActivities = Enum.GetValues<Terrain>().Select(t => "Mining " + TerrainName(t).ToLowerInvariant()).ToArray();
    private static readonly string[] BuildingActivities = Blueprint.All.Select(b => "Building " + b.Name.ToLowerInvariant()).ToArray();

    public Survivor? SpawnSurvivor()
    {
        int start = World.Index(World.Width / 2 + 9, World.Height / 2), cx = World.Width / 2, cy = World.Height / 2;
        // Reuse the power-network flood queue. This operation does not run concurrently with UpdatePower.
        var seen = new HashSet<int> { start };
        int head = 0, tail = 0;
        _networkQueue[tail++] = start;
        var candidates = new List<int>();
        Span<int> neighbors = stackalloc int[4];
        while (head < tail)
        {
            int i = _networkQueue[head++];
            if (World.Distance(start, i) > 90)
            {
                continue;
            }

            if ((Math.Abs(World.X(i) - cx) >= 11 || Math.Abs(World.Y(i) - cy) >= 7) && World.Tiles[i] == Terrain.Vacuum && SurvivorAt(i) is null)
            {
                candidates.Add(i);
            }

            int n = World.Neighbors(i, neighbors);
            for (int j = 0; j < n; j++)
            {
                if (World.Walkable(neighbors[j]) && seen.Add(neighbors[j]))
                {
                    _networkQueue[tail++] = neighbors[j];
                }
            }
        }
        if (candidates.Count == 0)
        {
            Event("A distant distress signal fades behind impassable terrain.", NoticeLevel.Warning);
            return null;
        }
        var used = new HashSet<string>(Crew.Select(c => c.Name).Concat(Survivors.Select(s => s.Name)));
        var profile = Profiles.FirstOrDefault(p => !used.Contains(p.Name));
        if (profile.Name is null)
        {
            profile = ("Wanderer " + (Survivors.Count + 1), "Miner", "Survivor");
        }

        (int dx, int dy) = (Survivors.Count % 5) switch
        {
            0 => (14, 2),
            1 => (13, -11),
            2 => (15, 8),
            3 => (-12, 8),
            _ => (-13, -8)
        };
        int target = World.Index(Math.Clamp(cx + dx, 0, World.Width - 1), Math.Clamp(cy + dy, 0, World.Height - 1));
        candidates.Sort((a, b) => { int result = World.Distance(a, target).CompareTo(World.Distance(b, target)); return result != 0 ? result : a.CompareTo(b); });
        int chosen = candidates[Survivors.Count == 0 ? 0 : Random.Next(0, Math.Min(5, candidates.Count))];
        var survivor = new Survivor { Id = Survivors.Count, Name = profile.Name, Role = profile.Role, Trait = profile.Trait, Cell = chosen };
        Survivors.Add(survivor);
        World.Reveal(chosen, 1);
        Event($"DISTRESS / {survivor.Name}, {survivor.Role.ToLowerInvariant()}, stranded at {World.X(chosen)},{World.Y(chosen)}. Click to send help.", NoticeLevel.Warning);
        return survivor;
    }

    public Result Dispatch(int survivorId, int? crewId = null)
    {
        if ((uint)survivorId >= Survivors.Count || Survivors[survivorId].State != ContactState.Stranded)
        {
            return new(false, "This contact is no longer awaiting rescue.");
        }

        if (ExpeditionFor(survivorId) is not null)
        {
            return new(false, "A rescue is already on its way.");
        }

        if (GameOver)
        {
            return new(false, "There is nobody left to send.");
        }

        if (!Resources.Covers(RescueCost))
        {
            return new(false, "A rescue kit requires 2 food and 2 water.");
        }

        var survivor = Survivors[survivorId];
        Colonist? best = null;
        List<int>? bestPath = null;
        var route = new List<int>();
        Span<int> target = stackalloc int[1];
        target[0] = survivor.Cell;
        foreach (var c in Crew)
        {
            if (!c.Alive || c.Location != ResidentLocation.Home || (crewId is int requested && c.Id != requested) || c.Expedition is not null || c.Rest || c.Health < 40 || c.Energy < 25 || c.Suit < 65)
            {
                continue;
            }

            if (!Router.Find(c.Cell, target, route))
            {
                continue;
            }

            if (bestPath is null || route.Count < bestPath.Count)
            {
                best = c;
                bestPath = [.. route];
            }
        }
        if (best is null || bestPath is null)
        {
            return new(false, "No ready colonist with a clear route. Let crew recover or clear a path.");
        }

        Resources -= RescueCost;
        Release(best);
        best.Path = bestPath;
        var expedition = new Expedition { Id = Expeditions.Count, CrewId = best.Id, SurvivorId = survivorId };
        Expeditions.Add(expedition);
        best.Expedition = expedition.Id;
        best.Activity = "Rescuing " + survivor.Name;
        string message = $"{best.Name} dispatched for {survivor.Name}. Kit: 2 food, 2 water.";
        Event(message, NoticeLevel.Good);
        return new(true, message);
    }
    private void EndExpedition(Colonist c, Expedition e, string reason)
    {
        var s = Survivors[e.SurvivorId];
        if (s.State == ContactState.Escorted)
        {
            s.State = ContactState.Stranded;
            s.Cell = c.Cell;
        }
        e.Phase = RescuePhase.Failed;
        c.Expedition = null;
        c.Path.Clear();
        c.PathCursor = 0;
        Event(reason, NoticeLevel.Danger);
    }
    private void RescueTick(Colonist c)
    {
        var e = Expeditions[c.Expedition!.Value];
        var s = Survivors[e.SurvivorId];
        if (!e.Active)
        {
            c.Expedition = null;
            c.Path.Clear();
            return;
        }
        if (s.State == ContactState.Lost)
        {
            EndExpedition(c, e, $"{c.Name}'s rescue aborted. {s.Name}'s signal is gone.");
            return;
        }
        if (e.Phase == RescuePhase.Outbound)
        {
            if (c.Suit < 28 || c.Energy < 18 || World.Fire[c.Cell] > .15f)
            {
                EndExpedition(c, e, $"{c.Name} aborted the rescue to seek safety. {s.Name} still needs help.");
                return;
            }
            if (c.Cell == s.Cell)
            {
                s.State = ContactState.Escorted;
                s.Suit = Math.Max(60, s.Suit);
                s.Health = Math.Min(100, s.Health + 8);
                e.Phase = RescuePhase.Returning;
                c.Path.Clear();
                c.PathCursor = 0;
                c.Activity = "Escorting " + s.Name + " home";
                Event($"{c.Name} reached {s.Name}. Emergency oxygen shared. Escorting home.", NoticeLevel.Good);
            }
            else
            {
                if (c.PathCursor >= c.Path.Count)
                {
                    Span<int> target = stackalloc int[1];
                    target[0] = s.Cell;
                    if (!Route(c, target))
                    {
                        if (_pathBudget > 0)
                        {
                            EndExpedition(c, e, $"Route blocked. {c.Name} cannot reach {s.Name}.");
                        }

                        return;
                    }
                }
                Move(c);
                return;
            }
        }
        if (World.Breathable(c.Cell, .5f))
        {
            var recruit = AddColonist(s.Name, s.Role, s.Trait, c.Cell);
            recruit.Health = s.Health;
            recruit.Hunger = 55;
            recruit.Energy = 20;
            recruit.Morale = 55;
            recruit.Suit = s.Suit;
            recruit.Rest = true;
            recruit.Activity = "Recovering from the wasteland";
            s.State = ContactState.Rescued;
            s.RecruitedResidentId = recruit.Id;
            s.Cell = c.Cell;
            e.Phase = RescuePhase.Complete;
            c.Expedition = null;
            c.Path.Clear();
            c.PathCursor = 0;
            c.Morale = Math.Min(100, c.Morale + 12);
            Event($"HOME / {s.Name} rescued and joined the colony. {LivingCount} living crew.", NoticeLevel.Good);
            return;
        }
        if (Tick % 2 != 0)
        {
            return;
        }

        if (c.PathCursor >= c.Path.Count && !RouteToSafety(c))
        {
            if (_pathBudget > 0)
            {
                EndExpedition(c, e, $"No pressurized shelter for {c.Name} and {s.Name}. Repair the habitat.");
            }

            return;
        }
        Move(c);
        s.Cell = c.Cell;
    }
    private void SurvivorTick()
    {
        foreach (var s in Survivors)
        {
            if (!s.Active || s.FieldEscort)
            {
                continue;
            }

            if (World.Breathable(s.Cell))
            {
                s.Suit = Math.Min(100, s.Suit + 1);
            }
            else
            {
                s.Suit = Math.Max(0, s.Suit - (Storm ? .24f : .16f));
                if (s.Suit <= 0)
                {
                    s.Health = Math.Max(0, s.Health - .6f);
                }
            }
            if (World.Fire[s.Cell] > .1f)
            {
                s.Health = Math.Max(0, s.Health - World.Fire[s.Cell]);
            }

            if (s.Suit <= 15 && !s.LowOxygenReported)
            {
                s.LowOxygenReported = true;
                Event(s.Name + ": reserve oxygen critical.", NoticeLevel.Danger);
            }
            if (s.Health <= 0)
            {
                s.State = ContactState.Lost;
                Event(s.Name + "'s distress signal went silent.", NoticeLevel.Danger);
            }
        }
    }

    private void FindWork(Colonist c)
    {
        if (_unassigned == 0 || _pathBudget <= 0)
        {
            return;
        }

        Array.Clear(_candidates);
        int found = 0, chunkWidth = (World.Width + 15) / 16, chunkHeight = (World.Height + 15) / 16;
        int cx = World.X(c.Cell) / 16, cy = World.Y(c.Cell) / 16, maxRadius = Math.Max(chunkWidth, chunkHeight);
        for (int radius = 0; radius < maxRadius && found < 8; radius++)
        {
            for (int y = Math.Max(0, cy - radius); y <= Math.Min(chunkHeight - 1, cy + radius); y++)
            {
                for (int x = Math.Max(0, cx - radius); x <= Math.Min(chunkWidth - 1, cx + radius); x++)
                {
                    if (radius > 0 && Math.Abs(x - cx) != radius && Math.Abs(y - cy) != radius)
                    {
                        continue;
                    }

                    if (!_workChunks.TryGetValue(y * chunkWidth + x, out var list))
                    {
                        continue;
                    }

                    foreach (var j in list)
                    {
                        if (j.Assigned is not null || j.RetryAfter > Tick)
                        {
                            continue;
                        }

                        int score = World.Distance(c.Cell, j.Cell) - (Specialist(c, j) ? 10 : 0), slot = Math.Min(found, 7);
                        while (slot > 0 && _candidates[slot - 1] is { } previous && score < World.Distance(c.Cell, previous.Cell) - (Specialist(c, previous) ? 10 : 0))
                        {
                            if (slot < 8)
                            {
                                _candidates[slot] = _candidates[slot - 1];
                            }

                            slot--;
                        }
                        if (found < 8 || slot < 7 || _candidates[7] is null || score < World.Distance(c.Cell, _candidates[7]!.Cell) - (Specialist(c, _candidates[7]!) ? 10 : 0))
                        {
                            _candidates[slot] = j;
                        }

                        found = Math.Min(8, found + 1);
                    }
                }
            }
        }
        Span<int> targets = stackalloc int[4];
        for (int n = 0; n < found && _pathBudget > 0; n++)
        {
            var j = _candidates[n]!;
            int count = JobTargets(j, targets);
            if (Route(c, targets[..count]))
            {
                c.Job = j.Id;
                j.Assigned = c.Id;
                _unassigned--;
                c.Activity = "Walking to work";
                return;
            }
            j.RetryAfter = Tick + 30;
        }
    }
    private static bool Specialist(Colonist c, WorkOrder j) => (j.Kind == WorkKind.Dig && c.Role == "Miner") || (j.Kind == WorkKind.Build && c.Role == "Engineer");

    private void Complete(Colonist c, WorkOrder job)
    {
        int cell = job.Cell;
        if (job.Kind == WorkKind.Survey)
        {
            World.Reveal(cell, 7);
            Event(c.Name + " surveyed a new sector.", NoticeLevel.Good);
        }
        else if (job.Kind is WorkKind.Dig or WorkKind.Salvage)
        {
            var machine = MachineAt(cell);
            if (machine is not null)
            {
                Resources += Blueprint.Get(machine.Kind).Cost * .5f;
                RemoveMachine(machine);
                Event(c.Name + " salvaged " + Blueprint.Get(machine.Kind).Name.ToLowerInvariant() + ".");
            }
            else
            {
                Stock reward = World.Tiles[cell] switch
                {
                    Terrain.Ice => new(0, 24, 0, 0),
                    Terrain.Ore => new(18, 0, 0, 0),
                    Terrain.Crystal => new(0, 0, 0, 10),
                    Terrain.Rock => new(2, 0, 0, 0),
                    Terrain.Airlock => new(4, 0, 0, 0),
                    _ => new(1, 0, 0, 0)
                };
                Resources += reward;
                World.SetTerrain(cell, Terrain.Floor);
                World.Reveal(cell, 4);
                Event(c.Name + " mined " + TerrainNameFromReward(reward) + ".", NoticeLevel.Good);
            }
        }
        else
        {
            if (job.Blueprint == ModuleKind.Wall)
            {
                Span<int> exits = stackalloc int[4];
                foreach (var other in Crew)
                {
                    if (!other.Alive || other.Location != ResidentLocation.Home || other.Cell != cell)
                    {
                        continue;
                    }

                    int count = World.Neighbors(cell, exits), exit = -1;
                    for (int n = 0; n < count; n++)
                    {
                        if (World.Walkable(exits[n]))
                        {
                            exit = exits[n];
                            break;
                        }
                    }

                    if (exit < 0)
                    {
                        return;
                    }

                    other.Cell = exit;
                    other.Path.Clear();
                    other.PathCursor = 0;
                }
                World.SetTerrain(cell, Terrain.Wall);
            }
            else if (job.Blueprint == ModuleKind.Floor)
            {
                World.SetTerrain(cell, Terrain.Floor);
            }
            else if (job.Blueprint == ModuleKind.Airlock)
            {
                World.SetTerrain(cell, Terrain.Airlock);
            }
            else
            {
                AddMachine(job.Blueprint, cell);
            }

            Event(c.Name + " completed " + Blueprint.Get(job.Blueprint).Name.ToLowerInvariant() + ".", NoticeLevel.Good);
        }
        // Topology changes are coalesced at the tick boundary. Rebuilding the
        // entire power graph per completed tile makes mass excavation quadratic.
        RemoveJob(job);
        c.Job = null;
        c.Path.Clear();
        c.PathCursor = 0;
        c.CompletedWork++;
        c.Morale = Math.Min(100, c.Morale + 1.5f);
    }
    private static string TerrainNameFromReward(Stock r) => r.Water > 0 ? "water ice" : r.Crystal > 0 ? "silicate crystal" : "metal";

    private bool HasMedicalCare(int cell)
    {
        foreach (var machine in _machines)
            if (machine.Kind == ModuleKind.Medbay && machine.Powered && World.Distance(cell, machine.Cell) <= 4) return true;
        return false;
    }
    private void CrewTick(Colonist c)
    {
        if (c.Location == ResidentLocation.Field)
        {
            c.Hunger = Math.Min(100, c.Hunger + .12f);
            c.Energy = Math.Max(0, c.Energy - .06f);
            c.Suit = Math.Max(0, c.Suit - (Storm ? .28f : .19f) * c.SuitEfficiency);
            if (c.Suit <= 0 && c.Health > 0) c.Health = Math.Max(1, c.Health - 1.6f);
            if (c.Hunger >= 85 && c.Health > 0) c.Health = Math.Max(1, c.Health - .12f);
            c.Activity = "Outside / " + c.SiteId;
            return;
        }

        int cell = c.Cell;
        bool breathable = World.Breathable(cell);
        c.Hunger = Math.Min(100, c.Hunger + .12f);
        c.Energy = Math.Max(0, c.Energy - (c.Job is not null ? .1f : .035f));
        if (breathable)
        {
            c.Suit = Math.Min(100, c.Suit + 1.6f);
            World.Oxygen[cell] = Math.Max(0, World.Oxygen[cell] - .002f);
        }
        else
        {
            c.Suit = Math.Max(0, c.Suit - (Storm ? .28f : .19f));
            if (c.Suit <= 0)
            {
                c.Health -= 1.6f;
            }
        }
        if (World.Fire[cell] > .15f)
        {
            c.Health -= World.Fire[cell] * 1.6f;
        }

        if (World.Temperature(cell) > 65)
        {
            c.Health -= .25f;
        }

        if (c.Hunger >= 85)
        {
            c.Health -= .12f;
            c.Morale = Math.Max(0, c.Morale - .1f);
        }
        if (c.Hunger >= 35 && breathable && Resources.Food >= 2)
        {
            Resources -= new Stock(0, 0, 2, 0);
            c.Hunger = Math.Max(0, c.Hunger - 27);
            c.Morale = Math.Min(100, c.Morale + 1);
        }
        if (c.Health <= 0)
        {
            c.Alive = false;
            c.Health = 0;
            c.Activity = "Lost";
            Release(c);
            if (c.Expedition is int id)
            {
                EndExpedition(c, Expeditions[id], c.Name + "'s rescue expedition was lost.");
            }

            Event(c.Name + " has died. The outpost falls silent for a moment.", NoticeLevel.Danger);
            foreach (var other in Crew)
            {
                if (other.Alive)
                {
                    other.Morale = Math.Max(0, other.Morale - 18);
                }
            }

            return;
        }
        if (c.Expedition is not null)
        {
            RescueTick(c);
            return;
        }
        if (World.Fire[cell] > .15f || c.Suit < 28 || (c.Health < 30 && !breathable))
        {
            Release(c);
            if (RouteToSafety(c) && Move(c))
            {
                c.Activity = "Emergency retreat";
                return;
            }
            if (!breathable)
            {
                c.Activity = "Seeking safe pressure";
                return;
            }
        }
        if (c.Energy < 18)
        {
            c.Rest = true;
            Release(c);
        }
        if (c.Rest)
        {
            RefreshSafety();
            if (_beds.Contains(cell) || (_beds.Count == 0 && breathable))
            {
                c.Activity = "Sleeping";
                c.Energy = Math.Min(100, c.Energy + 1.15f);
                c.Health = Math.Min(100, c.Health + (HasMedicalCare(c.Cell) ? .24f : .04f));
                c.Charge = Math.Min(40, c.Charge + 1);
                c.Morale = Math.Min(100, c.Morale + .025f);
                if (c.Energy >= 94)
                {
                    c.Rest = false;
                }
            }
            else
            {
                if (c.PathCursor >= c.Path.Count)
                {
                    RouteToSafety(c, true);
                }

                Move(c);
                c.Activity = "Going to sleep";
            }
            return;
        }
        if (c.Job is int jobId)
        {
            if (!_jobsById.TryGetValue(jobId, out var job))
            {
                Release(c);
            }
            else if (World.Distance(cell, job.Cell) == 1)
            {
                c.Path.Clear();
                c.PathCursor = 0;
                job.Progress += (Specialist(c, job) ? .56f : .34f) * (.7f + c.Morale / 200);
                c.Activity = job.Kind switch
                {
                    WorkKind.Dig => MiningActivities[(int)World.Tiles[job.Cell]],
                    WorkKind.Build => BuildingActivities[(int)job.Blueprint],
                    WorkKind.Salvage => "Salvaging",
                    _ => "Surveying"
                };
                if (job.Progress >= job.Required)
                {
                    Complete(c, job);
                }

                return;
            }
            else
            {
                if (c.PathCursor >= c.Path.Count && _pathBudget > 0)
                {
                    Span<int> targets = stackalloc int[4];
                    int n = JobTargets(job, targets);
                    if (!Route(c, targets[..n]))
                    {
                        Release(c);
                    }
                }
                if (Move(c))
                {
                    c.Activity = "Walking to work";
                    return;
                }
            }
        }
        Span<int> adjacent = stackalloc int[4];
        int neighborCount = World.Neighbors(c.Cell, adjacent);
        bool fought = false;
        if (breathable)
        {
            for (int n = 0; n < neighborCount; n++)
            {
                if (World.Fire[adjacent[n]] > 0)
                {
                    World.Fire[adjacent[n]] = Math.Max(0, World.Fire[adjacent[n]] - .22f);
                    fought = true;
                }
            }
        }

        if (fought)
        {
            c.Activity = "Fighting fire";
            return;
        }
        if (Tick % 6 == c.Id % 6)
        {
            FindWork(c);
            if (c.Job is not null)
            {
                return;
            }
        }
        if (Move(c))
        {
            c.Activity = c.Role == "Botanist" ? "Tending crops" : "Patrolling habitat";
            return;
        }
        if (c.Health < 90 && breathable)
        {
            bool medical = false;
            foreach (var m in _machines)
            {
                if (m.Kind == ModuleKind.Medbay && m.Powered && World.Distance(c.Cell, m.Cell) <= 4)
                {
                    medical = true;
                    break;
                }
            }

            c.Health = Math.Min(100, c.Health + (medical ? .25f : .015f));
        }
        if (Tick % 32 == c.Id % 32 && _pathBudget > 0)
        {
            if (!breathable || c.Suit < 80)
            {
                RouteToSafety(c);
            }
            else if (c.Role == "Botanist")
            {
                foreach (var m in _machines)
                {
                    if (m.Kind == ModuleKind.Farm && m.Powered)
                    {
                        Span<int> target = stackalloc int[1];
                        target[0] = m.Cell;
                        Route(c, target);
                        break;
                    }
                }
            }
            else
            {
                int x = Math.Clamp(World.X(c.Cell) + Random.Next(-7, 8), 0, World.Width - 1), y = Math.Clamp(World.Y(c.Cell) + Random.Next(-3, 4), 0, World.Height - 1);
                int destination = World.Index(x, y);
                Span<int> target = stackalloc int[1];
                if (World.Breathable(destination))
                {
                    target[0] = destination;
                    Route(c, target);
                }
            }
        }
        c.Activity = c.Role == "Botanist" ? "Tending crops" : "On standby";
    }

    public void Meteor(int? cell = null)
    {
        int cx = World.Width / 2, cy = World.Height / 2;
        int target = cell ?? World.Index(cx + Random.Next(-7, 8), cy + (Random.Next(0, 2) == 0 ? -5 : 5));
        if (!World.Valid(target))
        {
            throw new ArgumentOutOfRangeException(nameof(cell));
        }

        World.SetTerrain(target, Terrain.Vacuum);
        if (MachineAt(target) is { } m)
        {
            RemoveMachine(m);
        }

        World.Reveal(target, 4);
        Event($"Micrometeorite impact at {World.X(target)},{World.Y(target)}. Hull breach! Queue a hull repair.", NoticeLevel.Danger);
        UpdatePower();
        _safeTick = -1;
    }
    public bool IncidentsEnabled { get; set; } = true;
    private void Events()
    {
        if (IncidentsEnabled)
        {
            if (Tick >= _nextSignal)
            {
                int active = 0;
                foreach (var s in Survivors)
                {
                    if (s.Active)
                    {
                        active++;
                    }
                }

                if (active < 3)
                {
                    SpawnSurvivor();
                }

                _nextSignal = Tick + 240;
            }
            if (Tick == 280)
            {
                Event("Long-range radar: debris cloud approaching next cycle.", NoticeLevel.Warning);
            }

            if (Tick >= 540 && (Tick - 540) % 640 == 0)
            {
                Meteor();
            }

            if (Tick > 700 && Tick % 710 == 0)
            {
                StormUntil = Tick + 90;
                Event("Solar particle storm. Solar output drops; EVA consumes extra oxygen.", NoticeLevel.Warning);
            }
            if (Tick > 800 && Tick % 977 == 0 && _machines.Count > 0)
            {
                var m = _machines[Random.Next(0, _machines.Count)];
                World.Fire[m.Cell] = .5f;
                Event("Electrical fault in " + Blueprint.Get(m.Kind).Name.ToLowerInvariant() + ". Fire detected.", NoticeLevel.Danger);
            }
            if (Tick % 120 == 0)
            {
                if (Pressure < .4f)
                {
                    Event("Habitat pressure critical. Seal breaches and check life support.", NoticeLevel.Danger);
                }

                if (Resources.Water < 15)
                {
                    Event("Water reserve low. Designate ice seams for mining.", NoticeLevel.Warning);
                }

                if (Resources.Food < 15)
                {
                    Event("Food reserve low. Check hydroponics, water, and power.", NoticeLevel.Warning);
                }
            }
        }
        if (!Victory && RescuedCount >= 3 && Cycle >= 3 && LivingCount >= 4 && Resources.Food >= 20)
        {
            foreach (var m in _machines)
            {
                if (m.Kind == ModuleKind.Beacon && m.Powered && World.Oxygen[m.Cell] >= .4f)
                {
                    Victory = true;
                    Event("EARTH ANSWERS. Founding charter granted. This is home now. Keep building.", NoticeLevel.Good);
                    break;
                }
            }
        }

        if (!GameOver && LivingCount == 0)
        {
            GameOver = true;
            Event("No living crew remain. Their names will go back to Earth.", NoticeLevel.Danger);
        }
    }
}
