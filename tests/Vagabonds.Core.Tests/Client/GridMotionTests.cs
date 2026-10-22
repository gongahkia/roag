using Microsoft.Xna.Framework;
using Vagabonds.Client;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Tests.Client;

public sealed class GridMotionTests
{
    [Fact]
    public void CommittedStepsInterpolateAndFinishExactly()
    {
        var motion = new GridMotion(); motion.Reset(new(10, 20)); motion.Observe(new(11, 20), .14);
        Assert.Equal(new Vector2(10.5f, 20.5f), motion.Position);
        motion.Advance(TimeSpan.FromSeconds(.07));
        Assert.Equal(new Vector2(11, 20.5f), motion.Position); Assert.True(motion.Moving);
        motion.Advance(TimeSpan.FromSeconds(.07));
        Assert.Equal(new Vector2(11.5f, 20.5f), motion.Position); Assert.False(motion.Moving);
    }

    [Fact]
    public void QueuedTurnsKeepTheirCornersAndCarryTimeAcrossSteps()
    {
        var motion = new GridMotion(); motion.Reset(new(10, 20));
        motion.Observe(new(11, 20), .14); motion.Observe(new(11, 21), .14);
        motion.Advance(TimeSpan.FromSeconds(.07));
        Assert.Equal(new Vector2(11, 20.5f), motion.Position);
        motion.Advance(TimeSpan.FromSeconds(.14));
        Assert.Equal(new Vector2(11.5f, 21), motion.Position);
        motion.Advance(TimeSpan.FromSeconds(.07));
        Assert.Equal(new Vector2(11.5f, 21.5f), motion.Position); Assert.False(motion.Moving);
    }

    [Fact]
    public void FrameRateDoesNotChangeTravelOrFacing()
    {
        var fast = new GridMotion(); var slow = new GridMotion();
        foreach (var motion in new[] { fast, slow })
        { motion.Reset(new(10, 20)); motion.Observe(new(9, 20), .14); motion.Observe(new(9, 21), .14); }
        for (int i = 0; i < 21; i++) fast.Advance(TimeSpan.FromMilliseconds(10));
        slow.Advance(TimeSpan.FromMilliseconds(210));
        Assert.InRange(Vector2.Distance(fast.Position, slow.Position), 0, .00001f);
        Assert.True(fast.FacingLeft); Assert.True(slow.FacingLeft);
    }

    [Fact]
    public void HorizontalMovementTurnsTheSpriteAndVerticalMovementKeepsItsFacing()
    {
        var motion = new GridMotion(); motion.Reset(new(10, 20)); Assert.False(motion.FacingLeft);
        motion.Observe(new(9, 20), .14); motion.Advance(TimeSpan.FromSeconds(.14)); Assert.True(motion.FacingLeft);
        motion.Observe(new(9, 21), .14); motion.Advance(TimeSpan.FromSeconds(.14)); Assert.True(motion.FacingLeft);
        motion.Observe(new(10, 21), .14); motion.Advance(TimeSpan.FromSeconds(.14)); Assert.False(motion.FacingLeft);
        motion.Observe(new(10, 20), .14); motion.Advance(TimeSpan.FromSeconds(.14)); Assert.False(motion.FacingLeft);
    }

    [Fact]
    public void StairsAndLongTransfersSnapInsteadOfSlidingAcrossTheWorld()
    {
        var motion = new GridMotion(); motion.Reset(new(10, 20)); motion.Observe(new(9, 20), .14);
        motion.Observe(new(9, 20, 1), .14);
        Assert.Equal(new Vector2(9.5f, 20.5f), motion.Position); Assert.False(motion.Moving);
        motion.Observe(new(100, 100, 1), .14);
        Assert.Equal(new Vector2(100.5f, 100.5f), motion.Position); Assert.False(motion.Moving);
        motion.Reset(new(10, 20), resetFacing: true); Assert.False(motion.FacingLeft);
    }
}
