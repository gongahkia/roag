using System.Text.Json;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class WorldCollisionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-collision-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    internal static WorldSession Arena()
    {
        var s = WorldSession.Create(42); int parked = 0;
        foreach (var a in s.State.Actors.Where(a => !a.Recruited && a.Position.Distance(s.State.Position) < 100))
        { a.Position = s.State.Position.Offset(35 + parked++, 0); s.Chunks.Edit(a.Position, Material.Floor); }
        for (int y = -10; y <= 10; y++) for (int x = -10; x <= 10; x++) s.Chunks.Edit(s.State.Position.Offset(x, y), Material.Floor);
        s.Reveal(); return s;
    }
    [Fact]
    public void StartingActorsHaveFreeSeparateCellsAndRemainSeparateAsTheyWorkAndTravel()
    {
        var s = WorldSession.Create(42); s.Paused = false;
        for (int i = 0; i < 80; i++)
        {
            var local = s.State.Actors.Where(a => a.Life != LifeState.Dead && a.Position.Distance(s.State.Position) < 70).ToArray();
            Assert.Equal(local.Length, local.Select(a => a.Position).Distinct().Count());
            Assert.All(local, a => Assert.True(s.GroundOpen(a.Position), a.Name + " " + a.Position));
            s.AdvanceTicks(5);
        }
    }
    [Fact]
    public void OccupiedMovesFailWithoutMutationAndRoutesAvoidPeopleWithoutReadingHiddenOccupancy()
    {
        var s = Arena(); var origin = s.State.Position;
        var person = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited); person.Position = origin.Offset(1, 0);
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Move, person.Position)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
        var path = s.Route(origin.Offset(2, 0)); Assert.NotEmpty(path); Assert.DoesNotContain(person.Position, path);
        s.Visible.Remove(person.Position); // Remembered terrain, no current sight of the actor.
        path = s.Route(origin.Offset(2, 0)); Assert.Contains(person.Position, path);
        Assert.DoesNotContain(person.Name, s.MovementBlockReason(person.Position, observedOnly: true));
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
    [Theory]
    [InlineData(Material.Camp)] [InlineData(Material.Storage)] [InlineData(Material.Rock)] [InlineData(Material.Wall)] [InlineData(Material.Water)]
    public void SolidTerrainBlocksBothMovementAndRoutes(Material material)
    {
        var s = Arena(); var p = s.State.Position.Offset(1, 0); s.Chunks.Edit(p, material);
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Move, p)).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        var path = s.Route(p.Offset(1, 0)); Assert.NotEmpty(path); Assert.DoesNotContain(p, path);
    }
    [Theory]
    [InlineData(Material.Earth)] [InlineData(Material.Floor)] [InlineData(Material.Bridge)] [InlineData(Material.StairsDown)] [InlineData(Material.StairsUp)]
    public void FlatGroundBridgesAndStairsRemainWalkable(Material material)
    {
        var s = Arena(); var p = s.State.Position.Offset(1, 0); s.Chunks.Edit(p, material);
        Assert.True(s.Submit(new(WorldCommandKind.Move, p)).Success);
    }
    [Fact]
    public void RenderedBarrelsBlockWalkingAndTimedDismantlingPersistsWithoutRespawningOrDuplicateYield()
    {
        var s = WorldSession.Create(42); var origin = s.State.Position;
        var barrel = (from y in Enumerable.Range(-20, 41) from x in Enumerable.Range(-20, 41)
                      let p = origin.Offset(x, y) where s.ObjectAt(p) is { Removable: true } select p).First();
        var approach = WorldSession.Directions.Select(d => barrel.Offset(d.X, d.Y)).First(s.GroundOpen);
        s.State.Position = s.Party.Single().Position = approach; s.Reveal();
        Assert.False(s.Submit(new(WorldCommandKind.Move, barrel)).Success);
        int wood = s.Count("wood"); Assert.True(s.Submit(new(WorldCommandKind.Dig, barrel)).Success);
        Assert.True(s.ObjectAt(barrel)!.Solid); s.Submit(new(WorldCommandKind.Wait, Amount: 20));
        Assert.Null(s.ObjectAt(barrel)); Assert.Equal(wood + 1, s.Count("wood"));
        string before = JsonSerializer.Serialize(s.State); Assert.False(s.Submit(new(WorldCommandKind.Dig, barrel)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
        var slot = Path.Combine(_directory, "barrel.json"); s.Save(slot); var loaded = WorldSession.Load(slot);
        Assert.Null(loaded.ObjectAt(barrel)); Assert.True(loaded.GroundOpen(barrel));
    }
    [Fact]
    public void CachesAndLandmarkFurnitureStaySolidWhileFlatLandmarksStayWalkable()
    {
        var s = WorldSession.Create(42);
        foreach (var d in s.State.Discoveries)
        {
            var obj = s.ObjectAt(d.Position)!;
            Assert.Equal(obj.Sprite is not ("animal-bones" or "grave-slab"), obj.Solid);
            if (obj.Solid) Assert.False(s.GroundOpen(d.Position));
        }
        var cache = s.State.Loot.First(c => !s.State.Discoveries.Any(d => d.Position == c.Position));
        Assert.False(s.GroundOpen(cache.Position)); cache.Taken = true; Assert.True(s.GroundOpen(cache.Position));
    }
    [Fact]
    public void AnOccupiedStairLandingRejectsTransferWithoutChangingAnyActorOrTime()
    {
        var s = Arena(); var stair = s.Plan.Sites[0].Center.Offset(2, 0);
        s.State.Position = s.Party.Single().Position = stair; s.Reveal();
        var npc = s.State.Actors.First(a => !a.Recruited); npc.Position = stair.Offset(-4, 0) with { Floor = 1 };
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Interact, stair)).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
}
