using Microsoft.Xna.Framework;
using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private sealed record CombatFloat(long Target, string Text, string Kind, double Born);
    private readonly List<CombatFloat> _combatFloats = [];
    private WorldSession? _feedbackSession;
    private long _seenFeedback;
    private double _presentationTime, _combatVisualTime;
    private CombatPlayback? _combatPlayback;
    private CombatPlayback Playback => _combatPlayback ??= new();
    private CombatAudio? _combatAudio;
    private double _combatSpeed = 1;
    private float _combatVolume = .4f;
    private bool CombatStageVisible => _session.State.Dungeon == null && !_session.State.RoagCombat && (Playback.Busy || _session.State.Battle != null);
    private WorldBattle? DisplayBattle => Playback.View ?? _session.State.Battle;
    private void ResetCombatPresentation()
    {
        Playback.Reset(); _combatFloats.Clear(); _combatAudio?.Stop(); _enemyTimer = 0;
        _feedbackSession = _session; _seenFeedback = _session.State.NextCombatEvent - 1;
    }
    private void UpdateCombatPlayback(double elapsed)
    {
        if (InputFocused) _presentationTime += Math.Clamp(elapsed, 0, .2);
        CollectCombatFeedback();
        bool paused = !InputFocused || _panel.Length > 0;
        if (!paused) _combatVisualTime += Math.Clamp(elapsed, 0, .2) * _combatSpeed;
        string cue = Playback.Current?.Profile.Sound ?? "slash";
        bool hasTarget = Playback.Current?.Targets.Length > 0;
        Playback.Advance(elapsed, _combatSpeed, paused);
        var impacts = Playback.DrainImpacts();
        foreach (var e in impacts) ShowCombatEvent(e);
        if (impacts.Length > 0 && hasTarget && _combatVolume > 0)
        {
            _combatAudio ??= new();
            _combatAudio.Play(cue, _combatVolume);
        }
        if (paused) _combatAudio?.Stop();
        CompleteCombatPlayback();
        _combatFloats.RemoveAll(e => _combatVisualTime - e.Born > 1.35);
    }
    private void CompleteCombatPlayback()
    {
        if (!Playback.Completed) return;
        if (Playback.RewardPending && !_session.State.Endgame.Victory) { _panel = "spoils"; StopTravel(); }
        Playback.AcknowledgeCompletion(); _enemyTimer = 0;
    }
    private void ResolveEnemyPresentation()
    {
        if (Playback.Busy || _session.State.Battle is not { } b || b.Active.Friendly) return;
        var plan = b.Plans[b.Active.Id]; var skill = _session.Content.Get<AbilityDefinition>(plan.Ability).AsSkill();
        try { _notice = Playback.Resolve(_session, skill, _session.AdvanceEnemy, plan.Target).Message; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { _notice = "Death save failed: " + e.Message + " Use Save to retry."; }
    }
    private void CollectCombatFeedback()
    {
        if (!ReferenceEquals(_feedbackSession, _session)) { ResetCombatPresentation(); return; }
        // Resolution-owned events are emitted only when their playback reaches impact.
        if (!Playback.Busy)
            foreach (var e in _session.State.CombatEvents.Where(e => e.Sequence > _seenFeedback)) ShowCombatEvent(e);
        _seenFeedback = _session.State.NextCombatEvent - 1;
    }
    private void ShowCombatEvent(CombatEvent e)
    {
        if (e.Target != 0 && e.Kind is "damage" or "heal" or "status" or "expire" or "fall" or "injury" or "skip")
            _combatFloats.Add(new(e.Target, e.Kind == "damage" ? "-" + e.Amount : e.Kind == "heal" ? "+" + e.Amount : e.Label, e.Kind, _combatVisualTime));
        if (_combatFloats.Count > 32) _combatFloats.RemoveRange(0, _combatFloats.Count - 32);
    }
    private static Color EventColor(string kind) => kind switch
    {
        "damage" or "fall" or "injury" => new(241, 147, 126), "heal" => new(132, 220, 154),
        "status" or "skip" => new(235, 199, 116), "expire" => new(157, 184, 199), _ => Gold
    };
    private void DrawCombatFeedback()
    {
        if (DisplayBattle is not { } battle) return;
        foreach (var group in _combatFloats.Where(e => _combatVisualTime - e.Born <= 1.35).GroupBy(e => e.Target))
        {
            int index = battle.ActorIds.IndexOf(group.Key); if (index < 0) continue;
            var rect = UnitRect(battle.Units[index]);
            var visible = group.Where(e => e.Kind is "damage" or "heal").TakeLast(2).ToList();
            var changes = group.Where(e => e.Kind is not ("damage" or "heal")).DistinctBy(e => e.Text).ToArray();
            if (changes.Length > 0)
            {
                var last = changes.LastOrDefault(e => e.Kind == "fall") ?? changes[^1];
                visible.Add(last.Kind == "fall" || changes.Length == 1 ? last : last with { Text = changes.Length + " effects", Kind = "status" });
            }
            int stack = 0;
            foreach (var e in visible)
            {
                double age = _combatVisualTime - e.Born;
                float opacity = (float)Math.Clamp((1.35 - age) * 3, 0, 1);
                int y = rect.Top + 64 - stack++ * 34 - (_reducedMotion ? 0 : (int)(age * 12));
                float size = e.Kind is "damage" or "heal" ? 2 : 1;
                string label = Short(e.Text, Math.Max(6, (rect.Width - 8) / (8 * (int)size)));
                int width = label.Length * 8 * (int)size;
                Fill(new(rect.Center.X - width / 2 - 4, y - 2, width + 8, (int)(16 * size) + 4), new Color(16, 22, 27) * (.85f * opacity));
                Text(label, rect.Center.X - width / 2, y, EventColor(e.Kind) * opacity, size);
            }
        }
    }
}
