using Microsoft.Xna.Framework;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private int _specialDungeonPage;

    private SpecialDungeonReport? SelectedSpecialDungeon()
    {
        var reports = _session.KnownSpecialDungeons();
        _specialDungeonPage = Math.Clamp(_specialDungeonPage, 0, Math.Max(0, reports.Count - 1));
        return reports.ElementAtOrDefault(_specialDungeonPage);
    }

    private string SpecialDungeonStatus(SpecialDungeonReport report) => report.ExpiredAt != 0 ? "Expiry settled; persistent remains"
        : report.ActivatedAt == null ? "Inactive; exploration does not activate it"
        : $"ACTIVE / {Math.Max(0, report.ExpiresAt - _session.DungeonCombat.Clock) / (double)_session.State.Tactics!.Tuning.UnitsPerSecond:0.0}s remaining"
            + (report.PursuerWarningAt <= _session.DungeonCombat.Clock ? " / pursuer pressure window" : "");

    private void BuildSpecialDungeonControls(Rectangle box)
    {
        void Add(string id, string label, Rectangle bounds, bool enabled = true, string reason = "")
            => ControlCache.Add("dungeon:special-" + id, label, bounds, enabled, false, "", reason, null, "", -1, null, false);
        var report = SelectedSpecialDungeon();
        if (_panel == "dungeon-special-confirm")
        {
            Add("cancel", "Keep the seal inactive", new(box.X + 24, box.Bottom - 100, 300, 40));
            if (report != null)
            {
                var preview = _session.PreviewDungeonAim(new("activate_special", Target: report.Complex, Choice: 1));
                Add("activate", "Activate the announced deadline", new(box.X + 340, box.Bottom - 100, box.Width - 364, 40), preview.Valid, preview.Reason);
            }
            return;
        }
        if (report?.ActivatedAt == null && report != null)
        {
            var preview = _session.PreviewDungeonAim(new("activate_special", Target: report.Complex, Choice: 1));
            Add("review", "Review activation", new(box.X + 24, box.Bottom - 106, 260, 40), preview.Valid, preview.Reason);
        }
        Add("prev", "Previous", new(box.X + 24, box.Bottom - 54, 130, 34), _specialDungeonPage > 0);
        Add("next", "Next", new(box.Right - 154, box.Bottom - 54, 130, 34), _specialDungeonPage + 1 < _session.KnownSpecialDungeons().Count);
    }

    private void DrawSpecialDungeons(Rectangle box, int x, int y)
    {
        var report = SelectedSpecialDungeon();
        if (report == null)
        { Wrap("No unstable labyrinth is recorded. Rumours and observed entrance inscriptions identify them. Entering a labyrinth is safe from its deadline until you deliberately activate its seal.", x, y, box.Width - 48, Muted); return; }
        Text(report.Name, x, y, Gold); y += 30;
        y += Wrap(SpecialDungeonStatus(report), x, y, box.Width - 48, Muted);
        y += Wrap(report.Warning, x, y + 8, box.Width - 48);
        if (_panel == "dungeon-special-confirm")
            Wrap("Confirming starts the announced clock immediately. Keep inactive is the default. Neither inspecting nor cancelling spends time.", x, y + 12, box.Width - 48, Gold);
        else if (report.ActivatedAt == null)
            Wrap($"Activation seal: ({report.Activation.X}, {report.Activation.Y}), depth {report.Activation.Floor}. Stand on or beside it with the ready member, then review activation.", x, y + 12, box.Width - 48, Muted);
    }

    private void ActivateSpecialDungeon()
    {
        if (SelectedSpecialDungeon() is not { } report) return;
        _notice = _session.SubmitRoag(new("activate_special", Target: report.Complex, Choice: 1)).Message;
        CollectDungeonFeedback(); _panel = "dungeon-specials";
    }
}
