using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    private long _roadBlockRevision = -1;
    private readonly Dictionary<int, bool> _roadBlocks = [];
    public bool RoadBlocked(RoadPlan road)
    {
        if (_roadBlockRevision != State.NavigationRevision) { _roadBlocks.Clear(); _roadBlockRevision = State.NavigationRevision; }
        if (_roadBlocks.TryGetValue(road.Id, out bool blocked)) return blocked;
        return _roadBlocks[road.Id] = CalculateRoadBlocked(road);
    }
    private bool CalculateRoadBlocked(RoadPlan road)
    {
        foreach (var delta in State.Chunks.Where(c => c.Key.Floor == 0)) foreach (var (index, material) in delta.Edits)
        {
            if (Materials.Walkable(material) && material != Material.Camp) continue;
            var p = new WorldPosition(delta.Key.X * 64 + index % 64, delta.Key.Y * 64 + index / 64);
            if (!road.Contains(p, 0)) continue;
            bool horizontal = p.Y == road.A.Y && p.X != road.B.X;
            var a = p.Offset(horizontal ? 0 : -1, horizontal ? -1 : 0);
            var b = p.Offset(horizontal ? 0 : 1, horizontal ? 1 : 0);
            if (!GroundOpen(a) && !GroundOpen(b)) return true;
        }
        return false;
    }
    public List<int> RegionalRoute(int from, int to)
    {
        var q = new Queue<int>(); var previous = new Dictionary<int, (int Town, int Road)> { [from] = (from, -1) }; q.Enqueue(from);
        while (q.TryDequeue(out int current))
        {
            if (current == to)
            {
                var route = new List<int>(); while (current != from) { route.Add(previous[current].Road); current = previous[current].Town; } route.Reverse(); return route;
            }
            foreach (var road in Plan.Roads.Where(r => r.FromTown == current || r.ToTown == current))
            {
                int next = road.FromTown == current ? road.ToTown : road.FromTown;
                if (previous.ContainsKey(next) || RoadBlocked(road)) continue;
                previous[next] = (current, road.Id); q.Enqueue(next);
            }
        }
        return [];
    }
    private void AdvanceCaravans()
    {
        foreach (var a in State.Actors.Where(a => a.Kind == ActorKind.Caravan && a.Life == LifeState.Alive))
        {
            int destination = a.DestinationTown!.Value;
            if (State.Tick < a.NextTravelTick)
            {
                if (a.Traveling && (State.Tick % 5 == 0 || a.Position.Distance(State.Position) <= 120)) MoveCaravanSafely(a, CaravanPosition(a));
                continue;
            }
            if (a.Traveling)
            {
                if (a.Route.Any(id => RoadBlocked(Plan.Roads[id])))
                {
                    var reroute = RegionalRoute(a.HomeTown, destination);
                    if (reroute.Count == 0) { a.NextTravelTick = State.Tick + 100; continue; }
                    a.Route = reroute; a.DepartureTick = State.Tick; a.NextTravelTick = State.Tick + a.Route.Sum(id => Plan.Roads[id].Length); continue;
                }
                var town = State.Towns[destination];
                var arrival = Plan.Towns[destination].Center;
                if (a.Position.Distance(State.Position) <= 120 && a.Position.Distance(arrival) > 2)
                { MoveCaravanSafely(a, arrival); continue; }
                foreach (var (item, count) in a.Cargo) town.Stocks[item] = town.Stocks.GetValueOrDefault(item) + count;
                a.Cargo.Clear(); MoveCaravanSafely(a, Plan.Towns[destination].Center); a.DestinationTown = a.HomeTown; a.HomeTown = destination; a.Traveling = false; a.Route.Clear(); a.NextTravelTick = State.Tick + 300;
                continue;
            }
            var route = RegionalRoute(a.HomeTown, destination);
            if (route.Count == 0) { a.NextTravelTick = State.Tick + 100; continue; }
            var origin = State.Towns[a.HomeTown];
            string export = new[] { "food", "wood", "stone", "medicine" }[a.HomeTown % 4];
            var demand = origin.Stocks.OrderByDescending(e => e.Value - State.Towns[destination].Stocks[e.Key]).First();
            if (demand.Value - State.Towns[destination].Stocks[demand.Key] > 8) export = demand.Key;
            int quantity = Math.Min(12, Math.Max(0, origin.Stocks[export] - 20));
            if (quantity == 0) { a.NextTravelTick = State.Tick + 300; continue; }
            origin.Stocks[export] -= quantity; a.Cargo[export] = quantity;
            a.Route = route; a.Traveling = true; a.DepartureTick = State.Tick; a.NextTravelTick = State.Tick + route.Sum(id => Plan.Roads[id].Length);
        }
    }
    private WorldPosition CaravanPosition(Actor a)
    {
        int remaining = (int)Math.Max(0, State.Tick - a.DepartureTick), town = a.HomeTown;
        foreach (int roadId in a.Route)
        {
            var road = Plan.Roads[roadId]; bool forward = road.FromTown == town;
            if (remaining > road.Length) { remaining -= road.Length; town = forward ? road.ToTown : road.FromTown; continue; }
            int distance = forward ? remaining : road.Length - remaining;
            int horizontal = Math.Abs(road.B.X - road.A.X);
            return distance <= horizontal ? new(road.A.X + Math.Sign(road.B.X - road.A.X) * distance, road.A.Y) : new(road.B.X, road.A.Y + Math.Sign(road.B.Y - road.A.Y) * (distance - horizontal));
        }
        return Plan.Towns[a.DestinationTown!.Value].Center;
    }
    private void AdvanceEconomy()
    {
        int epoch = (int)(State.Tick / TicksPerHour);
        foreach (var road in Plan.Roads)
        {
            bool blocked = RoadBlocked(road); string id = "road-" + road.Id;
            var task = State.Tasks.FirstOrDefault(t => t.Id == id && !t.Complete);
            if (blocked && task == null)
            {
                State.Tasks.Add(new() { Id = id, TownId = road.FromTown, RoadId = road.Id, Description = $"Reopen the road between {Plan.Towns[road.FromTown].Name} and {Plan.Towns[road.ToTown].Name}." });
                Log($"The road from {Plan.Towns[road.FromTown].Name} to {Plan.Towns[road.ToTown].Name} is blocked; caravans seek alternatives. A repair request was posted.");
            }
            else if (!blocked && task != null) { task.Complete = true; Log("Trade route repaired: " + task.Description); }
        }
        foreach (var town in State.Towns)
        {
            var workers = State.Actors.Where(a => a.Kind == ActorKind.Person && a.HomeTown == town.Id && a.Life == LifeState.Alive && !a.Recruited && !HasWork(a) && a.Body.RestUntil <= State.Tick && a.Body.Fatigue < 75 && a.Body.ForageProgress == 0 && a.Body.Hunger < Simulation.StarveAt).ToArray();
            Produce("food", "farmer", 12); Produce("wood", "woodcutter", 9); Produce("stone", "quarrier", 8); Produce("medicine", "healer", 3);
            int population = 6; // Unmodeled residents; named actors eat through the shared body rules.
            int eaten = Math.Min(population, town.Stocks["food"]); town.Stocks["food"] -= eaten; State.ResourcesConsumed += eaten;
            if (eaten < population || town.Stocks["food"] < (workers.Length + population) * 2)
            {
                town.Shortage++;
                if (town.Shortage == 1 || town.Shortage % 8 == 0) Log($"Food shortage in {Plan.Towns[town.Id].Name}: suppliers need escorts or deliveries. Food costs {town.Price("food")} gold.");
                if (!State.Tasks.Any(t => t.TownId == town.Id && t.RoadId == null && !t.Complete)) State.Tasks.Add(new() { Id = $"food-{town.Id}-{State.Tick}", TownId = town.Id, Description = $"Deliver 20 food to {Plan.Towns[town.Id].Name} for 30 gold." });
            }
            else town.Shortage = Math.Max(0, town.Shortage - 1);
            // Regional raids depend on relations and guards; removed resources are accounted.
            int hostile = State.Factions[Plan.Towns[town.Id].FactionId].Relations.Count(r => r < -20);
            if (hostile > 0 && workers.All(w => w.Job != "guard") && WorldRandom.Hash(Seed, town.Id, epoch, 550) % 4 == 0)
            {
                int lost = Math.Min(12, town.Stocks["food"]); town.Stocks["food"] -= lost; town.RaidLosses += lost; State.ResourcesLost += lost;
                Log($"Raiders took {lost} food near {Plan.Towns[town.Id].Name}. A guard's vacant post left the road exposed.");
            }
            void Produce(string item, string job, int rate)
            {
                int amount = (int)workers.Where(w => w.Job == job).Sum(w => rate * WorkRate(w));
                int accepted = Math.Min(amount, Math.Max(0, 9999 - town.Stocks[item])); town.Stocks[item] += accepted; State.ResourcesProduced += accepted;
            }
        }
        // Patrols are persistent people, not duplicate actors created when chunks become active.
        foreach (var guard in State.Actors.Where(a => a.Job == "guard" && !HasWork(a) && !a.Recruited && a.Life == LifeState.Alive && a.Position.Distance(State.Position) > 100))
            { var destination = Plan.Towns[guard.HomeTown].Center.Offset((epoch % 5 - 2) * 4, 12); if (OccupantAt(destination, guard.Id) == null && (destination.Distance(State.Position) > 120 || GroundOpen(destination))) guard.Position = destination; }
    }
    private ActionResult Trade(WorldCommand command)
    {
        var townPlan = Plan.Towns.FirstOrDefault(t => State.Position.Distance(t.Center) <= 28);
        if (townPlan == null || command.Amount is < 1 or > 999 || command.Item is not ("food" or "wood" or "stone" or "medicine")) return new(false, "Trade near a town; choose food, wood, stone or medicine.");
        var town = State.Towns[townPlan.Id]; int price = town.Price(command.Item), amount = command.Amount;
        if (command.Kind == WorldCommandKind.Buy)
        {
            if (town.Stocks[command.Item] < amount || Count("gold") < (long)price * amount) return new(false, "The stock or your gold is insufficient.");
            town.Stocks[command.Item] -= amount; Add("gold", -price * amount); Add(command.Item, amount);
        }
        else
        {
            if (Count(command.Item) < amount) return new(false, "You do not have that stack.");
            Add(command.Item, -amount); town.Stocks[command.Item] += amount; Add("gold", Math.Max(1, price / 2) * amount);
        }
        return new(true, $"Traded {amount} {command.Item} in {townPlan.Name}.");
    }
    public long AccountedResources() => State.Towns.Sum(t => t.Stocks.Values.Sum(v => (long)v)) + State.Actors.Sum(a => a.Cargo.Values.Sum(v => (long)v));
}
