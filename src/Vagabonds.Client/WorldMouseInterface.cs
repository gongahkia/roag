using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.World;
using Vagabonds.Graphics;

namespace Vagabonds.Client;

internal sealed record WorldMouseControl(string Id, string Label, Rectangle Bounds, bool Enabled = true, bool Selected = false, string Tip = "", string DisabledReason = "")
{
    public static WorldMouseControl? Hit(IEnumerable<WorldMouseControl> controls, Point pointer) => controls.Reverse().FirstOrDefault(c => c.Bounds.Contains(pointer));
}

internal sealed partial class WorldGame
{
    private readonly List<WorldMouseControl> _controls = [];
    private int _workPage, _workerPage, _storagePage, _gearPage, _craftPage, _interactionPage;
    private WorldPosition _storage;
    private string _discoveryId = "";
    private WorldPosition _lootPosition;
    private int _combatLogOffset;
    private int _placesPage;
    private WorldPosition? _waypoint;
    private string _lastUiReport = "";
    private static readonly JsonSerializerOptions UiJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private Rectangle PanelBox(int w, int h) => new(32, 84, w - 64, h - 202);
    private static string Short(string text, int count) => text.Length > count ? text[..Math.Max(1, count - 3)] + "..." : text;
    private ItemDefinition[] EquipmentChoices() => _session.Content.All<ItemDefinition>().Where(i => i.Slot != "none").ToArray();
    private Actor SelectedMember() { var party = _session.Party.ToArray(); return party[_packMember % party.Length]; }
    private WorkOrder? SelectedWork() => _session.State.Work.ElementAtOrDefault(_workIndex % Math.Max(1, _session.State.Work.Count));
    private Dictionary<string, int> StoredItems() => _session.State.Chunks.FirstOrDefault(c => c.Key == _storage.Chunk)?.Storage.GetValueOrDefault(_storage.LocalIndex) ?? [];
    private string[] StorageItems() => _session.State.Inventory.Keys.Concat(StoredItems().Keys).Distinct().Where(i => _session.Count(i) > 0 || StoredItems().GetValueOrDefault(i) > 0).Order(StringComparer.Ordinal).ToArray();
    private string[] JournalLines(int width)
    {
        int columns = Math.Max(1, width / 8);
        var entries = _session.State.Tasks.Where(t => !t.Complete).Select(t => "Request: " + t.Description)
            .Concat(_session.State.Journal.AsEnumerable().Reverse());
        var lines = new List<string>();
        foreach (string entry in entries)
        {
            string remaining = entry;
            while (remaining.Length > columns)
            {
                int split = remaining.LastIndexOf(' ', columns, columns);
                if (split <= 0) split = columns;
                lines.Add(remaining[..split]); remaining = remaining[split..].TrimStart();
            }
            lines.Add(remaining); lines.Add("");
        }
        return lines.ToArray();
    }
    private void StopTravel() { _route.Clear(); _pendingInteraction = null; }
    private void ClosePanel() { _panel = ""; _tool = ""; StopTravel(); }
    private void Zoom(int direction) { _camera.ZoomAt(_map.Center.ToVector2(), direction); UpdateView(); }
    private void NewWorld()
    {
        if (_session.DeathPending) { _notice = "Save the recorded deaths before starting another world."; return; }
        _session = WorldSession.Create(WorldStartup.FreshSeed()); if (_autoSave) _session.SavePath = _options.Save;
        ClosePanel(); _packMember = _packItem = 0; _camera.Center = Vector2.Zero;
        _waypoint = null;
        _notice = "A fresh world begins. Click the ground to travel or choose Play.";
    }
    private void FinishApproach()
    {
        if (_pendingInteraction is not { } intent) return;
        var actor = intent.Actor == 0 ? null : _session.State.Actors.FirstOrDefault(a => a.Id == intent.Actor && a.Life == LifeState.Alive);
        if (intent.Actor != 0 && actor == null) { StopTravel(); return; }
        var cell = actor?.Position ?? intent.Cell;
        int range = intent.Kind is "person" or "actor" ? 2 : 1;
        if (cell.Distance(_session.State.Position) > range)
        {
            if (_route.Count == 0)
            {
                var replanned = WorldMouseNavigation.Plan(_session, cell);
                if (!replanned.Reachable || replanned.Path.Count == 0) { StopTravel(); _notice = "The target moved out of reach. Click to approach again."; return; }
                foreach (var step in replanned.Path) _route.Enqueue(step);
            }
            return;
        }
        StopTravel();
        if (intent.Kind == "person") { _person = intent.Actor; _interactionPage = 0; Execute(new(WorldCommandKind.Interact, ActorId: intent.Actor)); _panel = "person"; }
        else if (intent.Kind is "enemy" or "actor") Execute(new(WorldCommandKind.Interact, ActorId: intent.Actor));
        else if (intent.Kind == "loot") OpenLoot(cell);
        else if (intent.Kind == "discovery")
        {
            if (Execute(new(WorldCommandKind.Discover, cell))) { _discoveryId = _session.State.Discoveries.Single(d => d.Position == cell).Id; _panel = "discovery"; }
        }
        else if (intent.Kind == "storage") { _storage = cell; _storagePage = 0; _panel = "storage"; }
        else if (intent.Kind == "stairs") Execute(new(WorldCommandKind.Interact, cell));
    }
    private void BuildMouseControls(int w, int h)
    {
        _controls.Clear();
        void Add(string id, string label, int x, int y, int width, int height = 34, bool enabled = true, bool selected = false, string tip = "", string reason = "")
            => _controls.Add(new(id, label, new(x, y, width, height), enabled, selected, tip, enabled ? "" : reason.Length > 0 ? reason : DisabledControlReason(id)));
        bool ended = _session.State.Endgame.Victory || _session.State.AwaitingContinuation;
        Add("pause", _session.Paused ? "Play" : "Pause", w - 376, 16, 84, enabled: !ended && !_session.IsInCombat, tip: "Pause stops travel. Play closes the current panel and resumes time.");
        Add("save", "Save", w - 284, 16, 84); Add("load", "Load", w - 192, 16, 84); Add("panel:game", "Menu", w - 100, 16, 84);
        var box = PanelBox(w, h); int x = box.X + 20, y = box.Y + 64, width = box.Width - 40;
        if (ended && _panel != "game")
        {
            if (_session.State.Endgame.Victory) Add("new-world", "Begin a new world", w / 2 - 130, h / 2 + 42, 260, 40);
            else
            {
                Add("continue-world", "New traveller here", w / 2 - 264, h / 2 + 42, 256, 40, !_session.DeathPending);
                Add("new-world", "Begin a new world", w / 2 + 8, h / 2 + 42, 256, 40, !_session.DeathPending);
            }
            return;
        }
        if (_panel.Length > 0)
        {
            Add("close", "Close", box.Right - 100, box.Top + 12, 80);
            switch (_panel)
            {
                case "game":
                    string[] ids = ["panel:guide", "panel:inspector", "zoom:default", "fullscreen", "quit"];
                    string[] labels = ["How to play", "World inspector", "Reset exploration zoom", "Toggle fullscreen", "Save and quit"];
                    for (int i = 0; i < ids.Length; i++) Add(ids[i], labels[i], x, y + i * 52, Math.Min(360, width), 40);
                    break;
                case "person":
                    var interactions = _session.Interactions(_person);
                    _interactionPage = Math.Clamp(_interactionPage, 0, Math.Max(0, (interactions.Count - 1) / 6));
                    for (int row = 0; row < Math.Min(6, interactions.Count - _interactionPage * 6); row++)
                    {
                        int i = row; var interaction = interactions[_interactionPage * 6 + row]; string cost = string.Join(", ", interaction.Cost.Select(c => c.Value + " " + _session.Content.Get<ItemDefinition>(c.Key).Name));
                        Add("conversation:" + interaction.Id, interaction.Name + (cost.Length > 0 ? " / " + cost : ""), x + i % 2 * (width / 2 + 4), y + 154 + i / 2 * 44, width / 2 - 8, 36,
                            _session.InteractionBlockReason(_person, interaction.Id).Length == 0, tip: interaction.Description);
                    }
                    if (interactions.Count > 6)
                    {
                        Add("interactionpage:-1", "Previous", x, box.Bottom - 88, 112, enabled: _interactionPage > 0);
                        Add("interactionpage:1", "Next", x + 120, box.Bottom - 88, 96, enabled: (_interactionPage + 1) * 6 < interactions.Count);
                    }
                    break;
                case "build":
                    for (int i = 0; i < BuildChoices.Length; i++)
                    {
                        var material = BuildChoices[i]; var recipe = _session.Content.Game.Recipes.Select(_session.Content.Get<RecipeDefinition>).FirstOrDefault(r => r.Output == null && r.Material == material);
                        string cost = recipe == null ? "Unavailable" : string.Join(", ", recipe.Cost.Select(c => c.Value + " " + _session.Content.Get<ItemDefinition>(c.Key).Name));
                        Add("build:" + material, material + " / " + cost, x + i % 2 * (width / 2 + 4), y + 42 + i / 2 * 58, width / 2 - 8, 42, recipe != null && _session.RecipeSuppliesReason(material).Length == 0, tip: recipe?.Description ?? "", reason: _session.RecipeSuppliesReason(material));
                    }
                    break;
                case "trade":
                    var town = _session.Plan.Towns.FirstOrDefault(t => _session.State.Position.Distance(t.Center) <= 28);
                    if (town == null) break;
                    string[] stacks = ["food", "wood", "stone", "medicine"];
                    for (int i = 0; i < stacks.Length; i++)
                    {
                        string item = stacks[i]; var stocks = _session.State.Towns[town.Id];
                        Add("buy:" + item, "Buy 1", box.Right - 240, y + 46 + i * 56, 96, 36, stocks.Stocks[item] > 0 && _session.Count("gold") >= stocks.Price(item));
                        Add("sell:" + item, "Sell 1", box.Right - 132, y + 46 + i * 56, 96, 36, _session.Count(item) > 0);
                    }
                    break;
                case "inventory":
                    var party = _session.Party.ToArray(); var member = SelectedMember(); var items = EquipmentChoices();
                    int card = (width - 8 * (party.Length - 1)) / party.Length;
                    for (int i = 0; i < party.Length; i++) Add("party:" + i, party[i].Name, x + i * (card + 8), y, card, 32, selected: party[i].Id == member.Id,
                        tip: _session.Class(party[i]).Name + ": " + string.Join(", ", _session.Class(party[i]).Abilities.Select(id => _session.Content.Get<AbilityDefinition>(id).Name)));
                    _gearPage = Math.Clamp(_gearPage, 0, Math.Max(0, (items.Length - 1) / 8));
                    for (int row = 0; row < Math.Min(8, items.Length - _gearPage * 8); row++)
                    {
                        int i = row + _gearPage * 8; bool worn = member.Equipment.GetValueOrDefault(items[i].Slot)?.Item == items[i].Id;
                        var comparison = _session.CompareGear(member, items[i].Id);
                        Add("gear:" + i, $"{(worn ? "[worn] " : "")}{items[i].Name}  x{_session.Count(items[i].Stack)}   [{items[i].Slot}]   HP+{items[i].Health} P+{items[i].Power} A+{items[i].Armor} S+{items[i].Speed}", x, y + 126 + row * 28, width, 26, selected: i == _packItem,
                            tip: "Replaces " + comparison.Previous + ": " + GearDelta(comparison) + ". " + items[i].Description);
                    }
                    if (items.Length > 8)
                    {
                        Add("gearpage:-1", "Prev", box.Right - 172, y + 86, 64, 28, _gearPage > 0);
                        Add("gearpage:1", "Next", box.Right - 100, y + 86, 64, 28, (_gearPage + 1) * 8 < items.Length);
                    }
                    var gear = items.ElementAtOrDefault(_packItem % Math.Max(1, items.Length)); bool outside = !_session.IsInCombat;
                    int ay = box.Bottom - 88;
                    Add("equip", "Equip", x, ay, 112, enabled: outside && member.Life == LifeState.Alive && gear != null && _session.Count(gear.Stack) > 0);
                    Add("unequip", "Remove slot", x + 120, ay, 128, enabled: outside && gear != null && member.Equipment.ContainsKey(gear.Slot));
                    Add("revive", "Revive", x + 256, ay, 112, enabled: outside && member.Life == LifeState.Downed && _session.Count("medicine") > 0);
                    Add("dismiss", "Part ways", x + 376, ay, 128, enabled: outside && member.Life == LifeState.Alive && party.Count(a => a.Life == LifeState.Alive) > 1 && _session.State.Position.Distance(_session.Plan.Towns[member.HomeTown].Center) <= 28);
                    Add("abandon", "Abandon permanently", x + 512, ay, 208, enabled: outside && member.Life == LifeState.Downed, tip: "Records permanent death immediately.");
                    break;
                case "work":
                    var workers = WorkCandidates(); var orders = _session.State.Work;
                    _workerIndex = Math.Clamp(_workerIndex, 0, Math.Max(0, workers.Length - 1));
                    _workPage = Math.Clamp(_workPage, 0, Math.Max(0, (orders.Count - 1) / 6));
                    _workerPage = Math.Clamp(_workerPage, 0, Math.Max(0, (workers.Length - 1) / 6));
                    int leftWidth = width / 2 - 12, rightX = x + width / 2 + 8;
                    for (int i = 0; i < Math.Min(6, orders.Count - _workPage * 6); i++)
                    {
                        var order = orders[_workPage * 6 + i];
                        Add("work:" + order.Id, $"#{order.Id} {order.Kind} {order.Progress:0}/{order.Required:0}", x, y + 28 + i * 42, leftWidth, 36, selected: SelectedWork()?.Id == order.Id, tip: order.Status);
                    }
                    for (int i = 0; i < Math.Min(6, workers.Length - _workerPage * 6); i++)
                    {
                        var worker = workers[_workerPage * 6 + i];
                        Add("worker:" + worker.Id, worker.Name + " / " + worker.Job, rightX, y + 28 + i * 42, leftWidth, 36, selected: workers[_workerIndex % workers.Length].Id == worker.Id, tip: $"Fatigue {worker.Body.Fatigue:0}; wounds {worker.Body.Wounds}. One order per person.");
                    }
                    Add("workpage:-1", "Previous", x, y + 288, 112, enabled: _workPage > 0); Add("workpage:1", "Next", x + 120, y + 288, 96, enabled: (1 + _workPage) * 6 < orders.Count);
                    Add("workerpage:-1", "Previous", rightX, y + 288, 112, enabled: _workerPage > 0); Add("workerpage:1", "Next", rightX + 120, y + 288, 96, enabled: (1 + _workerPage) * 6 < workers.Length);
                    string assignment = _session.CommandBlockReason(new(WorldCommandKind.AssignWork, ActorId: workers.ElementAtOrDefault(_workerIndex)?.Id ?? 0, WorkId: SelectedWork()?.Id ?? 0));
                    Add("assign", "Assign selected worker", x, box.Bottom - 88, 256, enabled: assignment.Length == 0, reason: assignment);
                    Add("cancel-work", "Cancel and refund", x + 268, box.Bottom - 88, 224, enabled: SelectedWork() != null);
                    break;
                case "craft":
                    var recipes = CraftRecipes();
                    _craftPage = Math.Clamp(_craftPage, 0, Math.Max(0, (recipes.Length - 1) / 4));
                    for (int row = 0; row < Math.Min(4, recipes.Length - _craftPage * 4); row++)
                    {
                        var recipe = recipes[_craftPage * 4 + row];
                        Add("craft:" + recipe.Id, recipe.Name + " / " + recipe.Work + " work", x, y + 42 + row * 74, Math.Min(width, 640), 38, _session.CommandBlockReason(new(WorldCommandKind.Craft, Item: recipe.Id)).Length == 0,
                            tip: string.Join(", ", recipe.Cost.Select(c => c.Value + " " + _session.Content.Get<ItemDefinition>(c.Key).Name)));
                    }
                    if (recipes.Length > 4)
                    {
                        Add("craftpage:-1", "Previous", x, box.Bottom - 88, 112, enabled: _craftPage > 0);
                        Add("craftpage:1", "Next", x + 120, box.Bottom - 88, 96, enabled: (_craftPage + 1) * 4 < recipes.Length);
                    }
                    break;
                case "actions":
                    string[] actionIds = ["rest", "wait:5", "wait:300", "forage", "tool:repair", "storage", "side-arc", "delivery"];
                    string[] actionLabels = ["Rest at camp / 1 hour", "Wait / 1 minute", "Wait / 1 hour", "Gather food", "Repair a structure", "Open nearby storage", "Present side-arc evidence", "Deliver requested food"];
                    for (int i = 0; i < actionIds.Length; i++) Add(actionIds[i], actionLabels[i], x + i % 2 * (width / 2 + 4), y + i / 2 * 54, width / 2 - 8, 42, DisabledControlReason(actionIds[i]).Length == 0);
                    break;
                case "discovery":
                    if (_session.State.Discoveries.FirstOrDefault(d => d.Id == _discoveryId) is { } discovery)
                    {
                        bool guarded = _session.DiscoveryGuarded(discovery);
                        var cache = _session.State.Loot.FirstOrDefault(c => c.Id == discovery.Id);
                        Add("discovery-loot", cache?.Taken == true ? "Supplies already recovered" : "Open the supplies", x, box.Bottom - 96, 280, 40, cache != null && !cache.Taken && !guarded,
                            reason: guarded ? "Defeat the living guardians before claiming these supplies." : "No unclaimed supplies remain.");
                        if (guarded) Add("discovery-fight", "Approach a guarding foe", x + 292, box.Bottom - 96, 280, 40);
                        else if (_session.Content.Get<DiscoveryDefinition>(discovery.Definition).Action == "camp") Add("rest", "Rest here / 1 hour", x + 292, box.Bottom - 96, 280, 40, _session.CommandBlockReason(new(WorldCommandKind.Camp)).Length == 0);
                        Add("panel:map", "Show known places", x, box.Bottom - 146, 280, 36);
                        Add("mark-linked-site", "Mark the linked dungeon", x + 292, box.Bottom - 146, 280, 36);
                    }
                    break;
                case "loot":
                    if (_session.State.Loot.FirstOrDefault(c => c.Position == _lootPosition && !c.Taken) is { } chest)
                    {
                        var members = _session.Party.ToArray(); int cw = (width - 8 * (members.Length - 1)) / members.Length;
                        for (int i = 0; i < members.Length; i++) Add("party:" + i, members[i].Name, x + i * (cw + 8), y + 54, cw, 32, selected: SelectedMember().Id == members[i].Id);
                        for (int i = 0; i < chest.Choices.Length; i++)
                        {
                            var item = _session.Content.Get<ItemDefinition>(chest.Choices[i]);
                            Add("claim:" + item.Id, "Choose " + item.Name, x, y + 124 + i * 72, 300, 48, tip: item.Description);
                        }
                    }
                    break;
                case "spoils": Add("panel:inventory", "Manage party and equipment", x, box.Bottom - 96, 336, 40); break;
                case "combat-log":
                    Add("combat-history:6", "Older events", x, box.Bottom - 88, 176, enabled: _combatLogOffset + 6 < _session.State.CombatEvents.Count);
                    Add("combat-history:-6", "Newer events", x + 188, box.Bottom - 88, 176, enabled: _combatLogOffset > 0); break;
                case "map":
                    var places = KnownPlaces(); _placesPage = Math.Clamp(_placesPage, 0, Math.Max(0, (places.Length - 1) / 6));
                    for (int i = 0; i < Math.Min(6, places.Length - _placesPage * 6); i++)
                    {
                        var place = places[_placesPage * 6 + i];
                        Add("waypoint:" + place.Id, place.Name, box.Right - 320, y + 52 + i * 48, 280, 40, selected: _waypoint == place.Position,
                            tip: $"{place.Position.X}, {place.Position.Y}; {WorldSession.Direction(_session.State.Position, place.Position)}, {place.Position.Distance(_session.State.Position with { Floor = 0 })} surface tiles. Marks a destination; terrain remains unexplored.");
                    }
                    Add("placespage:-1", "Previous", box.Right - 320, box.Bottom - 66, 112, enabled: _placesPage > 0);
                    Add("placespage:1", "Next", box.Right - 200, box.Bottom - 66, 96, enabled: (_placesPage + 1) * 6 < places.Length);
                    break;
                case "storage":
                    var contents = StoredItems(); var storedStacks = StorageItems(); _storagePage = Math.Clamp(_storagePage, 0, Math.Max(0, (storedStacks.Length - 1) / 6));
                    for (int i = 0; i < Math.Min(6, storedStacks.Length - _storagePage * 6); i++)
                    {
                        string stack = storedStacks[_storagePage * 6 + i];
                        Add("store:" + stack, "Store 1", box.Right - 260, y + 32 + i * 44, 104, 36, _session.Count(stack) > 0);
                        Add("take:" + stack, "Take 1", box.Right - 144, y + 32 + i * 44, 104, 36, contents.GetValueOrDefault(stack) > 0);
                    }
                    Add("storagepage:-1", "Previous", x, box.Bottom - 88, 112, enabled: _storagePage > 0); Add("storagepage:1", "Next", x + 120, box.Bottom - 88, 96, enabled: (_storagePage + 1) * 6 < storedStacks.Length);
                    break;
                case "journal": Add("history:6", "Older history", x, box.Bottom - 88, 176); Add("history:-6", "Newer history", x + 188, box.Bottom - 88, 176, enabled: _journalOffset > 0); break;
                case "inspector":
                    for (int i = 0; i < 3; i++) Add("inspect:" + i, new[] { "Raw", "Final", "Repairs" }[i], x + i * 120, box.Bottom - 66, 112, selected: i == _inspectStage);
                    break;
            }
            return;
        }
        if (_session.IsInCombat)
        {
            Add("panel:journal", "Tactics", _map.Right - 284, _map.Top + 12, 92); Add("panel:combat-log", "Log", _map.Right - 184, _map.Top + 12, 80); Add("panel:inventory", "Party", _map.Right - 96, _map.Top + 12, 84);
            var battle = _session.State.Battle!;
            if (battle.Active.Friendly)
            {
                var skills = _session.ActiveSkills(); int bw = (_map.Width - 16) / 3;
                for (int i = 0; i < skills.Count; i++) Add("skill:" + i, skills[i].Name + (skills[i].Mana > 0 ? $" / {skills[i].Mana} MP" : ""), _map.X + i % 3 * (bw + 8), h - 94 + i / 3 * 44, bw, 36,
                    _session.SkillUnavailable(i).Length == 0, i == _skill, skills[i].Description);
            }
            return;
        }
        string[] barIds = ["interact", "panel:inventory", "panel:journal", "panel:map", "panel:actions", "panel:build", "tool:dig", "panel:work", "panel:craft", "stop"];
        string[] barLabels = ["Interact", "Party", "Journal", "World map", "Actions", "Build", "Dig", "Work", "Craft", "Stop"];
        int buttonWidth = (_map.Width - 32) / 5;
        for (int i = 0; i < barIds.Length; i++) Add(barIds[i], barLabels[i], _map.X + i % 5 * (buttonWidth + 8), h - 94 + i / 5 * 44, buttonWidth, 36, selected: barIds[i] == "tool:" + _tool);
        Add("zoom:out", "-", _map.Right - 172, _map.Top + 10, 36, 32, tip: "Zoom out; exploration keeps readable tiles.");
        Add("zoom:in", "+", _map.Right - 128, _map.Top + 10, 36, 32);
        Add("zoom:default", "View", _map.Right - 84, _map.Top + 10, 76, 32, tip: "Restore the RPG exploration camera.");
    }
    private void ActivateControl(string id)
    {
        string argument = id.Contains(':', StringComparison.Ordinal) ? id[(id.IndexOf(':') + 1)..] : "";
        string action = id.Split(':', 2)[0];
        if (action == "panel") { Toggle(argument); return; }
        if (action == "zoom") { if (argument == "default") WorldCameraScale.Apply(_camera, reset: true); else Zoom(argument == "in" ? 1 : -1); return; }
        if (action == "tool") { StopTravel(); _panel = ""; _tool = _tool == argument ? "" : argument; _notice = "Click adjacent terrain to " + _tool + ". Right-click cancels."; return; }
        switch (action)
        {
            case "close": ClosePanel(); break;
            case "pause":
                _session.Paused = !_session.Paused; _panel = "";
                if (_session.Paused) StopTravel(); _notice = _session.Paused ? "Paused. Click a destination to travel again." : "Time is running."; break;
            case "save": TrySave(); break;
            case "load": TryLoad(); break;
            case "stop": StopTravel(); _tool = ""; _notice = "Travel stopped."; break;
            case "interact": Interact(); break;
            case "build": _build = Enum.Parse<Material>(argument); _panel = ""; _tool = "build"; StopTravel(); _notice = "Click adjacent ground to build " + _build.ToString().ToLowerInvariant() + "."; break;
            case "conversation": PersonAction(_session.Content.Get<InteractionDefinition>(argument)); break;
            case "discovery-loot": OpenLoot(_session.State.Discoveries.Single(d => d.Id == _discoveryId).Position); break;
            case "discovery-fight":
                var foe = _session.State.Discoveries.Single(d => d.Id == _discoveryId).Enemies.Select(id => _session.State.Actors.Single(a => a.Id == id)).First(a => a.Life == LifeState.Alive);
                ClosePanel(); var approach = WorldMouseNavigation.Plan(_session, foe.Position); _pendingInteraction = approach.Interaction;
                foreach (var step in approach.Path) _route.Enqueue(step);
                if (approach.Reachable) { _session.Paused = false; FinishApproach(); } else _notice = approach.Reason; break;
            case "claim": if (Execute(new(WorldCommandKind.Loot, _lootPosition, Item: argument))) { _panel = "inventory"; _packItem = Array.FindIndex(EquipmentChoices(), i => i.Id == argument); _gearPage = _packItem / 8; } break;
            case "combat-history": _combatLogOffset = Math.Clamp(_combatLogOffset + int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture), 0, Math.Max(0, _session.State.CombatEvents.Count - 1)); break;
            case "placespage": _placesPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "waypoint": _waypoint = KnownPlaces().Single(p => p.Id == argument).Position; _notice = "Destination marked. Follow its banner; explore ground along the way."; break;
            case "mark-linked-site": _waypoint = _session.Plan.Sites[_session.State.Discoveries.Single(d => d.Id == _discoveryId).SiteId].Center; _notice = "Dungeon destination marked. It is accessible now, with its existing dangers."; break;
            case "buy": case "sell": Execute(new(action == "buy" ? WorldCommandKind.Buy : WorldCommandKind.Sell, Item: argument)); break;
            case "party": _packMember = int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "gearpage": _gearPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); _packItem = _gearPage * 8; break;
            case "craftpage": _craftPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "interactionpage": _interactionPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "gear": _packItem = int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "equip":
                if (EquipmentChoices().ElementAtOrDefault(_packItem) is { } item) Execute(new(WorldCommandKind.Equip, ActorId: SelectedMember().Id, Item: item.Id)); break;
            case "unequip":
                if (EquipmentChoices().ElementAtOrDefault(_packItem) is { } equipped) Execute(new(WorldCommandKind.Unequip, ActorId: SelectedMember().Id, Item: equipped.Slot)); break;
            case "revive": case "dismiss": case "abandon":
                Execute(new(action == "revive" ? WorldCommandKind.Revive : action == "dismiss" ? WorldCommandKind.Dismiss : WorldCommandKind.Abandon, ActorId: SelectedMember().Id)); break;
            case "work": _workIndex = _session.State.Work.FindIndex(o => o.Id == long.Parse(argument, System.Globalization.CultureInfo.InvariantCulture)); break;
            case "worker": _workerIndex = Array.FindIndex(WorkCandidates(), a => a.Id == long.Parse(argument, System.Globalization.CultureInfo.InvariantCulture)); break;
            case "workpage": _workPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "workerpage": _workerPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "assign":
                if (SelectedWork() is { } order && WorkCandidates().ElementAtOrDefault(_workerIndex) is { } worker) Execute(new(WorldCommandKind.AssignWork, ActorId: worker.Id, WorkId: order.Id)); break;
            case "cancel-work": if (SelectedWork() is { } pending) Execute(new(WorldCommandKind.CancelWork, WorkId: pending.Id)); break;
            case "craft":
                if (Execute(new(WorldCommandKind.Craft, Item: argument))) { _panel = ""; _session.Paused = false; } break;
            case "wait": _panel = ""; StopTravel(); Execute(new(WorldCommandKind.Wait, Amount: int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture))); break;
            case "rest": _panel = ""; StopTravel(); Execute(new(WorldCommandKind.Camp)); break;
            case "forage": _panel = ""; if (Execute(new(WorldCommandKind.Forage))) _session.Paused = false; break;
            case "side-arc": Execute(new(WorldCommandKind.ResolveArc)); break;
            case "delivery": Execute(new(WorldCommandKind.CompleteDelivery)); break;
            case "storage":
                var cells = new[] { _session.State.Position }.Concat(WorldSession.Directions.Select(d => _session.State.Position.Offset(d.X, d.Y)));
                var storage = cells.FirstOrDefault(p => _session.Chunks.At(p) == Material.Storage, new WorldPosition(-1, -1));
                if (storage.X < 0) _notice = "Approach a storage chest first."; else { _storage = storage; _panel = "storage"; } break;
            case "store": case "take": Execute(new(action == "store" ? WorldCommandKind.Store : WorldCommandKind.Take, _storage, Item: argument)); break;
            case "storagepage": _storagePage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "history": _journalOffset = Math.Clamp(_journalOffset + int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture), 0, Math.Max(0, JournalLines(PanelBox(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height).Width - 40).Length - 1)); break;
            case "inspect": _inspectStage = int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "skill":
                _skill = int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); _combatTarget = -1;
                var skill = _session.ActiveSkills()[_skill];
                if (skill.Target is TargetKind.Enemy or TargetKind.Ally) _notice = skill.Name + ": click " + (skill.Target == TargetKind.Enemy ? "an enemy" : "a companion") + " to choose the target.";
                else UseSkill();
                break;
            case "fullscreen": _graphics.ToggleFullScreen(); break;
            case "quit": TrySave(); if (_notice == "World saved.") Exit(); break;
            case "new-world": NewWorld(); break;
            case "continue-world": ClosePanel(); Execute(new(WorldCommandKind.Continue)); break;
        }
    }
    private void DrawMouseControls(int w, int h)
    {
        BuildMouseControls(w, h);
        if ((_session.State.Endgame.Victory || _session.State.AwaitingContinuation) && _panel != "game")
        {
            var box = new Rectangle(60, h / 2 - 110, w - 120, 220); Fill(box, _session.State.Endgame.Victory ? Panel : new Color(44, 27, 31));
            Text(_session.State.Endgame.Victory ? "EXPEDITION COMPLETE" : "THE EXPEDITION HAS ENDED", box.X + 24, box.Y + 24, Gold, 2);
            string message = _session.State.Endgame.Victory ? _session.FinalBoss.Name + " has fallen. Your party ended this world's final threat."
                : _session.DeathPending ? "Save the recorded deaths before continuing. The Save button retries." : "The party is permanently dead. Their belongings remain where they fell.";
            Wrap(message, box.X + 24, box.Y + 76, box.Width - 48);
        }
        var hot = WorldMouseControl.Hit(_controls, _mouse.Position);
        foreach (var control in _controls)
        {
            var r = control.Bounds;
            Fill(r, control.Selected ? new(69, 62, 42) : ReferenceEquals(control, hot) && control.Enabled ? new(53, 65, 69) : new(35, 43, 48));
            Color edge = control.Selected ? Gold : control.Enabled ? new(78, 91, 94) : new(47, 54, 58);
            Fill(new(r.X, r.Bottom - 2, r.Width, 2), edge);
            int characters = Math.Max(1, (r.Width - 14) / 8); string label = Short(control.Label, characters);
            Text(label, (int)(r.Center.X - SpriteLibrary.Measure(label) / 2), r.Center.Y - 6, control.Enabled ? control.Selected ? Gold : Ink : Muted * .55f);
        }
        int ny = h - 136;
        Fill(new(_map.X, ny, _map.Width, 36), new(16, 22, 27));
        string notice = hot is { Enabled: false } ? hot.DisabledReason : hot is { Tip.Length: > 0 } ? hot.Tip : _notice;
        if (hot == null && _waypoint is { } destination) notice = "Waypoint: " + WorldSession.Direction(_session.State.Position, destination) + ", " + destination.Distance(_session.State.Position with { Floor = 0 }) + " surface tiles. " + notice;
        if (_tool.Length > 0 && hot == null) notice = (_tool == "build" ? "Build " + _build : _tool) + ": click adjacent ground. Right-click cancels. " + notice;
        Wrap(Short(notice, 150), _map.X + 4, ny + 2, _map.Width - 8, hot != null ? Gold : Ink);
    }
    private void WriteUiReport(int w, int h)
    {
        if (_options.UiReport is not { } path) return;
        static object Rect(Rectangle r) => new { x = r.X, y = r.Y, width = r.Width, height = r.Height };
        string serialized = JsonSerializer.Serialize(new
        {
            width = w, height = h, panel = _panel, tool = _tool, paused = _session.Paused, frozen = _session.Frozen,
            focused = IsActive, pointer = new { x = _mouse.X, y = _mouse.Y },
            tick = _session.State.Tick, position = new { x = _session.State.Position.X, y = _session.State.Position.Y, floor = _session.State.Position.Floor },
            camera = new { x = _camera.Center.X, y = _camera.Center.Y, cellSize = _camera.CellSize }, map = Rect(_map),
            routeCount = _route.Count, notice = _notice,
            hover = _hover, waypoint = _waypoint is { } mark ? new { x = mark.X, y = mark.Y, floor = mark.Floor } : null,
            discoveries = _session.State.Discoveries.Where(d => d.Known).Select(d => new { id = d.Id, name = d.Name, x = d.Position.X, y = d.Position.Y, visited = d.Visited }),
            combatFeedback = _session.State.CombatEvents.TakeLast(8),
            controls = _controls.Select(c => new { id = c.Id, label = c.Label, enabled = c.Enabled, selected = c.Selected, reason = c.DisabledReason, bounds = Rect(c.Bounds) }),
            actors = _session.State.Actors.Where(a => !a.Recruited && a.Life == LifeState.Alive && _session.Visible.Contains(a.Position)).Select(a => new { id = a.Id, kind = a.Kind.ToString(), name = a.Name, x = a.Position.X, y = a.Position.Y, floor = a.Position.Floor }),
            battle = _session.State.Battle is { } battle ? new { round = battle.Round, activeFriendly = battle.Active.Friendly,
                units = battle.Units.Select(u => new { id = u.Id, friendly = u.Friendly, alive = u.Alive, bounds = Rect(UnitRect(u)) }) } : null
        }, UiJson);
        if (serialized == _lastUiReport) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, serialized); File.Move(temporary, path, true); _lastUiReport = serialized;
    }
}
