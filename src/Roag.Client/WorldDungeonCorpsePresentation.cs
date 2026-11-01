using Microsoft.Xna.Framework;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private long _nativeCorpse;

    private bool TryOpenDungeonCorpse(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var cell = new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        if (!_session.Visible.Contains(cell)) return false;
        var corpse = _session.Party.FirstOrDefault(a => a.Life == LifeState.Dead && a.Position == cell);
        if (corpse == null) return false;
        _nativeCorpse = corpse.Id; _panel = "dungeon-corpse"; return true;
    }

    private void BuildDungeonCorpseControls(Rectangle box)
    {
        var preview = _session.DungeonCombat.Preview(new(TacticalAction.RecoverEquipment, Target: _nativeCorpse));
        AddDungeonPartyControl("dungeon:recover-equipment", "Recover gear / one action", new(box.X + 24, box.Bottom - 62, 340, 40),
            DungeonSelectionReady && preview.Valid, reason: DungeonSelectionReady ? preview.Reason : "Select the ready member first.");
    }

    private void DrawDungeonCorpsePanel(Rectangle box, int x, int y)
    {
        var corpse = _session.ActorById(_nativeCorpse);
        Text(corpse.Name + " / permanently dead", x, y, Gold);
        if (corpse.Equipment.Count == 0) Text("No equipment remains on this body.", x, y + 54, Muted);
        int row = 0;
        foreach (var (slot, gear) in corpse.Equipment.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var item = _session.Item(gear.Item); int top = box.Y + 110 + row++ * 70;
            _sprites.Fit(_batch, item.Sprite!, new(x, top, 52, 52), Ink);
            Text(item.Name + " / " + slot, x + 72, top + 4);
            Text($"HP +{gear.Health} / power +{gear.Power} / armor +{gear.Armor} / speed +{gear.Speed}", x + 72, top + 28, Muted);
        }
        Wrap("Recover their equipment into the shared pack from beside the body. Their class, assigned boons and permanent death remain recorded.", x, box.Bottom - 116, box.Width - 48, Muted);
    }
}
