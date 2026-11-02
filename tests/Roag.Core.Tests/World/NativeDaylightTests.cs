using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeDaylightTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(8, true)]
    public void OutdoorEnemyAcquisitionAndLocalEngagementUseTheSamePrivateDaylightRules(int hour, bool detected)
    {
        using var world = WorldSession.Create(42); world.State.Tick = hour * WorldSession.TicksPerHour;
        var hero = world.Party.Single(); hero.Position = world.State.Position = new(100, 100);
        for (int y = 88; y <= 112; y++) for (int x = 88; x <= 120; x++) world.Chunks.Edit(new(x, y), Material.Floor);
        var combat = world.CreateTacticalSimulation(new(EnableSurvival: true));
        combat.Register(hero, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42), AutoEat = false });
        var enemy = ActorFactory.Create(world.Content, "character.raider", 42, world.State.NextActorId++, new(108, 100), 0);
        world.State.Actors.Add(enemy); combat.RegisterRetainedEnemy(enemy, combat.Clock + 10000);
        Assert.Same(hero, combat.AdvanceToDecision());
        Assert.Equal(detected ? hero.Id : (long?)null, combat.SelectEnemyTarget(enemy.Id));
        Assert.Equal(detected, combat.IsLocallyEngaged(hero.Id));
        Assert.Equal(detected, world.VisionFrom(hero.Position).Contains(enemy.Position));
        Assert.Equal(detected, world.VisionFrom(enemy.Position).Contains(hero.Position));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData(0, true)] [InlineData(5, true)] [InlineData(6, false)]
    [InlineData(19, false)] [InlineData(20, true)] [InlineData(23, true)]
    public void NativeSurfaceSightUsesOwnCalendarAndWeatherWithoutChangingUndergroundSight(int hour, bool night)
    {
        using var world = WorldSession.Create(42);
        world.State.Tick = hour * WorldSession.TicksPerHour;
        world.CreateTacticalSimulation(new(EnableSurvival: true));
        var origin = new WorldPosition(100, 100);
        for (int y = 88; y <= 112; y++) for (int x = 88; x <= 112; x++) world.Chunks.Edit(new(x, y), Material.Floor);
        var weather = world.WeatherAt(origin).Weather;
        int penalty = weather is WeatherKind.Storm or WeatherKind.Snow ? 3 : weather == WeatherKind.Rain ? 1 : 0;
        int radius = Math.Max(4, (night ? 6 : 11) - penalty);
        string before = JsonSerializer.Serialize(world.State);
        Assert.Equal(night, world.IsNight); Assert.Equal(hour, world.HourOfDay);
        Assert.Equal(radius, world.SightRadiusAt(origin)); Assert.Equal(11, world.SightRadiusAt(origin with { Floor = 1 }));
        var sight = world.VisionFrom(origin);
        Assert.Contains(origin.Offset(radius, 0), sight); Assert.DoesNotContain(origin.Offset(radius + 1, 0), sight);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void PaidActionsCrossDawnOnTheSameCalendarAndReloadWithTheSameSight()
    {
        using var world = WorldSession.Create(42); world.State.Tick = 6 * WorldSession.TicksPerHour - 1;
        var combat = world.CreateTacticalSimulation(new(EnableSurvival: true)); var hero = world.Party.Single();
        combat.Register(hero, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42), AutoEat = false });
        Assert.Same(hero, combat.AdvanceToDecision()); Assert.True(world.IsNight);
        int night = world.SightRadiusAt(hero.Position); long start = combat.Clock;
        for (int i = 0; i < 12; i++)
        {
            Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
            Assert.Same(hero, combat.AdvanceToPartyDecision());
        }
        Assert.Equal(start + 1200, combat.Clock); Assert.False(world.IsNight);
        Assert.True(world.SightRadiusAt(hero.Position) > night); Assert.Contains("06:00", world.CalendarDescription);
        string root = Path.Combine(Path.GetTempPath(), "roag-daylight-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(world.VisionFrom(hero.Position), loaded.VisionFrom(hero.Position));
            Assert.Equal(world.CalendarDescription, loaded.CalendarDescription);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
