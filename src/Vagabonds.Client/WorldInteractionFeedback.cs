using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.World;

namespace Vagabonds.Client;

internal sealed record WorldHoverInfo(string Title, string Detail, string Action, string Reason, string Kind);

internal static class WorldInspection
{
    public static WorldHoverInfo Inspect(WorldSession session, WorldPosition cell, WorldClickPlan? cachedPlan = null)
    {
        if (!session.Chunks.Seen(cell)) return new("Unexplored ground", "Its terrain and inhabitants are unknown.", "Explore toward the visible edge", "Choose observed ground first.", "unknown");
        var material = session.Chunks.At(cell);
        var actor = session.State.Actors.LastOrDefault(a => a.Life != LifeState.Dead && a.Position == cell && session.Visible.Contains(cell));
        if (actor != null)
        {
            if (session.SituationForActor(actor) is { } situation)
                return WithRoute(actor.Name, session.SituationDescription(situation) + " " + session.EncounterDescription(actor.Id), "Click to inspect the place and choose an approach", session.IsHostile(actor) ? "enemy" : "discovery");
            string stats = $"HP {actor.Health}/{actor.MaxHealth} / Power {actor.Power} / Armour {actor.Armor} / Speed {actor.Speed}.";
            bool hostile = session.IsHostile(actor), wildlife = session.Ecology(actor) != null;
            string detail = wildlife ? session.WildlifeDescription(actor) + " " + stats : hostile ? Threat(session, actor) + ". " + stats : actor.Kind == ActorKind.Person ? session.Class(actor).Name + " L" + actor.Level + "; " + actor.Job + ". " + stats : actor.Kind + " from " + session.Plan.Towns[actor.HomeTown].Name + ". " + stats;
            if (session.Content.Entries[actor.Archetype] is BossDefinition boss) detail += " " + boss.Weakness;
            if (actor.EncounterId.Length > 0) detail += " " + session.EncounterDescription(actor.Id);
            if (session.Content.Entries.GetValueOrDefault(actor.CombatTrait) is TalentDefinition talent) detail += " " + talent.Name + ": " + talent.Description;
            return WithRoute(actor.Name, detail, actor.Recruited ? "Manage companion; right-click to change leader" : wildlife ? "Click to hunt / start a turn-based encounter" : hostile ? "Approach and fight" : actor.Kind == ActorKind.Person ? "Approach and speak" : "Approach and interact", hostile ? "enemy" : wildlife ? "wildlife" : "person");
        }
        var discovery = session.State.Discoveries.FirstOrDefault(d => d.Position == cell);
        if (discovery != null && session.Situation(discovery) != null) return WithRoute(discovery.Name, session.SituationDescription(discovery), "Right-click for approaches; click to investigate", "discovery");
        if (discovery != null) return WithRoute(discovery.Name, session.DiscoveryComplete(discovery) ? "Explored; its history remains in your journal."
            : session.DiscoveryGuarded(discovery) ? "An inhabited landmark. Living foes guard its supplies." : "A local landmark connected to " + session.Plan.Towns[discovery.TownId].Name + ".", "Approach and investigate", "discovery");
        if (session.Visible.Contains(cell) && session.State.Loot.FirstOrDefault(c => c.Position == cell && !c.Taken) is { } cache) return WithRoute(session.LootName(cache), cache.SourceActor > 0 ? "Recover ingredients for camp cooking and crafting." : "Supplies and equipment. Open it to inspect any reward alternatives.", "Approach and recover", "loot");
        if (session.ObjectAt(cell) is { Kind: "decoration", Solid: true } prop) return WithRoute(prop.Name, "A solid object occupies this tile.", "Right-click to dismantle", "object");
        if (session.SituationAtWall(cell) is { Known: true, Outcome.Length: 0 } place)
            return new("Side wall / " + place.Name, "Excavating this wall opens a physical bypass to the supplies.", "Right-click and choose Dig; companions can do the work.", session.TargetBlockReason(WorldCommandKind.Dig, cell, requireAdjacent: false), "discovery");
        if (session.ResourceAt(cell) is { } resource)
            return new(resource.Definition.Name, session.GatheringDanger(cell) + resource.Description + " Each gather: " + session.ResourceYieldDescription(resource.Definition) + ".",
                "Right-click to gather; drag-select several resources", resource.Available > 0 ? "" : resource.RecoveryDescription, "resource");
        string action = material switch { Material.StairsDown => "Approach and descend", Material.StairsUp => "Approach and ascend", Material.Storage => "Approach and open storage", Material.Camp => "Right-click to approach, rest or craft", _ => "Travel here" };
        return WithRoute(material + (session.Visible.Contains(cell) ? "" : " (remembered)"), "Tile " + cell.X + ", " + cell.Y + ". " + session.Plan.BiomeAt(cell.X, cell.Y) + ".", action, "terrain");
        WorldHoverInfo WithRoute(string title, string detail, string action, string kind)
        { var plan = cachedPlan ?? WorldMouseNavigation.Plan(session, cell); return new(title, detail, action, plan.Reachable ? "" : plan.Reason, kind); }
    }
    private static string Threat(WorldSession session, Actor actor)
    {
        double party = session.Party.Where(a => a.Life == LifeState.Alive).Sum(a => a.Health * Math.Max(1, a.Power));
        var visibleGroup = session.EncounterOpponents(actor.Id).Where(a => session.Visible.Contains(a.Position)).ToArray();
        double ratio = visibleGroup.Sum(a => a.Health * (double)Math.Max(1, a.Power)) / Math.Max(1, party);
        return "Visible threat estimate: " + (ratio > 2 ? "overwhelming" : ratio > 1 ? "severe" : ratio > .45 ? "dangerous" : "manageable") + " against the current party";
    }
}

