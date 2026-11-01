using Microsoft.Xna.Framework;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private long _nativeRecruit;
    private Actor[] DungeonRoster => _session.Party.Where(a => a.Life != LifeState.Dead).ToArray();
    private bool DungeonSelectionReady => _session.State.Tactics!.ActiveActor == _session.DungeonMember.Id;

    private string DungeonReadiness(Actor member, bool compact = false)
    {
        if (member.Life == LifeState.Dead) return "permanently dead";
        if (member.Life == LifeState.Downed) return compact ? $"DOWN{member.Tactics!.BleedTurns}" : $"DOWN / {member.Tactics!.BleedTurns} own turns";
        if (_session.State.Tactics!.ActiveActor == member.Id) return "READY";
        var ready = _session.State.Tactics.Timeline.Pending.FirstOrDefault(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == member.Id);
        return ready == null ? "recovering" : (compact ? "+" : "ready +") + $"{(ready.Due - _session.DungeonCombat.Clock) / 100.0:0.00}s";
    }

    private void AddDungeonPartyControl(string id, string label, Rectangle bounds, bool enabled = true,
        bool selected = false, string reason = "", string? sprite = null, string detail = "")
        => ControlCache.Add(id, label, bounds, enabled, selected, "", reason, sprite, detail, -1, null, false);

    private void BuildDungeonMemberControls()
    {
        var members = DungeonRoster;
        for (int i = 0; i < members.Length; i++)
        {
            var member = members[i];
            AddDungeonPartyControl("dungeon:member:" + member.Id, $"{i + 1}. {ClassCatalog.Get(member.Tactics!.Build!.Class).Name}",
                new(_side.X + 10, _side.Y + 10 + i * 48, _side.Width - 20, 44),
                selected: member.Id == _session.DungeonMember.Id, sprite: member.Sprite,
                detail: $"{member.Health}/{member.MaxHealth}HP F{member.Position.Floor} {DungeonReadiness(member, compact: true)}");
        }
    }

    private void BuildDungeonPartyPanelControls(Rectangle box)
    {
        var members = DungeonRoster;
        for (int i = 0; i < members.Length; i++)
        {
            var member = members[i];
            int y = box.Y + 104 + i * 76;
            AddDungeonPartyControl("dungeon:member:" + member.Id, member.Name + " / " + ClassCatalog.Get(member.Tactics!.Build!.Class).Name,
                new(box.X + 24, y, box.Width - 224, 64), selected: member.Id == _session.DungeonMember.Id,
                sprite: member.Sprite, detail: $"HP {member.Health}/{member.MaxHealth} / F{member.Position.Floor} / {DungeonReadiness(member)}");
            if (member.Life != LifeState.Downed) continue;
            var preview = _session.DungeonCombat.Preview(new(TacticalAction.Revive, Target: member.Id));
            AddDungeonPartyControl("dungeon:revive:" + member.Id, "Revive", new(box.Right - 184, y + 14, 156, 42),
                DungeonSelectionReady && preview.Valid, reason: DungeonSelectionReady ? preview.Reason : "Select the ready member first.");
        }
        AddDungeonPartyControl("dungeon:ready", "View ready member", new(box.X + 24, box.Bottom - 62, 260, 40),
            _session.State.Tactics!.ActiveActor > 0);
    }

    private void BuildDungeonRecruitControls(Rectangle box)
    {
        var preview = _session.DungeonCombat.Preview(new(TacticalAction.Recruit, Target: _nativeRecruit));
        AddDungeonPartyControl("dungeon:recruit", "Recruit / one action", new(box.X + 28, box.Y + 254, 300, 42),
            DungeonSelectionReady && preview.Valid, reason: DungeonSelectionReady ? preview.Reason : "Select the ready member first.");
    }

    private void BuildDungeonChestRecipientControls(Rectangle box)
    {
        var chest = _session.State.Tactics!.Chests.Chests.Single(c => c.Id == _nativeChest);
        var members = DungeonRoster;
        for (int i = 0; i < members.Length; i++)
        {
            var member = members[i];
            bool beside = member.Life == LifeState.Alive && member.Position.Distance(chest.Position) == 1;
            bool ready = member.Id == _session.State.Tactics.ActiveActor;
            bool selected = member.Id == _session.DungeonMember.Id;
            AddDungeonPartyControl("dungeon:chest-recipient:" + member.Id,
                member.Name + " / " + ClassCatalog.Get(member.Tactics!.Build!.Class).Name,
                new(box.X + 24, box.Y + 140 + i * 76, box.Width - 48, 64),
                beside && ready && selected, selected,
                !beside ? "This member must stand beside the chest." : !ready ? "Wait for this member's scheduled turn." : "Select this member first.",
                member.Sprite, "Opener receives the boon permanently / " + DungeonReadiness(member));
        }
    }

    private void DrawDungeonPartyPanel(Rectangle box, int x, int y)
    {
        Wrap($"View any member; only the ready member can act. Shared medicine: {_session.Count("medicine")}.", x, y, box.Width - 48, Muted);
        int dead = _session.Party.Count(a => a.Life == LifeState.Dead);
        string footer = "Revive: adjacent conscious rescuer, one medicine and one action; restores 25% HP.";
        if (dead > 0) footer += $" {dead} permanently dead.";
        Wrap(footer, x, box.Bottom - 114, box.Width - 48, Muted);
    }

    private void DrawDungeonRecruitPanel(Rectangle box, int x, int y)
    {
        var recruit = _session.State.Dungeon!.Recruits.Single(r => r.Actor == _nativeRecruit);
        var actor = _session.State.Actors.Single(a => a.Id == recruit.Actor);
        var definition = ClassCatalog.Get(recruit.Class);
        _sprites.Fit(_batch, actor.Sprite, new(x, y + 4, 96, 96), Ink);
        Text(actor.Name, x + 116, y + 4, Gold);
        Text(definition.Name + " / fixed class", x + 116, y + 32);
        Wrap(DungeonClassCards.Description(recruit.Class), x + 116, y + 60, box.Width - 180, Muted);
        Wrap("Join this expedition for one action, with no gold fee. Everyone keeps their own class, build and assigned boons. Up to four surviving members can travel together.", x, y + 130, box.Width - 48, Muted);
    }

    private void SelectDungeonMember(long memberId)
    {
        var result = _session.SelectDungeonMember(memberId);
        _notice = result.Message;
        if (!result.Success) return;
        ClosePanel(); _roagAim = "move"; _roagVow = false;
        ClearWorldSelection(); _terrainLayerDirty = true;
    }

    private bool TryOpenDungeonPerson(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var cell = new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        if (!_session.Visible.Contains(cell)) return false;
        var actor = _session.State.Actors.FirstOrDefault(a => a.Life != LifeState.Dead && a.Position == cell);
        if (actor == null) return false;
        if (actor.Recruited)
        {
            if (actor.Life == LifeState.Downed && _session.State.Tactics!.ActiveActor != actor.Id) _panel = "dungeon-party";
            else SelectDungeonMember(actor.Id);
            return true;
        }
        if (!_session.State.Dungeon!.Recruits.Any(r => r.Actor == actor.Id)) return false;
        _nativeRecruit = actor.Id; _panel = "dungeon-recruit";
        return true;
    }
}
