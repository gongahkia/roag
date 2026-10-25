using Microsoft.Xna.Framework;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;

namespace Vagabonds.Client;

internal sealed partial class WorldGame
{
    private string _selectedTalent = "";
    private int _talentPage;

    private int ReadyDevelopmentCount(Actor actor) => _session.ProgressionChoices(actor.Id)
        .Where(t => _session.ProgressionChoiceReason(actor.Id, t.Id).Length == 0).Select(t => t.Level).Distinct().Count();

    private void AddProgressionControls(Rectangle box)
    {
        void Add(string id, string label, Rectangle bounds, bool enabled = true, bool selected = false,
            string? sprite = null, string detail = "", string tip = "", string reason = "")
            => ControlCache.Add(id, label, bounds, enabled, selected, tip, reason, sprite, detail);
        int x = box.X + 20, y = box.Y + 64, width = box.Width - 40;
        var party = _session.Party.ToArray(); var member = SelectedMember();
        int card = (width - (party.Length - 1) * 8) / party.Length;
        for (int i = 0; i < party.Length; i++)
            Add("party:" + i, party[i].Name, new(x + i * (card + 8), y, card, 64), selected: party[i].Id == member.Id,
                sprite: party[i].Sprite, detail: "L" + party[i].Level + " / Ready: " + ReadyDevelopmentCount(party[i]));
        var choices = _session.ProgressionChoices(member.Id);
        if (!choices.Any(t => t.Id == _selectedTalent))
            _selectedTalent = choices.FirstOrDefault(t => _session.ProgressionChoiceReason(member.Id, t.Id).Length == 0)?.Id ?? choices.FirstOrDefault()?.Id ?? "";
        _talentPage = Math.Clamp(_talentPage, 0, Math.Max(0, (choices.Count - 1) / 4));
        for (int i = 0; i < Math.Min(4, choices.Count - _talentPage * 4); i++)
        {
            var talent = choices[i + _talentPage * 4];
            string reason = _session.ProgressionChoiceReason(member.Id, talent.Id);
            string state = member.Talents.Contains(talent.Id) ? "Learned" : reason.Length == 0 ? "Available" : "Locked";
            Add("talent:" + talent.Id, talent.Name, new(x + i % 2 * ((width - 8) / 2 + 8), y + 136 + i / 2 * 68, (width - 8) / 2, 60),
                selected: talent.Id == _selectedTalent, sprite: talent.Sprite, detail: "Level " + talent.Level + " / " + state,
                tip: talent.Description + (reason.Length > 0 ? " " + reason : " Choose one development at this level."));
        }
        if (choices.Count > 4)
        {
            Add("talentpage:-1", "Prev", new(box.Right - 440, box.Y + 12, 64, 34), _talentPage > 0);
            Add("talentpage:1", "Next", new(box.Right - 368, box.Y + 12, 64, 34), (_talentPage + 1) * 4 < choices.Count);
        }
        string blocked = _session.ProgressionChoiceReason(member.Id, _selectedTalent);
        Add("learn-talent", "Learn permanently", new(x, box.Bottom - 88, 208, 36), blocked.Length == 0,
            tip: "Commit the selected development. One choice per milestone; this stays with the person.", reason: blocked);
        Add("panel:inventory", "Equipment", new(box.Right - 284, box.Y + 12, 164, 34));
    }

    private void DrawProgression(Rectangle box)
    {
        int x = box.X + 20, y = box.Y + 64, width = box.Width - 40;
        var actor = SelectedMember(); var innate = _session.Content.Get<TalentDefinition>(actor.CombatTrait);
        _sprites.Fit(_batch, innate.Sprite!, new(x, y + 76, 40, 40), Gold);
        Text(innate.Name + " / personal trait", x + 52, y + 76, Gold);
        Wrap(innate.Description, x + 52, y + 97, width - 60, Muted);
        if (_session.ProgressionChoices(actor.Id).FirstOrDefault(t => t.Id == _selectedTalent) is { } choice)
        {
            Text(choice.Name + " / " + _session.Class(actor).Name, x, y + 278, Gold);
            Wrap(choice.Description, x, y + 302, width);
            string reason = _session.ProgressionChoiceReason(actor.Id, choice.Id);
            Text(Short(reason.Length == 0 ? "One permanent choice for level " + choice.Level + "." : reason, (width - 228) / 8),
                x + 228, box.Bottom - 76, Muted);
        }
        Text("Choose one development at levels 2 and 4. Every dungeon remains accessible.", x, box.Bottom - 42, Muted);
    }
}
