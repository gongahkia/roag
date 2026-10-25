using Microsoft.Xna.Framework;

namespace Vagabonds.Client;

/// <summary>Reuse unchanged immutable controls after resolving every live field.</summary>
internal sealed class WorldControlCache(List<WorldMouseControl> controls)
{
    private readonly List<WorldMouseControl> _previous = [];
    public long Hits { get; private set; }
    public long Misses { get; private set; }
    public int Retained => controls.Count + _previous.Count;
    public void Begin()
    {
        _previous.Clear(); _previous.AddRange(controls); controls.Clear();
    }
    public void Add(string id, string label, Rectangle bounds, bool enabled = true, bool selected = false,
        string tip = "", string reason = "", string? sprite = null, string detail = "", float progress = -1,
        string? badge = null, bool dimmed = false)
    {
        var previous = controls.Count < _previous.Count ? _previous[controls.Count] : null;
        if (previous != null && previous.Id == id && previous.Label == label && previous.Bounds == bounds
            && previous.Enabled == enabled && previous.Selected == selected && previous.Tip == tip
            && previous.DisabledReason == reason && previous.Sprite == sprite && previous.Detail == detail
            && previous.Progress == progress && previous.BadgeSprite == badge && previous.Dimmed == dimmed)
        { controls.Add(previous); Hits++; }
        else
        { controls.Add(new(id, label, bounds, enabled, selected, tip, reason, sprite, detail, progress, badge, dimmed)); Misses++; }
    }
}
