using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public sealed record OccupationFixture(long Actor, WorldPosition Source, WorldPosition Home);
public static class OccupationPreview
{
    public static OccupationFixture Prepare(WorldSession world, string job = "farmer")
    {
        var towns = job == "quarrier" ? world.Plan.Towns.OrderBy(t => world.Plan.BiomeAt(t.Center.X, t.Center.Y) is Biome.Highland or Biome.Badland ? 0 : 1)
            : world.Plan.Towns.OrderBy(t => t.Id);
        WorldPosition? found = null; int town = 0;
        foreach (var candidate in towns)
        {
            if (job == "healer") { town = candidate.Id; found = candidate.Center.Offset(-6, 0); break; }
            for (int r = 1; r <= WorldSession.OccupationRadius && found == null; r++)
                for (int y = -r; y <= r && found == null; y++) for (int side = 0; side < (Math.Abs(y) == r ? 1 : 2); side++)
                {
                    int x = (r - Math.Abs(y)) * (side == 0 ? 1 : -1); var p = candidate.Center.Offset(x, y);
                    if (!world.Plan.Contains(p) || world.Chunks.IsEdited(p)) continue;
                    var material = world.Chunks.At(p); var node = world.ResourceAt(p);
                    if (job == "farmer" ? node?.Definition.Id == "resource.field-grain"
                        : material == (job == "woodcutter" ? Material.Tree : Material.Rock) && node == null && world.ObjectAt(p) == null)
                    { found = p; town = candidate.Id; break; }
                }
            if (found != null) break;
        }
        if (found is not { } source) throw new InvalidDataException("No natural occupation source in the bounded fixture: " + job);
        var home = world.Plan.Towns[town].Center;
        var actor = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited && a.HomeTown == town && a.Job == job);
        world.State.Tick = WorldSession.TicksPerHour * 6;
        int parked = 0;
        foreach (var other in world.State.Actors.Where(a => !a.Recruited && a.Id != actor.Id))
        {
            other.Body.RestUntil = world.State.Tick + 2000;
            if (other.Position.Distance(source) > 100 && other.Position.Distance(home) > 100) continue;
            other.Position = (world.Plan.Towns[(town + 12) % 24].Center with { Floor = other.Position.Floor }).Offset(20 + parked % 8 * 2, 20 + parked++ / 8 * 2);
            world.Chunks.Edit(other.Position, Material.Floor);
        }
        void Clear(WorldPosition p) { if (world.Plan.Contains(p) && (job == "healer" || p != source)) world.Chunks.Edit(p, Material.Floor); }
        for (int y = -10; y <= 10; y++) for (int x = -10; x <= 10; x++) Clear(home.Offset(x, y));
        for (int y = -3; y <= 3; y++) for (int x = -3; x <= 3; x++) Clear(source.Offset(x, y));
        var path = home;
        while (path.X != source.X) { Clear(path); path = path.Offset(Math.Sign(source.X - path.X), 0); }
        while (path.Y != source.Y) { Clear(path); path = path.Offset(0, Math.Sign(source.Y - path.Y)); }
        actor.Position = job == "healer" ? home.Offset(3, 0) : WorldSession.Directions.Select(d => source.Offset(d.X, d.Y)).MinBy(p => p.Distance(home));
        Clear(actor.Position); actor.Body.RestUntil = 0; actor.Body.Hunger = actor.Body.Fatigue = 0;
        actor.OccupationAfter = 0; actor.Routine = null;
        var observer = source.Offset(3, 1); Clear(observer); world.State.Position = world.Party.Single().Position = observer;
        world.Paused = false; world.AdvanceTicks(1); world.Paused = true;
        if (actor.Occupation == null) throw new InvalidDataException("The occupation fixture did not start real work: " + job);
        world.Reveal(); return new(actor.Id, source, home);
    }
    public static object Run(ContentCatalog content, int seed)
    {
        var cases = new List<object>();
        foreach (string role in new[] { "farmer", "woodcutter", "quarrier", "healer" })
        {
            using var world = WorldSession.Create(seed, content); var fixture = Prepare(world, role); var actor = world.ActorById(fixture.Actor);
            long balance = world.AccountedResources(), flow = world.RegionalResourceFlow, produced = world.State.NamedResourcesProduced;
            var stages = new List<object>(); (long, OccupationPhase)? previous = null; bool delivered = false; world.Paused = false;
            for (int i = 0; i < 1600 && !delivered; i++)
            {
                if (actor.Occupation is { } current && previous != (current.Id, current.Phase))
                {
                    previous = (current.Id, current.Phase); stages.Add(new { world.State.Tick, actor.Position, current.Id,
                        kind = current.Kind.ToString(), phase = current.Phase.ToString(), current.Target, current.Progress,
                        carried = new Dictionary<string, int>(current.Carried), home = new Dictionary<string, int>(world.State.Towns[actor.HomeTown].Stocks) });
                }
                var job = actor.Occupation;
                if (world.AdvanceTicks(1) != 1 || world.AccountedResources() != balance + world.RegionalResourceFlow - flow)
                    throw new InvalidDataException("Occupation preview froze or violated regional accounting: " + role);
                delivered = job is { Phase: OccupationPhase.Delivering } && actor.Occupation == null && job.Delivered.Values.Any(v => v > 0);
            }
            if (!delivered) throw new InvalidDataException("Occupation did not deliver in the bounded preview: " + role + " / " + world.RoutineDescription(actor));
            cases.Add(new { role, fixture, stages, final = new Dictionary<string, int>(world.State.Towns[actor.HomeTown].Stocks),
                namedProduced = world.State.NamedResourcesProduced - produced, playerOrders = world.State.Work.Count, world.State.Tick, conserved = true });
        }
        return new { passed = true, seed, cases, scope = "Natural crop/tree/rock extraction and real healer workplace construction, ingredient collection, crafting and delivery. Isolated approaches; real identities, source stocks, cargo and accounting. No player saves, graphics or model calls." };
    }
}
