using Microsoft.Xna.Framework;
using Roag.Core.Content;
using Roag.Core.World;
using Roag.Graphics;

namespace Roag.Client;

internal sealed record JournalRow(string Text, string Sprite, bool Start);

internal sealed partial class WorldGame
{
    private static Rectangle ControlSpriteBox(WorldMouseControl control)
    {
        int size = Math.Min(56, control.Bounds.Height - 8);
        return new(control.Bounds.X + 6, control.Bounds.Center.Y - size / 2, size, size);
    }

    private void DrawControlContents(WorldMouseControl control)
    {
        var r = control.Bounds;
        Color ink = !control.Enabled ? Muted * .65f : control.Selected ? Gold : control.Dimmed ? Muted : Ink;
        if (control.Sprite is not { } sprite)
        {
            string label = Short(control.Label, Math.Max(1, (r.Width - 14) / 8));
            Text(label, (int)(r.Center.X - SpriteLibrary.Measure(label) / 2), r.Center.Y - 6, ink);
            return;
        }
        var icon = ControlSpriteBox(control);
        _sprites.Fit(_batch, sprite, icon, control.Dimmed ? Muted * .6f : Ink);
        int tx = icon.Right + 10, right = r.Right - (control.BadgeSprite == null ? 8 : 42);
        int columns = Math.Max(1, (right - tx) / 8);
        Text(Short(control.Label, columns), tx, r.Center.Y - 14, ink);
        Text(Short(control.Detail, columns), tx, r.Center.Y + 1, Muted);
        if (control.BadgeSprite is { } badge) _sprites.Fit(_batch, badge, new(r.Right - 36, r.Y + 4, 28, 28), Ink);
        if (control.Progress >= 0)
        {
            int length = Math.Max(1, right - tx);
            Fill(new(tx, r.Bottom - 6, length, 3), new(15, 24, 27));
            Fill(new(tx, r.Bottom - 6, (int)(length * Math.Clamp(control.Progress, 0, 1)), 3), new(119, 182, 150));
        }
    }

    private void DrawInventorySummary(Rectangle box)
    {
        int x = box.X + 20, y = box.Y + 64, width = box.Width - 40;
        var member = SelectedMember();
        string[] stacks = ["food", "medicine", "gold", "wood", "stone"];
        int span = Math.Min(160, width / stacks.Length);
        for (int i = 0; i < stacks.Length; i++)
        {
            var item = _session.Content.All<ItemDefinition>().First(d => d.Stack == stacks[i]);
            _sprites.Fit(_batch, item.Sprite!, new(x + i * span, y + 80, 24, 24), Gold);
            Text(item.Name + " " + _session.Count(item.Stack), x + i * span + 30, y + 87, Gold);
        }
        Text($"{_session.Class(member).Name} / {member.Life} / Power {member.Power} / Armour {member.Armor} / Speed {member.Speed}", x, y + 114);
        Text($"Hunger {member.Body.Hunger:0}   Fatigue {member.Body.Fatigue:0}   Wounds {member.Body.Wounds}   Stress {member.Stress}", x, y + 136, Muted);
    }

    private string WorkName(WorkOrder order) => order.Kind == WorkKind.Discovery
        ? _session.Content.Get<DiscoveryDefinition>(order.Definition).Situation!.Approaches.Single(a => a.Action == order.Method).Name : order.Kind.ToString();
    private string WorkSprite(WorkOrder order)
    {
        if (order.Kind == WorkKind.Discovery) return _session.Content.Get<DiscoveryDefinition>(order.Definition).Situation!.Approaches.Single(a => a.Action == order.Method).Sprite;
        if (order.Kind == WorkKind.Camp) return _session.Content.Get<CampDefinition>(order.Definition).Sprite!;
        if (order.Kind == WorkKind.Harvest) return _session.Content.Get<ResourceDefinition>(order.Definition).Sprite!;
        if (order.Kind == WorkKind.Forage) return _session.Content.Get<ItemDefinition>("item.food").Sprite!;
        if (order.Kind == WorkKind.Craft && _session.Content.Entries.TryGetValue(order.Definition, out var definition) && definition is RecipeDefinition { Output: { } output })
            return _session.Content.Get<ItemDefinition>(output).Sprite!;
        return order.Kind == WorkKind.Dig && _session.ObjectAt(order.Target) is { Removable: true } prop ? prop.Sprite
            : Materials.Sprite(order.Kind == WorkKind.Build ? order.After : order.Before, _session.Plan.BiomeAt(order.Target.X, order.Target.Y));
    }

