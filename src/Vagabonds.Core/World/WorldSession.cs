using Vagabonds.Core.Expeditions;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public enum WorldCommandKind { Move, Interact, Recruit, Dig, Build, Revive, Abandon, Buy, Sell, Store, Take, Camp, ResolveArc, Continue, CompleteDelivery, Loot, Equip, Unequip, Wait, AssignWork, CancelWork, Repair, Craft, Forage, Dismiss, Discover, LeadParty, ConfigureWork, Harvest, CampActivity }
public sealed record WorldCommand(WorldCommandKind Kind, WorldPosition Target = default, long ActorId = 0, Material Material = Material.Wall, string Item = "food", int Amount = 1, long WorkId = 0);

public sealed partial class WorldSession : IGameSession
{
    public const int TicksPerSecond = 5;
    public const int TicksPerHour = 300;
    public WorldPlan Plan { get; }
    public WorldState State { get; }
    public ChunkCache Chunks { get; }
    public int Seed => Plan.Seed;
    public bool IsInCombat => State.Battle != null;
    public bool Paused { get; set; } = true;
    public bool ModalOpen { get; set; }
    public bool Focused { get; set; } = true;
    public bool Frozen => Paused || ModalOpen || !Focused || IsInCombat || State.AwaitingContinuation || DeathPending || State.Endgame.Victory;
    public bool DeathPending { get; private set; }
    public string? SavePath { get; set; }
    public HashSet<WorldPosition> Visible { get; } = [];
    public IEnumerable<Actor> Party => State.Party.Select(id => State.Actors.Single(a => a.Id == id));
    private long _frameRemainder;
    private (int Left, int Top, int Right, int Bottom)? _surfaceView;
    internal WorldSession(WorldPlan plan, WorldState state, ContentCatalog? content = null)
    {
        Content = content ?? ContentCatalog.Default;
        Plan = plan; State = state; Chunks = new(plan, state, Content);
        Chunks.Prefetch(state.Position); Reveal();
    }
    public static WorldSession Create(int seed, ContentCatalog? content = null)
    {
        content ??= ContentCatalog.Default;
        if (content.Issues.Count > 0) throw new InvalidDataException("Invalid content catalog.");
        var plan = WorldPlan.Generate(seed);
        var s = new WorldState { IncidentRandom = WorldRandom.Stream(seed, 0, salt: 610).State, CombatRandom = WorldRandom.Stream(seed, 0, salt: 600).State, Position = plan.Towns[0].Center,
            Inventory = content.Game.Inventory.ToDictionary(p => content.Get<ItemDefinition>(p.Key).Stack, p => p.Value) };
        for (int f = 0; f < 6; f++)
            s.Factions.Add(new() { Id = f, Name = SampleLibrary.Default.Cultures[f].Name + " compact", Relations = Enumerable.Range(0, 6).Select(i => i == f ? 100 : (int)(WorldRandom.Hash(seed, f, i, 180) % 81) - 40).ToArray() });
        foreach (var town in plan.Towns)
        {
            s.Towns.Add(new() { Id = town.Id });
            for (int i = 0; i < 8; i++)
            {
                var actor = ActorFactory.Create(content, i == 4 ? content.Game.Healer : content.Game.Villager, seed, 1000 + town.Id * 16 + i, town.Center, town.Id, town.Culture, town.FactionId);
                actor.Position = town.Center.Offset((i % 4 - 2) * 3, i / 4 * 6 - 3);
                actor.Job = new[] { "farmer", "farmer", "woodcutter", "quarrier", "healer", "trader", "guard", "scribe" }[i];
                if (i == 1 && town.Id % 3 != 0) actor.Job = town.Id % 2 == 0 ? "quarrier" : "woodcutter";
                actor.FriendId = 1000 + town.Id * 16 + (i + 1) % 8;
                s.Actors.Add(actor);
            }
        }
        foreach (var road in plan.Roads)
        {
            var caravan = ActorFactory.Create(content, content.Game.Caravan, seed, 20000 + road.Id, road.A, road.FromTown, faction: plan.Towns[road.FromTown].FactionId);
            caravan.Position = road.A.Offset((road.Id % 3 - 1) * 3, 7 + road.Id % 2 * 3);
            caravan.Name = "Caravan of " + plan.Towns[road.FromTown].Name; caravan.DestinationTown = road.ToTown; caravan.NextTravelTick = 100 + road.Id * 7;
            s.Actors.Add(caravan);
        }
        foreach (var site in plan.Sites)
        {
            for (int i = 0; i < 3; i++) s.Actors.Add(ActorFactory.Create(content, i == 0 ? content.Game.Beast : content.Game.Raider, seed, 30000 + site.Id * 4 + i,
                (site.Center with { Floor = i == 2 ? 1 : 0 }).Offset(i * 3 - 4, 4), site.Id % 24, faction: site.Id % 6));
        }
        var session = new WorldSession(plan, s, content);
        session.NewLoneHero();
        session.PopulateAdventures();
        session.PopulateDiscoveries();
        session.PrepareLocalCollision(force: true); session.Reveal();
        var rng = WorldRandom.Stream(seed, 200);
        int townId = rng.Next(24), siteId = rng.Next(48);
        s.Arc = new() { Kind = (ArcKind)rng.Next(3), TownId = townId, SiteId = siteId, PatronId = 1000 + townId * 16 + 7 };
        session.UpdateObjective();
        s.Journal.Add("You arrived alone. Click nearby inhabitants to find travelling companions. Play resumes the world.");
        s.Journal.Add(plan.History[0]);
        return session;
    }
    private void NewLoneHero()
    {
        State.Party.Clear(); State.PartyOrdinal++; State.WorkRally = null; PartyTravelling = false;
        var town = Plan.Towns.MinBy(t => t.Center.Distance(State.Position with { Floor = 0 }))!;
        State.Position = town.Center;
        var hero = ActorFactory.Create(Content, Content.Game.Traveller, Seed, State.NextActorId++, State.Position, town.Id, town.Culture, town.FactionId);
        hero.Job = "traveller"; hero.Position = State.Position; hero.Recruited = true;
        State.Actors.Add(hero); State.Party.Add(hero.Id); State.AwaitingContinuation = false;
        PrepareLocalCollision(force: true); Reveal();
    }
    public int AdvanceFrame(TimeSpan elapsed)
    {
        if (Frozen) { _frameRemainder = 0; return 0; }
        if (elapsed < TimeSpan.Zero || elapsed > TimeSpan.FromSeconds(5)) throw new ArgumentException("Frame interval must be 0..5 seconds.");
        _frameRemainder += elapsed.Ticks;
        int ticks = (int)(_frameRemainder / (TimeSpan.TicksPerSecond / TicksPerSecond));
        _frameRemainder %= TimeSpan.TicksPerSecond / TicksPerSecond;
        return AdvanceTicks(ticks);
    }
    public int AdvanceTicks(int count)
    {
        if (count is < 0 or > 1000000) throw new ArgumentOutOfRangeException(nameof(count));
        int advanced = 0;
        while (advanced < count && !Frozen)
        {
            AdvanceWorldTick(); advanced++;
        }
        return advanced;
    }
    private void AdvanceWorldTick()
    {
        State.Tick++;
        Contact(); AdvanceBodies(); AdvanceWork(); AdvanceIncidents();
        if (State.Tick % 5 == 0) MoveActors();
        AdvanceCaravans();
        if (State.Tick % TicksPerHour == 0) AdvanceEconomy();
        if (State.Tick % 50 == 0) CheckArc();
    }
    private void AdvanceCombatClock()
    {
        for (int i = 0; i < TicksPerSecond; i++) AdvanceWorldTick();
    }
    public ActionResult Submit(WorldCommand command)
    {
        if (State.Endgame.Victory) return new(false, "This expedition is complete. Start a new world to play again.");
        if (DeathPending) return new(false, "Save the recorded deaths before continuing.");
        if (State.AwaitingContinuation && command.Kind != WorldCommandKind.Continue) return new(false, "The expedition has ended. Continue with a new traveller or begin a fresh world.");
        if (IsInCombat) return new(false, "Finish the encounter first.");
        if (command.Kind != WorldCommandKind.Continue && CommandBlockReason(command) is { Length: > 0 } blocked) return new(false, blocked);
        var target = State.Actors.FirstOrDefault(a => a.Id == command.ActorId);
        bool near = Plan.Contains(command.Target) && State.Position.Distance(command.Target) <= 1;
        switch (command.Kind)
        {
            case WorldCommandKind.Loot: return Loot(command.Target, command.Item);
            case WorldCommandKind.Discover: return VisitDiscovery(command.Target);
            case WorldCommandKind.Equip: return Equip(command.ActorId, command.Item);
            case WorldCommandKind.Unequip:
                if (target == null || !State.Party.Contains(target.Id) || !target.Equipment.TryGetValue(command.Item, out var gear)) return new(false, "Choose an occupied equipment slot.");
                Add(Item(gear.Item).Stack, 1); target.Equipment.Remove(command.Item); target.Health = Math.Min(target.Health, target.MaxHealth); return new(true, "Equipment returned to the pack.");
            case WorldCommandKind.Move:
                if (!near || command.Target == State.Position) return new(false, "Choose an adjacent tile.");
                string collision = MovementBlockReason(command.Target, State.Party[0]);
                if (collision.Length > 0) return new(false, collision);
                if (!TryPartyStep(command.Target, out var step)) return new(false, "The party needs room to follow. Open a passage or choose another step.");
                ApplyPartyPlacement(command.Target, step);
                if (State.WorkRally != null) State.WorkRally = command.Target;
                foreach (var a in Party) a.Body.RestUntil = 0;
                PrepareLocalCollision(); Reveal(); Contact(); CheckArc(); return new(true, "Moved.");
            case WorldCommandKind.Interact:
                if (target != null && State.Position.Distance(target.Position) <= 2 && target.Life == LifeState.Alive)
                {
                    if (target.Kind is ActorKind.Raider or ActorKind.Beast) { StartBattle(target.Id); return new(true, "Encounter!"); }
                    if (target.Kind == ActorKind.Caravan) return new(true, $"{target.Name}: the roads carry food, materials and news between towns.");
                    return new(true, $"{target.Name}, {target.Trait} {target.Background}. Works as a {target.Job}; hopes to {target.Motivation}. Their friend is {State.Actors.FirstOrDefault(a => a.Id == target.FriendId)?.Name ?? "far away"}.", target.Name);
                }
                if (!near && command.Target != State.Position) return new(false, "Move closer to interact.");
                var material = Chunks.At(command.Target);
                if (material is Material.StairsDown or Material.StairsUp)
                {
                    var arrival = material == Material.StairsDown ? command.Target.Offset(-4, 0) with { Floor = command.Target.Floor + 1 } : command.Target.Offset(4, 0) with { Floor = command.Target.Floor - 1 };
                    if (!Plan.Contains(arrival) || !Materials.Walkable(Chunks.At(arrival))) return new(false, "The stair is obstructed.");
                    if (!TryPartyPlacement(arrival, out var landing)) return new(false, "The destination needs enough walkable space for every companion.");
                    ApplyPartyPlacement(arrival, landing);
                    if (State.WorkRally != null) State.WorkRally = arrival;
                    Chunks.Prefetch(arrival); PrepareLocalCollision(); Reveal(); CheckArc(); Contact(); return new(true, "You take the stairs.");
                }
                return new(true, $"{material}. {Plan.BiomeAt(command.Target.X, command.Target.Y)} country.");
            case WorldCommandKind.Recruit:
                if (target == null || target.Kind != ActorKind.Person || target.Life != LifeState.Alive || target.Recruited || State.Position.Distance(target.Position) > 2) return new(false, "Speak to an available inhabitant nearby.");
                if (!Interactions(target.Id).Any(i => i.Action == "recruit")) return new(false, "This person is not offering to travel.");
                if (State.Party.Count >= 4) return new(false, "Four travellers already fill the active party.");
                if (!TryPartyPlacement(State.Position, Party.Append(target).ToArray(), true, out var recruitedPositions)) return new(false, "The new companion needs a free walkable tile nearby.");
                target.Recruited = true; target.Body.ForageProgress = 0;
                foreach (var job in State.Work.Where(o => o.Worker == target.Id)) job.PartyApproach = true;
                Remember(target, "recruited", "I set out with a new travelling party.", 5);
                State.Party.Add(target.Id); ApplyPartyPlacement(State.Position, recruitedPositions); State.Towns[target.HomeTown].Vacancies++;
                Log($"{target.Name} joined you. {Plan.Towns[target.HomeTown].Name} needs a new {target.Job}."); return new(true, $"{target.Name} joins your expedition.");
            case WorldCommandKind.Dig: return QueueDig(command.Target);
            case WorldCommandKind.Build: return QueueBuild(command);
            case WorldCommandKind.Repair: return QueueRepair(command.Target);
            case WorldCommandKind.Craft: return QueueCraft(command.Item);
            case WorldCommandKind.Forage: return QueueForage();
            case WorldCommandKind.Harvest: return QueueHarvest(command.Target);
            case WorldCommandKind.CampActivity: return QueueCampActivity(command.Target, command.Item, command.ActorId);
            case WorldCommandKind.Wait: return Wait(command.Amount);
            case WorldCommandKind.ConfigureWork: return ConfigureWork(command.WorkId, command.Item, command.Amount);
            case WorldCommandKind.AssignWork: return AssignWork(command.WorkId, command.ActorId);
            case WorldCommandKind.CancelWork: return CancelWork(command.WorkId);
            case WorldCommandKind.LeadParty:
                if (target == null || !target.Recruited || target.Life != LifeState.Alive || !State.Party.Contains(target.Id)) return new(false, "Choose a conscious companion.");
                if (State.Party[^1] == target.Id) State.Party.Reverse();
                else { State.Party.Remove(target.Id); State.Party.Insert(0, target.Id); }
                UpdatePartyLeader(); return new(true, target.Name + " now leads the party.");
            case WorldCommandKind.Dismiss:
                if (target == null || !State.Party.Contains(target.Id) || target.Life != LifeState.Alive || !Party.Any(a => a.Id != target.Id && a.Life == LifeState.Alive) || State.Position.Distance(Plan.Towns[target.HomeTown].Center) > 28)
                    return new(false, "Return to a companion's home town with another living traveller before parting ways.");
                State.Party.Remove(target.Id); target.Recruited = false; State.Towns[target.HomeTown].Vacancies = Math.Max(0, State.Towns[target.HomeTown].Vacancies - 1);
                UpdatePartyLeader();
                Remember(target, "returned-home", "I returned to my old work, changed by the journey.", 5); return new(true, target.Name + " returned to their old job with their history and equipment.");
            case WorldCommandKind.Revive:
                if (target == null || !State.Party.Contains(target.Id) || target.Life != LifeState.Downed || Count("medicine") < 1) return new(false, "Revival needs a downed companion and medicine.");
                Add("medicine", -1); target.Life = LifeState.Alive; target.Health = Math.Max(1, target.MaxHealth / 3);
                foreach (var friend in Party.Where(a => a.Id != target.Id && a.Life == LifeState.Alive)) { ChangeTrust(target, friend, 12); Remember(target, "revived", friend.Name + " helped me back to my feet.", 8, friend.Id); }
                return new(true, $"{target.Name} revived.");
            case WorldCommandKind.Abandon:
                if (target == null || !State.Party.Contains(target.Id) || target.Life != LifeState.Downed) return new(false, "Only a downed companion may be deliberately abandoned.");
                target.Life = LifeState.Dead; State.Party.Remove(target.Id);
                UpdatePartyLeader();
                foreach (var friend in Party) Remember(friend, "abandoned-friend", target.Name + " was left behind.", -12, target.Id); Log($"{target.Name} was abandoned and died."); CommitDeaths(); return new(true, "Death recorded.");
            case WorldCommandKind.Buy:
            case WorldCommandKind.Sell:
                return Trade(command);
            case WorldCommandKind.Store:
            case WorldCommandKind.Take:
                if (!near || Chunks.At(command.Target) != Material.Storage || command.Amount < 1 || command.Amount > 999) return new(false, "Choose adjacent storage and a positive stack.");
                var contents = State.Chunks.FirstOrDefault(c => c.Key == command.Target.Chunk)?.Storage.GetValueOrDefault(command.Target.LocalIndex) ?? new Dictionary<string, int>();
                var source = command.Kind == WorldCommandKind.Store ? State.Inventory : contents;
                var destination = command.Kind == WorldCommandKind.Store ? contents : State.Inventory;
                if (source.GetValueOrDefault(command.Item) < command.Amount) return new(false, "Not enough in the source stack.");
                Chunks.Delta(command.Target.Chunk).Storage[command.Target.LocalIndex] = contents;
                source[command.Item] -= command.Amount; destination[command.Item] = destination.GetValueOrDefault(command.Item) + command.Amount; return new(true, "Stack transferred.");
            case WorldCommandKind.Camp:
                if (!NearbyCamp(State.Position)) return new(false, "Approach a serviceable camp first.");
                if (Count("food") < State.Party.Count) return new(false, "The party needs food to rest.");
                Add("food", -State.Party.Count);
                foreach (var a in Party.Where(a => a.Life == LifeState.Alive))
                {
                    if (!State.Work.Any(w => w.Kind == WorkKind.Camp && w.Worker == a.Id && !w.Suspended)) a.Body.RestUntil = State.Tick + TicksPerHour + 1;
                    a.Body.Hunger = Math.Max(0, a.Body.Hunger - Simulation.MealReduction);
                    Remember(a, "camp-rest", "We took time to eat and recover.", 8);
                }
                return Wait(TicksPerHour);
            case WorldCommandKind.ResolveArc:
                return ResolveArc();
            case WorldCommandKind.CompleteDelivery:
                var delivery = State.Tasks.FirstOrDefault(t => !t.Complete && t.RoadId == null && State.Position.Distance(Plan.Towns[t.TownId].Center) < 28);
                if (delivery == null || Count("food") < 20) return new(false, "A town with a delivery request needs 20 food.");
                Add("food", -20); State.Towns[delivery.TownId].Stocks["food"] += 20; delivery.Complete = true; Add("gold", 30); Log($"Delivery completed in {Plan.Towns[delivery.TownId].Name}; trade can recover.");
                return new(true, "Delivery accepted; 30 gold paid.");
            case WorldCommandKind.Continue:
                NewLoneHero(); Paused = true; Log("A new traveller takes up the story in the same changed world."); return new(true, "A new expedition begins alone.");
            default: return new(false, "Unknown command.");
        }
    }
    public int Count(string item) => State.Inventory.GetValueOrDefault(item);
    private void Add(string item, int amount) => State.Inventory[item] = Count(item) + amount;
    private IEnumerable<WorldPosition> Nearby()
    { yield return State.Position; foreach (var (x, y) in Directions) yield return State.Position.Offset(x, y); }
    public static readonly (int X, int Y)[] Directions = [(1, 0), (0, 1), (-1, 0), (0, -1)];
    private void Log(string text)
    {
        State.Journal.Add($"Day {State.Tick / (TicksPerHour * 24) + 1}: {text}");
        if (State.Journal.Count > 500) State.Journal.RemoveAt(0);
    }
    public void Reveal()
    {
        Visible.Clear(); var origin = State.Position;
        if (origin.Floor == 0 && _surfaceView is { } view)
        {
            for (int y = view.Top; y < view.Bottom; y++) for (int x = view.Left; x < view.Right; x++)
            {
                var p = new WorldPosition(x, y);
                Visible.Add(p); Chunks.Delta(p.Chunk).Discovered.Add(p.LocalIndex);
            }
            ObserveDiscoveries(); return;
        }
        _surfaceView = null;
        const int radius = 11;
        for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
        {
            if (dx * dx + dy * dy > radius * radius) continue;
            int length = Math.Max(Math.Abs(dx), Math.Abs(dy));
            for (int step = 0; step <= length; step++)
            {
                var p = origin.Offset(length == 0 ? 0 : (int)Math.Round(dx * step / (double)length), length == 0 ? 0 : (int)Math.Round(dy * step / (double)length));
                if (!Plan.Contains(p)) break;
                Visible.Add(p); Chunks.Delta(p.Chunk).Discovered.Add(p.LocalIndex);
                if (p != origin && Materials.Opaque(Chunks.At(p))) break;
            }
        }
        ObserveDiscoveries();
    }
    /// <summary>Observe the local surface view without occlusion; bounds are exclusive on the right/bottom.</summary>
    public void RevealSurface(int left, int top, int right, int bottom)
    {
        if (State.Position.Floor != 0) return;
        if (right < left || bottom < top) throw new ArgumentException("Surface bounds must be ordered.");
        var view = (Math.Clamp(left, 0, WorldPlan.Size), Math.Clamp(top, 0, WorldPlan.Size),
            Math.Clamp(right, 0, WorldPlan.Size), Math.Clamp(bottom, 0, WorldPlan.Size));
        if (_surfaceView == view) return;
        _surfaceView = view; Reveal();
    }
    public IReadOnlyList<WorldPosition> Route(WorldPosition destination)
    {
        if (destination.Floor != State.Position.Floor || !Chunks.Seen(destination)) return [];
        var occupied = State.Actors.Where(a => a.Life != LifeState.Dead && a.Id != State.Party[0] && Visible.Contains(a.Position)).Select(a => a.Position).ToHashSet();
        var previous = new Dictionary<WorldPosition, WorldPosition>(); var queue = new Queue<WorldPosition>();
        queue.Enqueue(State.Position); previous[State.Position] = State.Position;
        while (queue.TryDequeue(out var current) && previous.Count < 16384)
        {
            if (current == destination)
            {
                var path = new List<WorldPosition>();
                while (current != State.Position) { path.Add(current); current = previous[current]; }
                path.Reverse(); return path;
            }
            foreach (var (x, y) in Directions)
            {
                var next = current.Offset(x, y);
                if (previous.ContainsKey(next) || !Chunks.Seen(next) || occupied.Contains(next) || !GroundOpen(next)) continue;
                previous[next] = current; queue.Enqueue(next);
            }
        }
        return [];
    }
    public Actor? NearbyPerson() => State.Actors.Where(a => a.Kind == ActorKind.Person && a.Life == LifeState.Alive && !a.Recruited && State.Position.Distance(a.Position) <= 2).OrderBy(a => a.Position.Distance(State.Position)).ThenBy(a => a.Id).FirstOrDefault();
    private void Contact()
    {
        if (IsInCombat || State.AwaitingContinuation || State.Endgame.Victory) return;
        var positions = Party.Where(a => a.Life != LifeState.Dead).Select(a => a.Position).ToArray();
        var enemy = State.Actors.FirstOrDefault(a => a.Life == LifeState.Alive && a.Kind is ActorKind.Beast or ActorKind.Raider && positions.Any(p => a.Position.Distance(p) <= 1));
        if (enemy != null) StartBattle(enemy.Id);
    }
    private void MoveActors()
    {
        foreach (var a in State.Actors)
        {
            if (HasWork(a) || a.Body.RestUntil > State.Tick || a.Body.ForageProgress > 0 || a.EncounterLeader > 0 || IsInCombat && State.Battle!.ActorIds.Contains(a.Id) || a.Recruited || a.Life != LifeState.Alive || a.Kind == ActorKind.Caravan || a.Position.Floor != State.Position.Floor || a.Position.Distance(State.Position) > 100) continue;
            var rng = WorldRandom.Stream(Seed, (int)a.Id, unchecked((int)(State.Tick / 5)), 330);
            WorldPosition destination;
            if (a.Kind == ActorKind.Person)
            {
                var center = Plan.Towns[a.HomeTown].Center;
                int hour = (int)(State.Tick / TicksPerHour % 24);
                destination = center.Offset((int)(a.Id % 5) * 3 - 6, hour < 6 || hour > 19 ? -18 : 12);
            }
            else destination = a.Position.Distance(State.Position) < 7 ? State.Position : a.Position.Offset(rng.Next(3) - 1, rng.Next(3) - 1);
            var next = rng.Next(2) == 0 ? a.Position.Offset(Math.Sign(destination.X - a.Position.X), 0) : a.Position.Offset(0, Math.Sign(destination.Y - a.Position.Y));
            if (next == a.Position || !CanEnter(next, a.Id)) next = LocalStep(a.Position, destination, a.Id);
            if (CanEnter(next, a.Id)) a.Position = next;
        }
        Contact();
    }
    private WorldPosition LocalStep(WorldPosition start, WorldPosition destination, long actorId = 0)
    {
        var occupied = State.Actors.Where(a => a.Life != LifeState.Dead && a.Id != actorId).Select(a => a.Position).ToHashSet();
        var open = new PriorityQueue<WorldPosition, (int Score, int Tie)>();
        var previous = new Dictionary<WorldPosition, WorldPosition> { [start] = start };
        var costs = new Dictionary<WorldPosition, int> { [start] = 0 };
        WorldPosition best = start; int tie = 0;
        open.Enqueue(start, (start.Distance(destination), tie++));
        while (open.TryDequeue(out var current, out _) && previous.Count < 256)
        {
            if (current.Distance(destination) < best.Distance(destination)) best = current;
            if (current == destination) break;
            foreach (var (x, y) in Directions)
            {
                var next = current.Offset(x, y); int cost = costs[current] + 1;
                if (!Plan.Contains(next) || next.Distance(start) > 24 || occupied.Contains(next) || !GroundOpen(next) || costs.TryGetValue(next, out int old) && old <= cost) continue;
                costs[next] = cost; previous[next] = current; open.Enqueue(next, (cost + next.Distance(destination), tie++));
            }
        }
        if (best == start) return start;
        while (previous[best] != start) best = previous[best];
        return best;
    }
    private void CheckArc()
    {
        var arc = State.Arc;
        if (arc.Status != ArcStatus.Active) return;
        var site = Plan.Sites[arc.SiteId];
        if (State.Position.Floor > 0 && (State.Position with { Floor = 0 }).Distance(site.Center) < 28 && State.VisitedSites.Add(site.Id))
        { arc.Progress = 1; Log($"You found evidence beneath {site.Name}. Return to {Plan.Towns[arc.TownId].Name}."); UpdateObjective(); }
        if (State.Tick > 21600 && State.Towns[arc.TownId].Shortage > 20)
        { arc.Status = ArcStatus.Failed; Log("The compact collapsed during the shortages. The main arc has failed; the world continues."); }
    }
    private void UpdateObjective()
    {
        var arc = State.Arc; var town = Plan.Towns[arc.TownId]; var site = Plan.Sites[arc.SiteId];
        string premise = arc.Kind switch { ArcKind.TradeConflict => "Resolve the broken trade compact", ArcKind.SpreadingBlight => "Trace the blight affecting the harvest", _ => "Settle the disputed succession" };
        arc.Objective = arc.Progress == 0 ? $"{premise}: seek evidence beneath {site.Name} ({site.Center.X + 2}, {site.Center.Y})." : $"Bring the evidence to {town.Name} ({town.Center.X}, {town.Center.Y}) with {(arc.Kind == ArcKind.SpreadingBlight ? "3 medicine" : arc.Kind == ArcKind.TradeConflict ? "20 food" : "the support of a companion")}.";
    }
    private ActionResult ResolveArc()
    {
        var arc = State.Arc;
        var recipient = Plan.Towns.FirstOrDefault(t => State.Position.Distance(t.Center) <= 28 && (t.Id == arc.TownId || t.FactionId != Plan.Towns[arc.TownId].FactionId));
        if (arc.Status != ArcStatus.Active || arc.Progress < 1 || recipient == null) return new(false, "Find evidence below the marked site, then return to the patron's town or negotiate with another compact.");
        string item = arc.Kind == ArcKind.SpreadingBlight ? "medicine" : "food";
        int amount = arc.Kind == ArcKind.SpreadingBlight ? 3 : arc.Kind == ArcKind.TradeConflict ? 20 : 0;
        if (Count(item) < amount || arc.Kind == ArcKind.ContestedSuccession && State.Party.Count < 2) return new(false, arc.Objective);
        Add(item, -amount); State.Towns[recipient.Id].Stocks[item] += amount;
        arc.Status = ArcStatus.Resolved; Add("gold", 100);
        arc.Outcome = recipient.Id == arc.TownId ? "The original compact was restored." : $"A new compact formed under {recipient.Name}; the old patron lost influence.";
        foreach (var f in State.Factions) if (f.Id != Plan.Towns[arc.TownId].FactionId) f.Relations[Plan.Towns[arc.TownId].FactionId] = Math.Min(100, f.Relations[Plan.Towns[arc.TownId].FactionId] + 20);
        string patron = State.Actors.Single(a => a.Id == arc.PatronId).Life == LifeState.Dead ? "The townspeople accepted the evidence after the patron's death." : "Your patron accepted the evidence.";
        Log(patron + " " + arc.Outcome + " The main arc is resolved; trade, exploration and the world's history continue."); return new(true, arc.Outcome + " The world remains open.");
    }
    private void CommitDeaths()
    {
        DeathPending = true; Paused = true;
        if (SavePath != null) Save(SavePath);
    }
}
