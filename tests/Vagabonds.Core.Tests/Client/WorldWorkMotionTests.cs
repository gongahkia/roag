using System.Text.Json;
using Microsoft.Xna.Framework;
using Vagabonds.Client;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Tests.Client;

public sealed class WorldWorkMotionTests
{
    [Fact]
    public void NaturalOccupationDiggingHasTheSameQuietAndActiveGestures()
    {
        using var session = WorldSession.Create(42); var fixture = OccupationPreview.Prepare(session, "quarrier");
        var actor = session.ActorById(fixture.Actor); session.Paused = false; session.AdvanceTicks(2);
        string before = JsonSerializer.Serialize(session.State);
        double time = Enumerable.Range(0, 170).Select(i => i / 100.0).First(t => WorldWorkMotion.Offset(session, actor, false, t, false) != Vector2.Zero);
        Assert.Contains(Enumerable.Range(0, 170), i => WorldWorkMotion.Offset(session, actor, false, i / 100.0, false) == Vector2.Zero);
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
        session.PartyTravelling = true; Assert.NotEqual(Vector2.Zero, WorldWorkMotion.Offset(session, actor, false, time, false));
        actor.Body.RestUntil = session.State.Tick + 100;
        Assert.Equal(Vector2.Zero, WorldWorkMotion.Offset(session, actor, false, time, false));
        actor.Body.RestUntil = 0; actor.Occupation!.Phase = OccupationPhase.Delivering;
        Assert.Equal(Vector2.Zero, WorldWorkMotion.Offset(session, actor, false, time, false));
    }
    [Fact]
    public void ActualDiggingOccasionallyWigglesWithoutChangingSimulationState()
    {
        var session = Vagabonds.Core.Tests.World.WorldCollisionTests.Arena(); var actor = session.Party.Single();
        var target = actor.Position.Offset(1, 0); session.Chunks.Edit(target, Material.Rock);
        Assert.True(session.Submit(new(WorldCommandKind.Dig, target)).Success);
        session.Paused = false; session.AdvanceTicks(1);
        var order = Assert.Single(session.State.Work); Assert.StartsWith("Working:", order.Status);
        string before = JsonSerializer.Serialize(session.State);
        Vector2[] samples = Enumerable.Range(0, 170).Select(i => WorldWorkMotion.Offset(session, actor, false, i / 100.0, false)).ToArray();
        Assert.Contains(samples, p => p != Vector2.Zero); Assert.Contains(Vector2.Zero, samples);
        Assert.All(samples, p => { Assert.InRange(Math.Abs(p.X), 0, .055f); Assert.InRange(Math.Abs(p.Y), 0, .025f); });
        Assert.Equal(before, JsonSerializer.Serialize(session.State));

        double activeTime = Enumerable.Range(0, 170).Select(i => i / 100.0).First(t => WorldWorkMotion.Offset(session, actor, false, t, false) != Vector2.Zero);
        Assert.Equal(Vector2.Zero, WorldWorkMotion.Offset(session, actor, true, activeTime, false));
        Assert.Equal(Vector2.Zero, WorldWorkMotion.Offset(session, actor, false, activeTime, true));
        session.Paused = true; AssertStill(); session.Paused = false;
        order.Suspended = true; AssertStill(); order.Suspended = false;
        session.PartyTravelling = true; AssertStill(); session.PartyTravelling = false;
        actor.Body.RestUntil = session.State.Tick + 100; session.AdvanceTicks(1); AssertStill();
        actor.Body.RestUntil = 0; session.AdvanceTicks(100); Assert.Empty(session.State.Work); AssertStill();

        void AssertStill() => Assert.Equal(Vector2.Zero, WorldWorkMotion.Offset(session, actor, false, activeTime, false));
    }
}
