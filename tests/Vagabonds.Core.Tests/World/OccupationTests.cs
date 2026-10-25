using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class OccupationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-occupations-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private static WorldSession Prepare(string role, out Actor actor)
    {
        var world = WorldSession.Create(42); var fixture = OccupationPreview.Prepare(world, role);
        actor = world.ActorById(fixture.Actor); world.Paused = false; return world;
    }
    private static OccupationJob Until(WorldSession world, Actor actor, Func<OccupationJob, bool> predicate)
    {
        for (int i = 0; i < 1600; i++)
        { if (actor.Occupation is { } job && predicate(job)) return job; Assert.Equal(1, world.AdvanceTicks(1)); }
        throw new InvalidDataException(world.RoutineDescription(actor));
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void AuthoredPreviewExecutesAllFourProductionChains(int seed)
    { Assert.True(JsonSerializer.SerializeToNode(OccupationPreview.Run(ContentCatalog.Default, seed))!["passed"]!.GetValue<bool>()); }
    [Theory]
    [InlineData("farmer", "food")] [InlineData("woodcutter", "wood")] [InlineData("quarrier", "stone")]
    public void NaturalExtractionEntersRealCargoBeforeHomeStorage(string role, string stack)
    {
        using var world = Prepare(role, out var actor); var work = actor.Occupation!; int stock = world.State.Towns[actor.HomeTown].Stocks[stack];
        int pack = world.Count(stack); long resources = world.AccountedResources(), flow = world.RegionalResourceFlow;
        int? gathers = role == "farmer" ? world.ResourceAt(work.Target)!.Available : null;
        Until(world, actor, j => j.Phase == OccupationPhase.Delivering);
        Assert.Contains("Carrying", world.RoutineDescription(actor));
        Assert.Equal(work.Yield[stack], actor.Cargo[stack]); Assert.Equal(stock, world.State.Towns[actor.HomeTown].Stocks[stack]); Assert.Equal(pack, world.Count(stack));
        if (gathers != null) Assert.Equal(gathers - 1, world.ResourceAt(work.Target)!.Available); else Assert.Equal(Material.Earth, world.Chunks.At(work.Target));
        Assert.Equal(resources + world.RegionalResourceFlow - flow, world.AccountedResources());
        for (int i = 0; i < 500 && actor.Occupation == work; i++)
        {
            var previous = actor.Position; world.AdvanceTicks(1); Assert.InRange(previous.Distance(actor.Position), 0, 1);
            Assert.Equal(resources + world.RegionalResourceFlow - flow, world.AccountedResources());
        }
        Assert.NotSame(work, actor.Occupation); Assert.Equal(work.Yield[stack], work.Delivered[stack]); Assert.Equal(0, actor.Cargo[stack]); Assert.Equal(pack, world.Count(stack));
        Assert.DoesNotContain("Carrying", world.RoutineDescription(actor));
        Assert.InRange(actor.Position.Distance(world.Plan.Towns[actor.HomeTown].Center), 0, 1); Assert.Empty(world.State.Work);
    }
    [Fact]
    public void HealerBuildsAWorkplaceWithActualSuppliesThenCraftsAndDelivers()
    {
        using var world = Prepare("healer", out var actor); var build = actor.Occupation!; Assert.Equal(OccupationKind.BuildWorkplace, build.Kind);
        var initial = new Dictionary<string, int>(world.State.Towns[actor.HomeTown].Stocks);
        Until(world, actor, j => j.Kind == OccupationKind.BuildWorkplace && j.Phase == OccupationPhase.Working);
        foreach (var (stack, count) in build.Inputs)
        { Assert.Equal(count, actor.Cargo[stack]); Assert.Equal(initial[stack] - count, world.State.Towns[actor.HomeTown].Stocks[stack]); }
        var craft = Until(world, actor, j => j.Kind == OccupationKind.Craft && j.Phase == OccupationPhase.Working);
        Assert.Equal(Material.Camp, world.Chunks.At(build.Target)); Assert.Equal(build.Inputs, Assert.Single(world.State.Structures).Cost);
        foreach (var (stack, count) in craft.Inputs) Assert.Equal(count, craft.Carried[stack]);
        Until(world, actor, j => j.Kind == OccupationKind.Craft && j.Phase == OccupationPhase.Delivering);
        Assert.Equal(craft.Yield["medicine"], actor.Cargo["medicine"]); Assert.True(craft.Inputs.All(p => actor.Cargo.GetValueOrDefault(p.Key) == 0));
        world.AdvanceTicks(100); Assert.Equal(craft.Yield["medicine"], craft.Delivered["medicine"]);
        Assert.Equal(initial["medicine"] + craft.Yield["medicine"], world.State.Towns[actor.HomeTown].Stocks["medicine"]); Assert.Empty(world.State.Work);
    }
    [Theory]
    [InlineData("gather")] [InlineData("inputs")] [InlineData("output")] [InlineData("return")]
    public void SavedContinuationPreservesJobsCargoAndAccountingAcrossInterruption(string stage)
    {
        using var world = Prepare(stage is "inputs" or "return" ? "healer" : "farmer", out var actor);
        if (stage is "inputs" or "return") Until(world, actor, j => j.Phase == OccupationPhase.Working && j.Carried.Count > 0);
        if (stage == "output") Until(world, actor, j => j.Phase == OccupationPhase.Delivering);
        if (stage == "return")
        {
            var job = actor.Occupation!; actor.Position = job.Target.Offset(1, 0); world.Chunks.Edit(actor.Position, Material.Floor);
            world.Chunks.Edit(job.Target, Material.Water); Until(world, actor, j => j.Phase == OccupationPhase.Returning);
        }
        var far = JourneyPreview.PrepareFarField(world); world.State.Position = world.Party.Single().Position = far;
        string path = Path.Combine(_root, stage + ".json"); world.Save(path); using var loaded = WorldSession.Load(path); loaded.Paused = false;
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State)); long balance = world.AccountedResources(), flow = world.RegionalResourceFlow;
        for (int i = 0; i < 180; i++)
        {
            world.AdvanceTicks(1); loaded.AdvanceTicks(1); Assert.Equal(balance + world.RegionalResourceFlow - flow, world.AccountedResources());
            if (i % 11 == 0 || i == 179) Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
    }
    [Fact]
    public void FullStorageRetainsAPartialDeliveryAndAnExhaustedSourceCannotRunAgain()
    {
        using var world = Prepare("quarrier", out var actor); var job = Until(world, actor, j => j.Phase == OccupationPhase.Delivering);
        world.State.Towns[actor.HomeTown].Stocks["stone"] = 9998;
        for (int i = 0; i < 500 && job.Delivered.Count == 0; i++) world.AdvanceTicks(1);
        Assert.Equal(1, job.Delivered["stone"]); Assert.Equal(job.Yield["stone"] - 1, job.Carried["stone"]);
        long produced = world.State.NamedResourcesProduced, balance = world.AccountedResources(), flow = world.RegionalResourceFlow;
        world.AdvanceTicks(40); Assert.Same(job, actor.Occupation); Assert.Equal(produced, world.State.NamedResourcesProduced);
        Assert.Equal(balance + world.RegionalResourceFlow - flow, world.AccountedResources());
        world.State.Towns[actor.HomeTown].Stocks["stone"] -= 4; world.AdvanceTicks(1); Assert.Empty(job.Carried); Assert.NotSame(job, actor.Occupation);
    }
    [Fact]
    public void HungryWorkersMayEatTheirOwnHarvestButCannotEatCraftIngredients()
    {
        using var world = Prepare("farmer", out var farmer); var harvest = Until(world, farmer, j => j.Phase == OccupationPhase.Delivering);
        int amount = harvest.Carried["food"]; farmer.Body.Hunger = 40; world.AdvanceTicks(1);
        Assert.True(farmer.Body.Hunger < 10); Assert.Equal(1, harvest.Consumed["food"]); Assert.Equal(amount - 1, harvest.Carried["food"]);
        using var clinic = Prepare("healer", out var healer); var craft = Until(clinic, healer, j => j.Kind == OccupationKind.Craft && j.Phase == OccupationPhase.Working);
        clinic.State.Towns[healer.HomeTown].Stocks["food"] = 0; healer.Body.Hunger = 40; healer.Body.ForageAfter = clinic.State.Tick + 100;
        clinic.AdvanceTicks(1); Assert.True(healer.Body.Hunger >= 40); Assert.Equal(craft.Inputs["food"], healer.Cargo["food"]);
    }
    [Fact]
    public void RecruitmentTransfersOnlyOwnedJobCargoOnceAndAccountsForTheExport()
    {
        using var world = Prepare("farmer", out var actor); var job = Until(world, actor, j => j.Phase == OccupationPhase.Delivering); int food = world.Count("food");
        long raw = world.AccountedResources(), flow = world.RegionalResourceFlow, material = world.AccountedMaterial("food");
        world.State.Position = world.Party.Single().Position = actor.Position.Offset(0, 1); world.Chunks.Edit(world.State.Position, Material.Floor);
        Assert.True(world.Submit(new(WorldCommandKind.Recruit, ActorId: actor.Id)).Success);
        Assert.Null(actor.Occupation); Assert.Equal(food + job.Yield["food"], world.Count("food")); Assert.Equal(0, actor.Cargo["food"]);
        Assert.Equal(raw + world.RegionalResourceFlow - flow, world.AccountedResources()); Assert.Equal(material, world.AccountedMaterial("food"));
        Assert.False(world.Submit(new(WorldCommandKind.Recruit, ActorId: actor.Id)).Success); Assert.Equal(food + job.Yield["food"], world.Count("food"));
    }
    [Fact]
    public void WornWorkplacesAreRepairedUsingCarriedTownWood()
    {
        using var world = Prepare("healer", out var actor); Until(world, actor, j => j.Kind == OccupationKind.Craft);
        // Finish this real cycle before wear forces a separate maintenance trip.
        var craft = actor.Occupation!; for (int i = 0; i < 400 && actor.Occupation == craft; i++) world.AdvanceTicks(1);
        var station = Assert.Single(world.State.Structures); station.Condition = 20; actor.OccupationAfter = world.State.Tick;
        var repair = Until(world, actor, j => j.Kind == OccupationKind.RepairWorkplace && j.Phase == OccupationPhase.Working);
        Assert.Equal(1, repair.Carried["wood"]);
        for (int i = 0; i < 400 && actor.Occupation == repair; i++) world.AdvanceTicks(1);
        Assert.NotSame(repair, actor.Occupation); Assert.True(station.Condition > 95); Assert.Equal(0, actor.Cargo["wood"]);
    }
    [Fact]
    public void ChangedGatheringTargetsAndBlockedDeliveryDoNotDuplicateProduction()
    {
        using var world = Prepare("farmer", out var actor); var job = Until(world, actor, j => j.Phase == OccupationPhase.Delivering);
        int remaining = world.Chunks.ResourceStockAt(job.Target)!.Remaining; long produced = world.State.NamedResourcesProduced;
        var position = actor.Position; foreach (var (x, y) in WorldSession.Directions) world.Chunks.Edit(position.Offset(x, y), Material.Wall);
        world.AdvanceTicks(40); Assert.Equal(position, actor.Position); Assert.Same(job, actor.Occupation); Assert.Equal(produced, world.State.NamedResourcesProduced);
        Assert.Equal(remaining, world.Chunks.ResourceStockAt(job.Target)!.Remaining);
        var exit = WorldSession.Directions.Select(d => position.Offset(d.X, d.Y)).MinBy(p => p.Distance(world.Plan.Towns[actor.HomeTown].Center));
        world.Chunks.Edit(exit, Material.Floor); world.AdvanceTicks(100); Assert.NotSame(job, actor.Occupation); Assert.Equal(job.Yield["food"], job.Delivered["food"]);
    }
    [Fact]
    public void ActivatedWorkersStayOutOfRegionalProductionWhileUnvisitedWorkersStillProduce()
    {
        using var world = Prepare("farmer", out var actor);
        var remote = world.State.Actors.First(a => a.Kind == ActorKind.Person && a.Job == "farmer" && a.HomeTown != actor.HomeTown);
        foreach (var other in world.State.Actors.Where(a => !a.Recruited && a != actor && a != remote)) { other.Life = LifeState.Dead; other.Health = 0; }
        remote.Body.RestUntil = 0; remote.Body.Hunger = remote.Body.Fatigue = 0;
        foreach (var faction in world.State.Factions) Array.Fill(faction.Relations, 0);
        world.State.Tick = 2099; actor.Occupation = null; actor.OccupationAfter = 2399;
        var local = world.State.Towns[actor.HomeTown]; var distant = world.State.Towns[remote.HomeTown];
        int localFood = local.Stocks["food"], distantFood = distant.Stocks["food"], expected = (int)(12 * world.WorkRate(remote));
        long regional = world.State.RegionalResourcesProduced, named = world.State.NamedResourcesProduced;
        world.AdvanceTicks(1);
        Assert.Equal(1, actor.OccupationVersion); Assert.Equal(0, remote.OccupationVersion);
        Assert.Equal(localFood - 6, local.Stocks["food"]); Assert.Equal(distantFood + expected - 6, distant.Stocks["food"]);
        Assert.Equal(expected, world.State.RegionalResourcesProduced - regional); Assert.Equal(named, world.State.NamedResourcesProduced);
    }
    [Fact]
    public void RestAndPlayerAssignmentsPreserveTheSameOccupationUntilItCanResume()
    {
        using var world = Prepare("farmer", out var actor); world.AdvanceTicks(2); var job = actor.Occupation!;
        double progress = job.Progress; actor.Body.Fatigue = 80; world.AdvanceTicks(10);
        Assert.Same(job, actor.Occupation); Assert.Equal(progress, job.Progress); Assert.Equal(RoutineKind.Rest, actor.Routine!.Kind);
        actor.Body.Fatigue = 0; actor.Body.RestUntil = 0;
        var target = world.State.Position.Offset(1, 0); world.Chunks.Edit(target, Material.Rock); world.Reveal();
        Assert.True(world.Submit(new(WorldCommandKind.Dig, target)).Success); var assigned = Assert.Single(world.State.Work);
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: actor.Id, WorkId: assigned.Id)).Success);
        world.AdvanceTicks(10); Assert.Same(job, actor.Occupation); Assert.Equal(progress, job.Progress); Assert.Equal(RoutineKind.AssignedWork, actor.Routine!.Kind);
        Assert.True(world.Submit(new(WorldCommandKind.CancelWork, WorkId: assigned.Id)).Success);
        for (int i = 0; i < 200 && job.Progress == progress; i++) world.AdvanceTicks(1);
        Assert.True(job.Progress > progress); Assert.Same(job, actor.Occupation);
    }
    [Fact]
    public void OrdinaryOccupationsLeaveAllSixtyFourPlayerWorkSlotsAvailable()
    {
        using var world = Prepare("farmer", out var actor); var job = actor.Occupation!;
        var origin = world.State.Position;
        var targets = world.Visible.Where(p => p.X >= origin.X + 3 && p.Distance(origin) <= 14)
            .OrderBy(p => p.Y).ThenBy(p => p.X).Take(64).ToArray(); Assert.Equal(64, targets.Length);
        foreach (var target in targets) world.Chunks.Edit(target, Material.Rock);
        Assert.Equal(64, world.DesignateWork(WorldCommandKind.Dig, targets).Queued); world.StopPartyWorkApproach();
        long[] ids = world.State.Work.Select(w => w.Id).ToArray();
        Until(world, actor, j => j.Phase == OccupationPhase.Delivering);
        Assert.Same(job, actor.Occupation); Assert.Equal(ids, world.State.Work.Select(w => w.Id));
        Assert.DoesNotContain(job.Id, ids); Assert.Equal(job.Yield["food"], actor.Cargo["food"]);
    }
    [Fact]
    public void RecruitmentOverflowRemainsInAnActualRecoverableBundle()
    {
        using var world = Prepare("farmer", out var actor); var job = Until(world, actor, j => j.Phase == OccupationPhase.Delivering);
        world.State.Inventory["food"] = 9999; long amount = world.AccountedMaterial("food");
        world.State.Position = world.Party.Single().Position = actor.Position.Offset(0, 1); world.Chunks.Edit(world.State.Position, Material.Floor);
        Assert.True(world.Submit(new(WorldCommandKind.Recruit, ActorId: actor.Id)).Success);
        var bundle = Assert.Single(world.State.Loot, c => c.Id.StartsWith("work-return-", StringComparison.Ordinal));
        Assert.Equal(job.Yield["food"], bundle.Contents["food"]); Assert.InRange(bundle.Position.Distance(actor.Position), 0, 1);
        Assert.Equal(amount, world.AccountedMaterial("food")); Assert.Null(actor.Occupation); Assert.Equal(9999, world.Count("food"));
        string path = Path.Combine(_root, "overflow.json"); world.Save(path); using var loaded = WorldSession.Load(path);
        Assert.Equal(amount, loaded.AccountedMaterial("food")); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
    }
    [Theory]
    [InlineData("version")] [InlineData("id")] [InlineData("phase")] [InlineData("target")] [InlineData("yield")]
    [InlineData("progress")] [InlineData("cargo")] [InlineData("delivered")] [InlineData("counter")]
    public void MalformedOccupationStateIsRejectedWithoutRewritingTheSlot(string field)
    {
        using var world = Prepare("farmer", out var actor); Until(world, actor, j => j.Phase == OccupationPhase.Delivering);
        string path = Path.Combine(_root, "bad.json"); world.Save(path); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var person = json["state"]!["actors"]!.AsArray().First(a => a!["id"]!.GetValue<long>() == actor.Id)!; var job = person["occupation"]!;
        switch (field)
        {
            case "version": person["occupationVersion"] = 9; break;
            case "id": job["id"] = 0; break;
            case "phase": job["phase"] = 999; break;
            case "target": job["target"]!["floor"] = 1; break;
            case "yield": job["yield"]!["food"] = 9; break;
            case "progress": job["progress"] = 0; break;
            case "cargo": person["cargo"]!["food"] = 0; break;
            case "delivered": job["delivered"]!["food"] = 1; break;
            case "counter": json["state"]!["resourcesExported"] = -1; break;
        }
        File.WriteAllText(path, json.ToJsonString()); string before = File.ReadAllText(path);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(path)); Assert.Equal(before, File.ReadAllText(path));
    }
}
