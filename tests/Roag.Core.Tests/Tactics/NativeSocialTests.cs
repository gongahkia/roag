using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeSocialTests
{
    private sealed class Visit : IDisposable
    {
        public WorldSession World { get; } = WorldSession.CreateNativeCampaign(42);
        public Actor Hero { get; }
        public Actor Person => World.ActorById(1000);
        public TacticalCombat Combat => World.DungeonCombat;
        public Visit()
        {
            Hero = World.Party.Single(); Hero.Tactics!.AutoEat = false;
            Place(Hero, Person);
        }
        public void Place(Actor member, Actor person, int floor = 1)
        {
            foreach (var other in World.State.Actors.Where(a => a.Id != member.Id && a.Id != person.Id
                && a.Position == new WorldPosition(101, 100, floor))) other.Position = new(104, 104, floor);
            member.Position = new(100, 100, floor); person.Position = new(101, 100, floor);
            for (int y = 96; y <= 104; y++) for (int x = 96; x <= 104; x++) World.Chunks.Edit(new(x, y, floor), Material.Floor);
            Assert.True(World.SelectDungeonMember(member.Id).Success);
        }
        public Actor Recruit()
        {
            var candidate = World.ActorById(World.NativeAdventurers[0].Actor);
            Hero.Position = candidate.Position.Offset(0, -1); World.Chunks.Edit(Hero.Position, Material.Floor);
            Assert.True(World.SelectDungeonMember(Hero.Id).Success);
            Assert.True(World.SubmitRoag(new("recruit", Target: candidate.Id)).Success);
            Assert.True(World.SplitDungeonMember(candidate.Id).Success); candidate.Tactics!.AutoEat = false;
            Place(Hero, Person); candidate.Position = new(100, 100, 2); World.Chunks.Edit(candidate.Position, Material.Floor);
            return candidate;
        }
        public void Dispose() => World.Dispose();
    }

    [Fact]
    public void StoriesHavePureQuotesAndOwnPaidRecoveryWithPersistentMutualTrust()
    {
        using var f = new Visit();
        var intent = new TacticalIntent(TacticalAction.SocialInteraction, Target: f.Person.Id, Item: "interaction.stories");
        string before = JsonSerializer.Serialize(f.World.State);
        Assert.True(f.World.QuoteNativeInteraction(f.Hero.Id, f.Person.Id, intent.Item).Valid);
        var preview = f.Combat.Preview(intent); Assert.True(preview.Valid);
        Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
        long clock = f.Combat.Clock;
        Assert.True(f.Combat.Commit(intent).Success);
        Assert.Equal(3, f.Person.Trust.GetValueOrDefault(f.Hero.Id)); Assert.Equal(3, f.Hero.Trust.GetValueOrDefault(f.Person.Id));
        Assert.Contains(f.Person.Memories, m => m.Key == "conversation" && m.Other == f.Hero.Id);
        Assert.Equal(clock + preview.Recovery, Assert.Single(f.Combat.State.Timeline.Pending, w => w.ActorId == f.Hero.Id).Due);
        f.Combat.AdvanceToPartyDecision(); Assert.True(f.World.PerformInteraction(f.Person.Id, intent.Item).Success);
        Assert.Equal(3, f.Person.Trust[f.Hero.Id]); // The retained memory cooldown prevents repeated trust farming.
        RoundTrip(f.World);
    }

    [Fact]
    public void PaidRumourRevealsMetadataWithoutRevealingDistantTerrainOrConsumingRandomness()
    {
        using var f = new Visit();
        string chunks = JsonSerializer.Serialize(f.World.State.Chunks);
        uint random = f.World.State.CombatRandom; int nativeRandom = f.Combat.State.RandomState;
        Assert.True(f.Combat.Commit(new(TacticalAction.SocialInteraction, Target: f.Person.Id, Item: "interaction.rumours")).Success);
        var discovery = Assert.Single(f.World.State.Discoveries, d => d.Known && d.TownId == f.Person.HomeTown);
        Assert.False(f.World.Chunks.Seen(discovery.Position)); Assert.Equal(chunks, JsonSerializer.Serialize(f.World.State.Chunks));
        Assert.Equal(random, f.World.State.CombatRandom); Assert.Equal(nativeRandom, f.Combat.State.RandomState);
        Assert.Contains(f.World.State.Journal, line => line.Contains("tiles", StringComparison.Ordinal));
        Assert.Contains(f.Combat.State.Timeline.Pending, w => w.ActorId == f.Hero.Id && w.Due > 0);
        RoundTrip(f.World);
    }

    [Theory]
    [InlineData("distance")]
    [InlineData("floor")]
    [InlineData("wall")]
    [InlineData("cost")]
    [InlineData("healthy")]
    [InlineData("offered")]
    [InlineData("danger")]
    public void InvalidSocialActionsRejectBeforeInventoryMemoryOrTimelineChanges(string failure)
    {
        using var f = new Visit(); string interaction = "interaction.stories"; var person = f.Person;
        if (failure == "distance") person.Position = person.Position.Offset(8, 0);
        if (failure == "floor") person.Position = person.Position with { Floor = 2 };
        if (failure == "wall") { person.Position = new(102, 100, 1); f.World.Chunks.Edit(new(101, 100, 1), Material.Wall); }
        if (failure == "cost") { interaction = "interaction.meal"; f.World.State.Inventory["food"] = 0; }
        if (failure == "healthy") { person = f.World.ActorById(1004); f.Place(f.Hero, person); interaction = "interaction.healing"; }
        if (failure == "offered") interaction = "interaction.healing";
        if (failure == "danger")
        {
            var cell = f.Hero.Position.Offset(0, 1);
            var enemy = ActorFactory.Create(f.World.Content, f.World.Content.Game.Raider, 42, f.World.State.NextActorId++, cell, 0);
            f.World.State.Actors.Add(enemy); f.Combat.RegisterRetainedEnemy(enemy, f.Combat.Clock + 1000);
        }
        string before = JsonSerializer.Serialize(f.World.State);
        Assert.False(f.Combat.Commit(new(TacticalAction.SocialInteraction, Target: person.Id, Item: interaction)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
    }

    [Fact]
    public void SharingFoodFeedsOnlyThePayingMemberAndNpcAndUsesTheOwnersHungerOverflowWard()
    {
        using var f = new Visit(); var friend = f.Recruit();
        f.Hero.Health -= 5; f.Hero.Body.Hunger = 2; f.Person.Body.Hunger = 40;
        friend.Body.Hunger = 60; friend.Stress = 20;
        f.Hero.Tactics!.Boons.Inventory = f.Hero.Tactics.Boons.Inventory.Grant("core:boon/overflow_chalice", BoonRarity.Common, ClassKind.Adventurer);
        int food = f.World.Count("food"), hp = f.Hero.Health; long consumed = f.World.State.ResourcesConsumed;
        Assert.True(f.Combat.Commit(new(TacticalAction.SocialInteraction, Target: f.Person.Id, Item: "interaction.meal")).Success);
        Assert.Equal(food - 1, f.World.Count("food")); Assert.Equal(consumed + 1, f.World.State.ResourcesConsumed);
        Assert.Equal(0, f.Hero.Body.Hunger); Assert.Equal(40 - f.World.Simulation.MealReduction, f.Person.Body.Hunger);
        Assert.Equal(hp, f.Hero.Health); Assert.Equal(2, f.Hero.Tactics.Boons.WardTotal(f.Combat.Clock));
        Assert.Equal(60, friend.Body.Hunger); Assert.Equal(20, friend.Stress);
        Assert.DoesNotContain(friend.Memories, m => m.Key == "shared-meal");
        Assert.Equal(f.Hero.Id, f.Person.Consequences[ConsequenceKind.SuppliedFood].Other);
    }

    [Fact]
    public void AFedMemberCanOfferFoodWithoutTriggeringPersonalEatingBoonsOrFeedingCompanions()
    {
        using var f = new Visit(); f.Hero.Body.Hunger = 0;
        f.Hero.Tactics!.Boons.Inventory = f.Hero.Tactics.Boons.Inventory.Grant("core:boon/overflow_chalice", BoonRarity.Common, ClassKind.Adventurer);
        Assert.True(f.Combat.Commit(new(TacticalAction.SocialInteraction, Target: f.Person.Id, Item: "interaction.meal")).Success);
        Assert.DoesNotContain(f.Combat.State.Events, e => e.Kind == "FoodEaten");
        Assert.Equal(0, f.Hero.Tactics.Boons.WardTotal(f.Combat.Clock));
    }

    [Fact]
    public void TreatmentConsumesItsFeeAndHealsOnlyTheActingMemberWithAnExcessHealingWard()
    {
        using var f = new Visit(); var friend = f.Recruit(); var healer = f.World.ActorById(1004);
        f.Place(f.Hero, healer); f.Hero.Health -= 3; f.Hero.Body.Wounds = 1;
        friend.Health = 1; friend.Body.Wounds = 3;
        f.Hero.Tactics!.Boons.Inventory = f.Hero.Tactics.Boons.Inventory.Grant("core:boon/overflow_chalice", BoonRarity.Common, ClassKind.Adventurer);
        int gold = f.World.Count("gold");
        Assert.True(f.Combat.Commit(new(TacticalAction.SocialInteraction, Target: healer.Id, Item: "interaction.healing")).Success);
        Assert.Equal(gold - 5, f.World.Count("gold")); Assert.Equal(0, f.Hero.Body.Wounds); Assert.Equal(f.Hero.MaxHealth, f.Hero.Health);
        Assert.Equal(1, friend.Health); Assert.Equal(3, friend.Body.Wounds);
        Assert.Equal(2, f.Hero.Tactics.Boons.WardTotal(f.Combat.Clock));
        Assert.Contains(f.Combat.State.Events, e => e.Kind == "CurrencySpent" && e.Source == f.Hero.Id && e.Amount == 5);
        RoundTrip(f.World);
    }

    [Fact]
    public void BrowsingTradeIsFreeAndRegularResidentsCannotBypassFixedClassRecruitment()
    {
        using var f = new Visit(); string before = JsonSerializer.Serialize(f.World.State);
        Assert.True(f.World.PerformInteraction(f.Person.Id, "interaction.trade").Success);
        Assert.False(f.World.PerformInteraction(f.Person.Id, "interaction.recruit").Success);
        Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
    }

    [Fact]
    public void ASeparatedReadySpeakerUsesTheirOwnRelationshipsAndSightWhileTheInspectedLeaderFightsElsewhere()
    {
        using var f = new Visit(); var member = f.Recruit();
        for (int i = 0; i < 8 && f.Combat.Active?.Id != member.Id; i++)
        {
            Assert.True(f.World.SelectDungeonMember(f.World.State.Tactics!.ActiveActor).Success);
            Assert.True(f.World.SubmitRoag(new("wait")).Success);
        }
        Assert.Equal(member.Id, f.Combat.Active?.Id);
        f.Hero.Position = new(100, 100, 2); f.Place(member, f.Person);
        var cell = f.Hero.Position.Offset(1, 0); f.World.Chunks.Edit(cell, Material.Floor);
        var enemy = ActorFactory.Create(f.World.Content, f.World.Content.Game.Raider, 42, f.World.State.NextActorId++, cell, 0);
        f.World.State.Actors.Add(enemy); f.Combat.RegisterRetainedEnemy(enemy, f.Combat.Clock + 600);
        Assert.True(f.Combat.IsLocallyEngaged(f.Hero.Id)); Assert.False(f.Combat.IsLocallyEngaged(member.Id));
        Assert.True(f.World.SelectDungeonMember(f.Hero.Id).Success);
        var recovery = f.Combat.State.Timeline.Pending.Single(w => w.ActorId == f.Hero.Id);
        Assert.True(f.Combat.Commit(new(TacticalAction.SocialInteraction, Target: f.Person.Id, Item: "interaction.stories")).Success);
        Assert.Equal(3, f.Person.Trust.GetValueOrDefault(member.Id)); Assert.Equal(0, f.Person.Trust.GetValueOrDefault(f.Hero.Id));
        Assert.Contains(recovery, f.Combat.State.Timeline.Pending); Assert.Equal(f.Hero.Position, f.World.State.Position);
        var lines = WorldDialogue.Lines(f.World, f.Person.Id, "interaction.stories", listener: member.Id);
        Assert.Contains(lines, line => line.Speaker == member.Id); Assert.DoesNotContain(lines, line => line.Speaker == f.Hero.Id);
    }

    private static void RoundTrip(WorldSession world)
    {
        string folder = Path.Combine(Path.GetTempPath(), "roag-native-social-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(folder, "campaign.json"); world.Save(path);
            using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(world.DungeonCombat.AdvanceToPartyDecision()?.Id, loaded.DungeonCombat.AdvanceToPartyDecision()?.Id);
            Assert.True(world.SelectDungeonMember(world.State.Tactics!.ActiveActor).Success);
            Assert.True(loaded.SelectDungeonMember(loaded.State.Tactics!.ActiveActor).Success);
            Assert.True(world.SubmitRoag(new("wait")).Success); Assert.True(loaded.SubmitRoag(new("wait")).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
