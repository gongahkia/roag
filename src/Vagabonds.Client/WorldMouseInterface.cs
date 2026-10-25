using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.World;
using Vagabonds.Graphics;

namespace Vagabonds.Client;

internal sealed record WorldMouseControl(string Id, string Label, Rectangle Bounds, bool Enabled = true, bool Selected = false, string Tip = "", string DisabledReason = "", string? Sprite = null, string Detail = "", float Progress = -1, string? BadgeSprite = null, bool Dimmed = false)
{
    public static WorldMouseControl? Hit(IReadOnlyList<WorldMouseControl> controls, Point pointer)
    {
        for (int i = controls.Count - 1; i >= 0; i--) if (controls[i].Bounds.Contains(pointer)) return controls[i];
        return null;
    }
}
internal sealed record WorldMousePress(long Sequence, int Button, int X, int Y, string? Control);

internal sealed partial class WorldGame
{
    private readonly List<WorldMouseControl> _controls = [];
    private WorldControlCache? _controlCache;
    private WorldControlCache ControlCache => _controlCache ??= new(_controls);
    private int _workPage, _workerPage, _storagePage, _gearPage, _craftPage, _interactionPage;
    private WorldPosition _storage;
    private string _discoveryId = "";
    private WorldPosition _lootPosition;
    private int _combatLogOffset;
    private int _placesPage;
    private WorldPosition? _waypoint;
    private string _lastUiReport = "";
    private WorldMousePress? _lastMousePress;
    private static readonly JsonSerializerOptions UiJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private Rectangle PanelBox(int w, int h) => new(32, 84, w - 64, h - 202);
    private static string Short(string text, int count) => text.Length > count ? text[..Math.Max(1, count - 3)] + "..." : text;
    private WorldPosition _campPosition;
    private long _campWorker;
    private string _campRole = "cook";
    private int _campPage;
    private CampDefinition[] CampChoices() => _session.Content.Game.CampActivities.Select(_session.Content.Get<CampDefinition>).Where(d => d.Role == _campRole).ToArray();
    private int _suppliesPage;
    private ItemDefinition[] SupplyChoices() => _session.Content.All<ItemDefinition>().Where(i => i.Slot == "none").ToArray();
    private ItemDefinition[] EquipmentChoices() => _session.CarriedEquipment().ToArray();
    private Actor SelectedMember() { var party = _session.Party.ToArray(); return party[_packMember % party.Length]; }
    private long _selectedWorkId, _selectedWorkerId;
    private Actor? SelectedWorker() => _selectedWorkerId == 0 ? WorkCandidates().FirstOrDefault() : WorkCandidates().FirstOrDefault(a => a.Id == _selectedWorkerId);
    private WorkOrder? SelectedWork() => _selectedWorkId == 0 ? _session.State.Work.FirstOrDefault() : _session.State.Work.FirstOrDefault(o => o.Id == _selectedWorkId);
    private Dictionary<string, int> StoredItems() => _session.State.Chunks.FirstOrDefault(c => c.Key == _storage.Chunk)?.Storage.GetValueOrDefault(_storage.LocalIndex) ?? [];
    private string[] StorageItems() => _session.State.Inventory.Keys.Concat(StoredItems().Keys).Distinct().Where(i => _session.Count(i) > 0 || StoredItems().GetValueOrDefault(i) > 0).Order(StringComparer.Ordinal).ToArray();
    private string[] JournalLines(int width) => JournalRows(width).Select(row => row.Text).ToArray();
    private void StopTravel() { _route.Clear(); _pendingInteraction = null; _session.PartyTravelling = false; }
    private void ClosePanel() { _dialogue = null; _afterDialogue = ""; _voice?.Stop(); _contextCells = null; _panel = ""; StopTravel(); }
    private void Zoom(int direction) { _camera.ZoomAt(_map.Center.ToVector2(), direction); UpdateView(); }
    private void NewWorld()
    {
        if (_session.DeathPending) { _notice = "Save the recorded deaths before starting another world."; return; }
        var replacement = WorldSession.Create(WorldStartup.FreshSeed(), diagnostics: _session.DiagnosticSink);
        _session.Dispose(); _session = replacement; _streamPosition = null; _streamDx = _streamDy = 0; ResetCombatPresentation(); ClearWorldSelection(); if (_autoSave) _session.SavePath = _options.Save;
        _session.Paused = _options.Preview;
        _recording?.Record("new-world", new { _session.Seed }); RecordSnapshot();
        ClosePanel(); _packMember = _packItem = 0; _camera.Center = Vector2.Zero;
        _skill = 0; _cooperativePartner = _combatTarget = -1;
        _waypoint = null; _selectedWorkId = _selectedWorkerId = 0;
        _notice = "A fresh world begins. Click the ground to travel.";
    }
    private void FinishApproach()
    {
        if (_pendingInteraction is not { } intent) return;
        var actor = intent.Actor == 0 ? null : _session.State.Actors.FirstOrDefault(a => a.Id == intent.Actor && a.Life != LifeState.Dead);
        if (intent.Actor != 0 && actor == null) { StopTravel(); return; }
        if (intent.Kind == "party")
        { StopTravel(); _packMember = _session.State.Party.IndexOf(intent.Actor); _panel = "inventory"; return; }
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
        if (intent.Kind.StartsWith("situation:", StringComparison.Ordinal)) { ChooseDiscoveryApproach(cell, intent.Kind[10..]); return; }
        if (intent.Kind == "person") { _person = intent.Actor; _interactionPage = 0; Execute(new(WorldCommandKind.Interact, ActorId: intent.Actor)); _panel = "person"; _dialogue = null; _afterDialogue = ""; EnsureConversation(); }
        else if (intent.Kind is "enemy" or "actor") Execute(new(WorldCommandKind.Interact, ActorId: intent.Actor));
        else if (intent.Kind == "loot") OpenLoot(cell);
        else if (intent.Kind == "discovery")
        {
            if (Execute(new(WorldCommandKind.Discover, cell))) { _discoveryId = _session.State.Discoveries.Single(d => d.Position == cell).Id; _panel = "discovery"; }
        }
        else if (intent.Kind == "storage") { _storage = cell; _storagePage = 0; _panel = "storage"; }
        else if (intent.Kind == "rest") Execute(new(WorldCommandKind.Camp));
        else if (intent.Kind == "craft") _panel = "craft";
        else if (intent.Kind == "camp") { _campPosition = cell; _campWorker = _session.State.Party[0]; _panel = "camp"; }
        else if (intent.Kind == "stairs") Execute(new(WorldCommandKind.Interact, cell));
    }
    private void ChooseDiscoveryApproach(WorldPosition cell, string method)
    {
        if (!Execute(new(WorldCommandKind.ApproachDiscovery, cell, Item: method))) return;
        if (_session.State.Work.Any(w => w.Target == cell && w.Kind == WorkKind.Discovery)) { _panel = "work"; _session.Paused = false; }
        else { _discoveryId = _session.State.Discoveries.Single(d => d.Position == cell).Id; _panel = "discovery"; }
    }
    private void BuildMouseControls(int w, int h)
    {
        ControlCache.Begin();
        void Add(string id, string label, int x, int y, int width, int height = 34, bool enabled = true, bool selected = false, string tip = "", string reason = "", string? sprite = null, string detail = "", float progress = -1, string? badge = null, bool dimmed = false)
            => ControlCache.Add(id, label, new(x, y, width, height), enabled, selected, tip, enabled ? "" : reason.Length > 0 ? reason : DisabledControlReason(id), sprite, detail, progress, badge, dimmed);
        bool ended = !Playback.Busy && (_session.State.Endgame.Victory || _session.State.AwaitingContinuation);
        Add("pause", _session.Paused ? "Play" : "Pause", w - 376, 16, 84, enabled: !ended && !CombatStageVisible, tip: "Pause stops travel. Work and Party stay open while time runs. Conversations remain paused.");
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
                    string[] ids = ["panel:guide", "panel:inspector", "zoom:default", "fullscreen", "mark-moment", "quit"];
                    string[] labels = ["How to play", "World inspector", "Reset exploration zoom", "Toggle fullscreen", "Mark this moment", "Save and quit"];
                    for (int i = 0; i < ids.Length; i++) Add(ids[i], labels[i], x, y + i * 46, Math.Min(360, width), 40,
                        enabled: ids[i] != "mark-moment" || _recording is { Error: null }, reason: "Session recording is disabled or unavailable.");
                    break;
                case "person": case "camp-story":
                    EnsureConversation();
                    Add("voice-volume", $"Voice: {_voiceVolume:P0}", box.Right - 450, box.Top + 12, 140, tip: _voice?.Unavailable == true ? "No audio device available. Dialogue remains readable." : "Cycle speech volume; 0% mutes voices.");
                    Add("speech-motion", _reducedMotion ? "Motion: Reduced" : "Motion: Full", box.Right - 300, box.Top + 12, 188);
                    var speech = SpeechBox(box);
                    if (_dialogue?.Finished != true)
                    {
                        Add("dialogue-next", _dialogue?.Speaking == true ? "Show full line" : "Continue", speech.Right - 164, speech.Bottom - 36, 152, 30);
                        break;
                    }
                    if (_panel == "camp-story") { Add("panel:camp", "Back to duties", x, box.Bottom - 88, 192); break; }
                    var interactions = _session.Interactions(_person);
                    _interactionPage = Math.Clamp(_interactionPage, 0, Math.Max(0, (interactions.Count - 1) / 6));
                    for (int row = 0; row < Math.Min(6, interactions.Count - _interactionPage * 6); row++)
                    {
                        int i = row; var interaction = interactions[_interactionPage * 6 + row]; string cost = string.Join(", ", interaction.Cost.Select(c => c.Value + " " + _session.Content.Get<ItemDefinition>(c.Key).Name));
                        Add("conversation:" + interaction.Id, interaction.Name + (cost.Length > 0 ? " / " + cost : ""), x + i % 2 * (width / 2 + 4), box.Bottom - 140 + i / 2 * 40, width / 2 - 8, 36,
                            _session.InteractionBlockReason(_person, interaction.Id).Length == 0, tip: interaction.Description);
                    }
                    if (interactions.Count > 6)
                    {
                        Add("interactionpage:-1", "Previous", speech.Right - 240, speech.Bottom - 36, 112, enabled: _interactionPage > 0);
                        Add("interactionpage:1", "Next", speech.Right - 120, speech.Bottom - 36, 96, enabled: (_interactionPage + 1) * 6 < interactions.Count);
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
                case "camp":
                    var campParty = _session.Party.ToArray(); int campCardWidth = (width - 8 * (campParty.Length - 1)) / campParty.Length;
                    if (!campParty.Any(a => a.Id == _campWorker)) _campWorker = campParty[0].Id;
                    for (int i = 0; i < campParty.Length; i++)
                        Add("campworker:" + campParty[i].Id, campParty[i].Name, x + i * (campCardWidth + 8), y, campCardWidth, 64, campParty[i].Life == LifeState.Alive, selected: _campWorker == campParty[i].Id,
                            sprite: campParty[i].Sprite, tip: _session.PreparationDescription(campParty[i]), detail: _session.State.Work.Any(w => w.Worker == campParty[i].Id) ? "Assigned to work" : "Available for duty");
                    string[] campRoles = ["cook", "treat", "watch", "stories"], roleLabels = ["Cooking", "Treatment", "Watch", "Stories"];
                    for (int i = 0; i < campRoles.Length; i++) Add("camprole:" + campRoles[i], roleLabels[i], x + i * ((width - 24) / 4 + 8), y + 74, (width - 24) / 4, 32, selected: _campRole == campRoles[i]);
                    var duties = CampChoices(); _campPage = Math.Clamp(_campPage, 0, Math.Max(0, (duties.Length - 1) / 3));
                    for (int i = 0; i < Math.Min(3, duties.Length - _campPage * 3); i++)
                    {
                        var duty = duties[_campPage * 3 + i]; string unavailable = _session.CampActivityReason(_campPosition, duty.Id, _campWorker);
                        Add("campduty:" + duty.Id, duty.Name, x, y + 122 + i * 54, width, 48, unavailable.Length == 0, sprite: duty.Sprite,
                            detail: duty.Cost.Count == 0 ? "Time and effort" : string.Join(", ", duty.Cost.Select(c => c.Value + " " + _session.Content.Get<ItemDefinition>(c.Key).Name)),
                            tip: duty.Description + " " + duty.Work + " work. Effects reach companions within six tiles.", reason: unavailable);
                    }
                    Add("camppage:-1", "Prev", x, y + 290, 72, enabled: _campPage > 0);
                    Add("camppage:1", "Next", x + 80, y + 290, 72, enabled: (_campPage + 1) * 3 < duties.Length);
                    Add("panel:work", "Manage duties", x + 160, y + 290, 152);
                    Add("campstory", "Camp conversation", x + 320, y + 290, 200, enabled: _session.State.LastCampStory is { } story && story.Camp == _campPosition);
                    break;
                case "supplies":
                    var supplies = SupplyChoices(); _suppliesPage = Math.Clamp(_suppliesPage, 0, Math.Max(0, (supplies.Length - 1) / 12));
                    for (int row = 0; row < Math.Min(12, supplies.Length - _suppliesPage * 12); row++)
                    {
                        var supply = supplies[row + _suppliesPage * 12]; int sw = (width - 8) / 2;
                        Add("supply:" + supply.Id, supply.Name + " / x" + _session.Count(supply.Stack), x + row % 2 * (sw + 8), y + 34 + row / 2 * 48, sw, 44,
                            sprite: supply.Sprite, detail: supply.Description, tip: supply.Description, dimmed: _session.Count(supply.Stack) == 0);
                    }
                    Add("supplypage:-1", "Previous", x, box.Bottom - 88, 112, enabled: _suppliesPage > 0);
                    Add("supplypage:1", "Next", x + 120, box.Bottom - 88, 96, enabled: (_suppliesPage + 1) * 12 < supplies.Length);
                    Add("panel:inventory", "Party equipment", x + 224, box.Bottom - 88, 192); break;
                case "inventory":
                    int ready = _session.Party.Sum(ReadyDevelopmentCount);
                    Add("panel:progression", "Developments" + (ready > 0 ? " / " + ready : ""), box.Right - 464, box.Y + 12, 184,
                        tip: "Personal combat traits and permanent class specialisations at levels two and four.");
                    Add("panel:supplies", "Shared pack", box.Right - 272, box.Y + 12, 152);
                    var party = _session.Party.ToArray(); var member = SelectedMember(); var items = EquipmentChoices();
                    int card = (width - 8 * (party.Length - 1)) / party.Length;
                    for (int i = 0; i < party.Length; i++) Add("party:" + i, party[i].Name, x + i * (card + 8), y, card, 72, selected: party[i].Id == member.Id,
                        tip: _session.Class(party[i]).Name + ": " + string.Join(", ", _session.Class(party[i]).Abilities.Select(id => _session.Content.Get<AbilityDefinition>(id).Name)) + ". Preparations: " + _session.PreparationDescription(party[i]),
                        sprite: party[i].Sprite, detail: $"L{party[i].Level} / HP {party[i].Health}/{party[i].MaxHealth}", progress: (float)party[i].Health / Math.Max(1, party[i].MaxHealth), dimmed: party[i].Life != LifeState.Alive);
                    _gearPage = Math.Clamp(_gearPage, 0, Math.Max(0, (items.Length - 1) / 8));
                    for (int row = 0; row < Math.Min(8, items.Length - _gearPage * 8); row++)
                    {
                        int i = row + _gearPage * 8; bool worn = member.Equipment.GetValueOrDefault(items[i].Slot)?.Item == items[i].Id;
                        var comparison = _session.CompareGear(member, items[i].Id);
                        int iw = (width - 8) / 2;
                        Add("gear:" + i, items[i].Name + (worn ? " / worn" : " / x" + _session.Count(items[i].Stack)), x + row % 2 * (iw + 8), y + 164 + row / 2 * 46, iw, 42, selected: i == _packItem,
                            tip: $"{items[i].Slot}: HP+{items[i].Health} Power+{items[i].Power} Armour+{items[i].Armor} Speed+{items[i].Speed}. Replaces " + comparison.Previous + ": " + GearDelta(comparison) + ". " + _session.EquipmentDescription(items[i]),
                            sprite: items[i].Sprite, detail: items[i].Slot + " / " + (worn ? "Equipped" : _session.Count(items[i].Stack) == 0 ? "None in pack" : "In pack"), dimmed: !worn && _session.Count(items[i].Stack) == 0);
                    }
                    if (items.Length > 8)
                    {
                        Add("gearpage:-1", "Prev", box.Right - 172, y + 128, 64, 28, _gearPage > 0);
                        Add("gearpage:1", "Next", box.Right - 100, y + 128, 64, 28, (_gearPage + 1) * 8 < items.Length);
                    }
                    var gear = items.ElementAtOrDefault(_packItem % Math.Max(1, items.Length)); bool outside = !_session.IsInCombat;
                    int ay = box.Bottom - 88;
                    Add("equip", "Equip", x, ay, 112, enabled: outside && member.Life == LifeState.Alive && gear != null && _session.Count(gear.Stack) > 0);
                    Add("unequip", "Remove slot", x + 120, ay, 128, enabled: outside && gear != null && member.Equipment.ContainsKey(gear.Slot));
                    Add("revive", "Revive", x + 256, ay, 112, enabled: outside && member.Life == LifeState.Downed && _session.Count("medicine") > 0);
                    Add("dismiss", "Part ways", x + 376, ay, 128, enabled: outside && member.Life == LifeState.Alive && party.Count(a => a.Life == LifeState.Alive) > 1 && _session.State.Position.Distance(_session.Plan.Towns[member.HomeTown].Center) <= 28);
                    Add("abandon", "Abandon permanently", x + 512, ay, 208, enabled: outside && member.Life == LifeState.Downed, tip: "Records permanent death immediately.");
                    break;
                case "progression": AddProgressionControls(box); break;
                case "work":
                    var workers = WorkCandidates(); var orders = _session.State.Work;
                    if (_selectedWorkId == 0) _selectedWorkId = orders.FirstOrDefault()?.Id ?? 0;
                    if (_selectedWorkerId == 0) _selectedWorkerId = workers.FirstOrDefault()?.Id ?? 0;
                    _workPage = Math.Clamp(_workPage, 0, Math.Max(0, (orders.Count - 1) / 5));
                    _workerPage = Math.Clamp(_workerPage, 0, Math.Max(0, (workers.Length - 1) / 5));
                    int leftWidth = width / 2 - 12, rightX = x + width / 2 + 8;
                    for (int i = 0; i < Math.Min(5, orders.Count - _workPage * 5); i++)
                    {
                        var order = orders[_workPage * 5 + i];
                        var assigned = _session.State.Actors.FirstOrDefault(a => a.Id == order.Worker);
                        Add("work:" + order.Id, WorkName(order) + " / " + (assigned?.Name ?? "Waiting for worker"), x, y + 28 + i * 42, leftWidth, 38, selected: SelectedWork()?.Id == order.Id,
                            tip: $"#{order.Id} at {order.Target.X}, {order.Target.Y}: {order.Status}. {order.Progress:0}/{order.Required:0} work. " + _session.WorkSupplyDescription(order) + (order.Output == null ? " " + _session.WorkOutputDescription(order) : ""),
                            sprite: WorkSprite(order), detail: order.Suspended ? "Suspended" : order.Status, progress: (float)(order.Progress / Math.Max(1, order.Required)), badge: assigned?.Sprite);
                    }
                    for (int i = 0; i < Math.Min(5, workers.Length - _workerPage * 5); i++)
                    {
                        var worker = workers[_workerPage * 5 + i];
                        Add("worker:" + worker.Id, worker.Name, rightX, y + 28 + i * 42, leftWidth, 38, selected: _selectedWorkerId == worker.Id,
                            tip: $"{worker.Job}. HP {worker.Health}/{worker.MaxHealth}; hunger {worker.Body.Hunger:0}; fatigue {worker.Body.Fatigue:0}; wounds {worker.Body.Wounds}. One order per person.",
                            sprite: worker.Sprite, detail: (worker.Recruited ? "Party / " : "Resident / ") + worker.Job + (_session.State.Work.Any(o => o.Worker == worker.Id) ? " / busy" : ""));
                    }
                    Add("workpage:-1", "Previous", x, y + 244, 112, enabled: _workPage > 0); Add("workpage:1", "Next", x + 120, y + 244, 96, enabled: (1 + _workPage) * 5 < orders.Count);
                    Add("workerpage:-1", "Previous", rightX, y + 244, 112, enabled: _workerPage > 0); Add("workerpage:1", "Next", rightX + 120, y + 244, 96, enabled: (1 + _workerPage) * 5 < workers.Length);
                    string assignment = _session.CommandBlockReason(new(WorldCommandKind.AssignWork, ActorId: SelectedWorker()?.Id ?? 0, WorkId: SelectedWork()?.Id ?? 0));
                    Add("assign", "Assign selected worker", x, box.Bottom - 104, 256, enabled: assignment.Length == 0, reason: assignment);
                    Add("cancel-work", "Cancel and release", x + 268, box.Bottom - 104, 224, enabled: SelectedWork() != null, tip: "Return nearby supplies; remote materials remain in a recoverable supply bundle.");
                    var selectedOrder = SelectedWork();
                    string delivery = selectedOrder?.DeliveryStorage is { } depot ? $"To: {depot.X}, {depot.Y}" : "To: Pack";
                    if (width / 2 - 236 >= 96) Add("work-delivery", delivery, rightX + 228, y + 244, width / 2 - 236,
                        enabled: selectedOrder != null && selectedOrder.Yield.Values.Any(v => v > 0),
                        tip: "Cycle output delivery between the shared pack and observed rally storage. A full or blocked destination keeps its cargo with the worker.",
                        reason: "Select work that produces material to deliver.");
                    Add("work-setting:" + (selectedOrder?.Suspended == true ? "resume" : "suspend"), selectedOrder?.Suspended == true ? "Resume" : "Suspend", x, box.Bottom - 60, 140, enabled: selectedOrder != null);
                    Add("work-setting:priority", "Priority: " + new[] { "Low", "Normal", "High" }[selectedOrder?.Priority ?? 1], x + 152, box.Bottom - 60, 200, enabled: selectedOrder != null);
                    Add("work-setting:automatic", "Automatic worker", x + 364, box.Bottom - 60, 220, enabled: selectedOrder != null);
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
                    string[] actionIds = ["wait:5", "wait:300", "forage"];
                    string[] actionLabels = ["Wait / 1 minute", "Wait / 1 hour", "Gather food"];
                    for (int i = 0; i < actionIds.Length; i++) Add(actionIds[i], actionLabels[i], x + i % 2 * (width / 2 + 4), y + i / 2 * 54, width / 2 - 8, 42, DisabledControlReason(actionIds[i]).Length == 0);
                    break;
                case "discovery":
                    if (_session.State.Discoveries.FirstOrDefault(d => d.Id == _discoveryId) is { } discovery)
                    {
                        bool guarded = _session.DiscoveryGuarded(discovery);
                        var cache = _session.State.Loot.FirstOrDefault(c => c.Id == discovery.Id);
                        if (_session.Situation(discovery) is { } situation && discovery.Outcome.Length == 0)
                        {
                            int cw = (width - (situation.Approaches.Length - 1) * 8) / situation.Approaches.Length;
                            for (int i = 0; i < situation.Approaches.Length; i++)
                            {
                                var method = situation.Approaches[i]; string reason = _session.SituationApproachReason(discovery, method.Action);
                                Add("approach:" + method.Action, method.Name, x + i * (cw + 8), box.Bottom - 208, cw, 56, reason.Length == 0,
                                    reason: reason, sprite: method.Sprite, detail: _session.ApproachCost(method), tip: method.Description + (method.Work > 0 ? $" {method.Work} work; companions can be assigned." : " Payment is immediate."));
                            }
                        }
                        Add("discovery-loot", cache?.Taken == true ? "Supplies already recovered" : "Open the supplies", x, box.Bottom - 96, 280, 40, cache != null && !cache.Taken && !guarded,
                            reason: guarded ? _session.Situation(discovery) != null ? "Resolve this place through work, negotiation, combat or tunnelling." : "Defeat the living guardians before claiming these supplies." : "No unclaimed supplies remain.");
                        if (guarded) Add("discovery-fight", _session.Situation(discovery) != null ? "Challenge the defenders" : "Approach a guarding foe", x + 292, box.Bottom - 96, 280, 40);
                        else if (_session.Content.Get<DiscoveryDefinition>(discovery.Definition).Action == "camp") Add("rest", "Rest here / 1 hour", x + 292, box.Bottom - 96, 280, 40, _session.CommandBlockReason(new(WorldCommandKind.Camp)).Length == 0);
                        if (_session.Situation(discovery) != null && discovery.Outcome.Length == 0) Add("show-bypass", "Show the tunnelling wall", x, box.Bottom - 146, 280, 36);
                        else Add("panel:map", "Show known places", x, box.Bottom - 146, 280, 36);
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
                            var item = _session.Item(chest.Choices[i]);
                            Add("claim:" + item.Id, _session.State.GeneratedEquipment.TryGetValue(item.Id, out var variant) ? _session.Content.Get<ItemDefinition>(variant.Base).Name : item.Name, x, y + 124 + i * 72, 300, 48, tip: item.Name + ". " + _session.EquipmentDescription(item), sprite: item.Sprite, detail: _session.EquipmentEffectNames(item));
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
                case "journal": Add("history:6", "Older history", x, box.Bottom - 88, 176, enabled: _journalOffset + JournalVisibleRows(box) < JournalRows(width - 64).Length); Add("history:-6", "Newer history", x + 188, box.Bottom - 88, 176, enabled: _journalOffset > 0); break;
                case "inspector":
                    for (int i = 0; i < 3; i++) Add("inspect:" + i, new[] { "Raw", "Final", "Repairs" }[i], x + i * 120, box.Bottom - 66, 112, selected: i == _inspectStage);
                    break;
            }
            return;
        }
        if (CombatStageVisible)
        {
            var area = CombatArea;
            Add("combat-speed", _combatSpeed > 1 ? "Speed 2x" : "Speed 1x", area.Right - 508, area.Top + 8, 96, 28, tip: "Change presentation speed. Outcomes and world time stay the same.");
            Add("combat-motion", _reducedMotion ? "Motion low" : "Motion full", area.Right - 404, area.Top + 8, 112, 28, tip: "Reduce travel, recoil, pulses and floating text motion.");
            Add("combat-volume", _combatVolume <= 0 ? "Sound off" : "Sound " + (int)Math.Round(_combatVolume * 100), area.Right - 284, area.Top + 8, 100, 28, tip: "Cycle battle sound volume, including mute.");
            Add("combat-skip", "Skip", area.Right - 176, area.Top + 8, 72, 28, Playback.Busy, tip: "Finish this visual sequence. The action is already resolved.", reason: "No action is playing.");
            Add("panel:combat-log", "Log", area.Right - 96, area.Top + 8, 80, 28);
            Add("panel:tactics", "Tactics", area.Right - 96, area.Top + 44, 80, 28);
            Add("panel:inventory", "Party", area.Right - 184, area.Top + 44, 80, 28);
            var battle = DisplayBattle;
            if (battle?.Active.Friendly == true)
            {
                var skills = Playback.Busy ? Playback.Skills : _session.ActiveSkills(); int columns = 4, bw = (area.Width - 24) / columns;
                for (int i = 0; i < skills.Count; i++)
                {
                    string reason = Playback.Busy ? "Wait for the current action or choose Skip." : _session.SkillUnavailable(i, skills);
                    Add("skill:" + i, skills[i].Name, area.X + i % columns * (bw + 8), h - 94 + i / columns * 44, bw, 36,
                        reason.Length == 0, Playback.Busy ? skills[i].Id == Playback.Current?.AbilityId : i == _skill,
                        skills[i].Cooperative ? _session.CooperationDescription(skills[i]) : skills[i].Description,
                        reason, sprite: skills[i].Icon, detail: skills[i].Mana > 0 ? skills[i].Mana + " MP" : "No MP cost");
                }
            }
            return;
        }
        string[] barIds = ["panel:inventory", "panel:journal", "panel:map", "panel:actions", "panel:work", "stop"];
        string[] barLabels = ["Party", "Journal", "World map", "Actions", "Work", "Stop"];
        int buttonWidth = (_map.Width - 16) / 3;
        for (int i = 0; i < barIds.Length; i++) Add(barIds[i], barLabels[i], _map.X + i % 3 * (buttonWidth + 8), h - 94 + i / 3 * 44, buttonWidth, 36);
        Add("zoom:out", "-", _map.Right - 172, _map.Top + 10, 36, 32, tip: "Zoom out; exploration keeps readable tiles.");
        Add("zoom:in", "+", _map.Right - 128, _map.Top + 10, 36, 32);
        Add("zoom:default", "View", _map.Right - 84, _map.Top + 10, 76, 32, tip: "Restore the RPG exploration camera.");
        BuildContextControls();
    }
    private void ActivateControl(string id)
    {
        _recording?.Record("ui-control", new { id, panel = _panel }, _session.State.Tick);
        string argument = id.Contains(':', StringComparison.Ordinal) ? id[(id.IndexOf(':') + 1)..] : "";
        string action = id.Split(':', 2)[0];
        if (action == "skill" && Playback.Busy) return;
        if (action == "context") { ActivateContext(argument); return; }
        if (action == "panel") { _contextCells = null; Toggle(argument); return; }
        if (action == "zoom") { if (argument == "default") WorldCameraScale.Apply(_camera, reset: true); else Zoom(argument == "in" ? 1 : -1); return; }
        switch (action)
        {
            case "combat-speed": _combatSpeed = _combatSpeed > 1 ? 1 : 2; SaveConversationSettings(); break;
            case "combat-motion": _reducedMotion = !_reducedMotion; SaveConversationSettings(); break;
            case "combat-volume": _combatVolume = _combatVolume >= .99f ? 0 : Math.Min(1, _combatVolume + .3f); _combatAudio?.Stop(); SaveConversationSettings(); break;
            case "combat-skip": Playback.Skip(); _combatAudio?.Stop(); _combatFloats.Clear(); CompleteCombatPlayback(); break;
            case "close": ClosePanel(); break;
            case "mark-moment": ClosePanel(); MarkMoment("Player marked this moment"); break;
            case "pause":
                _session.Paused = !_session.Paused;
                if (_session.Paused) StopTravel(); _notice = _session.Paused ? "Paused. Click a destination to travel again." : "Time is running."; break;
            case "save": TrySave(); break;
            case "load": TryLoad(); break;
            case "stop": StopTravel(); _session.StopPartyWorkApproach(); ClearWorldSelection(); _notice = "Travel stopped and party work suspended. Resume or cancel orders in Work."; break;
            case "dialogue-next": AdvanceDialogue(); break;
            case "voice-volume": _voiceVolume = _voiceVolume >= .99f ? 0 : Math.Min(1, (float)Math.Round(_voiceVolume * 4 + 1) / 4); _voice?.Stop(); SaveConversationSettings(); break;
            case "speech-motion": _reducedMotion = !_reducedMotion; SaveConversationSettings(); break;
            case "conversation": ChooseDialogue(_session.Content.Get<InteractionDefinition>(argument)); break;
            case "discovery-loot": OpenLoot(_session.State.Discoveries.Single(d => d.Id == _discoveryId).Position); break;
            case "approach": ChooseDiscoveryApproach(_session.State.Discoveries.Single(d => d.Id == _discoveryId).Position, argument); break;
            case "show-bypass":
                if (_session.DiscoveryBypass(_session.State.Discoveries.Single(d => d.Id == _discoveryId)) is { } wall)
                { ClosePanel(); Selection.Single(wall); _notice = "Right-click the selected side wall and choose Dig. Companions can do the work."; } break;
            case "discovery-fight":
                var foe = _session.State.Discoveries.Single(d => d.Id == _discoveryId).Enemies.Select(id => _session.State.Actors.Single(a => a.Id == id)).First(a => a.Life == LifeState.Alive);
                ClosePanel(); var fightRoute = _session.RouteToAdjacent(foe.Position);
                BeginApproach(new(fightRoute, new("enemy", foe.Position, foe.Id), _session.State.Position.Distance(foe.Position) <= 1 || fightRoute.Count > 0, "No observed approach reaches the defender.")); break;
            case "claim": if (Execute(new(WorldCommandKind.Loot, _lootPosition, Item: argument))) { _panel = "inventory"; _packItem = Array.FindIndex(EquipmentChoices(), i => i.Id == argument); _gearPage = _packItem / 8; } break;
            case "combat-history": _combatLogOffset = Math.Clamp(_combatLogOffset + int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture), 0, Math.Max(0, _session.State.CombatEvents.Count - 1)); break;
            case "placespage": _placesPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "waypoint": _waypoint = KnownPlaces().Single(p => p.Id == argument).Position; _notice = "Destination marked. Follow its banner; explore ground along the way."; break;
            case "mark-linked-site": _waypoint = _session.Plan.Sites[_session.State.Discoveries.Single(d => d.Id == _discoveryId).SiteId].Center; _notice = "Dungeon destination marked. It is accessible now, with its existing dangers."; break;
            case "buy": case "sell": Execute(new(action == "buy" ? WorldCommandKind.Buy : WorldCommandKind.Sell, Item: argument)); break;
            case "party": _packMember = int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); _selectedTalent = ""; _talentPage = 0; break;
            case "talent": _selectedTalent = argument; break;
            case "talentpage": _talentPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "learn-talent": Execute(new(WorldCommandKind.ChooseProgression, ActorId: SelectedMember().Id, Item: _selectedTalent)); break;
            case "campworker": _campWorker = long.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "camprole": _campRole = argument; _campPage = 0; break;
            case "camppage": _campPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "campduty": if (Execute(new(WorldCommandKind.CampActivity, _campPosition, ActorId: _campWorker, Item: argument))) _session.Paused = false; break;
            case "campstory":
                if (_session.State.LastCampStory is { } campStory && _session.State.Actors.FirstOrDefault(a => a.Id == campStory.Speaker) is { } speaker)
                { _person = speaker.Id; _panel = "camp-story"; ShowDialogue([new(speaker.Id, speaker.Name, speaker.Sprite, campStory.Text)]); _afterDialogue = "camp"; } break;
            case "supplypage": _suppliesPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "supply": break;
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
            case "work": _selectedWorkId = long.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "worker": _selectedWorkerId = long.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "workpage": _workPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "workerpage": _workerPage += int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "assign":
                if (SelectedWork() is { } order && SelectedWorker() is { } worker) Execute(new(WorldCommandKind.AssignWork, ActorId: worker.Id, WorkId: order.Id)); break;
            case "work-setting":
                if (SelectedWork() is { } configured) Execute(new(WorldCommandKind.ConfigureWork, WorkId: configured.Id, Item: argument == "priority" ? "priority" : argument, Amount: (configured.Priority + 1) % 3)); break;
            case "work-delivery":
                if (SelectedWork() is { } deliveryOrder)
                {
                    var destinations = _session.WorkDeliveryStorage().Where(p => _session.WorkDeliveryReason(deliveryOrder, p).Length == 0).ToArray();
                    int index = deliveryOrder.DeliveryStorage is { } current ? Array.IndexOf(destinations, current) : -1;
                    WorldPosition? next = index + 1 < destinations.Length ? destinations[index + 1] : null;
                    Execute(new(WorldCommandKind.ConfigureWork, next ?? default, WorkId: deliveryOrder.Id, Item: next == null ? "deliver-pack" : "deliver-storage"));
                }
                break;
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
            case "history": _journalOffset = Math.Clamp(_journalOffset + int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture), 0, Math.Max(0, JournalRows(PanelBox(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height).Width - 104).Length - 1)); break;
            case "inspect": _inspectStage = int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); break;
            case "skill":
                _skill = int.Parse(argument, System.Globalization.CultureInfo.InvariantCulture); _cooperativePartner = _combatTarget = -1;
                var skill = _session.ActiveSkills()[_skill];
                if (skill.Cooperative) _notice = skill.Name + ": choose a ready companion. Costs " + skill.Mana + " MP + " + skill.PartnerMana + " helper MP and both actions.";
                else if (skill.Target is TargetKind.Enemy or TargetKind.Ally) _notice = skill.Name + ": click " + (skill.Target == TargetKind.Enemy ? "an enemy" : "a companion") + " to choose the target.";
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
        if (!Playback.Busy && (_session.State.Endgame.Victory || _session.State.AwaitingContinuation) && _panel != "game")
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
            DrawControlContents(control);
        }
        int ny = h - 136;
        Fill(new(_map.X, ny, CombatStageVisible ? CombatArea.Width : _map.Width, 36), new(16, 22, 27));
        string notice = hot is { Enabled: false } ? hot.DisabledReason : hot is { Tip.Length: > 0 } ? hot.Tip : _notice;
        if (hot == null && _waypoint is { } destination) notice = "Waypoint: " + WorldSession.Direction(_session.State.Position, destination) + ", " + destination.Distance(_session.State.Position with { Floor = 0 }) + " surface tiles. " + notice;
        Wrap(Short(notice, 150), _map.X + 4, ny + 2, _map.Width - 8, hot != null ? Gold : Ink);
    }
    private void WriteUiReport(int w, int h)
    {
        if (_options.UiReport is not { } path) return;
        string serialized = BuildUiReport(w, h);
        if (serialized == _lastUiReport) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, serialized); File.Move(temporary, path, true); _lastUiReport = serialized;
    }
    private string BuildUiReport(int w, int h, bool playerView = false)
    {
        static object Rect(Rectangle r) => new { x = r.X, y = r.Y, width = r.Width, height = r.Height };
        bool playing = playerView && Playback.Busy;
        var reportedBattle = playerView ? DisplayBattle : _session.State.Battle;
        var skills = playing ? Array.Empty<Skill>() : _session.ActiveSkills();
        CombatUnit? Presented(Actor actor) => playerView && reportedBattle is { } b && b.ActorIds.IndexOf(actor.Id) is var index && index >= 0 ? b.Units[index] : null;
        return JsonSerializer.Serialize(new
        {
            seed = _session.Seed, contentHash = _session.Content.Hash, sessionId = _recording?.Id,
            width = w, height = h, panel = _panel, paused = _session.Paused, frozen = _session.Frozen,
            focused = _session.Focused, nativeFocused = IsActive, agentHold = _playtest != null && (_playtest.Holding || _playtest.NeedsObservation), pointer = new { x = _mouse.X, y = _mouse.Y },
            mouseInput = new { pressSequence = _lastMousePress?.Sequence ?? 0, lastPress = _lastMousePress,
                leftPressed = _mouse.LeftButton == ButtonState.Pressed, rightPressed = _mouse.RightButton == ButtonState.Pressed },
            tick = _session.State.Tick, position = new { x = _session.State.Position.X, y = _session.State.Position.Y, floor = _session.State.Position.Floor },
            journeyFixture = playerView ? null : _routineFarField,
            occupationFixture = playerView ? null : _occupationFixture,
            residentJourneys = playerView ? null : _session.ResidentJourneys,
            camera = new { x = _camera.Center.X, y = _camera.Center.Y, cellSize = _camera.CellSize }, map = Rect(_map),
            terrainCache = new { hits = _terrainCacheHits, rebuilds = _terrainCacheRebuilds, bypasses = _terrainCacheBypasses,
                bytes = _terrainLayer == null ? 0L : (long)_terrainLayer.Width * _terrainLayer.Height * 4 },
            textCache = new { entries = _sprites.TextLayouts.Count, characters = _sprites.TextLayouts.Characters,
                hits = _sprites.TextLayouts.Hits, misses = _sprites.TextLayouts.Misses },
            controlCache = new { hits = ControlCache.Hits, misses = ControlCache.Misses, retained = ControlCache.Retained },
            routeCount = _route.Count, notice = _notice,
            route = _route.Take(16).ToArray(), inventory = _session.State.Inventory,
            selectedSkill = _skill, cooperativePartner = _cooperativePartner,
            skills = skills.Select((skill, index) => new { index, skill.Id, skill.Name, skill.Mana,
                target = skill.Target.ToString(), skill.Cooperative, skill.PartnerMana, reason = _session.SkillUnavailable(index, skills) }),
            context = _contextCells == null ? null : new { bounds = Rect(_contextBox), building = _contextBuilding, hint = _contextHint },
            selection = Selection.Cells.Select(p => new { x = p.X, y = p.Y, floor = p.Floor }),
            selecting = _dragStart != null,
            cells = _session.Visible.Where(p => _map.Contains(TileRect(p).Center)).Select(p => new { x = p.X, y = p.Y, floor = p.Floor,
                material = _session.Chunks.At(p).ToString(), groundOpen = _session.GroundOpen(p), occupied = _session.OccupantAt(p) != null,
                clickable = WorldMouseControl.Hit(_controls, TileRect(p).Center) == null,
                prop = _session.ObjectAt(p)?.Name, resource = _session.ResourceAt(p) is { } resource ? new { id = resource.Definition.Id, available = resource.Available, sprite = resource.Sprite, recovery = resource.RecoveryDescription } : null }),
            work = _session.State.Work.Select(o => new { id = o.Id, kind = o.Kind.ToString(), x = o.Target.X, y = o.Target.Y, progress = o.Progress, partyApproach = o.PartyApproach, worker = o.Worker, status = o.Status, priority = o.Priority, suspended = o.Suspended, pinnedWorker = o.PinnedWorker,
                supplies = o.Supplies?.Select(s => new { s.Stack, s.Count, phase = s.Phase.ToString(), s.Carrier, s.Storage }), supplyDescription = _session.WorkSupplyDescription(o),
                output = o.Output, deliveryStorage = o.DeliveryStorage, outputDescription = _session.WorkOutputDescription(o) }),
            rally = _session.State.WorkRally,
            dialogue = _panel is not ("person" or "camp-story") || _dialogue == null ? null : new { speaker = _dialogue.Line?.Speaker, name = _dialogue.Line?.Name, text = _dialogue.Text, fullText = playerView ? _dialogue.Text : _dialogue.Line?.Text, speaking = _dialogue.Speaking, finished = _dialogue.Finished, volume = _voiceVolume, reducedMotion = _reducedMotion, audioUnavailable = _voice?.Unavailable ?? false, playedSyllables = _voice?.PlayedSyllables ?? 0, portraits = _session.State.Actors.Where(a => a.Id == _person || a.Id == _session.State.Party[0]).Select(a => new { id = a.Id, bounds = Rect(SpeakingPortrait(a, PanelBox(w, h), a.Id == _person)) }) },
            hover = _hover, waypoint = _waypoint is { } mark ? new { x = mark.X, y = mark.Y, floor = mark.Floor } : null,
            discoveries = _session.State.Discoveries.Where(d => d.Known).Select(d => new { id = d.Id, name = d.Name, x = d.Position.X, y = d.Position.Y, floor = d.Position.Floor, visited = d.Visited, outcome = d.Outcome, bypass = playerView ? null : _session.DiscoveryBypass(d) }),
            places = _panel == "map" ? KnownPlaces().Skip(_placesPage * 6).Take(6).Select(p => new { id = p.Id, name = p.Name, x = p.Position.X, y = p.Position.Y, floor = p.Position.Floor }) : null,
            combatFeedback = playing ? Enumerable.Empty<Vagabonds.Core.World.CombatEvent>() : _session.State.CombatEvents.TakeLast(8),
            progression = _panel == "progression" ? new { actorId = SelectedMember().Id, combatTrait = SelectedMember().CombatTrait,
                talents = SelectedMember().Talents, selected = _selectedTalent, ready = ReadyDevelopmentCount(SelectedMember()),
                choices = _session.ProgressionChoices(SelectedMember().Id).Select(t => new { id = t.Id, t.Name, t.Description, t.Level,
                    learned = SelectedMember().Talents.Contains(t.Id), reason = _session.ProgressionChoiceReason(SelectedMember().Id, t.Id) }) } : null,
            encounterReward = playing ? null : _session.State.LastEncounterReward,
            equipment = _panel == "inventory" ? EquipmentChoices().Select((item, index) => new { index, id = item.Id, name = item.Name, slot = item.Slot, count = _session.Count(item.Stack) }) : null,
            combatIntents = playing ? null : _session.State.Battle?.Units.Where(u => !u.Friendly).Select(u => BattleIntent(u.Id)),
            combatPresentation = new { busy = Playback.Busy, phase = Playback.Phase, progress = Playback.Progress, phaseProgress = Playback.PhaseProgress,
                stageVisible = CombatStageVisible, endingPending = Playback.EndingPending, sequence = Playback.Sequence,
                abilityId = Playback.Current?.AbilityId, family = Playback.Current?.Profile.Family, source = Playback.Current?.Source, targets = Playback.Current?.Targets,
                speed = _combatSpeed, reducedMotion = _reducedMotion, volume = _combatVolume, audioUnavailable = _combatAudio?.Unavailable ?? false,
                playedCues = _combatAudio?.PlayedCues ?? 0, profileHash = CombatVisualProfiles.Default.Hash, area = Rect(CombatArea),
                units = DisplayBattle?.Units.Select(u => new { id = u.Id, actorId = DisplayBattle.ActorIds[u.Id], name = u.Name, friendly = u.Friendly, alive = u.Alive, health = u.Health, mana = u.Mana, bounds = Rect(UnitRect(u)), spriteBounds = Rect(CombatSpriteBox(u)) }) },
            party = _session.Party.Where(a => a.Life != LifeState.Dead || playing && Presented(a) != null).Select(a => new { id = a.Id, name = a.Name, x = a.Position.X, y = a.Position.Y, floor = a.Position.Floor,
                classId = _session.Class(a).Id, a.Level, a.Experience, health = Presented(a)?.Health ?? a.Health, a.MaxHealth, mana = Presented(a)?.Mana ?? a.Mana, a.MaxMana,
                life = Presented(a) is { } shown ? (shown.Alive ? LifeState.Alive : LifeState.Downed).ToString() : a.Life.ToString(), a.Body.Hunger, a.Body.Fatigue, a.Body.Wounds,
                bounds = _actorMotions.TryGetValue(a.Id, out var motion) ? Rect(ActorRect(motion.Position)) : null }),
            journal = _panel == "journal" ? JournalRows(PanelBox(w, h).Width - 104).Skip(_journalOffset).Take(JournalVisibleRows(PanelBox(w, h))) : null,
            controls = _controls.Select(c => new { id = c.Id, label = c.Label, enabled = c.Enabled, selected = c.Selected, reason = c.DisabledReason, tip = c.Tip, sprite = c.Sprite, detail = c.Detail, progress = c.Progress, badgeSprite = c.BadgeSprite, dimmed = c.Dimmed, bounds = Rect(c.Bounds), spriteBounds = c.Sprite == null ? null : (object)Rect(ControlSpriteBox(c)) }),
            actors = _session.State.Actors.Where(a => !a.Recruited && a.Life == LifeState.Alive && _session.Visible.Contains(a.Position)).Select(a => new { id = a.Id, kind = a.Kind.ToString(), name = a.Name, archetype = a.Archetype, habitat = a.Habitat, hostile = _session.IsHostile(a), behaviour = _session.WildlifeDescription(a), routine = a.Routine?.Kind.ToString(), activity = _session.RoutineDescription(a), x = a.Position.X, y = a.Position.Y, floor = a.Position.Floor,
                occupation = playerView || a.Occupation is not { } job ? null : new { id = job.Id, kind = job.Kind.ToString(), phase = job.Phase.ToString(), target = job.Target, progress = job.Progress, required = job.Required, carried = job.Carried, delivered = job.Delivered },
                bounds = _actorMotions.TryGetValue(a.Id, out var motion) ? (object)Rect(ActorRect(motion.Position)) : null }),
            battle = reportedBattle is { } battle ? new { round = battle.Round, activeFriendly = battle.Active.Friendly, active = battle.Active.Id, partner = _cooperativePartner, assisting = battle.Assisting, prepared = battle.Prepared,
                units = battle.Units.Select(u => new { id = u.Id, friendly = u.Friendly, alive = u.Alive, mana = u.Mana, partnerReason = playing ? "Wait for presentation." : _session.CooperationPartnerReason(_skill, u.Id), bounds = Rect(UnitRect(u)) }) } : null
        }, UiJson);
    }
}
