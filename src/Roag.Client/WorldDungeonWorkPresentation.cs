using Microsoft.Xna.Framework;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private WorldPosition _nativeWorkTarget;
    private string _nativeWorkView = "actions";
    private int _nativeWorkPage;

    private void OpenDungeonWork(WorldPosition? target = null)
    {
        var actor = _session.DungeonMember;
        var (dx, dy) = AttackGeometry.Offset(actor.Tactics!.Facing);
        _nativeWorkTarget = target ?? actor.Position.Offset(dx, dy);
        _nativeWorkView = "actions"; _nativeWorkPage = 0; _panel = "dungeon-work";
    }

    private void OpenDungeonWorkAtPointer(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        OpenDungeonWork(new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.DungeonMember.Position.Floor));
    }

    private void AddNativeWorkChoice(string id, string name, Rectangle bounds, WorldCommand command, string? sprite = null, string detail = "")
    {
        var preview = _session.DungeonCombat.Preview(new(TacticalAction.StartWork, Work: command));
        AddDungeonPartyControl(id, name, bounds, DungeonSelectionReady && preview.Valid,
            reason: DungeonSelectionReady ? preview.Reason : "Select the ready member first.", sprite: sprite, detail: detail);
    }

    private void BuildDungeonWorkControls(Rectangle box)
    {
        int width = (box.Width - 72) / 2;
        Rectangle Card(int i) => new(box.X + 24 + i % 2 * (width + 24), box.Y + 184 + i / 2 * 60, width, 52);
        if (_nativeWorkView == "actions")
        {
            var directions = new[] { Facing.North, Facing.East, Facing.South, Facing.West };
            int step = (box.Width - 48) / 4;
            for (int i = 0; i < directions.Length; i++)
                AddDungeonPartyControl("dungeon:work-target:" + directions[i], directions[i].ToString(), new(box.X + 24 + i * step, box.Y + 148, step - 8, 28));
            AddNativeWorkChoice("dungeon:work-action:Dig", "Excavate / one action to start", Card(0), new(WorldCommandKind.Dig, _nativeWorkTarget), "pickaxe");
            AddNativeWorkChoice("dungeon:work-action:Harvest", "Harvest / one action to start", Card(1), new(WorldCommandKind.Harvest, _nativeWorkTarget), "ration");
            AddNativeWorkChoice("dungeon:work-action:Repair", "Repair / one action to start", Card(2), new(WorldCommandKind.Repair, _nativeWorkTarget), "wood");
            AddNativeWorkChoice("dungeon:work-action:Forage", "Gather food here / start", Card(3), new(WorldCommandKind.Forage), "ration");
            AddDungeonPartyControl("dungeon:work-view:build", "Construction recipes", Card(4), sprite: "ruin-wall");
            AddDungeonPartyControl("dungeon:work-view:craft", "Crafting recipes / camp", Card(5), sprite: "anvil");
            AddDungeonPartyControl("dungeon:work-view:orders", "Assigned work / progress", Card(6), sprite: "book");
        }
        else if (_nativeWorkView is "build" or "craft")
        {
            var recipes = _session.Content.Game.Recipes.Select((id, index) => (Recipe: _session.Content.Get<RecipeDefinition>(id), Index: index))
                .Where(p => (p.Recipe.Output == null) == (_nativeWorkView == "build")).ToArray();
            _nativeWorkPage = Math.Clamp(_nativeWorkPage, 0, Math.Max(0, (recipes.Length - 1) / 8));
            for (int i = 0; i < 8 && _nativeWorkPage * 8 + i < recipes.Length; i++)
            {
                var (recipe, index) = recipes[_nativeWorkPage * 8 + i];
                var command = recipe.Output == null ? new WorldCommand(WorldCommandKind.Build, _nativeWorkTarget, Material: recipe.Material)
                    : new WorldCommand(WorldCommandKind.Craft, Item: recipe.Id);
                string? sprite = recipe.Output == null ? Materials.Sprite(recipe.Material, Biome.Grassland) : _session.Content.Get<ItemDefinition>(recipe.Output).Sprite;
                string cost = string.Join(" / ", recipe.Cost.Select(p => p.Value + " " + _session.Content.Get<ItemDefinition>(p.Key).Name));
                AddNativeWorkChoice("dungeon:work-recipe:" + index, recipe.Name, Card(i), command, sprite, cost + " / paid start");
            }
            BuildNativeWorkPaging(box, recipes.Length, 8);
        }
        else
        {
            var orders = _session.State.Work.Where(w => _session.State.Party.Contains(w.Worker)).OrderBy(w => w.Id).ToArray();
            _nativeWorkPage = Math.Clamp(_nativeWorkPage, 0, Math.Max(0, (orders.Length - 1) / 4));
            for (int i = 0; i < 4 && _nativeWorkPage * 4 + i < orders.Length; i++)
            {
                var order = orders[_nativeWorkPage * 4 + i]; var owner = _session.ActorById(order.Worker);
                AddNativeWorkChoice("dungeon:work-resume:" + order.Id, order.Kind + " / " + owner.Name, Card(i * 2),
                    new(WorldCommandKind.AssignWork, WorkId: order.Id), owner.Sprite,
                    $"{order.Progress:0.0}/{order.Required:0.0} / work with {_session.DungeonMember.Name}");
                AddDungeonPartyControl("dungeon:work-cancel:" + order.Id, "Cancel / return unspent supplies", Card(i * 2 + 1), detail: order.Status);
            }
            BuildNativeWorkPaging(box, orders.Length, 4);
        }
        if (_nativeWorkView != "actions") AddDungeonPartyControl("dungeon:work-view:actions", "Work tools", new(box.X + 24, box.Y + 132, 170, 32));
    }

    private void BuildNativeWorkPaging(Rectangle box, int count, int size)
    {
        AddDungeonPartyControl("dungeon:work-page:-1", "Previous", new(box.X + 24, box.Bottom - 54, 150, 34), _nativeWorkPage > 0);
        AddDungeonPartyControl("dungeon:work-page:1", "Next", new(box.Right - 174, box.Bottom - 54, 150, 34), (_nativeWorkPage + 1) * size < count);
    }

    private void DrawDungeonWorkPanel(Rectangle box, int x, int y)
    {
        var member = _session.DungeonMember;
        Text(member.Name + " / " + DungeonReadiness(member), x, y, Gold); y += 30;
        string location = _nativeWorkView == "craft" ? "Equipment crafting requires an adjacent constructed camp."
            : _nativeWorkView == "orders" ? "Resuming or taking over costs one action. Cancelling returns unspent supplies."
            : $"Target {_nativeWorkTarget.X}, {_nativeWorkTarget.Y}, floor {_nativeWorkTarget.Floor} / {WorldInspection.Inspect(_session, _nativeWorkTarget).Title}";
        Wrap(location, x, y, box.Width - 48, Muted);
        if (_nativeWorkView == "orders" && !_session.State.Work.Any(w => _session.State.Party.Contains(w.Worker)))
            Text("No unfinished party work.", x, box.Y + 190, Muted);
        Wrap("Workers spend their own turns walking and working. Danger or an alarm can interrupt them. Close this page to continue; open a menu to inspect or cancel freely.", x, box.Bottom - 92, box.Width - 48, Muted);
    }

    private void CommitDungeonWork(WorldCommand command)
    {
        var result = _session.SubmitDungeon(new(TacticalAction.StartWork, Work: command)); _notice = result.Message;
        if (!result.Success) return;
        StopTravel(); _roagAim = "move"; _terrainLayerDirty = true; CollectDungeonFeedback(); ClosePanel();
    }

    private void ActivateDungeonWorkControl(string[] parts)
    {
        switch (parts[1])
        {
            case "work": OpenDungeonWork(); break;
            case "work-target":
                var (dx, dy) = AttackGeometry.Offset(Enum.Parse<Facing>(parts[2]));
                _nativeWorkTarget = _session.DungeonMember.Position.Offset(dx, dy); break;
            case "work-view": _nativeWorkView = parts[2]; _nativeWorkPage = 0; break;
            case "work-page": _nativeWorkPage += int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture); break;
            case "work-action": CommitDungeonWork(new(Enum.Parse<WorldCommandKind>(parts[2]), _nativeWorkTarget)); break;
            case "work-recipe":
                var recipe = _session.Content.Get<RecipeDefinition>(_session.Content.Game.Recipes[int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)]);
                CommitDungeonWork(recipe.Output == null ? new(WorldCommandKind.Build, _nativeWorkTarget, Material: recipe.Material) : new(WorldCommandKind.Craft, Item: recipe.Id)); break;
            case "work-resume": CommitDungeonWork(new(WorldCommandKind.AssignWork, WorkId: long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture))); break;
            case "work-cancel": _notice = _session.CancelDungeonWork(long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)).Message; break;
        }
    }
}
