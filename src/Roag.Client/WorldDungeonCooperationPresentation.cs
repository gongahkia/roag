using Microsoft.Xna.Framework;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private long _nativeCooperationHelper;

    private void BuildDungeonCooperationControls(Rectangle box)
    {
        var helpers = DungeonRoster.Where(a => a.Id != _session.DungeonMember.Id).ToArray();
        int width = (box.Width - 72) / 2;
        for (int i = 0; i < helpers.Length; i++)
        {
            var helper = helpers[i];
            foreach (var option in _session.DungeonCombat.CooperativeOptions(helper.Id, _session.DungeonMember.Id))
            {
                bool ready = DungeonSelectionReady;
                AddDungeonPartyControl($"dungeon:cooperate-choice:{helper.Id}:{option.Choice}", option.Name + " / " + helper.Name,
                    new(box.X + 24 + (option.Choice - 1) * (width + 24), box.Y + 240 + i * 80, width, 72),
                    ready && option.Enabled, reason: ready ? option.Reason : "Select the ready member first.", sprite: helper.Sprite,
                    detail: $"You +{option.Recovery / 100.0:0.00}s / helper +{option.HelperRecovery / 100.0:0.00}s");
            }
        }
    }

    private void ChooseDungeonCooperation(long helper, int choice)
    {
        if (!DungeonSelectionReady) { _notice = "Select the ready member first."; return; }
        var option = _session.DungeonCombat.CooperativeOptions(helper).FirstOrDefault(o => o.Choice == choice);
        if (option == null || !option.Enabled) { _notice = option?.Reason ?? "Choose an available cooperative ability."; return; }
        if (choice == 2) { CommitRoag(new("cooperate", Target: helper, Choice: choice)); ClosePanel(); return; }
        _nativeCooperationHelper = helper; ClosePanel(); _roagAim = "cooperate";
        _notice = "Twin strike with " + _session.ActorById(helper).Name + ". Aim at a visible enemy; both members pay recovery.";
    }

    private void DrawDungeonCooperationPanel(Rectangle box, int x, int y)
    {
        var tuning = _session.State.Tactics!.Cooperation;
        y += Wrap($"Helpers must be conscious, unstunned and within {tuning.PartnerRange} clear tiles. Recovery is allowed; assisting extends their existing next turn. Each helper may assist once before their own next turn.", x, y, box.Width - 48, Muted);
        Wrap($"Twin strike: aim using your class's basic range; combines power against one enemy, applying armor once. Shared aid: heals, cleanses and calms living members within {tuning.AidRadius} clear tiles. Both choices spend recovery; no mana is used.", x, y + 8, box.Width - 48, Muted);
        if (DungeonRoster.Length < 2) Text("Recruit another adventurer to cooperate.", x, box.Y + 190, Gold);
    }
}
