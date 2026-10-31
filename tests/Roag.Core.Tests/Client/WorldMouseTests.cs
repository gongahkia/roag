using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Roag.Client;
using Roag.Core.World;
using Roag.Core.Tactics;
using Roag.Graphics;
using Xunit;

namespace Roag.Tests.Client;

public sealed class WorldMouseTests
{
    private static FieldInfo Field(string name) => typeof(WorldGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static object? Invoke(WorldGame client, string name, params object[] args) => typeof(WorldGame).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, args);
    private static WorldGame Client(WorldSession session)
    {
        var client = (WorldGame)RuntimeHelpers.GetUninitializedObject(typeof(WorldGame)); GC.SuppressFinalize(client);
        Field("_options").SetValue(client, Options.Parse(["--scene", "world"])); Field("_session").SetValue(client, session);
        Field("_controls").SetValue(client, new List<WorldMouseControl>());
        Field("_camera").SetValue(client, new MapCamera { Viewport = new(16, 84, 674, 490) });
        Field("_map").SetValue(client, new Rectangle(16, 84, 674, 490)); Field("_side").SetValue(client, new Rectangle(706, 84, 302, 620));
        Field("_combatFloats").SetValue(client, Activator.CreateInstance(Field("_combatFloats").FieldType));
        Field("_panel").SetValue(client, ""); return client;
    }
    private static List<WorldMouseControl> Controls(WorldGame client, string panel)
    {
        Field("_panel").SetValue(client, panel); Invoke(client, "BuildMouseControls", 1024, 720);
        return ((List<WorldMouseControl>)Field("_controls").GetValue(client)!).ToList();
    }
    [Fact]
    public void RememberedServiceInspectionHidesItsCurrentReceipt()
    {
        using var session = WorldSession.CreateDungeonRun(42); var hero = session.Party.Single();
        var cell = hero.Position.Offset(1, 0); session.Chunks.Edit(cell, Material.Floor);
        var node = session.DungeonCombat.SpawnDungeonService(DungeonServiceKind.ReturnToken, "test:private-service", cell);
        session.Reveal(); Assert.True(session.SubmitDungeon(new(TacticalAction.Interact, Target: node.Id, Choice: 1)).Success);
        Assert.Contains("Already used", WorldInspection.Inspect(session, cell).Detail);
        session.Visible.Clear(); var remembered = WorldInspection.Inspect(session, cell);
        Assert.Contains("remembered", remembered.Title); Assert.DoesNotContain("used", remembered.Detail);
        Assert.Contains("Observe", remembered.Detail);
    }

