using Microsoft.Xna.Framework;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private string _nativeWorldCache = "";
    private LootCache? SelectedWorldCache => _session.State.Loot.FirstOrDefault(c => c.Id == _nativeWorldCache && !c.Taken);

    private bool TryOpenWorldCache(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var cell = new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        var cache = _session.State.Loot.FirstOrDefault(c => !c.Taken && c.Position == cell);
        if (cache == null || !_session.Visible.Contains(cell)) return false;
        OpenWorldCache(cache); return true;
    }

    private void OpenWorldCache(LootCache cache)
    {
        _nativeWorldCache = cache.Id; _panel = "dungeon-world-loot";
        _dungeonFeedback.OpenChest(); PlayDungeonSound("chest-open");
    }

    private void BuildWorldCacheControls(Rectangle box)
    {
        if (SelectedWorldCache is not { } cache) return;
        int choices = Math.Max(1, cache.Choices.Length);
        for (int i = 0; i < choices; i++)
        {
            int choice = cache.Choices.Length == 0 ? 0 : i + 1;
            var item = choice == 0 ? null : _session.Item(cache.Choices[i]);
            var preview = _session.DungeonCombat.Preview(new(TacticalAction.CollectCache, Choice: choice, Item: cache.Id));
            AddDungeonPartyControl("dungeon:world-loot:" + choice, item == null ? "Recover supplies / one action" : "Take " + item.Name,
                new(box.X + 24, box.Y + 236 + i * 72, box.Width - 48, 64), DungeonSelectionReady && preview.Valid,
                reason: DungeonSelectionReady ? preview.Reason : "Select the ready member first.",
                sprite: item?.Sprite ?? WorldSession.LootSprite(cache),
                detail: item == null ? "Into the shared pack / food is eaten separately" : _session.EquipmentDescription(item));
        }
    }

    private void DrawWorldCachePanel(Rectangle box, int x, int y)
    {
        if (SelectedWorldCache is not { } cache) { Wrap("This cache has already been recovered.", x, y, box.Width - 48, Muted); return; }
        _sprites.Fit(_batch, WorldSession.LootSprite(cache), new(x, y, 64, 64), Gold);
        Text(_session.LootName(cache), x + 84, y + 4, Gold);
        Wrap("Confirming spends the ready member's action. Supplies and the chosen equipment enter the shared pack.",
            x + 84, y + 30, box.Width - 156, Muted);
        string contents = string.Join(" / ", cache.Contents.Where(p => p.Value > 0).Select(p => p.Value + " "
            + (_session.Content.All<ItemDefinition>().FirstOrDefault(i => i.Stack == p.Key)?.Name ?? p.Key)));
        Wrap(contents.Length == 0 ? "Choose one item below." : contents, x, y + 92, box.Width - 48, Ink);
        Wrap("Immediate danger prevents recovery. Browsing and closing consume no time.", x, box.Bottom - 50, box.Width - 48, Muted);
    }

    private void ClaimWorldCache(int choice)
    {
        var result = _session.SubmitDungeon(new(TacticalAction.CollectCache, Choice: choice, Item: _nativeWorldCache));
        _notice = result.Message;
        if (result.Success) { CollectDungeonFeedback(); ClosePanel(); _terrainLayerDirty = true; }
    }
}
