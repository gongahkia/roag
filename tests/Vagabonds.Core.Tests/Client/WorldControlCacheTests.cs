using Microsoft.Xna.Framework;
using Vagabonds.Client;
using Xunit;

namespace Vagabonds.Core.Tests.Client;

public sealed class WorldControlCacheTests
{
    [Fact]
    public void IdenticalControlsReuseTheObjectWithoutAllocatingAndRemovedControlsLeaveHitTesting()
    {
        var controls = new List<WorldMouseControl>(); var cache = new WorldControlCache(controls);
        cache.Begin(); cache.Add("action", "Work", new(1, 2, 30, 20)); var first = Assert.Single(controls);
        long bytes = WarmAllocationProbe.Measure(() => { cache.Begin(); cache.Add("action", "Work", new(1, 2, 30, 20)); });
        Assert.Same(first, Assert.Single(controls)); Assert.InRange(bytes, 0, 1024);
        cache.Begin(); Assert.Null(WorldMouseControl.Hit(controls, new(5, 5)));
        cache.Begin(); Assert.Equal(0, cache.Retained);
    }

    [Fact]
    public void EveryLivePresentationFieldInvalidatesReuseIncludingAvailabilityAndReason()
    {
        var original = new WorldMouseControl("id", "label", new(1, 2, 3, 4));
        WorldMouseControl[] changed = [original with { Id = "other" }, original with { Label = "changed" },
            original with { Bounds = new(2, 2, 3, 4) }, original with { Enabled = false }, original with { Selected = true },
            original with { Tip = "tip" }, original with { DisabledReason = "No mana" }, original with { Sprite = "apple" },
            original with { Detail = "detail" }, original with { Progress = .5f }, original with { BadgeSprite = "banner" }, original with { Dimmed = true }];
        foreach (var next in changed)
        {
            var controls = new List<WorldMouseControl>(); var cache = new WorldControlCache(controls);
            cache.Begin(); Add(original); var before = Assert.Single(controls);
            cache.Begin(); Add(next); Assert.Equal(next, Assert.Single(controls)); Assert.NotSame(before, controls[0]);
            void Add(WorldMouseControl c) => cache.Add(c.Id, c.Label, c.Bounds, c.Enabled, c.Selected, c.Tip, c.DisabledReason,
                c.Sprite, c.Detail, c.Progress, c.BadgeSprite, c.Dimmed);
        }
    }

    [Fact]
    public void ChangedControlOrderCannotReuseAnUnrelatedAction()
    {
        var controls = new List<WorldMouseControl>(); var cache = new WorldControlCache(controls);
        cache.Begin(); cache.Add("first", "First", new(0, 0, 20, 20)); cache.Add("last", "Last", new(0, 0, 20, 20));
        Assert.Equal("last", WorldMouseControl.Hit(controls, new(5, 5))!.Id);
        cache.Begin(); cache.Add("last", "Last", new(0, 0, 20, 20)); cache.Add("first", "First", new(0, 0, 20, 20));
        Assert.Equal("first", WorldMouseControl.Hit(controls, new(5, 5))!.Id);
        Assert.Equal(4, cache.Retained);
    }
}
