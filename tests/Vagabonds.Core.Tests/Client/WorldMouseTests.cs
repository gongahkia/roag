using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Vagabonds.Client;
using Vagabonds.Core.World;
using Vagabonds.Graphics;
using Xunit;

namespace Vagabonds.Tests.Client;

public sealed class WorldMouseTests
{
    private static FieldInfo Field(string name) => typeof(WorldGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static object? Invoke(WorldGame client, string name, params object[] args) => typeof(WorldGame).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, args);
    private static WorldGame Client(WorldSession session)
    {
        var client = (WorldGame)RuntimeHelpers.GetUninitializedObject(typeof(WorldGame)); GC.SuppressFinalize(client);
        Field("_options").SetValue(client, Options.Parse(["--scene", "world"])); Field("_session").SetValue(client, session);
        Field("_controls").SetValue(client, new List<WorldMouseControl>()); Field("_route").SetValue(client, new Queue<WorldPosition>());
        Field("_camera").SetValue(client, new MapCamera { Viewport = new(16, 84, 674, 490) });
        Field("_map").SetValue(client, new Rectangle(16, 84, 674, 490)); Field("_side").SetValue(client, new Rectangle(706, 84, 302, 620));
        Field("_panel").SetValue(client, ""); Field("_tool").SetValue(client, ""); return client;
    }
    private static List<WorldMouseControl> Controls(WorldGame client, string panel)
    {
        Field("_panel").SetValue(client, panel); Invoke(client, "BuildMouseControls", 1024, 720);
        return ((List<WorldMouseControl>)Field("_controls").GetValue(client)!).ToList();
    }
    [Fact]
    public void ClickingAPersonPlansAnObservedApproachWithoutMovingOrSpendingTime()
    {
        var session = WorldSession.Create(42); var person = session.NearbyPerson() ?? session.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).MinBy(a => a.Position.Distance(session.State.Position))!;
        string before = JsonSerializer.Serialize(session.State); uint rng = session.State.CombatRandom;
        var plan = WorldMouseNavigation.Plan(session, person.Position);
        Assert.True(plan.Reachable); Assert.Equal("person", plan.Interaction!.Kind); Assert.Equal(person.Id, plan.Interaction.Actor);
        Assert.NotEmpty(plan.Path); Assert.InRange(plan.Path[^1].Distance(person.Position), 0, 2);
        Assert.All(plan.Path, p => Assert.True(session.Chunks.Seen(p)));
        Assert.Equal(before, JsonSerializer.Serialize(session.State)); Assert.Equal(rng, session.State.CombatRandom);
    }
    [Fact]
    public void StorageCanBeApproachedEvenThoughItsOwnCellBlocksMovement()
    {
        var session = WorldSession.Create(42); var origin = session.State.Position; var chest = origin.Offset(4, 0);
        for (int x = 0; x < 5; x++) session.Chunks.Edit(origin.Offset(x, 0), Material.Floor);
        session.Chunks.Edit(chest, Material.Storage); session.Reveal();
        var plan = WorldMouseNavigation.Plan(session, chest);
        Assert.True(plan.Reachable); Assert.Equal("storage", plan.Interaction!.Kind); Assert.Equal(origin.Offset(3, 0), plan.Path[^1]);
        var camera = new MapCamera { Center = new(origin.X + .5f, origin.Y + .5f), Viewport = new(0, 0, 480, 480) };
        var pointer = new WorldPointerPreview(); pointer.Update(session, camera, camera.WorldToScreen(new(chest.X + .5f, chest.Y + .5f)).ToPoint());
        Assert.Equal(plan.Path, pointer.Path);
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
        Assert.Equal(WorldMouseNavigation.Plan(session, person.Position).Path, pointer.Path);
        pointer.Update(session, camera, point);
        Assert.Equal(cursorCell, pointer.NavigationCell);
    }
    [Fact]
    public void MouseControlsCoverGameplayAndStayInsideTheMinimumWindow()
    {
        var session = WorldSession.Create(42); var client = Client(session);
        var normal = Controls(client, "");
        foreach (string id in new[] { "pause", "save", "load", "panel:game", "interact", "panel:inventory", "panel:journal", "panel:map", "panel:actions", "panel:build", "tool:dig", "panel:work", "panel:craft", "stop", "zoom:default" }) Assert.Contains(normal, c => c.Id == id);
        foreach (string panel in new[] { "", "inventory", "trade", "build", "work", "craft", "actions", "guide", "map", "journal", "inspector", "game" })
        {
            var controls = Controls(client, panel);
            Assert.All(controls, c => Assert.True(new Rectangle(0, 0, 1024, 720).Contains(c.Bounds), panel + ": " + c.Id));
            for (int i = 0; i < controls.Count; i++) for (int j = i + 1; j < controls.Count; j++)
                Assert.False(controls[i].Bounds.Intersects(controls[j].Bounds), panel + ": " + controls[i].Id + " / " + controls[j].Id);
            if (panel.Length > 0) Assert.Contains(controls, c => c.Id == "close");
        }
        Assert.Equal(6, Controls(client, "build").Count(c => c.Id.StartsWith("build:", StringComparison.Ordinal)));
        Assert.Contains(Controls(client, "actions"), c => c.Id == "rest");
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
        Assert.True(session.Submit(new(WorldCommandKind.Build, session.State.Position, Material: Material.Camp)).Success);
        Field("_workerIndex").SetValue(client, 500);
        Field("_workerPage").SetValue(client, 500);
        var selected = Controls(client, "work").Single(c => c.Id.StartsWith("worker:", StringComparison.Ordinal) && c.Selected);
        Invoke(client, "ActivateControl", "assign");
        Assert.Equal(long.Parse(selected.Id[7..], System.Globalization.CultureInfo.InvariantCulture), session.State.Work.Single().Worker);
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
        Assert.Contains("Dig", WorldInspection.Inspect(session, rock).Reason);
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
}
