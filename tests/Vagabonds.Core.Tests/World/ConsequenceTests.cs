using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class ConsequenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-consequences-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static WorldSession Prepare(out ConsequenceFixture fixture, out Actor person)
    { var world = WorldSession.Create(42); fixture = ConsequencePreview.Prepare(world); person = world.ActorById(fixture.Actor); return world; }
    private static string State(WorldSession world) => JsonSerializer.Serialize(world.State);
    private static WorkOrder QueueOtherWork(WorldSession world)
    {
        Assert.True(world.Submit(new(WorldCommandKind.Build, world.State.Position.Offset(0, 1), Material: Material.Wall)).Success);
        return Assert.Single(world.State.Work);
    }
    private static void Approach(WorldSession world, Actor person)
    {
        var p = person.Position.Offset(1, 0); world.Chunks.Edit(p, Material.Floor);
        world.State.Position = world.Party.Single().Position = p; world.Reveal();
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void PreviewExecutesAllFourConsequencesThroughRealActions(int seed)
    { Assert.True(JsonSerializer.SerializeToNode(ConsequencePreview.Run(ContentCatalog.Default, seed))!["passed"]!.GetValue<bool>()); }

    [Fact]
    public void QueuedDamageCapturesTheOriginalStakeholderButCancellingCostsNoTrust()
    {
        using var world = Prepare(out var fixture, out var person);
        Assert.True(world.Submit(new(WorldCommandKind.Build, fixture.Source, Material: Material.Floor)).Success);
        var order = Assert.Single(world.State.Work); Assert.Equal(world.Party.Single().Id, order.RequestedBy);
        Assert.Equal(new[] { person.Id }, order.LivelihoodStakeholders); Assert.Empty(person.Consequences);
        Assert.True(world.Submit(new(WorldCommandKind.CancelWork, WorkId: order.Id)).Success);
        Assert.Empty(person.Consequences); Assert.Equal(0, person.Trust.GetValueOrDefault(world.Party.Single().Id));
    }
    [Fact]
    public void ActualDamageChangesDialogueAndRefusesNewWorkAndRecruitmentWithoutCost()
    {
        using var world = Prepare(out var fixture, out var person); ConsequencePreview.DamageSource(world, fixture);
        long leader = world.Party.Single().Id;
        Assert.Equal(-12, person.Trust[leader]); Assert.StartsWith("work:", person.Consequences[ConsequenceKind.DamagedLivelihood].Event);
        Assert.Contains("livelihood", world.ConsequenceDialogue(person, world.Party.Single()));
        var order = QueueOtherWork(world); Approach(world, person);
        var assign = new WorldCommand(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: order.Id);
        Assert.Contains("trust", world.CommandBlockReason(assign));
        string before = State(world);
        Assert.False(world.Submit(assign).Success); Assert.False(world.Submit(new(WorldCommandKind.Recruit, ActorId: person.Id)).Success);
        Assert.False(world.PerformInteraction(person.Id, "interaction.recruit").Success); Assert.Equal(before, State(world));
        Assert.NotEqual(RoutineKind.Social, person.Routine?.Kind);
    }
    [Fact]
    public void FoodReliefUsesRealStocksRepairsTrustAndCannotBeRepeatedForTheSameTask()
    {
        using var world = Prepare(out var fixture, out var person); ConsequencePreview.DamageSource(world, fixture);
        int food = world.Count("food"), gold = world.Count("gold"); long imports = world.State.ResourcesImported;
        Assert.True(world.Submit(new(WorldCommandKind.CompleteDelivery)).Success);
        Assert.Equal(food - 20, world.Count("food")); Assert.Equal(gold + 30, world.Count("gold"));
        Assert.Equal(20, world.State.Towns[fixture.Town].Stocks["food"]); Assert.Equal(imports + 20, world.State.ResourcesImported);
        Assert.Equal(-4, person.Trust[world.Party.Single().Id]); Assert.Empty(world.ResidentGrievance(person));
        Assert.Contains("food", world.ConsequenceDialogue(person, world.Party.Single()));
        Assert.All(world.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited && a.HomeTown == fixture.Town),
            a => Assert.True(a.Consequences.ContainsKey(ConsequenceKind.SuppliedFood)));
        Assert.All(world.State.Actors.Where(a => a.Kind == ActorKind.Person && a.HomeTown != fixture.Town), a => Assert.Empty(a.Consequences));
        string before = State(world); Assert.False(world.Submit(new(WorldCommandKind.CompleteDelivery)).Success); Assert.Equal(before, State(world));
        var order = QueueOtherWork(world); Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: order.Id)).Success);
    }
    [Fact]
    public void RoutinePaidDeliveryToAWellSuppliedTownDoesNotInventFoodNeed()
    {
        using var world = Prepare(out var fixture, out var person); world.State.Towns[fixture.Town].Shortage = 0; world.State.Towns[fixture.Town].Stocks["food"] = 100;
        Assert.True(world.Submit(new(WorldCommandKind.CompleteDelivery)).Success); Assert.Empty(person.Consequences);
    }
    [Fact]
    public void AMealForAHungryResidentCostsFoodAndItsCooldownSurvivesMemoryEviction()
    {
        using var world = Prepare(out _, out var person); Approach(world, person); person.Body.Hunger = 40;
        int food = world.Count("food"); Assert.True(world.PerformInteraction(person.Id, "interaction.meal").Success);
        Assert.Equal(food - 1, world.Count("food")); Assert.True(person.Body.Hunger < 40);
        long leader = world.Party.Single().Id; Assert.Equal(8, person.Trust[leader]);
        person.Memories.Clear(); person.Body.Hunger = 40;
        Assert.Contains("food", world.ConsequenceDialogue(person, world.Party.Single()));
        Assert.True(world.PerformInteraction(person.Id, "interaction.meal").Success);
        Assert.Equal(8, person.Trust[leader]); Assert.Single(person.Consequences);
        world.State.Tick += WorldSession.ConsequenceDuration; person.Body.Hunger = 40;
        Assert.True(world.PerformInteraction(person.Id, "interaction.meal").Success); Assert.Equal(16, person.Trust[leader]);
    }
    [Fact]
    public void OverridingAnActualOccupationIsRememberedButAutomaticNeedInterruptionsAreNot()
    {
        using var world = Prepare(out _, out var person); var occupation = person.Occupation!; Assert.True(occupation.Progress > 0);
        var order = QueueOtherWork(world);
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: order.Id)).Success);
        long leader = world.Party.Single().Id; Assert.Equal(-3, person.Trust[leader]); Assert.Same(occupation, person.Occupation);
        Assert.Contains("interrupted", world.ConsequenceDialogue(person, world.Party.Single()));
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: order.Id)).Success); Assert.Equal(-3, person.Trust[leader]);
        person.Body.Fatigue = 80; world.Paused = false; world.AdvanceTicks(5);
        Assert.Equal(RoutineKind.Rest, person.Routine!.Kind); Assert.Equal(-3, person.Trust[leader]); Assert.Equal(person.Id, order.Worker);
    }
    [Theory]
    [InlineData("suspend")] [InlineData("automatic")] [InlineData("cancel")] [InlineData("reassign")]
    public void DeliberateInterruptionOfStartedAssignedWorkIsCountedOnlyOnce(string action)
    {
        using var world = Prepare(out _, out var person); person.Occupation = null; person.OccupationAfter = world.State.Tick + 1000;
        var order = QueueOtherWork(world); Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: order.Id)).Success);
        world.Paused = false;
        for (int i = 0; i < 100 && order.Progress == 0; i++) world.AdvanceTicks(1);
        Assert.True(order.Progress > 0); Assert.Empty(person.Consequences);
        var command = action == "cancel" ? new WorldCommand(WorldCommandKind.CancelWork, WorkId: order.Id)
            : action == "reassign" ? new(WorldCommandKind.AssignWork, ActorId: world.Party.Single().Id, WorkId: order.Id)
            : new(WorldCommandKind.ConfigureWork, WorkId: order.Id, Item: action);
        Assert.True(world.Submit(command).Success); Assert.Equal(-3, person.Trust[world.Party.Single().Id]);
        if (action != "cancel") { Assert.True(world.Submit(command).Success); Assert.Equal(-3, person.Trust[world.Party.Single().Id]); }
    }
    [Fact]
    public void AChangedTargetReleasesAnAssignedOrderWithoutBlamingThePlayer()
    {
        using var world = Prepare(out _, out var person); person.Occupation = null;
        var order = QueueOtherWork(world); Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: order.Id)).Success);
        world.Paused = false; for (int i = 0; i < 100 && order.Progress == 0; i++) world.AdvanceTicks(1);
        Assert.True(order.Progress > 0); world.Chunks.Edit(order.Target, Material.Earth); world.AdvanceTicks(1);
        Assert.Empty(world.State.Work); Assert.Empty(person.Consequences);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OnlyWinningAgainstTheThreatTheResidentFearsCreatesAid(bool retreat)
    {
        using var world = Prepare(out var fixture, out var person); long enemy = ConsequencePreview.PrepareDanger(world, fixture);
        world.StartBattle(enemy);
        if (retreat) Assert.True(world.Act(world.ActiveSkills().ToList().FindIndex(s => s.Effect == "flee")).Success);
        else for (int i = 0; i < 30 && world.IsInCombat; i++)
            if (world.State.Battle!.Active.Friendly) world.Act(0, world.State.Battle.Units.First(u => !u.Friendly && u.Alive).Id); else world.AdvanceEnemy();
        Assert.False(world.IsInCombat);
        if (retreat) Assert.Empty(person.Consequences);
        else { Assert.Equal(12, person.Trust[world.Party.Single().Id]); Assert.Contains("danger", world.ConsequenceDialogue(person, world.Party.Single())); }
        Assert.All(world.State.Actors.Where(a => a.Id != person.Id && a.Kind == ActorKind.Person), a => Assert.Empty(a.Consequences));
    }
    [Fact]
    public void ConsequenceSpeechIsPureIdentitySpecificAndARecentGrievanceSurvivesMemoryEviction()
    {
        using var world = Prepare(out var fixture, out var person); ConsequencePreview.DamageSource(world, fixture); person.Memories.Clear();
        Assert.NotEmpty(world.ResidentGrievance(person));
        string before = State(world); var lines = WorldDialogue.Lines(world, person.Id);
        var greeting = Assert.Single(lines); Assert.Equal(person.Id, greeting.Speaker); Assert.Contains("livelihood", greeting.Text); Assert.Equal(before, State(world));
        var other = world.State.Actors.First(a => a.Kind == ActorKind.Person && a.Id != person.Id && !a.Recruited);
        Assert.Empty(world.ConsequenceDialogue(person, other));
        world.State.Tick += WorldSession.ConsequenceDuration;
        Assert.Empty(world.ResidentGrievance(person)); Assert.Empty(world.ConsequenceDialogue(person, world.Party.Single()));
    }
    [Fact]
    public void DamageBlamesTheOriginalRequesterAfterThePartyChangesLeader()
    {
        using var world = Prepare(out var fixture, out var person); long requester = world.Party.Single().Id;
        Assert.True(world.Submit(new(WorldCommandKind.Build, fixture.Source, Material: Material.Floor)).Success);
        var companion = world.State.Actors.First(a => a.Kind == ActorKind.Person && a.Id != person.Id && !a.Recruited);
        companion.Position = world.State.Position.Offset(0, -1); world.Chunks.Edit(companion.Position, Material.Floor);
        Assert.True(world.Submit(new(WorldCommandKind.Recruit, ActorId: companion.Id)).Success);
        Assert.True(world.Submit(new(WorldCommandKind.LeadParty, ActorId: companion.Id)).Success);
        world.Paused = false; for (int i = 0; i < 120 && world.State.Work.Count > 0; i++) world.AdvanceTicks(1);
        Assert.Empty(world.State.Work); Assert.Equal(requester, person.Consequences[ConsequenceKind.DamagedLivelihood].Other);
        Assert.Equal(-12, person.Trust[requester]); Assert.Equal(0, person.Trust.GetValueOrDefault(companion.Id));
        Assert.Empty(world.ResidentGrievance(person)); Assert.Empty(world.ConsequenceDialogue(person, companion));
    }
    [Fact]
    public void ARecentGrievanceChangesSocialChoicesAndActualFoodReliefReopensThem()
    {
        using var world = Prepare(out var fixture, out var person); ConsequencePreview.DamageSource(world, fixture);
        person.FriendId = world.Party.Single().Id; person.Body.Hunger = person.Body.Fatigue = 0;
        world.State.Tick = WorldSession.TicksPerHour * 17; world.Paused = false; world.AdvanceTicks(5);
        Assert.NotEqual(RoutineKind.Social, person.Routine!.Kind);
        Assert.True(world.Submit(new(WorldCommandKind.CompleteDelivery)).Success); world.AdvanceTicks(5);
        Assert.Equal(RoutineKind.Social, person.Routine!.Kind); Assert.Equal(person.FriendId, person.Routine.Other);
    }
    [Theory]
    [InlineData(1)] [InlineData(3)]
    public void SharingAClaimedHarvestOnlyDamagesLivelihoodWhenItExhaustsTheSource(int remaining)
    {
        using var world = Prepare(out var fixture, out var person); var node = world.ResourceAt(fixture.Source)!;
        world.Chunks.Delta(fixture.Source.Chunk).Resources[fixture.Source.LocalIndex] = new(node.Definition.Id, remaining, 0);
        Assert.True(world.Submit(new(WorldCommandKind.Harvest, fixture.Source)).Success);
        var order = Assert.Single(world.State.Work); Assert.Contains(person.Id, order.LivelihoodStakeholders);
        world.Paused = false; for (int i = 0; i < 120 && world.State.Work.Count > 0; i++) world.AdvanceTicks(1);
        Assert.Empty(world.State.Work); Assert.Equal(remaining - 1, world.ResourceAt(fixture.Source)!.Available);
        Assert.Equal(remaining == 1, person.Consequences.ContainsKey(ConsequenceKind.DamagedLivelihood));
    }
    [Fact]
    public void TheResidentsOwnExtractionDoesNotBlameThePlayer()
    {
        using var world = Prepare(out _, out var person); var job = person.Occupation!; world.Paused = false;
        for (int i = 0; i < 120 && job.Phase != OccupationPhase.Delivering; i++) world.AdvanceTicks(1);
        Assert.Equal(OccupationPhase.Delivering, job.Phase); Assert.NotEmpty(job.Carried); Assert.Empty(person.Consequences);
    }
    [Fact]
    public void FourRealConsequenceKindsStayBoundedUnderRepeatedInteractionsAndMemoryPressure()
    {
        using var world = Prepare(out var fixture, out var person); var order = QueueOtherWork(world);
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: order.Id)).Success);
        Assert.True(world.Submit(new(WorldCommandKind.CancelWork, WorkId: order.Id)).Success);
        ConsequencePreview.DamageSource(world, fixture); Assert.True(world.Submit(new(WorldCommandKind.CompleteDelivery)).Success);
        long enemy = ConsequencePreview.PrepareDanger(world, fixture); world.StartBattle(enemy);
        for (int i = 0; i < 30 && world.IsInCombat; i++)
            if (world.State.Battle!.Active.Friendly) world.Act(0, world.State.Battle.Units.First(u => !u.Friendly && u.Alive).Id); else world.AdvanceEnemy();
        Assert.Equal(4, person.Consequences.Count); long leader = world.Party.Single().Id;
        person.Memories.Clear();
        for (int i = 0; i < 12; i++) person.Memories.Add(new("later-" + i, "Later event", world.State.Tick, world.State.Tick + 100, 0));
        Approach(world, person); int trust = person.Trust[leader];
        for (int i = 0; i < 20; i++)
        { person.Body.Hunger = 40; Assert.True(world.PerformInteraction(person.Id, "interaction.meal").Success); }
        Assert.Equal(trust, person.Trust[leader]); Assert.Equal(4, person.Consequences.Count); Assert.InRange(person.Memories.Count, 1, 12);
        string path = Path.Combine(_directory, "bounded.json"); world.Save(path); using var restored = WorldSession.Load(path); Assert.Equal(State(world), State(restored));
    }
    [Fact]
    public void CompatibleSavesWithoutTheNewFieldsLoadWithoutInventingPastAttribution()
    {
        using var world = Prepare(out var fixture, out var person);
        Assert.True(world.Submit(new(WorldCommandKind.Build, fixture.Source, Material: Material.Floor)).Success);
        string path = Path.Combine(_directory, "legacy.json"); world.Save(path); var json = JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var actor in json["state"]!["actors"]!.AsArray()) actor!.AsObject().Remove("consequences");
        foreach (var order in json["state"]!["work"]!.AsArray())
        { order!.AsObject().Remove("requestedBy"); order.AsObject().Remove("livelihoodStakeholders"); }
        File.WriteAllText(path, json.ToJsonString()); using var loaded = WorldSession.Load(path);
        Assert.All(loaded.State.Actors, a => Assert.Empty(a.Consequences)); Assert.Equal(0, loaded.State.Work.Single().RequestedBy);
        loaded.Paused = false; for (int i = 0; i < 120 && loaded.State.Work.Count > 0; i++) loaded.AdvanceTicks(1);
        Assert.Empty(loaded.State.Work); Assert.Empty(loaded.ActorById(person.Id).Consequences);
    }
    [Fact]
    public void WinningAnUnrelatedFightDoesNotInventAnAidMemory()
    {
        using var world = Prepare(out _, out var person); var hero = world.Party.Single(); hero.Role = 0; hero.BaseSpeed = 30; hero.BasePower = 100;
        var enemy = world.State.Actors.First(a => a.Kind == ActorKind.Raider && a.EncounterLeader == 0);
        enemy.Position = world.State.Position.Offset(40, 0); world.Chunks.Edit(enemy.Position, Material.Floor); world.StartBattle(enemy.Id);
        for (int i = 0; i < 30 && world.IsInCombat; i++)
            if (world.State.Battle!.Active.Friendly) world.Act(0, world.State.Battle.Units.First(u => !u.Friendly && u.Alive).Id); else world.AdvanceEnemy();
        Assert.False(world.IsInCombat); Assert.Equal(LifeState.Dead, enemy.Life); Assert.Empty(person.Consequences);
    }
    [Fact]
    public void ExactSavedContinuationKeepsQueuedAttributionAndCompletedConsequences()
    {
        using var world = Prepare(out var fixture, out var person);
        Assert.True(world.Submit(new(WorldCommandKind.Build, fixture.Source, Material: Material.Floor)).Success);
        string path = Path.Combine(_directory, "world.json"); world.Save(path);
        using var loaded = WorldSession.Load(path); Assert.Equal(State(world), State(loaded)); world.Paused = loaded.Paused = false;
        for (int i = 0; i < 70; i++)
        { Assert.Equal(1, world.AdvanceTicks(1)); Assert.Equal(1, loaded.AdvanceTicks(1)); Assert.Equal(State(world), State(loaded)); }
        Assert.True(person.Consequences.ContainsKey(ConsequenceKind.DamagedLivelihood));
        Assert.True(world.Submit(new(WorldCommandKind.CompleteDelivery)).Success); Assert.True(loaded.Submit(new(WorldCommandKind.CompleteDelivery)).Success);
        Assert.Equal(State(world), State(loaded)); world.Save(path);
        using var restored = WorldSession.Load(path); Assert.Equal(State(world), State(restored));
        Assert.Equal(person.Trust, restored.ActorById(person.Id).Trust);
    }
    [Theory]
    [InlineData("other")] [InlineData("tick")] [InlineData("after")] [InlineData("event")] [InlineData("kind")] [InlineData("memory")]
    [InlineData("requester")] [InlineData("stakeholder")] [InlineData("duplicate")]
    public void ForgedConsequenceStateIsRejectedWithoutChangingTheSave(string field)
    {
        using var world = Prepare(out var fixture, out var person); ConsequencePreview.DamageSource(world, fixture); QueueOtherWork(world);
        string path = Path.Combine(_directory, "world.json"); world.Save(path); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var actor = json["state"]!["actors"]!.AsArray().Single(a => a!["id"]!.GetValue<long>() == person.Id)!;
        var receipt = actor["consequences"]!["DamagedLivelihood"]!;
        switch (field)
        {
            case "other": receipt[field] = person.Id; break;
            case "tick": receipt[field] = -1; break;
            case "after": receipt[field] = 0; break;
            case "event": receipt[field] = new string('x', 129); break;
            case "kind": actor["consequences"]!["999"] = receipt.DeepClone(); break;
            case "memory": actor["memories"]!.AsArray().Single(m => m!["key"]!.GetValue<string>() == "consequence.livelihood")!["mood"] = 1; break;
            case "requester": json["state"]!["work"]![0]!["requestedBy"] = long.MaxValue; break;
            case "stakeholder": json["state"]!["work"]![0]!["livelihoodStakeholders"] = new JsonArray(long.MaxValue); break;
            case "duplicate": json["state"]!["work"]![0]!["livelihoodStakeholders"] = new JsonArray(person.Id, person.Id); break;
        }
        File.WriteAllText(path, json.ToJsonString()); string before = File.ReadAllText(path);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(path)); Assert.Equal(before, File.ReadAllText(path));
    }
}
