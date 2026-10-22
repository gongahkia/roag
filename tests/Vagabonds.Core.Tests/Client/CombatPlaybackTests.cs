using System.Text.Json;
using Microsoft.Xna.Framework;
using Vagabonds.Client;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Tests.Client;

public sealed class CombatPlaybackTests
{
    private static WorldSession Fight() { var s = WorldSession.Create(42); CooperationPreview.Prepare(s); return s; }
    private static int Enemy(WorldSession s) => s.State.Battle!.Units.First(u => !u.Friendly).Id;
    private static string State(WorldSession s) => JsonSerializer.Serialize(s.State);
    private static void Finish(CombatPlayback playback, double speed = 1)
    {
        int budget = 100;
        while (playback.Busy && budget-- > 0) playback.Advance(.05, speed);
        Assert.True(budget > 0);
    }
    [Fact]
    public void NormalFastAndSkippedPlaybackHaveExactlyTheSamePersistentOutcome()
    {
        var normal = Fight(); var fast = Fight(); var skipped = Fight();
        foreach (var (session, speed) in new[] { (normal, 1d), (fast, 2d), (skipped, 0d) })
        {
            var playback = new CombatPlayback(); var skill = session.ActiveSkills()[6];
            Assert.True(playback.Resolve(session, skill, () => session.Act(6, Enemy(session), 3), Enemy(session), 3).Success);
            string resolved = State(session); uint rng = session.State.CombatRandom; long tick = session.State.Tick;
            if (speed == 0) { playback.Skip(); playback.Skip(); } else Finish(playback, speed);
            Assert.Equal(resolved, State(session)); Assert.Equal(rng, session.State.CombatRandom); Assert.Equal(tick, session.State.Tick);
        }
        Assert.Equal(State(normal), State(fast)); Assert.Equal(State(normal), State(skipped));
    }
    [Fact]
    public void HpAndManaStayInTheBeforeViewUntilImpactAndSnapshotsDoNotAliasTheGame()
    {
        var s = Fight(); var b = s.State.Battle!; var p = new CombatPlayback(); int enemy = Enemy(s), hp = b.Units[enemy].Health, mana = b.Active.Mana;
        Assert.True(p.Resolve(s, s.ActiveSkills()[6], () => s.Act(6, enemy, 3), enemy, 3).Success);
        Assert.Equal("anticipation", p.Phase); Assert.Equal(hp, p.View!.Units[enemy].Health); Assert.Equal(mana, p.View.Units[0].Mana);
        Assert.True(b.Units[enemy].Health < hp); Assert.NotSame(b.Units[enemy], p.View.Units[enemy]);
        while (p.Phase is "anticipation" or "travel") p.Advance(.01);
        Assert.Equal("impact", p.Phase); Assert.Equal(b.Units[enemy].Health, p.View.Units[enemy].Health); Assert.Equal(mana - 3, p.View.Units[0].Mana);
        Assert.Single(p.DrainImpacts(), e => e.Kind == "damage");
    }
    [Fact]
    public void FinishingHitRetainsFallenFighterBeforeRewardsAndSkipDoesNotAwardTwice()
    {
        var s = Fight(); int enemy = Enemy(s); var b = s.State.Battle!; b.Units[enemy].Health = 1;
        s.State.Actors.Single(a => a.Id == b.ActorIds[enemy]).Health = 1;
        var p = new CombatPlayback(); int gold = s.Count("gold");
        Assert.True(p.Resolve(s, s.ActiveSkills()[0], () => s.Act(0, enemy), enemy).Success);
        Assert.Null(s.State.Battle); Assert.True(p.Busy); Assert.True(p.EndingPending); Assert.True(p.RewardPending); Assert.False(p.Completed);
        Assert.Equal(1, p.View!.Units[enemy].Health);
        while (p.Phase is "anticipation" or "travel") p.Advance(.01);
        Assert.False(p.View!.Units[enemy].Alive); Assert.True(p.Busy); Assert.Equal(gold + 12, s.Count("gold"));
        string resolved = State(s); p.Skip(); p.Skip(); Assert.Equal(resolved, State(s)); Assert.True(p.Completed); Assert.True(p.RewardPending);
        p.AcknowledgeCompletion(); Assert.False(p.RewardPending);
    }
    [Fact]
    public void InvalidAndDuplicateSubmissionsCannotResolveOrSpendAnything()
    {
        var s = Fight(); var p = new CombatPlayback(); var skill = s.ActiveSkills()[0]; string before = State(s);
        Assert.False(p.Resolve(s, skill, () => s.Act(99)).Success); Assert.False(p.Busy); Assert.Equal(before, State(s));
        Assert.True(p.Resolve(s, skill, () => s.Act(0, Enemy(s)), Enemy(s)).Success); before = State(s);
        int calls = 0; Assert.False(p.Resolve(s, skill, () => { calls++; return s.Act(3); }).Success);
        Assert.Equal(0, calls); Assert.Equal(before, State(s));
    }
    [Fact]
    public void FocusAndInspectionPausePlaybackAndLongStallsDoNotCatchUp()
    {
        var s = Fight(); var p = new CombatPlayback(); p.Resolve(s, s.ActiveSkills()[0], () => s.Act(0, Enemy(s)), Enemy(s));
        p.Advance(30, paused: true); Assert.Equal(0, p.Progress); Assert.Equal("anticipation", p.Phase);
        p.Advance(double.NaN); Assert.Equal(0, p.Progress);
        p.Advance(30); Assert.True(p.Busy); Assert.InRange(p.Progress, 0, .4);
    }
    [Fact]
    public void BurnTickIsItsOwnBeatAfterAttackAndDoesNotAppearInTheFirstImpact()
    {
        var s = Fight(); var b = s.State.Battle!; var next = b.Units[1]; next.Burn = 2; int old = next.Health;
        var p = new CombatPlayback(); p.Resolve(s, s.ActiveSkills()[0], () => s.Act(0, Enemy(s)), Enemy(s));
        while (p.Phase is "anticipation" or "travel") p.Advance(.01);
        Assert.Equal(old, p.View!.Units[1].Health);
        while (p.Current!.Effect != "status") p.Advance(.01);
        Assert.Equal("Burn", p.Current.Label); Assert.Equal(old, p.View!.Units[1].Health);
        while (p.Phase is "anticipation" or "travel") p.Advance(.01);
        Assert.Equal(next.Health, p.View!.Units[1].Health); Assert.True(next.Health < old);
    }
    [Fact]
    public void SharedAidIncludesCasterAndHelperButTwinStrikeOnlyTargetsTheFoe()
    {
        var s = Fight(); var p = new CombatPlayback(); var b = s.State.Battle!;
        p.Resolve(s, s.ActiveSkills()[7], () => s.Act(7, -1, 3), helper: 3);
        Assert.Equal(b.ActorIds.Take(4), p.Current!.Targets); Assert.Equal(b.ActorIds[3], p.Current.Helper);
        s = Fight(); b = s.State.Battle!; p.Reset(); int enemy = Enemy(s);
        p.Resolve(s, s.ActiveSkills()[6], () => s.Act(6, enemy, 3), enemy, 3);
        Assert.Equal(new[] { b.ActorIds[enemy] }, p.Current!.Targets); Assert.Equal(b.ActorIds[3], p.Current.Helper);
    }
    [Fact]
    public void SavingMidPlaybackLoadsResolvedStateAndDoesNotNeedPresentationToContinue()
    {
        var s = Fight(); var p = new CombatPlayback(); p.Resolve(s, s.ActiveSkills()[0], () => s.Act(0, Enemy(s)), Enemy(s));
        var directory = Path.Combine(Path.GetTempPath(), "vagabonds-playback-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(directory, "world.json"); s.Save(path); var restored = WorldSession.Load(path);
            Assert.Equal(State(s), State(restored)); p.Reset(); Assert.False(p.Busy);
            s.Act(3); restored.Act(3); Assert.Equal(State(s), State(restored));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    [Fact]
    public void AFixedEnemyPlanAgainstAFallenCompanionHasNoVisualHitTarget()
    {
        var s = Fight(); var b = s.State.Battle!; var enemy = b.Units[Enemy(s)];
        foreach (var ally in b.Units.Where(u => u.Friendly)) { while (s.State.Battle!.Active.Friendly && s.State.Battle.Active.Id == ally.Id) s.Act(3); }
        Assert.False(b.Active.Friendly);
        b.Units[0].Health = 0; var actor = s.State.Actors.Single(a => a.Id == b.ActorIds[0]); actor.Health = 0; actor.Life = LifeState.Downed;
        b.Plans[enemy.Id] = new("ability.strike", 0);
        var skill = s.Content.Get<Vagabonds.Core.Content.AbilityDefinition>("ability.strike").AsSkill(); var p = new CombatPlayback();
        Assert.True(p.Resolve(s, skill, s.AdvanceEnemy, 0).Success);
        Assert.Empty(p.Current!.Targets); Assert.Contains("no living target", p.Current.Label);
        Assert.DoesNotContain(p.Current.Events, e => e.Kind == "damage");
        string resolved = State(s); Finish(p); Assert.Equal(resolved, State(s));
    }
    [Theory]
    [InlineData(1024, 720)] [InlineData(1440, 900)] [InlineData(1920, 1080)]
    public void EightStableTargetAreasFitTheStageAndNeverOverlap(int width, int height)
    {
        var b = new WorldBattle(); for (int i = 0; i < 8; i++) b.Units.Add(new() { Id = i, HeroId = i < 4 ? i : -1 });
        var area = new Rectangle(16, 84, width - 32, height - 230);
        var rectangles = b.Units.Select(u => BattleStageLayout.Unit(area, b, u)).ToArray();
        Assert.All(rectangles, r => { Assert.True(area.Contains(r)); Assert.True(r.Width >= 180); Assert.True(r.Height >= 160); });
        for (int i = 0; i < 8; i++) for (int j = i + 1; j < 8; j++) Assert.False(rectangles[i].Intersects(rectangles[j]));
    }
}
