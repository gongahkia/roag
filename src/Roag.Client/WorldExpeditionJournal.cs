using Microsoft.Xna.Framework;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private int _expeditionPage;

    private JournalRow[] ExpeditionJournalRows(int width)
    {
        var entries = new List<(string Text, string Sprite)>();
        if (_session.CampaignFinale != null) entries.Add(("CAMPAIGN OBJECTIVE / " + _session.FinalObjective, "banner"));
        foreach (var site in _session.GeneratedBossSites)
        {
            string name = CampaignBossCatalog.Definition(site.Kind, false, 0).Name;
            entries.Add(($"Guardian report: {name}, {_session.Plan.Sites[site.SiteId].Name}. Surface entrance ({site.Entrance.X}, {site.Entrance.Y}); the chamber is on depth 1. Reports locate a destination without revealing its terrain or current occupants.", "banner"));
        }
        var entrances = _session.KnownDungeonEntrances();
        entries.Add((entrances.Count == 0 ? "No labyrinth entrances recorded. Ask residents for rumours, or find stairs while exploring."
            : "REPORTED ENTRANCES / Reports mark locations, not explored terrain. Travel there with ordinary paid movement.", "banner"));
        var origin = _session.DungeonMember.Position with { Floor = 0 };
        foreach (var entrance in entrances)
            entries.Add(($"{entrance.Name}: ({entrance.Position.X}, {entrance.Position.Y}), {entrance.Position.Distance(origin)} surface tiles {WorldSession.Direction(origin, entrance.Position)}. Physical stairs lead underground; return by the same stairs.", "banner"));
        foreach (var special in _session.KnownSpecialDungeons())
            entries.Add((special.Name + " / " + SpecialDungeonStatus(special) + ". " + special.Warning + " Review it under Menu / Unstable labyrinths.", "banner"));
        entries.AddRange(_session.State.Tasks.Where(t => !t.Complete).Select(t => ("Request: " + t.Description, "banner")));
        entries.Add(("EXPEDITION HISTORY / Newest first", "banner"));
        entries.AddRange(_session.State.Journal.AsEnumerable().Reverse().Select(t => (t, JournalSprite(t))));
        int advance = Math.Max(8, _uiFonts?.Advance ?? 8);
        var rows = new List<JournalRow>();
        foreach (var entry in entries)
        {
            var lines = FeedbackLines(entry.Text, Math.Max(8, width / advance * 8));
            for (int i = 0; i < Math.Max(2, lines.Length); i++) rows.Add(new(i < lines.Length ? lines[i] : "", entry.Sprite, i == 0));
            rows.Add(new("", entry.Sprite, false));
        }
        return rows.ToArray();
    }

    private int ExpeditionVisibleRows(Rectangle box) => Math.Max(1, (box.Height - 172) / Math.Max(24, (_uiFonts?.LineHeight ?? 20) + 4));

    private void BuildExpeditionJournalControls(Rectangle box)
    {
        int rows = ExpeditionJournalRows(box.Width - 108).Length, count = ExpeditionVisibleRows(box);
        _expeditionPage = Math.Clamp(_expeditionPage, 0, Math.Max(0, (rows - 1) / count));
        void Add(string id, string text, int x, bool enabled) => ControlCache.Add("dungeon:journal-" + id, text,
            new(x, box.Bottom - 54, 130, 34), enabled, false, "", "", null, "", -1, null, false);
        Add("prev", "Previous", box.X + 24, _expeditionPage > 0);
        Add("next", "Next", box.Right - 154, (_expeditionPage + 1) * count < rows);
    }

    private void DrawExpeditionJournal(Rectangle box, int x, int y)
    {
        var rows = ExpeditionJournalRows(box.Width - 108); int count = ExpeditionVisibleRows(box);
        Text($"Reading is free / page {_expeditionPage + 1}/{Math.Max(1, (rows.Length + count - 1) / count)}", x, y, Muted);
        y += 32;
        int start = _expeditionPage * count, height = Math.Max(24, (_uiFonts?.LineHeight ?? 20) + 4);
        for (int i = 0; i < Math.Min(count, rows.Length - start); i++)
        {
            var row = rows[start + i];
            if (row.Start || i == 0) _sprites.Fit(_batch, row.Sprite, new(x + 4, y, 32, 32), Gold);
            Text(row.Text, x + 52, y + 3); y += height;
        }
    }
}