    // Journal strings predate illustrated panels. Resolve only an unambiguous named
    // subject at the start of an entry; never guess a portrait from a passing mention.
    private string JournalSprite(string text)
    {
        if (text.StartsWith("Day ", StringComparison.Ordinal) && text.IndexOf(": ", StringComparison.Ordinal) is var colon && colon >= 0) text = text[(colon + 2)..];
        var actors = _session.State.Actors.Where(a => text.StartsWith(a.Name + " ", StringComparison.Ordinal) || text.StartsWith(a.Name + ":", StringComparison.Ordinal)).Take(2).ToArray();
        if (actors.Length == 1) return actors[0].Sprite;
        var discovery = _session.State.Discoveries.FirstOrDefault(d => text.StartsWith(d.Name + ":", StringComparison.Ordinal));
        return discovery == null ? "banner" : _session.Content.Get<DiscoveryDefinition>(discovery.Definition).Sprite!;
    }

    private JournalRow[] JournalRows(int textWidth)
    {
        var rows = new List<JournalRow>();
        var entries = _session.State.Tasks.Where(t => !t.Complete).Select(t => (Text: "Request: " + t.Description, Sprite: "banner"))
            .Concat(_session.State.Journal.AsEnumerable().Reverse().Select(t => (Text: t, Sprite: JournalSprite(t))));
        foreach (var entry in entries)
        {
            var lines = FeedbackLines(entry.Text, textWidth);
            // At least two text rows plus a gap give each 32px portrait breathing room.
            for (int i = 0; i < Math.Max(2, lines.Length); i++) rows.Add(new(i < lines.Length ? lines[i] : "", entry.Sprite, i == 0));
            rows.Add(new("", entry.Sprite, false));
        }
        return rows.ToArray();
    }
    private static int JournalVisibleRows(Rectangle box) => Math.Max(6, (box.Height - 64 - 208 - 100) / 19);

    private void DrawJournal(Rectangle box)
    {
        int x = box.X + 20, y = box.Y + 64, width = box.Width - 40;
        var actor = _session.State.Battle?.ActorIds.Select(id => _session.State.Actors.Single(a => a.Id == id)).FirstOrDefault(a => _session.Content.Entries[a.Archetype] is BossDefinition) ?? _session.FinalBoss;
        var boss = _session.Content.Get<BossDefinition>(actor.Archetype);
        Fill(new(x, y, width, 112), new(38, 34, 37));
        _sprites.Fit(_batch, actor.Sprite, new(x + 12, y + 14, 80, 80), new(224, 151, 132));
        Text(Short((actor.Id == _session.FinalBoss.Id ? "FINAL THREAT / " : "CURRENT FOE / ") + actor.Name, (width - 112) / 8), x + 104, y + 10, Gold);
        string site = actor.Id == _session.FinalBoss.Id ? _session.Plan.Sites[_session.State.Endgame.SiteId].Name : "Encounter";
        Text($"{site} ({actor.Position.X}, {actor.Position.Y}) / depth {actor.Position.Floor}" + (_session.State.Endgame.Victory ? " / defeated" : " / accessible now"), x + 104, y + 32, Muted);
        Wrap(boss.Tactics + " " + boss.Weakness, x + 104, y + 54, width - 120);
        y += 120;
        Fill(new(x, y, width, 64), new(29, 40, 42));
        var patron = _session.State.Actors.FirstOrDefault(a => a.Id == _session.State.Arc.PatronId);
        _sprites.Fit(_batch, patron?.Sprite ?? "banner", new(x + 10, y + 10, 44, 44), Ink);
        Text("SIDE STORY / " + _session.State.Arc.Status, x + 68, y + 8, Gold);
        Wrap(_session.State.Arc.Objective, x + 68, y + 28, width - 80, Muted);
        Text("EXPEDITION HISTORY", x, y + 72, Gold);
        y += 88;
        var rows = JournalRows(width - 64); _journalOffset = Math.Clamp(_journalOffset, 0, Math.Max(0, rows.Length - JournalVisibleRows(box)));
        for (int i = 0; i < Math.Min(JournalVisibleRows(box), rows.Length - _journalOffset); i++)
        {
            var row = rows[_journalOffset + i];
            if (row.Start || i == 0)
            {
                Fill(new(x, y, width, 1), new(46, 57, 61));
                _sprites.Fit(_batch, row.Sprite, new(x + 8, y + 3, 32, 32), Ink);
            }
            Text(row.Text, x + 56, y + 3); y += 19;
        }
    }
}
