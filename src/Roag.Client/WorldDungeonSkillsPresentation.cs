using Microsoft.Xna.Framework;
using Roag.Core.Content;
using Roag.Core.Expeditions;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private string _nativeSkillBoon = "";
    private int _nativeSkillPage;

    private void OpenDungeonSkills()
    { _nativeSkillBoon = ""; _nativeSkillPage = 0; _panel = "dungeon-skills"; }

    private RetainedSkillOption? SelectedNativeSkill => _session.DungeonCombat.RetainedSkills(_session.DungeonMember.Id)
        .FirstOrDefault(s => s.Boon == _nativeSkillBoon);

    private void BuildDungeonSkillControls(Rectangle box)
    {
        int width = (box.Width - 72) / 2;
        Rectangle Card(int i) => new(box.X + 24 + i % 2 * (width + 24), box.Y + 210 + i / 2 * 72, width, 64);
        var skills = _session.DungeonCombat.RetainedSkills(_session.DungeonMember.Id);
        if (SelectedNativeSkill is not { } selected)
        {
            var entries = RetainedBoons.All.Where(e => e.Active).ToArray();
            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i]; int index = Array.FindIndex(entries, e => e.Id == skill.Boon);
                AddDungeonPartyControl("dungeon:skill-open:" + index, skill.Name, Card(i), sprite: skill.Sprite, detail: "Choose targets / review paid recovery");
            }
            return;
        }
        AddDungeonPartyControl("dungeon:skills", "All unlocked skills", new(box.X + 24, box.Y + 150, 220, 34));
        var definition = _session.Content.Get<AbilityDefinition>(selected.Ability);
        bool group = definition.Target is TargetKind.AllAllies or TargetKind.AllEnemies;
        Actor[] targets = group ? [_session.DungeonMember] : definition.Target == TargetKind.Ally
            ? DungeonRoster : _session.State.Actors.Where(a => a.Life != LifeState.Dead
                && (_session.IsHostile(a) || _session.CanHuntNativeWildlife(a)) && _session.Visible.Contains(a.Position))
                .OrderBy(a => a.Position.Distance(_session.DungeonMember.Position)).ThenBy(a => a.Id).ToArray();
        _nativeSkillPage = Math.Clamp(_nativeSkillPage, 0, Math.Max(0, (targets.Length - 1) / 6));
        for (int i = 0; i < 6 && _nativeSkillPage * 6 + i < targets.Length; i++)
        {
            var target = targets[_nativeSkillPage * 6 + i]; long id = group ? 0 : target.Id;
            var preview = DungeonSelectionReady ? _session.DungeonCombat.Preview(new(TacticalAction.RetainedSkill, Item: selected.Boon, Target: id))
                : new TacticalPreview(false, "Select the ready member first.", 0, []);
            string name = !_session.IsHostile(target) && _session.CanHuntNativeWildlife(target) ? "Hunt " + target.Name : target.Name;
            AddDungeonPartyControl("dungeon:skill-cast:" + id, group ? selected.Name + " / confirm" : name, Card(i), preview.Valid,
                reason: preview.Reason, sprite: group ? selected.Sprite : target.Sprite,
                detail: preview.Valid ? $"{preview.Recovery / 100.0:0.00}s recovery / {preview.Cells.Count} targets" + (preview.Damage is { } damage ? $" / power {damage}" : "")
                    : group ? preview.Reason : target.Life == LifeState.Alive ? $"HP {target.Health}/{target.MaxHealth}" : target.Life.ToString());
        }
        if (targets.Length > 6)
        {
            AddDungeonPartyControl("dungeon:skill-page:-1", "Previous", new(box.X + 24, box.Bottom - 54, 150, 34), _nativeSkillPage > 0);
            AddDungeonPartyControl("dungeon:skill-page:1", "Next", new(box.Right - 174, box.Bottom - 54, 150, 34), (_nativeSkillPage + 1) * 6 < targets.Length);
        }
    }

    private void DrawDungeonSkillsPanel(Rectangle box, int x, int y)
    {
        Text(_session.DungeonMember.Name + " / " + DungeonReadiness(_session.DungeonMember), x, y, Gold); y += 30;
        if (SelectedNativeSkill is { } skill)
        {
            Wrap(skill.Name + ": " + skill.Description, x, y, box.Width - 48, Muted);
        }
        else
            Wrap(_session.DungeonCombat.RetainedSkills(_session.DungeonMember.Id).Count == 0
                ? "Find class-appropriate skill boons in chests to unlock additional actions. Each member owns their own skills."
                : "Choose one of this member's unlocked skills, then review its targets and paid recovery.", x, y, box.Width - 48, Muted);
        Wrap("Existing class moves remain available. Only the ready member can cast; targets must be physically visible and in range. Browsing is free.", x, box.Bottom - 92, box.Width - 48, Muted);
    }

    private void ActivateDungeonSkillControl(string[] parts)
    {
        switch (parts[1])
        {
            case "skills": OpenDungeonSkills(); break;
            case "skill-open":
                _nativeSkillBoon = RetainedBoons.All.Where(e => e.Active).ElementAt(int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)).Id;
                _nativeSkillPage = 0; break;
            case "skill-page": _nativeSkillPage += int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture); break;
            case "skill-cast":
                if (SelectedNativeSkill is not { } selected) return;
                var result = _session.SubmitDungeon(new(TacticalAction.RetainedSkill, Item: selected.Boon,
                    Target: long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)));
                _notice = result.Message;
                if (result.Success) { StopTravel(); _roagAim = "move"; CollectDungeonFeedback(); ClosePanel(); }
                break;
        }
    }
}
