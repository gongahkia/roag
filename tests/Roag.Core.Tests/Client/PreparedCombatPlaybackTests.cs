using System.Text.Json;
using Roag.Client;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Client;

public sealed class PreparedCombatPlaybackTests
{
    [Fact]
    public void WindingUpShowsTheCasterWithoutDamagingPartyPresentation()
    {
        var world = WorldSession.Create(42); EncounterGroupPreview.Prepare(world);
        Assert.True(world.Act(3).Success); // Ranger guards, then the channeler begins preparing.
        var battle = world.State.Battle!; var source = battle.Active;
        int[] health = battle.Units.Select(u => u.Health).ToArray(); int mana = source.Mana;
        var plan = battle.Plans[source.Id];
        var playback = new CombatPlayback();
        Assert.True(playback.Resolve(world, world.Content.Get<AbilityDefinition>(plan.Ability).AsSkill(),
            world.AdvanceEnemy, plan.Target).Success);

        Assert.Equal("guard", playback.Current!.Profile.Family);
        Assert.Equal("guard", playback.Current.Effect);
        Assert.StartsWith("Preparing ", playback.Current.Label);
        Assert.Equal(new[] { battle.ActorIds[source.Id] }, playback.Current.Targets);
        Assert.DoesNotContain(playback.Current.Events, e => e.Kind is "damage" or "ability");
        Assert.Equal(health, battle.Units.Select(u => u.Health)); Assert.Equal(mana, source.Mana);
        string resolved = JsonSerializer.Serialize(world.State);
        while (playback.Phase is "anticipation" or "travel") playback.Advance(.01);
        Assert.Equal(health, playback.View!.Units.Select(u => u.Health));
        Assert.DoesNotContain(playback.DrainImpacts(), e => e.Kind == "damage");
        playback.Skip(); Assert.Equal(resolved, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void UnaffordableReleaseShowsNoAttackTargetsOrDamagingEffects()
    {
        var world = WorldSession.Create(42); EncounterGroupPreview.Prepare(world);
        Assert.True(world.Act(3).Success); int channeler = world.State.Battle!.Active.Id;
        Assert.True(world.AdvanceEnemy().Success);
        int budget = 16;
        while (world.State.Battle!.Active.Id != channeler && budget-- > 0)
            Assert.True(world.State.Battle.Active.Friendly ? world.Act(3).Success : world.AdvanceEnemy().Success);
        Assert.True(budget > 0);
        var battle = world.State.Battle!; var source = battle.Active;
        Assert.False(battle.Plans[channeler].Preparing);
        source.Mana = 0; world.State.Actors.Single(a => a.Id == battle.ActorIds[channeler]).Mana = 0;
        int[] health = battle.Units.Select(u => u.Health).ToArray(); var plan = battle.Plans[channeler];
        var playback = new CombatPlayback();
        Assert.True(playback.Resolve(world, world.Content.Get<AbilityDefinition>(plan.Ability).AsSkill(),
            world.AdvanceEnemy, plan.Target).Success);

        Assert.Equal("Not enough mana", playback.Current!.Label);
        Assert.Equal("guard", playback.Current.Profile.Family);
        Assert.Empty(playback.Current.Targets);
        Assert.DoesNotContain(playback.Current.Events, e => e.Kind is "damage" or "ability");
        Assert.Equal(health, battle.Units.Select(u => u.Health));
        string resolved = JsonSerializer.Serialize(world.State);
        while (playback.Busy) playback.Advance(.05, 2);
        Assert.DoesNotContain(playback.DrainImpacts(), e => e.Kind == "damage");
        Assert.Equal(resolved, JsonSerializer.Serialize(world.State));
    }
}
