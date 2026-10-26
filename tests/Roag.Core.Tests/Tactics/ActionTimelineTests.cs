using System.Text.Json;
using Roag.Core.Tactics;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class ActionTimelineTests
{
    [Fact]
    public void SameTimeImpactsAndDeadlinesResolveBeforePartyDecisionsAndEnemies()
    {
        var timeline = new ActionTimeline();
        var enemy = timeline.Schedule(ScheduledKind.ActorReady, 100, 99);
        var member1 = timeline.Schedule(ScheduledKind.ActorReady, 100, 10, true);
        var effect = timeline.Schedule(ScheduledKind.Effect, 100, 10, referenceId: 1);
        var member2 = timeline.Schedule(ScheduledKind.ActorReady, 100, 11, true);
        var impact = timeline.Schedule(ScheduledKind.Impact, 100, 99, referenceId: 2);
        var deadline = timeline.Schedule(ScheduledKind.StageDeadline, 100, referenceId: 3);
        var earlier = timeline.Schedule(ScheduledKind.StagePressure, 50, referenceId: 3);
        Assert.Equal(new[] { earlier, effect, impact, deadline, member1, member2, enemy }, Drain(timeline));
        Assert.Equal(100, timeline.Clock);
        Assert.Null(timeline.TakeNext());
        Assert.Equal(100, timeline.Clock);
    }

    [Fact]
    public void InspectionAndRejectedSchedulingConsumeNeitherTimeNorSequence()
    {
        var timeline = new ActionTimeline(50);
        var ready = timeline.Schedule(ScheduledKind.ActorReady, 100, 1, true);
        string before = JsonSerializer.Serialize(timeline.Snapshot());
        for (int i = 0; i < 100; i++) Assert.Equal(ready, timeline.Peek);
        Assert.Throws<ArgumentOutOfRangeException>(() => timeline.Schedule(ScheduledKind.Impact, 49));
        Assert.Throws<ArgumentOutOfRangeException>(() => timeline.Schedule(ScheduledKind.ActorReady, 50));
        Assert.Throws<ArgumentException>(() => timeline.Schedule(ScheduledKind.Impact, 50, playerControlled: true));
        Assert.Throws<ArgumentOutOfRangeException>(() => timeline.Schedule((ScheduledKind)999, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => timeline.Schedule(ScheduledKind.Impact, 50, referenceId: -1));
        var snapshot = timeline.Snapshot();
        snapshot.Pending[0] = ready with { Due = 999 };
        Assert.Equal(before, JsonSerializer.Serialize(timeline.Snapshot()));
    }

    [Fact]
    public void SaveContinuationPreservesExactOrderAfterCancellationAndNewWork()
    {
        var timeline = new ActionTimeline(70);
        var cancelled = timeline.Schedule(ScheduledKind.ActorReady, 90, 1, true);
        timeline.Schedule(ScheduledKind.Impact, 90, 1, referenceId: 77);
        timeline.Schedule(ScheduledKind.ActorReady, 90, 2, true);
        timeline.Schedule(ScheduledKind.ActorReady, 90, 3);
        Assert.True(timeline.Cancel(cancelled.Sequence));
        Assert.False(timeline.Cancel(cancelled.Sequence));
        var json = JsonSerializer.Serialize(timeline.Snapshot());
        var restored = ActionTimeline.Restore(JsonSerializer.Deserialize<TimelineSnapshot>(json)!);
        Assert.Equal(timeline.Schedule(ScheduledKind.Effect, 90, 2, referenceId: 78),
            restored.Schedule(ScheduledKind.Effect, 90, 2, referenceId: 78));
        Assert.Equal(Drain(timeline), Drain(restored));
        Assert.Equal(timeline.Clock, restored.Clock);
        Assert.Equal(timeline.NextSequence, restored.NextSequence);
    }

    [Fact]
    public void RemovingReadinessDoesNotCancelOwnedPendingAttacksOrOtherMembers()
    {
        var timeline = new ActionTimeline();
        timeline.Schedule(ScheduledKind.ActorReady, 100, 1, true);
        var impact = timeline.Schedule(ScheduledKind.Impact, 150, 1, referenceId: 5);
        var other = timeline.Schedule(ScheduledKind.ActorReady, 100, 2, true);
        Assert.Equal(1, timeline.CancelActor(1, ScheduledKind.ActorReady));
        Assert.Equal(new[] { other, impact }, Drain(timeline));
    }

    [Fact]
    public void MalformedSavesCannotIntroducePastWorkDuplicateIdsOrInvalidPriority()
    {
        var entry = new ScheduledAction(1, 10, ScheduledKind.ActorReady, 1, true, 0);
        var good = new TimelineSnapshot(10, 2, [entry]);
        foreach (var bad in new[] {
            good with { Clock = -1 }, good with { NextSequence = 1 },
            good with { Pending = [entry, entry] },
            good with { Pending = [entry with { Due = 9 }] },
            good with { Pending = [entry with { ActorId = 0 }] },
            good with { Pending = [entry with { Kind = ScheduledKind.Impact }] },
            good with { Pending = [entry with { Kind = (ScheduledKind)999 }] },
            good with { Pending = [entry with { ReferenceId = -1 }] }
        }) Assert.Throws<InvalidDataException>(() => ActionTimeline.Restore(bad));
        Assert.Equal(entry, ActionTimeline.Restore(good).TakeNext());
    }

    private static ScheduledAction[] Drain(ActionTimeline timeline)
    {
        var result = new List<ScheduledAction>();
        while (timeline.TakeNext() is { } next) result.Add(next);
        return result.ToArray();
    }
}
