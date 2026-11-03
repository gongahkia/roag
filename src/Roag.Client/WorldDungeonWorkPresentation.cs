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
    private long _nativeWorkOrder;

    private WorldPosition NativeDutyCamp()
    {
        var member = _session.DungeonMember;
        return _session.ServiceableCamp(_nativeWorkTarget) ? _nativeWorkTarget : _session.Visible
            .Where(p => p.Distance(member.Position) <= 4 && _session.ServiceableCamp(p)).OrderBy(p => p.Distance(member.Position))
            .ThenBy(p => p.Y).ThenBy(p => p.X).Select(p => (WorldPosition?)p).FirstOrDefault() ?? _nativeWorkTarget;
    }

    private static string NativeMealBenefit(CampDefinition meal) => meal.Effect switch
    {
        "burn-resist" => "Burn damage -" + meal.Power,
        "bleed-resist" => "Bleed damage -" + meal.Power,
        "armor" => "Armor +" + meal.Power,
        "focus" => "Dread stress -" + meal.Power * 3,
        _ => meal.Name
    };

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
            AddDungeonPartyControl("dungeon:work-view:camp", "Camp duties / prepared meals", Card(7), sprite: "campfire");
        }
        else if (_nativeWorkView == "camp")
        {
            var camp = NativeDutyCamp();
            for (int i = 0; i < _session.Content.Game.CampActivities.Length; i++)
            {
                var duty = _session.Content.Get<CampDefinition>(_session.Content.Game.CampActivities[i]);
                string cost = string.Join(" / ", duty.Cost.Select(p => p.Value + " " + _session.Content.Get<ItemDefinition>(p.Key).Name));
                string benefit = duty.Role switch
                {
                    "cook" => "Shared servings / eat individually",
                    "watch" => "+2 speed / alerts while preparing",
                    "treat" => "12 HP / removes 2 wounds nearby",
                    _ => "Share memories / reveal a site"
                };
                AddNativeWorkChoice("dungeon:work-duty:" + i, duty.Name, Card(i),
                    new(WorldCommandKind.CampActivity, camp, Item: duty.Id), duty.Sprite, benefit + (cost == "" ? "" : " / " + cost));
            }
            AddDungeonPartyControl("dungeon:work-view:meals", "Shared prepared servings", Card(7), sprite: "ration");
        }
        else if (_nativeWorkView == "meals")
        {
            var meals = _session.Content.Game.CampActivities.Select(_session.Content.Get<CampDefinition>).Where(d => d.Role == "cook").ToArray();
            for (int i = 0; i < meals.Length; i++)
            {
                var meal = meals[i]; var preview = _session.DungeonCombat.Preview(new(TacticalAction.Eat, Item: meal.Id));
                AddDungeonPartyControl("dungeon:work-eat:" + meal.Id, meal.Name + " / " + _session.State.Tactics!.PreparedMeals.GetValueOrDefault(meal.Id), Card(i),
                    DungeonSelectionReady && preview.Valid, reason: DungeonSelectionReady ? preview.Reason : "Select the ready member first.",
                    sprite: meal.Sprite, detail: $"One paid Eat / {NativeMealBenefit(meal)} / {meal.Duration / WorldSession.TicksPerHour}h");
            }
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
        else if (_nativeWorkView == "residents")
        {
            var residents = _session.State.Actors.Where(a => a is { Kind: ActorKind.Person, Recruited: false, Life: LifeState.Alive }
                && _session.Visible.Contains(a.Position)).OrderBy(a => a.Position.Distance(_session.DungeonMember.Position)).ThenBy(a => a.Id).ToArray();
            _nativeWorkPage = Math.Clamp(_nativeWorkPage, 0, Math.Max(0, (residents.Length - 1) / 8));
            for (int i = 0; i < 8 && _nativeWorkPage * 8 + i < residents.Length; i++)
            {
                var resident = residents[_nativeWorkPage * 8 + i];
                AddNativeWorkChoice("dungeon:work-resident:" + resident.Id, resident.Name, Card(i),
                    new(WorldCommandKind.AssignWork, ActorId: resident.Id, WorkId: _nativeWorkOrder), resident.Sprite,
                    resident.Job + " / assign for 1 action");
            }
            BuildNativeWorkPaging(box, residents.Length, 8);
        }
        else
        {
            var orders = _session.DungeonWorkOrders;
            _nativeWorkPage = Math.Clamp(_nativeWorkPage, 0, Math.Max(0, (orders.Count - 1) / 4));
            bool delegateWork = _session.State.NativeCampaign != null;
            Rectangle OrderCard(int row, int column)
            {
                int columns = delegateWork ? 3 : 2, cardWidth = (box.Width - 48 - (columns - 1) * 16) / columns;
                return new(box.X + 24 + column * (cardWidth + 16), box.Y + 184 + row * 60, cardWidth, 52);
            }
            for (int i = 0; i < 4 && _nativeWorkPage * 4 + i < orders.Count; i++)
            {
                var order = orders[_nativeWorkPage * 4 + i]; var owner = _session.State.Actors.FirstOrDefault(a => a.Id == order.Worker);
                AddNativeWorkChoice("dungeon:work-resume:" + order.Id, order.Kind + " / " + (owner?.Name ?? "Unassigned"), OrderCard(i, 0),
                    new(WorldCommandKind.AssignWork, WorkId: order.Id), owner?.Sprite ?? "book",
                    $"{order.Progress:0.0}/{order.Required:0.0} / take over");
                if (delegateWork) AddDungeonPartyControl("dungeon:work-delegate:" + order.Id, "Assign a resident", OrderCard(i, 1), sprite: "book", detail: "Nearby resident / 1 action");
                AddDungeonPartyControl("dungeon:work-cancel:" + order.Id, "Cancel / return supplies", OrderCard(i, delegateWork ? 2 : 1), detail: order.Status);
            }
            BuildNativeWorkPaging(box, orders.Count, 4);
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
            : _nativeWorkView == "camp" ? "Choose a serviceable camp in this member's sight, within four tiles. Each member can perform a different duty."
            : _nativeWorkView == "meals" ? "Shared servings restore hunger and give their timed buff only to the member spending an Eat action."
            : _nativeWorkView == "orders" ? "Resuming or assigning work costs one action. Cancelling returns unspent supplies."
            : _nativeWorkView == "residents" ? "Speak within two visible tiles. Their usual occupation pauses while they complete this order."
            : $"Target {_nativeWorkTarget.X}, {_nativeWorkTarget.Y}, floor {_nativeWorkTarget.Floor} / {WorldInspection.Inspect(_session, _nativeWorkTarget).Title}";
        Wrap(location, x, y, box.Width - 48, Muted);
        if (_nativeWorkView == "orders" && _session.DungeonWorkOrders.Count == 0)
            Text("No unfinished expedition work.", x, box.Y + 190, Muted);
        Wrap(_nativeWorkView == "meals"
            ? "Automatic eating uses provisions first, then broth, eggs, tea and stew when available. Your individual eating and combat policies still apply. Browsing is free."
            : "Workers spend their own turns walking and working. Danger or an alarm can interrupt them. Close this page to continue; open a menu to inspect or cancel freely.", x, box.Bottom - 92, box.Width - 48, Muted);
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
            case "work-meals": OpenDungeonWork(); _nativeWorkView = "meals"; break;
            case "work-target":
                var (dx, dy) = AttackGeometry.Offset(Enum.Parse<Facing>(parts[2]));
                _nativeWorkTarget = _session.DungeonMember.Position.Offset(dx, dy); break;
            case "work-view": _nativeWorkView = parts[2]; _nativeWorkPage = 0; break;
            case "work-page": _nativeWorkPage += int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture); break;
            case "work-action": CommitDungeonWork(new(Enum.Parse<WorldCommandKind>(parts[2]), _nativeWorkTarget)); break;
            case "work-duty":
                var duty = _session.Content.Get<CampDefinition>(_session.Content.Game.CampActivities[int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)]);
                CommitDungeonWork(new(WorldCommandKind.CampActivity, NativeDutyCamp(), Item: duty.Id)); break;
            case "work-eat":
                var result = _session.SubmitDungeon(new(TacticalAction.Eat, Item: parts[2])); _notice = result.Message;
                if (result.Success) { CollectDungeonFeedback(); ClosePanel(); }
                break;
            case "work-recipe":
                var recipe = _session.Content.Get<RecipeDefinition>(_session.Content.Game.Recipes[int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)]);
                CommitDungeonWork(recipe.Output == null ? new(WorldCommandKind.Build, _nativeWorkTarget, Material: recipe.Material) : new(WorldCommandKind.Craft, Item: recipe.Id)); break;
            case "work-resume": CommitDungeonWork(new(WorldCommandKind.AssignWork, WorkId: long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture))); break;
            case "work-delegate":
                _nativeWorkOrder = long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                _nativeWorkView = "residents"; _nativeWorkPage = 0; break;
            case "work-resident": CommitDungeonWork(new(WorldCommandKind.AssignWork,
                ActorId: long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture), WorkId: _nativeWorkOrder)); break;
            case "work-cancel": _notice = _session.CancelDungeonWork(long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)).Message; break;
        }
    }
}
