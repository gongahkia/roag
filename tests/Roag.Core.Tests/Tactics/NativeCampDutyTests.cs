using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeCampDutyTests
{
    private static WorldSession Prepare(out Actor member, out WorldPosition camp)
    {
        var world = WorldSession.CreateDungeonRun(37, survival: true);
        member = world.Party.Single(); member.Tactics!.AutoEat = false;
        member.Body.Hunger = 45;
        camp = member.Position.Offset(0, -1);
        for (int y = -3; y <= 3; y++) for (int x = -3; x <= 3; x++) world.Chunks.Edit(member.Position.Offset(x, y), Material.Floor);
        world.Chunks.Edit(camp, Material.Camp); world.Reveal();
        foreach (var item in world.Content.Game.CampActivities.Select(world.Content.Get<CampDefinition>).SelectMany(d => d.Cost.Keys).Distinct())
            world.State.Inventory[world.Content.Get<ItemDefinition>(item).Stack] = 12;
        return world;
    }

    private static void Start(WorldSession world, WorldPosition camp, string duty)
    {
        var result = world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.CampActivity, camp, Item: duty)));
        Assert.True(result.Success, result.Message);
    }

    private static void Finish(WorldSession world, Actor member)
    {
        for (int i = 0; i < 200 && member.Tactics!.Activity != null; i++) world.ContinueDungeonActivities(32);
        Assert.Null(member.Tactics!.Activity); Assert.Empty(world.State.Work);
    }

    private static Actor SafeSeparatedMember(WorldSession world, ClassKind kind)
    {
        var member = DungeonPartyTests.AddMember(world, kind, 2);
        var room = world.State.Dungeon!.Floors.Single(f => f.Origin.Floor == 2).Rooms.Single(r => r.Role == "start");
        member.Position = room.Origin.Offset(room.Width / 2, room.Height / 2);
        world.Chunks.Edit(member.Position, Material.Floor); member.Body.Hunger = 45;
        return member;
    }

    [Theory]
    [InlineData("camp.apple-broth")]
    [InlineData("camp.mushroom-stew")]
    [InlineData("camp.honey-tea")]
    [InlineData("camp.cherry-eggs")]
    public void CookingPreservesAuthoredCostsAndWorkButFeedsAndBuffsOnlyOnPaidEating(string id)
    {
        using var world = Prepare(out var member, out var camp);
        var recipe = world.Content.Get<CampDefinition>(id);
        int provisions = world.Count("food"); double hunger = member.Body.Hunger;
        string before = JsonSerializer.Serialize(world.State);
        Assert.True(world.DungeonCombat.Preview(new(TacticalAction.StartWork, Work: new(WorldCommandKind.CampActivity, camp, Item: id))).Valid);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Start(world, camp, id);
        var order = Assert.Single(world.State.Work);
        Assert.Equal(recipe.Work, order.Required); Assert.Equal(0, order.Progress);
        foreach (var (item, amount) in recipe.Cost) Assert.Equal(12 - amount, world.Count(world.Content.Get<ItemDefinition>(item).Stack));
        Finish(world, member);
        Assert.Equal(1, world.State.Tactics!.PreparedMeals[id]); Assert.Empty(member.Preparations);
        Assert.True(member.Body.Hunger >= hunger); Assert.Equal(provisions, world.Count("food"));
        member.Body.Hunger = 33.25; member.Health -= 5; int hp = member.Health;
        member.Tactics!.Boons.Inventory = member.Tactics.Boons.Inventory.Grant("core:boon/overflow_chalice", BoonRarity.Common, ClassKind.Adventurer);
        var clock = world.DungeonCombat.Clock;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Eat, Item: id)).Success);
        Assert.Equal(clock + 100, world.DungeonCombat.Clock);
        Assert.Equal(0, member.Body.Hunger); Assert.Equal(hp, member.Health); Assert.Equal(provisions, world.Count("food"));
        Assert.Empty(world.State.Tactics.PreparedMeals);
        var buff = Assert.Single(member.Preparations);
        Assert.Equal(id, buff.Definition); Assert.Equal(recipe.Power, world.PreparationPower(member, recipe.Effect));
        Assert.Equal(1, member.Tactics.Boons.WardTotal(world.DungeonCombat.Clock));
        before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Eat, Item: id)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void SuspendedCookingRetainsIngredientsAndContinuesExactlyAcrossSave()
    {
        using var world = Prepare(out var member, out var camp);
        Start(world, camp, "camp.apple-broth"); world.ContinueDungeonActivities(4);
        var order = Assert.Single(world.State.Work); Assert.True(order.Progress > 0);
        Assert.True(world.CancelDungeonActivity(member.Id).Success); Assert.True(order.Suspended);
        double progress = order.Progress; long clock = world.DungeonCombat.Clock;
        string root = Path.Combine(Path.GetTempPath(), "roag-native-camp-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            var resume = new TacticalIntent(TacticalAction.StartWork, Work: new(WorldCommandKind.AssignWork, WorkId: order.Id));
            Assert.True(world.SubmitDungeon(resume).Success); Assert.True(loaded.SubmitDungeon(resume).Success);
            Assert.Equal(progress, order.Progress); Assert.Equal(clock + 100, world.DungeonCombat.Clock);
            Finish(world, member); Finish(loaded, loaded.Party.Single());
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(1, world.State.Tactics!.PreparedMeals["camp.apple-broth"]);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void TreatmentRemovesNearbyWoundsWithActualHealingCreditAndNoCrossFloorHealing()
    {
        using var world = Prepare(out var member, out var camp);
        var distant = SafeSeparatedMember(world, ClassKind.Ranger);
        member.Health -= 12; member.Body.Wounds = 3; distant.Health -= 12; distant.Body.Wounds = 3;
        int before = member.Health, remote = distant.Health;
        Start(world, camp, "camp.treatment");
        var watching = world.SubmitDungeon(new(TacticalAction.Watch, Choice: 60)); Assert.True(watching.Success, watching.Message);
        Finish(world, member);
        Assert.Equal(before + 12, member.Health); Assert.Equal(1, member.Body.Wounds);
        Assert.Equal(remote, distant.Health); Assert.Equal(3, distant.Body.Wounds);
        Assert.Contains(world.State.Tactics!.Events, e => e.Kind == "Healed" && e.Source == member.Id && e.Target == member.Id && e.Amount == 12 && e.Ability == "camp.treatment");
        Assert.Equal(11, world.Count("medicine"));
    }

    [Fact]
    public void StoriesRevealARealSiteAndRespectTheSpeakersCooldown()
    {
        using var world = Prepare(out var member, out var camp); int sites = world.State.KnownSites.Count;
        member.Memories.Add(new("fallen-friend", "Our friend fell on the old road.", 0, 1800, -8));
        Start(world, camp, "camp.stories"); Finish(world, member);
        Assert.Equal(sites + 1, world.State.KnownSites.Count); Assert.Contains("friend", world.State.LastCampStory!.Text);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.CampActivity, camp, Item: "camp.stories"))).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void CookingCreatesOnlyPhysicalNearbyServingsWhichRemainGloballySharedAndIndividuallyEaten()
    {
        using var world = Prepare(out var cook, out var camp);
        var nearby = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 1);
        nearby.Position = cook.Position.Offset(1, 0); nearby.Body.Hunger = 45;
        var separated = SafeSeparatedMember(world, ClassKind.Berserker);
        var walled = DungeonPartyTests.AddMember(world, ClassKind.Arcanist, 1);
        walled.Position = camp.Offset(0, -2); walled.Body.Hunger = 45;
        world.Chunks.Edit(camp.Offset(0, -1), Material.Wall);
        Start(world, camp, "camp.apple-broth");
        foreach (var member in new[] { nearby, separated, walled })
        { Assert.Equal(member.Id, world.DungeonMember.Id); var result = world.SubmitDungeon(new(TacticalAction.Watch, Choice: 240)); Assert.True(result.Success, result.Message); }
        Finish(world, cook);
        Assert.Equal(2, world.State.Tactics!.PreparedMeals["camp.apple-broth"]);
        Assert.All(world.Party, a => Assert.Empty(a.Preparations));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Eat, Item: "camp.apple-broth")).Success);
        Assert.Equal(1, world.State.Tactics.PreparedMeals["camp.apple-broth"]);
        Assert.Equal(2, world.PreparationPower(cook, "burn-resist"));
        Assert.Equal(0, world.PreparationPower(nearby, "burn-resist"));
        // The stock is shared across floors, but the separated eater still pays
        // their own scheduled action and keeps the already committed recovery.
        Assert.True(world.CancelDungeonActivity(separated.Id).Success);
        for (int i = 0; i < 200 && world.State.Tactics.ActiveActor != separated.Id; i++)
        {
            if (world.DungeonMember.Tactics!.Activity != null) world.ContinueDungeonActivities(1);
            else Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        }
        Assert.Equal(separated.Id, world.DungeonMember.Id);
        var clock = world.DungeonCombat.Clock;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Eat, Item: "camp.apple-broth")).Success);
        Assert.Empty(world.State.Tactics.PreparedMeals); Assert.Equal(2, world.PreparationPower(separated, "burn-resist"));
        Assert.Equal(clock + ActionTiming.Recovery(100, separated.Speed), world.State.Tactics.Queue.Snapshot().Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == separated.Id).Due);
    }

    [Fact]
    public void WatchPreparationImprovesFutureRecoveryWithoutAutomaticallyGuardingOrRetimingOthers()
    {
        using var world = Prepare(out var watcher, out var camp);
        var other = SafeSeparatedMember(world, ClassKind.Ranger);
        Start(world, camp, "camp.watch");
        Assert.True(world.SubmitDungeon(new(TacticalAction.Watch, Choice: 240)).Success);
        Finish(world, watcher);
        Assert.Equal(2, world.PreparationPower(watcher, "watch")); Assert.False(watcher.Tactics!.Guarding);
        Assert.Equal(0, world.PreparationPower(other, "watch"));
        var queued = world.State.Tactics!.Timeline;
        var scheduled = queued.Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == other.Id);
        var preview = world.DungeonCombat.Preview(new(TacticalAction.Guard));
        Assert.Equal(ActionTiming.Recovery(100, watcher.Speed + 2), preview.Recovery);
        // Inspect the commit boundary before advancing to another member's
        // naturally due readiness (which removes that scheduled item).
        Assert.True(world.DungeonCombat.Commit(new(TacticalAction.Guard)).Success); Assert.True(watcher.Tactics.Guarding);
        Assert.Contains(world.State.Tactics.Timeline.Pending, w => w == scheduled);
    }

    [Fact]
    public void ChangedVisibleCampRefundsUnfinishedIngredientsWithoutProducingMeals()
    {
        using var world = Prepare(out var member, out var camp);
        Start(world, camp, "camp.apple-broth"); world.ContinueDungeonActivities(4);
        world.Chunks.Edit(camp, Material.Floor);
        world.ContinueDungeonActivities(1);
        Assert.Null(member.Tactics!.Activity); Assert.Empty(world.State.Work);
        Assert.Equal(12, world.Count("apple")); Assert.Equal(12, world.Count("herbs"));
        Assert.Empty(world.State.Tactics!.PreparedMeals);
    }

    [Fact]
    public void ASeparatedWorkersUnobservedDestroyedCampDoesNotInvalidateTheirSuspendedContinuingSave()
    {
        using var world = Prepare(out var member, out var camp);
        Start(world, camp, "camp.apple-broth"); world.ContinueDungeonActivities(4);
        var order = Assert.Single(world.State.Work);
        Assert.True(world.CancelDungeonActivity(member.Id).Success);
        member.Position = world.State.Position = world.State.Dungeon!.Floors.Single(f => f.Origin.Floor == 2).Entry;
        world.Reveal(); world.Chunks.Edit(camp, Material.Floor);
        Assert.DoesNotContain(camp, world.Visible);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-hidden-camp-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.True(Assert.Single(loaded.State.Work).Suspended);
            Assert.True(world.CancelDungeonWork(order.Id).Success); Assert.True(loaded.CancelDungeonWork(order.Id).Success);
            Assert.Equal(12, loaded.Count("apple")); Assert.Equal(12, loaded.Count("herbs"));
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void AWatchPreparationDutySoundsTheSamePhysicalAlarmAndSuspendsOnlyInformedMembers()
    {
        using var world = Prepare(out var watcher, out var camp);
        var sleeper = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 1);
        var far = SafeSeparatedMember(world, ClassKind.Sentinel);
        var origin = watcher.Position;
        for (int y = -2; y <= 2; y++) for (int x = 1; x <= 12; x++) world.Chunks.Edit(origin.Offset(x, y), Material.Floor);
        sleeper.Position = origin.Offset(6, 0);
        Start(world, camp, "camp.watch");
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 60)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 60)).Success);
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy is { Dormant: false } && a.Position.Floor == 1);
        enemy.Position = origin.Offset(10, 0);
        world.ContinueDungeonActivities(1);
        Assert.Null(watcher.Tactics!.Activity); Assert.Null(sleeper.Tactics!.Activity); Assert.NotNull(far.Tactics!.Activity);
        Assert.True(Assert.Single(world.State.Work).Suspended);
        Assert.Contains(world.State.Tactics!.Events, e => e.Kind == "CampAlarm" && e.Source == watcher.Id);
        Assert.Empty(watcher.Preparations);
    }

    [Theory]
    [InlineData("camp.apple-broth", 0)]
    [InlineData("camp.apple-broth", 1001)]
    [InlineData("camp.watch", 1)]
    [InlineData("camp.missing", 1)]
    public void PreparedServingSaveValidationRejectsForgedStock(string id, int quantity)
    {
        using var world = Prepare(out _, out _);
        world.State.Tactics!.PreparedMeals[id] = quantity;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State, world.Content));
    }

    [Fact]
    public void ConcurrentCooksCannotOverbookSharedServingCapacityOrReserveExtraIngredients()
    {
        using var world = Prepare(out var cook, out var camp);
        var other = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 1); other.Position = cook.Position.Offset(1, 0);
        var secondCamp = other.Position.Offset(0, 1); world.Chunks.Edit(secondCamp, Material.Camp); world.Reveal();
        world.State.Tactics!.PreparedMeals["camp.apple-broth"] = 996;
        Start(world, camp, "camp.apple-broth");
        Assert.Equal(other.Id, world.DungeonMember.Id);
        string before = JsonSerializer.Serialize(world.State);
        var result = world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.CampActivity, secondCamp, Item: "camp.apple-broth")));
        Assert.False(result.Success); Assert.Contains("servings", result.Message);
        Assert.Equal(before, JsonSerializer.Serialize(world.State)); Assert.Single(world.State.Work);
        Assert.Equal(10, world.Count("apple")); Assert.Equal(11, world.Count("herbs"));
    }
}
