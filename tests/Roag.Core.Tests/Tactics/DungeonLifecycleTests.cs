using Roag.Client;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    private static void PrepareTerminalAttack(WorldSession world)
    {
        var hero = world.Party.Single(); hero.Health = 1;
        var enemy = ReplaceRegular(world, "core:enemy/mossbound_guard"); enemy.Position = hero.Position.Offset(1, 0);
        world.Reveal();
    }

    private static void WaitThroughTerminalAction(WorldSession world)
    {
        for (int i = 0; i < 10 && !world.State.AwaitingContinuation; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.True(world.State.AwaitingContinuation); Assert.Equal(LifeState.Dead, world.Party.Single().Life);
    }

    [Fact]
    public void FinalNativeDeathReplacesTheContinuingSlotBeforeReturningAndCannotResumeWithAnotherTraveller()
    {
        using var world = Floor(); PrepareTerminalAttack(world);
        string root = Path.Combine(Path.GetTempPath(), "roag-terminal-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); byte[] alive = File.ReadAllBytes(slot);
            WaitThroughTerminalAction(world); Assert.False(world.DeathPending); Assert.NotEqual(alive, File.ReadAllBytes(slot));
            using var loaded = WorldSession.Load(slot);
            Assert.True(loaded.State.AwaitingContinuation); Assert.Equal(LifeState.Dead, loaded.Party.Single().Life);
            long clock = loaded.DungeonCombat.Clock; int actors = loaded.State.Actors.Count;
            Assert.False(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.False(loaded.Submit(new(WorldCommandKind.Continue)).Success);
            Assert.Equal(clock, loaded.DungeonCombat.Clock); Assert.Equal(actors, loaded.State.Actors.Count);
            var (resumed, continued) = WorldStartup.Open(Options.Parse(["--save", slot]));
            using var startup = Assert.IsType<WorldSession>(resumed); Assert.True(continued); Assert.True(startup.State.AwaitingContinuation);
            var (fresh, resumedOld) = WorldStartup.Open(Options.Parse(["--new", "--seed", "919", "--save", slot]));
            using var replacement = Assert.IsType<WorldSession>(fresh); Assert.False(resumedOld);
            Assert.Equal(919, replacement.Seed); Assert.False(replacement.State.AwaitingContinuation);
            Assert.Equal(0, replacement.DungeonCombat.Clock); Assert.Equal(LifeState.Alive, replacement.Party.Single().Life);
            // Merely constructing the explicit new run does not overwrite the terminal slot.
            using var stillTerminal = WorldSession.Load(slot); Assert.True(stillTerminal.State.AwaitingContinuation);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void FailedNativeDeathSaveKeepsTheTerminalSessionAndRequiresASuccessfulRetry()
    {
        using var world = Floor(); PrepareTerminalAttack(world);
        string root = Path.Combine(Path.GetTempPath(), "roag-terminal-io-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); byte[] before = File.ReadAllBytes(slot);
            world.SavePath = Path.Combine(slot, "cannot-write.json");
            WaitThroughTerminalAction(world); Assert.True(world.DeathPending); Assert.Equal(before, File.ReadAllBytes(slot));
            Assert.False(world.SubmitDungeon(new(TacticalAction.Wait)).Success); Assert.True(world.State.AwaitingContinuation);
            world.Save(slot); Assert.False(world.DeathPending);
            using var loaded = WorldSession.Load(slot); Assert.True(loaded.State.AwaitingContinuation); Assert.Equal(LifeState.Dead, loaded.Party.Single().Life);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
