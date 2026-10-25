using System.Text.Json;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class BodyScheduleTests
{
    [Theory]
    [InlineData(.04)] [InlineData(.015)] [InlineData(.016544117647058824)] [InlineData(0)]
    public void TimelineRetainsEveryFloatingAdditionAcrossReadAndRateBoundaries(double rate)
    {
        var need = new NeedTimeline(); need.Set(0, 12.345); need.RateAt(0, rate);
        double expected = 12.345;
        for (int tick = 1; tick <= 5000; tick++)
        {
            expected = Math.Clamp(expected + rate, 0, 100);
            if (tick % 73 == 0 || tick == 5000)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(need.At(tick)));
                need.At(tick - 1); Assert.Equal(expected, need.At(tick));
            }
        }
        need.RateAt(5000, -.15);
        for (int tick = 5001; tick <= 6000; tick++) expected = Math.Clamp(expected - .15, 0, 100);
        Assert.Equal(expected, need.At(6000));
        need.Reanchor(6000, 20); Assert.Equal(expected, need.At(20));
        need.Freeze(20); Assert.Equal(expected, need.At(0)); Assert.Equal(expected, need.At(1_000_000_000));
    }

    [Fact]
    public void ThresholdPredictionCannotAdvanceTheObservedBodyOrItsSavedValue()
    {
        long tick = 0; var body = new BodyState { Hunger = 34.95, Fatigue = 10 };
        body.Bind(() => tick, 0); body.RatesAt(0, .04, .015); tick = 1;
        string before = JsonSerializer.Serialize(body);
        Assert.Equal(1, body.ThresholdIn(50, 35)); Assert.Equal(before, JsonSerializer.Serialize(body));
        tick = 2; Assert.Equal((34.95 + .04) + .04, body.Hunger);
        body.Unbind(); tick = 100; Assert.Equal((34.95 + .04) + .04, body.Hunger);
    }

    [Theory]
    [InlineData(42)] [InlineData(1337)] [InlineData(-42)]
    public void ScheduledAndDetailedWorldsMatchThroughNeedsRestForageExpiryAndNearFarTransitions(int seed)
    {
        using var scheduled = WorldSession.Create(seed); using var detailed = WorldSession.Create(seed);
        detailed.DetailedBodiesEverywhere = true;
        foreach (var world in new[] { scheduled, detailed }) Prepare(world);
        int[] checkpoints = [1, 17, 23, 49, 50, 79, 80, 179, 180, 200, 250, 270, 280, 299, 300, 310, 411, 600, 900, 1500];
        for (int tick = 1; tick <= 1500; tick++)
        {
            foreach (var world in new[] { scheduled, detailed })
            {
                var actor = Resident(world);
                if (tick == 80) actor.Position = world.State.Position.Offset(2, 2);
                if (tick == 180) actor.Position = world.Plan.Towns[1].Center;
                if (tick == 200) { actor.Level = 8; actor.TraitId = world.Party.First().TraitId; actor.Body.Fatigue = 74.99; }
                if (tick == 250) actor.Body.RestUntil = world.State.Tick + 3;
                if (tick == 270) actor.Life = LifeState.Downed;
                if (tick == 280) actor.Life = LifeState.Alive;
                if (tick == 310) actor.Memories = [new("replacement", "New memory", world.State.Tick, world.State.Tick + 7, 0)];
                if (tick == 411) actor.Body = new() { Hunger = 34.99, Fatigue = 20, WorkDone = 5 };
                Assert.Equal(1, world.AdvanceTicks(1));
            }
            if (checkpoints.Contains(tick)) Assert.Equal(JsonSerializer.Serialize(detailed.State), JsonSerializer.Serialize(scheduled.State));
            Assert.InRange(scheduled.BodySimulation.QueuedDeadlines, 0, scheduled.State.Actors.Count);
        }
        Assert.True(scheduled.BodySimulation.Processed < detailed.BodySimulation.Processed / 3,
            $"Scheduled {scheduled.BodySimulation.Processed}, detailed {detailed.BodySimulation.Processed}");
    }

    [Fact]
    public void SavedContinuationReconstructsDeadlinesDespiteDifferentReadGrouping()
    {
        using var world = WorldSession.Create(42); Prepare(world); world.AdvanceTicks(137);
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-body-clock-" + Guid.NewGuid().ToString("N"));
        try
        {
            string file = Path.Combine(directory, "world.json"); world.Save(file);
            using var loaded = WorldSession.Load(file); loaded.Paused = false;
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            for (int i = 0; i < 700; i++)
            {
                // Looking at all distant bodies every tick must not change continuation.
                foreach (var actor in world.State.Actors) { _ = actor.Body.Hunger; _ = actor.Body.Fatigue; }
                world.AdvanceTicks(1); loaded.AdvanceTicks(1);
                if (i % 49 == 0 || i == 699) Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void RemovalReplacementDeathAndDisposalCannotKeepOldBodiesOnTheClock()
    {
        using var world = WorldSession.Create(42); world.Paused = false; world.AdvanceTicks(1);
        var town = world.Plan.Towns[5];
        var extra = ActorFactory.Create(world.Content, world.Content.Game.Villager, world.Seed, world.State.NextActorId++, town.Center, town.Id, town.Culture, town.FactionId);
        extra.Body.Hunger = 10; world.State.Actors.Add(extra); world.AdvanceTicks(3);
        Assert.True(extra.Body.Hunger > 10);
        world.State.Actors.Remove(extra); world.AdvanceTicks(1); double removed = extra.Body.Hunger;
        world.AdvanceTicks(20); Assert.Equal(removed, extra.Body.Hunger);
        var resident = Resident(world); var oldBody = resident.Body;
        resident.Body = new() { Hunger = 12.5, Fatigue = 20 }; world.AdvanceTicks(1);
        double replaced = oldBody.Hunger; world.AdvanceTicks(20); Assert.Equal(replaced, oldBody.Hunger);
        double expected = 12.5;
        for (int i = 0; i < 21; i++) expected = Math.Min(100, expected + world.Simulation.HungerPerTick);
        Assert.Equal(expected, resident.Body.Hunger);
        resident.Life = LifeState.Dead; world.AdvanceTicks(1); double dead = resident.Body.Hunger;
        var replacement = new ActorCollection(); replacement.AddRange(world.State.Actors);
        world.State.Actors = replacement; world.AdvanceTicks(1);
        Assert.Equal(dead, resident.Body.Hunger);
        world.Dispose(); world.State.Tick += 1000;
        Assert.Equal(dead, resident.Body.Hunger);
    }

    [Fact]
    public void DirectClockFixturesReanchorWithoutSimulatingOfflineNeeds()
    {
        using var world = WorldSession.Create(42); world.Paused = false; world.AdvanceTicks(10);
        var actor = Resident(world); double hunger = actor.Body.Hunger;
        world.State.Tick += 10000;
        Assert.Equal(hunger, actor.Body.Hunger);
        world.AdvanceTicks(1); Assert.Equal(hunger + world.Simulation.HungerPerTick, actor.Body.Hunger);
    }

    private static Actor Resident(WorldSession world) => world.State.Actors.First(a => a.Kind == ActorKind.Person && a.HomeTown == 1);
    [Fact]
    public void ReusableAuditExercisesRealPipelinesAndInterruptedContinuation()
    {
        var report = BodyScheduleAudit.Run(ContentCatalog.Default, [42], 100);
        Assert.True(report.Passed); var seed = Assert.Single(report.Seeds);
        Assert.True(seed.SaveContinued); Assert.True(seed.ScheduledVisits < seed.DetailedVisits / 3);
        Assert.All(seed.Checkpoints, point => Assert.Equal(point.ScheduledHash, point.DetailedHash));
    }

    private static void Prepare(WorldSession world)
    {
        var actor = Resident(world); actor.Body.Hunger = world.Simulation.MealAt - .01; actor.Body.Fatigue = 74.99;
        actor.Memories.Add(new("short-memory", "A brief thought", 0, 17, 0));
        var camp = world.Content.Get<CampDefinition>("camp.apple-broth");
        actor.Preparations.Add(new(camp.Id, camp.Effect!, camp.Power, 23));
        world.State.Towns[1].Stocks["food"] = 0;
        var forager = world.State.Actors.First(a => a.Kind == ActorKind.Person && a.HomeTown == 2);
        forager.Body.Hunger = 100; forager.Body.Fatigue = 0; world.State.Towns[2].Stocks["food"] = 0;
        world.Paused = false;
    }
}
