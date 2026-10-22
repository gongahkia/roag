using Microsoft.Xna.Framework;
using Vagabonds.Core.World;

namespace Vagabonds.Client;

internal sealed partial class WorldGame
{
    private sealed record CombatFloat(long Target, string Text, string Kind, double Born);
    private readonly List<CombatFloat> _combatFloats = [];
    private WorldSession? _feedbackSession;
    private long _seenFeedback;
    private double _presentationTime;
    private void CollectCombatFeedback()
    {
        if (!ReferenceEquals(_feedbackSession, _session))
        {
            _feedbackSession = _session; _seenFeedback = _session.State.NextCombatEvent - 1; _combatFloats.Clear(); return;
        }
        foreach (var e in _session.State.CombatEvents.Where(e => e.Sequence > _seenFeedback))
        {
            if (e.Kind == "ability" && e.Target != 0 && e.Label.StartsWith("Assists ", StringComparison.Ordinal)) _combatFloats.Add(new(e.Target, "Assists", "status", _presentationTime));
            if (e.Target != 0 && e.Kind is "damage" or "heal" or "status" or "expire" or "fall" or "injury")
                _combatFloats.Add(new(e.Target, e.Kind == "damage" ? "-" + e.Amount : e.Kind == "heal" ? "+" + e.Amount : e.Label, e.Kind, _presentationTime));
            if (e.Kind == "reward" && !_session.State.Endgame.Victory) { _panel = "spoils"; StopTravel(); }
        }
        _seenFeedback = _session.State.NextCombatEvent - 1;
        _combatFloats.RemoveAll(e => _presentationTime - e.Born > 1.35);
        if (_combatFloats.Count > 32) _combatFloats.RemoveRange(0, _combatFloats.Count - 32);
    }
    private static Color EventColor(string kind) => kind switch
    {
        "damage" or "fall" or "injury" => new(241, 147, 126), "heal" => new(132, 220, 154),
        "status" or "skip" => new(235, 199, 116), "expire" => new(157, 184, 199), _ => Gold
    };
    private void DrawCombatFeedback()
    {
        if (_session.State.Battle is not { } battle) return;
        var occupied = new Dictionary<long, int>();
        foreach (var e in _combatFloats.Where(e => _presentationTime - e.Born <= 1.35))
        {
            int index = battle.ActorIds.IndexOf(e.Target); if (index < 0) continue;
            var rect = UnitRect(battle.Units[index]); double age = _presentationTime - e.Born;
            int stack = occupied.GetValueOrDefault(e.Target); occupied[e.Target] = stack + 1;
            float opacity = (float)Math.Clamp((1.35 - age) * 2, 0, 1);
            int y = Math.Max(_map.Top + 56, rect.Top - 10 - stack * 17 - (int)(age * 20));
            Text(e.Text, rect.Center.X - e.Text.Length * 4, y, EventColor(e.Kind) * opacity);
            if (age < .4 && e.Kind is "damage" or "heal")
                _sprites.Fit(_batch, e.Kind == "heal" ? "heart" : "slash", new(rect.Right - 30, rect.Top + 8, 22, 22), EventColor(e.Kind) * opacity);
        }
        int row = _map.Bottom - 90;
        foreach (var e in _session.State.CombatEvents.Where(e => e.Detail.Length > 0).TakeLast(3))
        { Text(Short(e.Detail, (_map.Width - 40) / 8), _map.X + 20, row, EventColor(e.Kind)); row += 24; }
    }
}