    [Fact]
    public void NativeServiceInspectionIsFreeAndItsConfirmedChoiceUsesTheRealPaidAction()
    {
        using var session = WorldSession.CreateDungeonRun(42); var client = Client(session); var hero = session.Party.Single();
        var cell = hero.Position.Offset(1, 0); session.Chunks.Edit(cell, Material.Floor);
        var node = session.DungeonCombat.SpawnDungeonService(DungeonServiceKind.ReturnToken, "test:client-token", cell);
        session.Reveal(); Field("_roagAim").SetValue(client, "move");
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.Enter), true);
        Assert.Equal("dungeon-service", Field("_panel").GetValue(client)); Assert.Equal(0, session.DungeonCombat.Clock);
        Assert.Contains(Controls(client, "dungeon-service"), c => c.Id == "dungeon:service-choice:1" && c.Enabled);
        Invoke(client, "ActivateDungeonControl", "dungeon:service-choice:1");
        Assert.Equal(100, session.DungeonCombat.Clock); Assert.Equal(UtilityKind.ReturnToken, hero.Tactics!.Supplies.Utility!.Kind);
        Assert.Equal(hero.Id, session.State.Tactics!.Services.Nodes.Single(n => n.Id == node.Id).UsedBy);
    }

    [Fact]
    public void NativeServiceUtilityExchangeCanBeReviewedAndCancelledWithoutSpending()
    {
        using var session = WorldSession.CreateDungeonRun(42); var client = Client(session); var hero = session.Party.Single();
        var cell = hero.Position.Offset(1, 0); session.Chunks.Edit(cell, Material.Floor);
        var node = session.DungeonCombat.SpawnDungeonService(DungeonServiceKind.ReturnToken, "test:client-exchange", cell);
        hero.Tactics!.Supplies.Utility = new(UtilityKind.CleanseFlask); session.Reveal();
        Invoke(client, "DungeonInteract"); string before = JsonSerializer.Serialize(session.State);
        Invoke(client, "ActivateDungeonControl", "dungeon:service-choice:1");
        Assert.Equal("dungeon-service-replace", Field("_panel").GetValue(client));
        Invoke(client, "ActivateDungeonControl", "dungeon:service-back"); Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Invoke(client, "ActivateDungeonControl", "dungeon:service-choice:1");
        Invoke(client, "ActivateDungeonControl", "dungeon:service-confirm");
        Assert.Equal(100, session.DungeonCombat.Clock); Assert.Equal(UtilityKind.ReturnToken, hero.Tactics.Supplies.Utility!.Kind);
        Assert.Equal(hero.Id, session.State.Tactics!.Services.Nodes.Single(n => n.Id == node.Id).UsedBy);
    }

    [Fact]
    public void NativeUtilityPanelIsFreeAndOnlyAConfirmedDirectionSpendsTheScroll()
    {
        using var session = WorldSession.CreateDungeonRun(42); var client = Client(session);
        var hero = session.Party.Single(); var origin = hero.Position;
        for (int x = 1; x <= 3; x++) session.Chunks.Edit(origin.Offset(x, 0), Material.Floor);
        session.Reveal(); hero.Tactics!.Supplies.Utility = new(UtilityKind.EscapeScroll);
        Field("_roagAim").SetValue(client, "move");
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.U, Keys.D), true);
        Assert.Equal("dungeon-utility", Field("_panel").GetValue(client));
        Assert.Equal(origin, hero.Position); Assert.Equal(0, session.DungeonCombat.Clock);
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(), true);
        Invoke(client, "ActivateDungeonControl", "dungeon:utility-steps:2");
        Invoke(client, "ActivateDungeonControl", "dungeon:utility-use");
        Assert.Equal("use_utility", Field("_roagAim").GetValue(client)); Assert.Equal(0, session.DungeonCombat.Clock);
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.D), true);
        Assert.Equal(origin.Offset(2, 0), hero.Position); Assert.Null(hero.Tactics.Supplies.Utility);
        Assert.Equal(100, session.DungeonCombat.Clock);
    }

    [Fact]
    public void NativeUtilityPanelDisablesAFlaskThatCannotCleanseAnything()
    {
        using var session = WorldSession.CreateDungeonRun(42); var client = Client(session);
        session.Party.Single().Tactics!.Supplies.Utility = new(UtilityKind.CleanseFlask);
        string before = JsonSerializer.Serialize(session.State);
        var controls = Controls(client, "dungeon-utility");
        var use = Assert.Single(controls, c => c.Id == "dungeon:utility-use");
        Assert.False(use.Enabled); Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }

    [Fact]
    public void SimultaneousWaitAndDirectionCannotSpendTwoNativePartyTurns()
    {
        using var session = WorldSession.CreateDungeonRun(42); var client = Client(session);
        Field("_roagAim").SetValue(client, "move"); var origin = session.State.Position;
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.Space, Keys.D), true);
        Assert.Equal(origin, session.State.Position); Assert.Equal(100, session.DungeonCombat.Clock);
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.D), true);
        Assert.Equal(origin, session.State.Position); Assert.Equal(100, session.DungeonCombat.Clock);
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(), true);
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.S), true);
        Assert.Equal(origin.Offset(0, 1), session.State.Position); Assert.Equal(200, session.DungeonCombat.Clock);
    }
    [Fact]
    public void AnUnownedVowToggleDoesNotLockNativeMovementOrConsumeTime()
    {
        using var session = WorldSession.CreateDungeonRun(42); var client = Client(session);
        Field("_roagAim").SetValue(client, "move"); var origin = session.State.Position;
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.P), true);
        Assert.False((bool)Field("_roagVow").GetValue(client)!); Assert.Equal(0, session.DungeonCombat.Clock);
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.S), true);
        Assert.Equal(origin.Offset(0, 1), session.State.Position); Assert.Equal(100, session.DungeonCombat.Clock);
    }
    [Fact]
    public void KeyboardMovementRequiresANewPressForEachSingleTile()
    {
        using var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var client = Client(session);
        var origin = session.State.Position;
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.D), true);
        Assert.Equal(origin.Offset(1, 0), session.State.Position);
        string held = JsonSerializer.Serialize(session.State);
        for (int i = 0; i < 20; i++) Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.D), true);
        Assert.Equal(held, JsonSerializer.Serialize(session.State));
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(), true);
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.Right), true);
        Assert.Equal(origin.Offset(2, 0), session.State.Position);
    }
    [Fact]
    public void SimultaneousDirectionsDoNotQueueMultipleMovementSteps()
    {
        using var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var client = Client(session);
        var origin = session.State.Position;
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.D, Keys.S), true);
        Assert.Equal(1, origin.Distance(session.State.Position));
        string held = JsonSerializer.Serialize(session.State);
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.S), true);
        Assert.Equal(held, JsonSerializer.Serialize(session.State));
    }
    [Fact]
    public void KeyboardMovementDoesNotLeakThroughPanelsOrFocusChanges()
    {
        using var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var client = Client(session);
        string before = JsonSerializer.Serialize(session.State);
        Field("_panel").SetValue(client, "inventory");
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.D), true);
        Field("_panel").SetValue(client, "");
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.D), true);
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.A), false);
        Invoke(client, "HandleRoagKeyboard", new KeyboardState(Keys.A), true);
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }
    [Theory]
    [InlineData("move")]
    [InlineData("dash")]
    public void MouseCannotCommitMovementDirectionsInCombat(string action)
    {
        using var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var client = Client(session);
        Field("_roagAim").SetValue(client, action);
        string before = JsonSerializer.Serialize(session.State);
        Invoke(client, "RoagClick", new Point(100, 100));
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Assert.Contains("keyboard", (string)Field("_notice").GetValue(client)!);
    }
    [Fact]
    public void ClickingOpenGroundNeverCreatesARouteOrAdvancesTime()
    {
        using var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var client = Client(session);
        var target = session.State.Position.Offset(4, 0);
        string before = JsonSerializer.Serialize(session.State);
        var plan = WorldMouseNavigation.Plan(session, target);
        Invoke(client, "BeginApproach", plan);
        Assert.Empty(plan.Path); Assert.False(plan.Reachable);
        Assert.DoesNotContain(WorldContextActions.Options(session, [target]), c => c.Id == "travel");
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Assert.False(session.PartyTravelling);
    }
    [Fact]
    public void ClickingADistantPersonRequiresKeyboardApproachWithoutMovingOrSpendingTime()
    {
        var session = WorldSession.Create(42); var person = session.NearbyPerson() ?? session.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).MinBy(a => a.Position.Distance(session.State.Position))!;
        string before = JsonSerializer.Serialize(session.State); uint rng = session.State.CombatRandom;
        var plan = WorldMouseNavigation.Plan(session, person.Position);
        Assert.False(plan.Reachable); Assert.Equal("person", plan.Interaction!.Kind); Assert.Equal(person.Id, plan.Interaction.Actor);
        Assert.Empty(plan.Path); Assert.Contains("WASD", plan.Reason);
        Assert.Equal(before, JsonSerializer.Serialize(session.State)); Assert.Equal(rng, session.State.CombatRandom);
    }
    [Fact]
    public void DistantStorageRequiresKeyboardApproachButAdjacentStorageCanBeOpened()
    {
        var session = WorldSession.Create(42); var origin = session.State.Position; var chest = origin.Offset(4, 0);
        for (int x = 0; x < 5; x++) session.Chunks.Edit(origin.Offset(x, 0), Material.Floor);
        session.Chunks.Edit(chest, Material.Storage); session.Reveal();
        var plan = WorldMouseNavigation.Plan(session, chest);
        Assert.False(plan.Reachable); Assert.Equal("storage", plan.Interaction!.Kind); Assert.Empty(plan.Path);
        var near = origin.Offset(1, 0); session.Chunks.Edit(near, Material.Storage);
        var adjacent = WorldMouseNavigation.Plan(session, near);
        Assert.True(adjacent.Reachable); Assert.Empty(adjacent.Path); Assert.Equal("storage", adjacent.Interaction!.Kind);
        var camera = new MapCamera { Center = new(origin.X + .5f, origin.Y + .5f), Viewport = new(0, 0, 480, 480) };
        var pointer = new WorldPointerPreview(); pointer.Update(session, camera, camera.WorldToScreen(new(chest.X + .5f, chest.Y + .5f)).ToPoint());
        Assert.Equal(chest, pointer.Cell); Assert.Empty(pointer.BlockReason);
    }
    [Fact]
    public void AnApproachCannotInventAnUnknownConnectionToAKnownObject()
    {
        var session = WorldSession.Create(42); var origin = session.State.Position; var chest = origin.Offset(4, 0);
        session.Chunks.Edit(chest, Material.Storage);
        foreach (var delta in session.State.Chunks) delta.Discovered.Clear();
        session.Chunks.Delta(origin.Chunk).Discovered.UnionWith([origin.LocalIndex, chest.LocalIndex]);
        string before = JsonSerializer.Serialize(session.State);
        var plan = WorldMouseNavigation.Plan(session, chest);
        Assert.False(plan.Reachable); Assert.Empty(plan.Path); Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }
    [Fact]
    public void MovingSpriteHitTestingKeepsTheSelectorOnTheActualCursorCell()
    {
        var session = WorldSession.Create(42); var origin = session.State.Position;
        var person = session.State.Actors.First(a => a.Kind == ActorKind.Person);
        person.Position = origin.Offset(4, 0); session.Reveal();
        var camera = new MapCamera { Center = new(origin.X + .5f, origin.Y + .5f), Viewport = new(0, 0, 480, 480) };
        var cursorCell = origin.Offset(3, 0); var pointer = new WorldPointerPreview();
        var point = camera.WorldToScreen(new(cursorCell.X + .5f, cursorCell.Y + .5f)).ToPoint();
        pointer.Update(session, camera, point, interactionCell: person.Position);
        Assert.Equal(cursorCell, pointer.Cell); Assert.Equal(person.Position, pointer.NavigationCell);
        Assert.Empty(pointer.BlockReason);
        pointer.Update(session, camera, point);
        Assert.Equal(cursorCell, pointer.NavigationCell);
    }
    [Fact]
    public void MouseControlsCoverGameplayAndStayInsideTheMinimumWindow()
    {
        var session = WorldSession.Create(42); var client = Client(session);
        var normal = Controls(client, "");
        foreach (string id in new[] { "pause", "save", "load", "panel:game", "panel:inventory", "panel:journal", "panel:map", "panel:actions", "panel:work", "stop", "zoom:default" }) Assert.Contains(normal, c => c.Id == id);
        foreach (string panel in new[] { "", "inventory", "trade", "work", "craft", "actions", "guide", "map", "journal", "inspector", "game" })
        {
            var controls = Controls(client, panel);
            Assert.All(controls, c => Assert.True(new Rectangle(0, 0, 1024, 720).Contains(c.Bounds), panel + ": " + c.Id));
            for (int i = 0; i < controls.Count; i++) for (int j = i + 1; j < controls.Count; j++)
                Assert.False(controls[i].Bounds.Intersects(controls[j].Bounds), panel + ": " + controls[i].Id + " / " + controls[j].Id);
            if (panel.Length > 0) Assert.Contains(controls, c => c.Id == "close");
        }
        Assert.DoesNotContain(Controls(client, "actions"), c => c.Id == "rest");
        Assert.Contains(Controls(client, "actions"), c => c.Id == "wait:300");
    }
    [Fact]
    public void DisabledControlsStillInterceptTheClick()
    {
        var controls = new[] { new WorldMouseControl("map", "Map", new(0, 0, 100, 100)), new WorldMouseControl("blocked", "Blocked", new(10, 10, 50, 50), Enabled: false) };
        var hit = WorldMouseControl.Hit(controls, new(25, 25));
        Assert.Equal("blocked", hit!.Id); Assert.False(hit.Enabled);
    }
    [Fact]
    public void WorkerSelectionStaysUsableWhenAvailablePeopleChange()
    {
        var session = WorldSession.Create(42); var client = Client(session);
        Assert.True(session.Submit(new(WorldCommandKind.Build, session.State.Position.Offset(1, 0), Material: Material.Camp)).Success);
        Field("_workerPage").SetValue(client, 0);
        var selected = Controls(client, "work").Single(c => c.Id.StartsWith("worker:", StringComparison.Ordinal) && c.Selected);
        Invoke(client, "ActivateControl", "assign");
        Assert.Equal(long.Parse(selected.Id[7..], System.Globalization.CultureInfo.InvariantCulture), session.State.Work.Single().Worker);
    }
    [Fact]
    public void DeliveryControlCyclesKnownStorageAndPackThroughRealCommands()
    {
        using var session = WorldSession.Create(42); var (target, storage) = WorkOutputPreview.Prepare(session); var client = Client(session);
        Assert.Equal(1, session.DesignateWork(WorldCommandKind.Harvest, [target]).Queued); var order = Assert.Single(session.State.Work);
        var control = Controls(client, "work").Single(c => c.Id == "work-delivery"); Assert.True(control.Enabled); Assert.Equal("To: Pack", control.Label);
        string before = JsonSerializer.Serialize(session.State); Controls(client, "work"); Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Invoke(client, "ActivateControl", "work-delivery"); Assert.Equal(storage, order.DeliveryStorage);
        Assert.Contains(storage.X.ToString(), Controls(client, "work").Single(c => c.Id == "work-delivery").Label);
        Invoke(client, "ActivateControl", "work-delivery"); Assert.Null(order.DeliveryStorage);
    }
    [Fact]
    public void ConversationChoicesWaitForSpeechAndPaidEffectsRunOncePerChoice()
    {
        var session = WorldSession.Create(42); var client = Client(session);
        var actor = session.State.Actors.First(a => session.Interactions(a.Id).Any(i => i.Action == "share-meal"));
        actor.Position = session.State.Position.Offset(1, 0); Field("_person").SetValue(client, actor.Id);
        string before = JsonSerializer.Serialize(session.State);
        var controls = Controls(client, "person"); Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Assert.DoesNotContain(controls, c => c.Id.StartsWith("conversation:", StringComparison.Ordinal));
        for (int i = 0; i < 10 && ((DialoguePlayback)Field("_dialogue").GetValue(client)!).Finished == false; i++) Invoke(client, "AdvanceDialogue");
        controls = Controls(client, "person"); Assert.Contains(controls, c => c.Id == "conversation:interaction.meal");
        int food = session.Count("food"); Invoke(client, "ActivateControl", "conversation:interaction.meal");
        Assert.Equal(food - 1, session.Count("food")); before = JsonSerializer.Serialize(session.State);
        Invoke(client, "ActivateControl", "conversation:interaction.meal"); Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Invoke(client, "AdvanceDialogue"); Invoke(client, "AdvanceDialogue"); Assert.Equal(before, JsonSerializer.Serialize(session.State));
        foreach (var control in Controls(client, "person")) Assert.True(new Rectangle(0, 0, 1024, 720).Contains(control.Bounds));
        Invoke(client, "ClosePanel"); Assert.Null(Field("_dialogue").GetValue(client));
    }
    [Fact]
    public void FinishingASelectedJobDoesNotRetargetAnAssignmentOrCancellation()
    {
        var session = WorldSession.Create(42); var client = Client(session);
        session.State.Inventory["stone"] = 100;
        var targets = new[] { session.State.Position.Offset(1, 0), session.State.Position.Offset(-1, 0) };
        foreach (var p in targets) session.Chunks.Edit(p, Material.Floor);
        session.DesignateWork(WorldCommandKind.Build, targets, Material.Wall);
        Controls(client, "work"); long selected = (long)Field("_selectedWorkId").GetValue(client)!;
        session.Submit(new(WorldCommandKind.CancelWork, WorkId: selected));
        Invoke(client, "ActivateControl", "cancel-work"); Assert.Single(session.State.Work);
        Assert.False(Controls(client, "work").Single(c => c.Id == "assign").Enabled);
    }
    [Fact]
    public void InspectionExplainsVisiblePeopleAndBlockedTerrainWithoutRevealingUnknownActors()
    {
        var session = WorldSession.Create(42); var origin = session.State.Position;
        var person = session.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited); person.Position = origin.Offset(1, 0);
        var rock = origin.Offset(0, 1); session.Chunks.Edit(rock, Material.Rock); session.Reveal();
        string before = JsonSerializer.Serialize(session.State);
        var info = WorldInspection.Inspect(session, person.Position);
        Assert.Equal(person.Name, info.Title); Assert.Contains("speak", info.Action); Assert.Contains("HP", info.Detail);
        Assert.Contains("dig", WorldInspection.Inspect(session, rock).Reason);
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
        var unknown = origin.Offset(80, 80); person.Position = unknown; before = JsonSerializer.Serialize(session.State);
        var hidden = WorldInspection.Inspect(session, unknown);
        Assert.Equal("unknown", hidden.Kind); Assert.DoesNotContain(person.Name, hidden.Detail + hidden.Title);
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }
    [Fact]
    public void NewPanelsFitTheMinimumWindowAndUnavailableActionsExplainTheirRequirements()
    {
        var session = WorldSession.Create(42); var client = Client(session);
        var d = session.State.Discoveries.First(d => d.Enemies.Length == 0 && session.State.Loot.Any(c => c.Id == d.Id && c.Choices.Length == 3));
        Field("_discoveryId").SetValue(client, d.Id); Field("_lootPosition").SetValue(client, d.Position);
        session.State.LastEncounterReward = new(12, 3, 12, []);
        foreach (string panel in new[] { "discovery", "loot", "spoils", "combat-log", "map" })
        {
            var controls = Controls(client, panel);
            Assert.All(controls, c => Assert.True(new Rectangle(0, 0, 1024, 720).Contains(c.Bounds), panel + "/" + c.Id));
            for (int i = 0; i < controls.Count; i++) for (int j = i + 1; j < controls.Count; j++)
                Assert.False(controls[i].Bounds.Intersects(controls[j].Bounds), panel + "/" + controls[i].Id + "/" + controls[j].Id);
            Assert.All(controls.Where(c => !c.Enabled), c => Assert.NotEmpty(c.DisabledReason));
        }
        var craft = Controls(client, "craft").Where(c => c.Id.StartsWith("craft:", StringComparison.Ordinal)).ToArray();
        Assert.All(craft, c => { Assert.False(c.Enabled); Assert.Contains("camp", c.DisabledReason); });
        Assert.Equal(3, Controls(client, "loot").Count(c => c.Id.StartsWith("claim:", StringComparison.Ordinal)));
    }
    [Fact]
    public void MapWaypointsNeverDiscoverTerrainOrInventTravelToRumouredGround()
    {
        var session = WorldSession.Create(42); var client = Client(session); var d = session.State.Discoveries.First(); d.Known = true;
        string before = JsonSerializer.Serialize(session.State);
        Invoke(client, "ActivateControl", "waypoint:" + d.Id);
        Assert.Equal(d.Position, Field("_waypoint").GetValue(client));
        Assert.Empty(session.Route(d.Position)); Assert.False(session.Chunks.Seen(d.Position));
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }
    [Fact]
    public void MouseJournalRetainsRequestsAndMakesLongHistoryReachable()
    {
        var session = WorldSession.Create(42); var client = Client(session);
        session.State.Tasks.Add(new() { Id = "request-test", Description = "Reopen the road to Cairnford." });
        session.State.Journal.Add(new string('q', 250)); string before = JsonSerializer.Serialize(session.State);
        var lines = (string[])Invoke(client, "JournalLines", 80)!;
        Assert.All(lines, line => Assert.True(line.Length <= 10));
        Assert.Contains("Request: Reopen the road to Cairnford.", string.Join(" ", lines));
        Assert.Contains(new string('q', 250), string.Concat(lines));
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }
    [Fact]
    public void MouseCombatRequiresAnExplicitTargetAndPanelsDoNotSpendATurn()
    {
        var session = WorldSession.Create(42); session.StartBattle(session.State.Actors.First(a => a.Kind == ActorKind.Beast).Id);
        while (!session.State.Battle!.Active.Friendly) session.AdvanceEnemy();
        var client = Client(session); var controls = Controls(client, ""); Assert.Equal(6, controls.Count(c => c.Id.StartsWith("skill:", StringComparison.Ordinal)));
        string before = JsonSerializer.Serialize(session.State);
        Invoke(client, "ActivateControl", "skill:0"); Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Invoke(client, "ActivateControl", "panel:inventory"); Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Invoke(client, "ActivateControl", "close");
        var enemy = session.State.Battle!.Units.First(u => !u.Friendly && u.Alive); int hp = enemy.Health;
        var rect = (Rectangle)Invoke(client, "UnitRect", enemy)!; Invoke(client, "CombatClick", rect.Center);
        Assert.True(enemy.Health < hp); Assert.NotEqual(before, JsonSerializer.Serialize(session.State));
    }
    [Fact]
    public void SkippingAWinningHitOpensRewardsBeforeTheSameFramesWorldAdvance()
    {
        var session = WorldSession.Create(42); CooperationPreview.Prepare(session); session.Paused = false;
        var battle = session.State.Battle!; var enemy = battle.Units.First(u => !u.Friendly); enemy.Health = 1;
        session.State.Actors.Single(a => a.Id == battle.ActorIds[enemy.Id]).Health = 1;
        var client = Client(session); var rect = (Rectangle)Invoke(client, "UnitRect", enemy)!;
        Invoke(client, "CombatClick", rect.Center); Assert.Null(session.State.Battle);
        var playback = (CombatPlayback)Field("_combatPlayback").GetValue(client)!; Assert.True(playback.RewardPending);
        long tick = session.State.Tick; Invoke(client, "ActivateControl", "combat-skip");
        Assert.Equal("spoils", Field("_panel").GetValue(client)); Assert.False(playback.Completed);
        session.ModalOpen = WorldGame.PanelPausesTime((string)Field("_panel").GetValue(client)!);
        session.AdvanceFrame(TimeSpan.FromSeconds(.2)); Assert.Equal(tick, session.State.Tick);
    }
    [Theory]
    [InlineData(674, 490)]
    [InlineData(1558, 890)]
    [InlineData(3490, 1930)]
    public void ExplorationZoomCannotCollapseIntoATinyWorldOverview(int width, int height)
    {
        var camera = new MapCamera { Viewport = new(0, 0, width, height) }; WorldCameraScale.Apply(camera, reset: true);
        Assert.True(camera.CellSize >= 36); Assert.True(width / camera.CellSize <= 48.01f);
        for (int i = 0; i < 100; i++) camera.ZoomAt(camera.Viewport.Center.ToVector2(), -1);
        Assert.True(camera.CellSize >= 24); Assert.True(width / camera.CellSize <= 48.01f); Assert.True(height / camera.CellSize <= 32.01f);
        WorldCameraScale.Apply(camera, reset: true); Assert.True(camera.CellSize >= 36);
    }
    [Theory]
    [InlineData(1024, 720)] [InlineData(1440, 900)]
    public void CooperativeMouseActionsSelectAHelperThenATargetAndFitBothWindowSizes(int width, int height)
    {
        var session = WorldSession.Create(42); CooperationPreview.Prepare(session); var b = session.State.Battle!;
        var client = Client(session); Field("_map").SetValue(client, new Rectangle(16, 84, width - 350, height - 230));
        Invoke(client, "BuildMouseControls", width, height);
        var controls = (List<WorldMouseControl>)Field("_controls").GetValue(client)!;
        var skills = controls.Where(c => c.Id.StartsWith("skill:", StringComparison.Ordinal)).ToArray(); Assert.Equal(8, skills.Length);
        Assert.All(skills, c => Assert.True(new Rectangle(0, 0, width, height).Contains(c.Bounds)));
        for (int i = 0; i < skills.Length; i++) for (int j = i + 1; j < skills.Length; j++) Assert.False(skills[i].Bounds.Intersects(skills[j].Bounds));
        string before = JsonSerializer.Serialize(session.State); Invoke(client, "ActivateControl", "skill:6");
        var helper = (Rectangle)Invoke(client, "UnitRect", b.Units[3])!; Invoke(client, "CombatClick", helper.Center);
        Assert.Equal(3, Field("_cooperativePartner").GetValue(client)); Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Invoke(client, "ActivateControl", "skill:0"); Assert.Equal(-1, Field("_cooperativePartner").GetValue(client));
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Invoke(client, "ActivateControl", "skill:6"); Invoke(client, "CombatClick", helper.Center);
        var enemy = (Rectangle)Invoke(client, "UnitRect", b.Units[4])!; Invoke(client, "CombatClick", enemy.Center);
        Assert.Contains(3, b.Assisting); Assert.Equal(1, b.Active.Id); Assert.Equal(-1, Field("_cooperativePartner").GetValue(client));
        Assert.NotEqual(before, JsonSerializer.Serialize(session.State));
        Invoke(client, "BuildMouseControls", width, height);
        var playingControls = ((List<WorldMouseControl>)Field("_controls").GetValue(client)!).Where(c => c.Id.StartsWith("skill:", StringComparison.Ordinal)).ToArray();
        Assert.Equal("skill:6", Assert.Single(playingControls, c => c.Selected).Id);
        Assert.All(playingControls, c => Assert.False(c.Enabled));
        before = JsonSerializer.Serialize(session.State); Invoke(client, "ActivateControl", "skill:7");
        Assert.Equal(before, JsonSerializer.Serialize(session.State)); // An unresolved visual sequence consumes duplicate input.
        var playback = (CombatPlayback)Field("_combatPlayback").GetValue(client)!; playback.Skip();
        Invoke(client, "ActivateControl", "skill:7");
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
        var readyHelper = (Rectangle)Invoke(client, "UnitRect", b.Units[2])!; Invoke(client, "CombatClick", readyHelper.Center);
        Assert.NotEqual(before, JsonSerializer.Serialize(session.State)); Assert.False(b.Active.Friendly);
    }
    [Fact]
    public void ContextOffersRelevantActionsAndExplainsUnavailableWorkWithoutMutatingState()
    {
        var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var rock = session.State.Position.Offset(4, 0);
        session.Chunks.Edit(rock, Material.Rock); session.State.Inventory["food"] = 0;
        string before = JsonSerializer.Serialize(session.State);
        var options = WorldContextActions.Options(session, [rock]);
        Assert.Contains(options, o => o.Id == "dig" && o.Reason.Length == 0);
        Assert.DoesNotContain(options, o => o.Id is "build" or "rest" or "craft");
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
        var ground = session.State.Position.Offset(0, 2);
        Assert.Contains(WorldContextActions.Options(session, [ground]), o => o.Id == "build");
        Assert.DoesNotContain(WorldContextActions.Options(session, [ground]), o => o.Id == "dig");
    }
    [Fact]
    public void SixRockTilesCanBeDesignatedWithoutFoodAndExplainOrdinaryNeeds()
    {
        var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var client = Client(session);
        var rocks = Enumerable.Range(2, 6).Select(x => session.State.Position.Offset(x, 0)).ToArray();
        foreach (var rock in rocks) session.Chunks.Edit(rock, Material.Rock);
        session.State.Inventory["food"] = 0;
        Field("_contextCells").SetValue(client, rocks); Field("_contextAnchor").SetValue(client, new Point(600, 480));
        var selection = new WorldTileSelection(); selection.Rectangle(session, rocks[0], rocks[^1]); Field("_selection").SetValue(client, selection);
        string before = JsonSerializer.Serialize(session.State);
        var dig = Controls(client, "").Single(c => c.Id == "context:dig");
        Assert.True(dig.Enabled); Assert.Contains("no food is reserved", dig.Tip);
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
        Invoke(client, "ActivateControl", "context:dig");
        Assert.Empty(selection.Cells); Assert.Null(Field("_contextCells").GetValue(client));
        Assert.Equal(6, session.State.Work.Count); Assert.Equal(0, session.Count("food"));
        Assert.All(session.State.Work, w => Assert.Empty(w.Reserved));
    }
    [Theory]
    [InlineData(1024, 720)] [InlineData(1908, 1120)]
    public void IllustratedPartyAndWorkCardsKeepSpritesAndControlsWithinBounds(int width, int height)
    {
        var session = Roag.Core.Tests.World.WorldCollisionTests.Arena();
        foreach (var person in session.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(3))
        { person.Recruited = true; session.State.Party.Add(person.Id); }
        var client = Client(session);
        session.DesignateWork(WorldCommandKind.Build, [session.State.Position.Offset(1, 0)], Material.Camp);
        string before = JsonSerializer.Serialize(session.State);
        foreach (string panel in new[] { "inventory", "work" })
        {
            Field("_panel").SetValue(client, panel); Invoke(client, "BuildMouseControls", width, height);
            var controls = ((List<WorldMouseControl>)Field("_controls").GetValue(client)!).ToArray();
            foreach (var c in controls)
            {
                Assert.True(new Rectangle(0, 0, width, height).Contains(c.Bounds));
                if (c.Sprite != null)
                {
                    var method = typeof(WorldGame).GetMethod("ControlSpriteBox", BindingFlags.Static | BindingFlags.NonPublic)!;
                    Assert.True(c.Bounds.Contains((Rectangle)method.Invoke(null, [c])!));
                }
            }
            for (int i = 0; i < controls.Length; i++) for (int j = i + 1; j < controls.Length; j++) Assert.False(controls[i].Bounds.Intersects(controls[j].Bounds));
            if (panel == "inventory")
            {
                Assert.Equal(4, controls.Count(c => c.Id.StartsWith("party:", StringComparison.Ordinal) && c.Sprite != null && c.Progress >= 0));
                var items = session.CarriedEquipment().ToArray();
                foreach (var c in controls.Where(c => c.Id.StartsWith("gear:", StringComparison.Ordinal))) Assert.Equal(items[int.Parse(c.Id[5..])].Sprite, c.Sprite);
            }
            else
            {
                Assert.All(controls.Where(c => c.Id.StartsWith("worker:", StringComparison.Ordinal)), c => Assert.Equal(session.State.Actors.Single(a => a.Id == long.Parse(c.Id[7..])).Sprite, c.Sprite));
                Assert.Equal(session.Party.First().Sprite, controls.Single(c => c.Id.StartsWith("work:", StringComparison.Ordinal)).BadgeSprite);
            }
        }
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }
    [Theory]
    [InlineData(1024, 720)] [InlineData(1908, 1120)]
    public void CampControlsShowCompanionsCostsAndNoOverlaps(int width, int height)
    {
        var session = WorldSession.Create(42); var camp = CampPreview.Prepare(session); var client = Client(session);
        Field("_campPosition").SetValue(client, camp); Field("_campRole").SetValue(client, "cook");
        string before = JsonSerializer.Serialize(session.State);
        Field("_panel").SetValue(client, "camp"); Invoke(client, "BuildMouseControls", width, height);
        var controls = ((List<WorldMouseControl>)Field("_controls").GetValue(client)!).ToArray();
        Assert.False(WorldGame.PanelPausesTime("camp")); Assert.Equal(4, controls.Count(c => c.Id.StartsWith("campworker:", StringComparison.Ordinal) && c.Sprite != null));
        Assert.Contains(controls, c => c.Id == "campduty:camp.apple-broth" && c.Enabled && c.Detail.Contains("apple", StringComparison.Ordinal));
        for (int i = 0; i < controls.Length; i++)
        {
            Assert.True(new Rectangle(0, 0, width, height).Contains(controls[i].Bounds));
            for (int j = i + 1; j < controls.Length; j++) Assert.False(controls[i].Bounds.Intersects(controls[j].Bounds));
        }
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }
    [Fact]
    public void IllustratedJournalUsesUnambiguousSubjectsAndPreservesLongText()
    {
        var session = WorldSession.Create(42); var client = Client(session); var hero = session.Party.First();
        session.State.Journal.Add($"Day 1: {hero.Name} completed dig.");
        string before = JsonSerializer.Serialize(session.State);
        var rows = (JournalRow[])Invoke(client, "JournalRows", 256)!;
        Assert.Equal(hero.Sprite, rows[0].Sprite); Assert.True(rows[0].Start);
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
        var other = session.State.Actors.First(a => a.Id != hero.Id); other.Name = hero.Name;
        rows = (JournalRow[])Invoke(client, "JournalRows", 256)!;
        Assert.Equal("banner", rows[0].Sprite);
        session.State.Journal.Add("An old tale mentioned " + hero.Name + ".");
        rows = (JournalRow[])Invoke(client, "JournalRows", 256)!; Assert.Equal("banner", rows[0].Sprite);
    }
    [Fact]
    public void DragSelectsObservedTilesWithoutWalkingAndCapsLargeSelections()
    {
        var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var origin = session.State.Position;
        var selection = new WorldTileSelection(); var unknown = origin.Offset(70, 0);
        selection.Rectangle(session, unknown, unknown.Offset(2, 2)); Assert.Empty(selection.Cells);
        selection.Rectangle(session, origin, origin.Offset(16, 16)); Assert.True(selection.TooLarge); Assert.Empty(selection.Cells);
        var client = Client(session); var camera = (MapCamera)Field("_camera").GetValue(client)!;
        camera.Center = new(origin.X + .5f, origin.Y + .5f); camera.Reset(36);
        var start = camera.WorldToScreen(new(origin.X + 1.5f, origin.Y + 1.5f)).ToPoint();
        var end = camera.WorldToScreen(new(origin.X + 3.5f, origin.Y + 2.5f)).ToPoint();
        string before = JsonSerializer.Serialize(session.State);
        Invoke(client, "BeginSelection", start);
        Invoke(client, "UpdateSelection", new MouseState(end.X, end.Y, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));
        Invoke(client, "UpdateSelection", new MouseState(start.X, start.Y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));
        selection = (WorldTileSelection)Field("_selection").GetValue(client)!;
        Assert.Equal(6, selection.Cells.Count); Assert.Equal(before, JsonSerializer.Serialize(session.State));

        Invoke(client, "OpenContext", start); Assert.Equal(6, ((WorldPosition[])Field("_contextCells").GetValue(client)!).Length);
    }
    [Fact]
    public void CursorMenusAndBuildSubmenusStayInsideTheMinimumMapAtEveryCorner()
    {
        var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var client = Client(session);
        var map = (Rectangle)Field("_map").GetValue(client)!;
        foreach (var corner in new[] { map.Location, new Point(map.Right - 1, map.Top), new Point(map.Left, map.Bottom - 1), new Point(map.Right - 1, map.Bottom - 1) })
        {
            Field("_contextCells").SetValue(client, new[] { session.State.Position.Offset(1, 0) }); Field("_contextAnchor").SetValue(client, corner);
            foreach (bool building in new[] { false, true })
            {
                Field("_contextBuilding").SetValue(client, building);
                var controls = Controls(client, "").Where(c => c.Id.StartsWith("context:", StringComparison.Ordinal)).ToArray();
                Assert.NotEmpty(controls); Assert.All(controls, c => Assert.True(map.Contains(c.Bounds), c.Id));
                Assert.True(map.Contains((Rectangle)Field("_contextBox").GetValue(client)!));
                for (int a = 0; a < controls.Length; a++) for (int b = a + 1; b < controls.Length; b++) Assert.False(controls[a].Bounds.Intersects(controls[b].Bounds));
            }
        }
    }

}
