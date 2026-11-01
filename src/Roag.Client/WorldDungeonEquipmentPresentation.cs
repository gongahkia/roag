using Microsoft.Xna.Framework;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private int _nativeGearPage;
    private string _nativeGearItem = "", _nativeGearSlot = "";
    private long _nativeGearMember;
    private static readonly string[] DungeonGearSlots = ["weapon", "armor", "charm", "tool"];

    private ItemDefinition[] DungeonPackGear => _session.CarriedEquipment()
        .Where(i => _session.Count(i.Stack) > 0).OrderBy(i => i.Slot, StringComparer.Ordinal)
        .ThenBy(i => i.Name, StringComparer.Ordinal).ThenBy(i => i.Id, StringComparer.Ordinal).ToArray();

    private void BuildDungeonGearControls(Rectangle box)
    {
        var member = _session.DungeonMember;
        int width = (box.Width - 72) / 2, right = box.X + 48 + width;
        for (int i = 0; i < DungeonGearSlots.Length; i++)
        {
            string slot = DungeonGearSlots[i];
            var gear = member.Equipment.GetValueOrDefault(slot);
            var item = gear == null ? null : _session.Item(gear.Item);
            AddDungeonPartyControl("dungeon:gear-slot:" + slot, item?.Name ?? slot + " / empty",
                new(box.X + 24, box.Y + 124 + i * 72, width, 64), item != null,
                sprite: item?.Sprite, detail: slot + " / inspect to remove");
        }
        var pack = DungeonPackGear;
        _nativeGearPage = Math.Clamp(_nativeGearPage, 0, Math.Max(0, (pack.Length - 1) / 3));
        foreach (var (item, index) in pack.Skip(_nativeGearPage * 3).Take(3).Select((item, index) => (item, index)))
            AddDungeonPartyControl("dungeon:gear-item:" + item.Id, item.Name,
                new(right, box.Y + 124 + index * 96, width, 84), sprite: item.Sprite,
                detail: $"{item.Slot} / pack {_session.Count(item.Stack)} / compare");
        AddDungeonPartyControl("dungeon:gear-prev", "Previous", new(right, box.Bottom - 54, 130, 34), _nativeGearPage > 0);
        AddDungeonPartyControl("dungeon:gear-next", "Next", new(box.Right - 154, box.Bottom - 54, 130, 34), (_nativeGearPage + 1) * 3 < pack.Length);
    }

    private TacticalIntent DungeonGearIntent => new(_nativeGearSlot.Length == 0 ? TacticalAction.Equip : TacticalAction.Unequip,
        _session.DungeonMember.Tactics!.Facing, Target: _nativeGearMember, Item: _nativeGearSlot.Length == 0 ? _nativeGearItem : _nativeGearSlot);

    private void OpenDungeonGearDetail(string item, string slot = "")
    {
        _nativeGearItem = item; _nativeGearSlot = slot; _nativeGearMember = _session.DungeonMember.Id;
        _panel = "dungeon-gear-detail";
    }

    private void BuildDungeonGearDetailControls(Rectangle box)
    {
        var preview = _session.DungeonCombat.Preview(DungeonGearIntent);
        bool ready = DungeonSelectionReady && _nativeGearMember == _session.DungeonMember.Id;
        AddDungeonPartyControl("dungeon:gear-confirm", (_nativeGearSlot.Length == 0 ? "Equip" : "Remove") + " / one action",
            new(box.X + 24, box.Bottom - 62, 280, 40), ready && preview.Valid,
            reason: ready ? preview.Reason : "View the ready member to change their equipment.");
        AddDungeonPartyControl("dungeon:gear", "Back to pack", new(box.X + 324, box.Bottom - 62, 240, 40));
    }

    private void ConfirmDungeonGear()
    {
        if (_panel != "dungeon-gear-detail" || _nativeGearMember != _session.DungeonMember.Id) return;
        var result = _session.SubmitDungeon(DungeonGearIntent); _notice = result.Message;
        if (!result.Success) return;
        StopTravel(); _roagAim = "move"; _terrainLayerDirty = true; CollectDungeonFeedback(); ClosePanel();
    }

    private void DrawDungeonGearPanel(Rectangle box, int x, int y)
    {
        var member = _session.DungeonMember;
        Text(member.Name + " / " + DungeonReadiness(member), x, y, Gold);
        int width = (box.Width - 72) / 2;
        Text("Equipped / this member", x, box.Y + 100, Muted);
        Text("Shared pack / " + DungeonPackGear.Length + " kinds", box.X + 48 + width, box.Y + 100, Muted);
        if (DungeonPackGear.Length == 0) Wrap("No unequipped gear. Find equipment in the world.", box.X + 48 + width, box.Y + 144, width, Muted);
        Text($"HP {member.Health}/{member.MaxHealth} / power {member.Power} / armor {member.Armor} / speed {member.Speed}", x, box.Bottom - 82);
        Text("Browsing is free. Equipping or removing costs one action.", x, box.Bottom - 28, Muted);
    }

    private void DrawDungeonGearDetail(Rectangle box, int x, int y)
    {
        var member = _session.DungeonMember; var item = _session.Item(_nativeGearItem);
        _sprites.Fit(_batch, item.Sprite!, new(x, y, 72, 72), Gold);
        Text(item.Name, x + 92, y + 4, Gold);
        Text(item.Slot + (_nativeGearSlot.Length == 0 ? $" / shared pack {_session.Count(item.Stack)}" : " / worn by " + member.Name), x + 92, y + 30);
        Text($"HP +{item.Health} / power +{item.Power} / armor +{item.Armor} / speed +{item.Speed}", x, y + 92);
        var comparison = _session.CompareGear(member, item.Id);
        string Signed(int value) => value.ToString("+0;-0;0", System.Globalization.CultureInfo.InvariantCulture);
        int sign = _nativeGearSlot.Length == 0 ? 1 : -1;
        int hp = sign < 0 ? -item.Health : comparison.Health;
        int power = sign < 0 ? -item.Power : comparison.Power;
        int armor = sign < 0 ? -item.Armor : comparison.Armor;
        int speed = sign < 0 ? -item.Speed : comparison.Speed;
        Wrap((_nativeGearSlot.Length == 0 ? "Replaces " + comparison.Previous : "Returns to the shared pack")
            + $". Change: HP {Signed(hp)}, power {Signed(power)}, armor {Signed(armor)}, speed {Signed(speed)}.", x, y + 124, box.Width - 48, Muted);
        Wrap(_session.EquipmentDescription(item), x, y + 184, box.Width - 48, Muted);
        var preview = _session.DungeonCombat.Preview(DungeonGearIntent);
        string notice = !DungeonSelectionReady ? "Only the ready member may change their own gear. Browsing stays free."
            : preview.Valid ? $"One paid action / recovery {preview.Recovery / 100.0:0.00}s at current speed." : preview.Reason;
        if (_nativeGearSlot.Length > 0 && _session.EquipmentEffectPower(member, "protect") > 0)
            notice += member.Tactics!.EquipmentCoverUsed ? " Ally cover spent until your next turn." : " Ally cover ready.";
        Wrap(notice, x, box.Bottom - 112, box.Width - 48, Gold);
    }
}