internal sealed partial class WorldGame
{
    private sealed record KnownPlace(string Id, string Name, WorldPosition Position);
    private KnownPlace[] KnownPlaces() => _session.State.Discoveries.Where(d => d.Known).Select(d => new KnownPlace(d.Id, d.Name, d.Position))
        .Concat(_session.Plan.Sites.Where(s => _session.State.KnownSites.Contains(s.Id)).Select(s => new KnownPlace("site-" + s.Id, s.Name, s.Center)))
        .Concat(_session.Plan.Towns.Where(t => _session.Chunks.Seen(t.Center)).Select(t => new KnownPlace("town-" + t.Id, t.Name, t.Center)))
        .OrderBy(p => p.Position.Distance(_session.State.Position with { Floor = 0 })).ThenBy(p => p.Id).ToArray();
    private WorldHoverInfo? _hover;
    private string _cursorMode = "";
    private void OpenLoot(WorldPosition cell)
    {
        var cache = _session.State.Loot.FirstOrDefault(c => c.Position == cell && !c.Taken);
        if (cache == null) { _notice = "No unopened supplies remain here."; return; }
        if (cache.Choices.Length > 0) { _lootPosition = cell; _panel = "loot"; StopTravel(); }
        else Execute(new(WorldCommandKind.Loot, cell));
    }
    private string DisabledControlReason(string id)
    {
        string action = id.Split(':', 2)[0], argument = id.Contains(':', StringComparison.Ordinal) ? id[(id.IndexOf(':') + 1)..] : "";
        Actor? member = _session.Party.FirstOrDefault();
        if (member != null) member = SelectedMember();
        var gear = EquipmentChoices().ElementAtOrDefault(_packItem);
        WorldCommand? command = action switch
        {
            "rest" => new(WorldCommandKind.Camp), "forage" => new(WorldCommandKind.Forage),
            "craft" => new(WorldCommandKind.Craft, Item: argument),
            "equip" => new(WorldCommandKind.Equip, ActorId: member?.Id ?? 0, Item: gear?.Id ?? ""),
            "unequip" => new(WorldCommandKind.Unequip, ActorId: member?.Id ?? 0, Item: gear?.Slot ?? ""),
            "revive" => new(WorldCommandKind.Revive, ActorId: member?.Id ?? 0), "dismiss" => new(WorldCommandKind.Dismiss, ActorId: member?.Id ?? 0), "abandon" => new(WorldCommandKind.Abandon, ActorId: member?.Id ?? 0), _ => null
        };
        if (command != null) return _session.CommandBlockReason(command);
        if (action == "conversation") return _session.InteractionBlockReason(_person, argument);
        if (action == "skill") return _session.SkillUnavailable(int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture));
        if (action == "buy")
        {
            var town = _session.Plan.Towns.FirstOrDefault(t => t.Center.Distance(_session.State.Position) <= 28);
            if (town == null) return "Approach a town to trade.";
            return _session.State.Towns[town.Id].Stocks[argument] <= 0 ? "The town has no stock remaining." : $"Needs {_session.State.Towns[town.Id].Price(argument)} gold; {_session.Count("gold")} carried.";
        }
        if (action == "sell") return "None of this supply remains in the shared pack.";
        if (action == "take") return "This stored stack is empty.";
        if (action == "store") return "None of this supply remains in the shared pack.";
        if (action == "pause") return _session.IsInCombat ? "Combat choices already pause world time." : "This expedition has ended.";
        if (action == "side-arc") return _session.State.Arc.Status != ArcStatus.Active ? "The optional side arc has ended." : _session.State.Arc.Progress == 0 ? "Find evidence beneath " + _session.Plan.Sites[_session.State.Arc.SiteId].Name + " first." : "";
        if (action == "delivery") return !_session.State.Tasks.Any(t => !t.Complete && t.RoadId == null && _session.State.Position.Distance(_session.Plan.Towns[t.TownId].Center) < 28)
            ? "No food delivery request is available at this town." : _session.Count("food") < 20 ? "Delivery requires 20 food." : "";
        if (action is "rest" or "forage" or "wait" or "storage" or "tool") return "";
        if (action == "assign") return "Select an order and an available worker.";
        if (action == "cancel-work") return "There is no selected pending work order.";
        if (action.EndsWith("page", StringComparison.Ordinal) || action is "history" or "combat-history") return "No further entries in this direction.";
        if (action is "new-world" or "continue-world") return "Save the recorded deaths before continuing.";
        return "This action is unavailable in the current state.";
    }
    private void DrawHoverFeedback()
    {
        _hover = null;
        if (PointerModal) return;
        var control = WorldMouseControl.Hit(_controls, _mouse.Position);
        bool hand = control is { Enabled: true };
        if (_panel.Length == 0 && !_session.State.Endgame.Victory && !_session.State.AwaitingContinuation && control == null)
        {
            if (CombatStageVisible)
            {
                if (Playback.Busy) { if (_cursorMode != "arrow") { Mouse.SetCursor(MouseCursor.Arrow); _cursorMode = "arrow"; } return; }
                var b = _session.State.Battle!; var unit = b.Units.LastOrDefault(u => UnitRect(u).Contains(_mouse.Position));
                if (unit != null)
                {
                    var preview = _session.ActionPreview(_skill, unit.Id, _cooperativePartner);
                    string detail = $"HP {unit.Health}/{unit.MaxHealth}; MP {unit.Mana}; armour {unit.Armor}. " + WorldSession.StatusDescription(unit);
                    if (!unit.Friendly && BattleIntent(unit.Id) is { } intent)
                        detail += " Intent: " + intent.Name + ". " + (intent.Reason.Length > 0 ? intent.Reason : string.Join("; ", intent.Targets.Select(t => t.Name + " " + t.Minimum + "-" + t.Maximum)) + ". Current defenses; may change before its turn.");
                    bool helper = b.Active.Friendly && _session.ActiveSkills().ElementAtOrDefault(_skill)?.Cooperative == true && (_cooperativePartner < 0 || unit.Friendly);
                    _hover = new(unit.Name, detail, helper ? "Assist: spend this round's action and " + _session.ActiveSkills()[_skill].PartnerMana + " MP." : b.Active.Friendly ? preview.Description : _session.EnemyIntent(unit.Id), helper ? _session.CooperationPartnerReason(_skill, unit.Id) : b.Active.Friendly ? preview.Reason : "Wait for a companion's turn.", "combat");
                    hand = b.Active.Friendly && _hover.Reason.Length == 0;
                }
            }
            else if (_pointer.Cell is { } cell)
            {
                _hover = WorldInspection.Inspect(_session, _pointer.NavigationCell ?? cell, _pointer.Plan); hand = _hover.Reason.Length == 0;
            }
        }
        string mode = hand ? "hand" : "arrow";
        if (_cursorMode != mode) { Mouse.SetCursor(hand ? MouseCursor.Hand : MouseCursor.Arrow); _cursorMode = mode; }
        if (_hover == null) return;
        var area = CombatStageVisible ? CombatArea : _map;
        int width = Math.Min(388, (area.Width - 40) / 2);
        var details = FeedbackLines(Short(_hover.Detail, CombatStageVisible ? 360 : 180), width - 24);
        var actions = FeedbackLines(Short(_hover.Reason.Length > 0 ? _hover.Reason : _hover.Action, 180), width - 24);
        int height = 46 + (details.Length + actions.Length) * 19;
        int px = _mouse.X + width + 18 < area.Right ? _mouse.X + 18 : _mouse.X - width - 18;
        int py = _mouse.Y + height + 18 < area.Bottom ? _mouse.Y + 18 : _mouse.Y - height - 18;
        int x = Math.Clamp(px, area.Left + 6, area.Right - width - 6), y = Math.Clamp(py, area.Top + 6, area.Bottom - height - 6);
        var box = new Rectangle(x, y, width, height); Fill(box, new Color(20, 29, 34) * .98f); Fill(new(x, y, 3, box.Height), _hover.Reason.Length > 0 ? new(207, 126, 113) : Gold);
        Text(Short(_hover.Title, (width - 24) / 8), x + 12, y + 10, Gold);
        int row = y + 34; foreach (string line in details) { Text(line, x + 12, row); row += 19; }
        foreach (string line in actions) { Text(line, x + 12, row, _hover.Reason.Length > 0 ? new(224, 151, 132) : new(129, 200, 204)); row += 19; }
    }
    private static string[] FeedbackLines(string text, int width)
    {
        int columns = Math.Max(1, width / 8); var lines = new List<string>();
        while (text.Length > columns)
        {
            int split = text.LastIndexOf(' ', columns, columns); if (split <= 0) split = columns;
            lines.Add(text[..split]); text = text[split..].TrimStart();
        }
        if (text.Length > 0) lines.Add(text); return lines.ToArray();
    }
}
