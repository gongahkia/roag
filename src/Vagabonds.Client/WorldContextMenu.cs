using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Vagabonds.Core.World;

namespace Vagabonds.Client;

internal sealed class WorldTileSelection
{
    public const int Maximum = 256;
    public IReadOnlyList<WorldPosition> Cells { get; private set; } = [];
    public bool TooLarge { get; private set; }
    public void Clear() { Cells = []; TooLarge = false; }
    public void Single(WorldPosition cell) { Cells = [cell]; TooLarge = false; }
    public void Rectangle(WorldSession session, WorldPosition start, WorldPosition end)
    {
        Clear();
        if (start.Floor != end.Floor) return;
        int left = Math.Min(start.X, end.X), right = Math.Max(start.X, end.X), top = Math.Min(start.Y, end.Y), bottom = Math.Max(start.Y, end.Y);
        TooLarge = (long)(right - left + 1) * (bottom - top + 1) > Maximum;
        if (TooLarge) return;
        var cells = new List<WorldPosition>();
        for (int y = top; y <= bottom; y++) for (int x = left; x <= right; x++)
        { var cell = new WorldPosition(x, y, start.Floor); if (session.Chunks.Seen(cell)) cells.Add(cell); }
        Cells = cells;
    }
}

internal sealed record WorldContextOption(string Id, string Label, string Reason = "", string Tip = "");

internal static class WorldContextActions
{
    public static IReadOnlyList<WorldContextOption> Options(WorldSession session, IReadOnlyList<WorldPosition> cells, bool building = false)
    {
        var options = new List<WorldContextOption>();
        cells = cells.Where(session.Chunks.Seen).ToArray();
        if (cells.Count == 0) return options;
        var p = cells[0];
        void Work(string id, string name, WorldCommandKind kind, Material material = Material.Wall)
        {
            var reasons = cells.Select(c => session.TargetBlockReason(kind, c, material, requireAdjacent: false)).ToArray();
            int valid = reasons.Count(r => r.Length == 0);
            string tip = kind == WorldCommandKind.Dig ? "Uses work time and ordinary fatigue/hunger; no food is reserved. A pickaxe speeds digging."
                : kind == WorldCommandKind.Harvest ? "One gather per resource. Companions share jobs; tools speed fishing/mining. Hover for yield and recovery."
                : "Supplies are reserved per order; unaffordable tiles are skipped.";
            options.Add(new(id, name + (cells.Count > 1 ? " selection" : ""), valid > 0 ? "" : reasons[0], $"{valid}/{cells.Count} suitable tiles. " + tip));
        }
        if (building)
        {
            foreach (var material in new[] { Material.Wall, Material.Floor, Material.Door, Material.Bridge, Material.Storage, Material.Camp })
                Work("construct:" + material, "Build " + material.ToString().ToLowerInvariant(), WorldCommandKind.Build, material);
            options.Add(new("back", "Back")); return options;
        }
        if (cells.Count == 1 && session.Chunks.Seen(p))
        {
            var actor = session.OccupantAt(p, observedOnly: true);
            if (actor?.Recruited == true)
            {
                options.Add(new("party", "Manage " + actor.Name));
                if (actor.Id != session.State.Party[0]) options.Add(new("lead", "Lead with " + actor.Name, actor.Life != LifeState.Alive ? "Choose a conscious companion." : "",
                    "Change the leader to turn back through a narrow passage without walking through companions."));
                if (session.Chunks.At(p) is Material.StairsDown or Material.StairsUp) options.Add(new("interact", "Take stairs"));
            }
            else if (WorldMouseNavigation.Intent(session, p) is { } intent)
            {
                var plan = WorldMouseNavigation.Plan(session, p);
                string label = intent.Kind switch { "person" => "Talk to " + actor?.Name, "enemy" => "Approach and fight", "loot" => "Open cache", "storage" => "Open storage", "stairs" => "Take stairs", "discovery" => "Investigate landmark", _ => "Approach and interact" };
                options.Add(new("interact", label, plan.Reachable ? "" : plan.Reason));
            }
            else if (session.GroundOpen(p))
            { var plan = WorldMouseNavigation.Plan(session, p); options.Add(new("travel", "Travel here", plan.Reachable ? "" : plan.Reason)); }
            if (session.Chunks.At(p) == Material.Camp)
            {
                string reason = session.State.Position.Distance(p) <= 1 || session.RouteToAdjacent(p).Count > 0 ? "" : "No free observed approach reaches this camp.";
                if (reason.Length == 0 && session.State.Structures.Any(s => s.Position == p && s.Condition <= 20)) reason = "Repair this worn camp first.";
                string restReason = reason.Length > 0 ? reason : session.Count("food") < session.State.Party.Count ? $"Rest needs {session.State.Party.Count} food." : "";
                options.Add(new("camp", "Assign camp duties", reason)); options.Add(new("rest", "Rest here / 1 hour", restReason)); options.Add(new("craft", "Craft here", reason));
            }
            if (actor is { Kind: ActorKind.Person, Recruited: false } && session.State.Position.Distance(session.Plan.Towns[actor.HomeTown].Center) <= 28)
            {
                if (session.State.Arc.Progress > 0) options.Add(new("evidence", "Present side-arc evidence"));
                if (session.State.Tasks.Any(t => t.TownId == actor.HomeTown && t.RoadId == null && !t.Complete)) options.Add(new("delivery", "Deliver requested food"));
            }
        }
        if (cells.Any(c => session.Chunks.At(c) is Material.Rock or Material.Wall or Material.Tree or Material.Door or Material.Storage or Material.Camp || session.ObjectAt(c) is { Removable: true } || session.State.Structures.Any(s => s.Position == c)))
            Work("dig", "Dig / dismantle", WorldCommandKind.Dig);
        if (cells.Any(c => session.ResourceAt(c) != null)) Work("harvest", "Gather resources", WorldCommandKind.Harvest);
        if (cells.Any(c => session.State.Structures.Any(s => s.Position == c && s.Condition < 95))) Work("repair", "Repair", WorldCommandKind.Repair);
        if (cells.Any(c => session.Chunks.At(c) is Material.Grass or Material.Earth or Material.Floor or Material.Water or Material.Bridge && session.ObjectAt(c) is not { Solid: true }))
            options.Add(new("build", "Build..."));
        if (session.State.Work.Any(w => cells.Contains(w.Target)))
        { options.Add(new("work", "Manage work here")); options.Add(new("resume", "Resume party work here")); options.Add(new("suspend", "Suspend selected work")); options.Add(new("cancel", "Cancel selected orders")); }
        options.Add(new("clear", cells.Count > 1 ? "Clear selection" : "Close"));
        return options;
    }
}

internal sealed partial class WorldGame
{
    private WorldTileSelection? _selection;
    private WorldTileSelection Selection => _selection ??= new();
    private WorldPosition? _dragStart;
    private Point _dragScreen;
    private bool _dragging;
    private WorldPosition[]? _contextCells;
    private Point _contextAnchor;
    private Rectangle _contextBox;
    private bool _contextBuilding;
    private string[] _contextHint = [];
    private bool PointerModal => _contextCells != null || _dragStart != null;

    private WorldPosition PointerCell(Point point)
    {
        var world = _camera.ScreenToWorld(new(Math.Clamp(point.X, _map.Left, _map.Right - 1), Math.Clamp(point.Y, _map.Top, _map.Bottom - 1)));
        return new((int)Math.Floor(world.X), (int)Math.Floor(world.Y), _session.State.Position.Floor);
    }
    private void ClearWorldSelection() { _dragStart = null; _dragging = false; _contextCells = null; _contextBuilding = false; Selection.Clear(); }
    private void BeginSelection(Point point)
    {
        StopTravel();
        _dragScreen = point; _dragStart = PointerCell(point); _dragging = false; Selection.Clear();
    }
    private void UpdateSelection(MouseState mouse)
    {
        if (_dragStart is not { } start) return;
        if (mouse.LeftButton == ButtonState.Pressed || !_dragging)
        {
            _dragging |= Vector2.Distance(_dragScreen.ToVector2(), mouse.Position.ToVector2()) >= 6;
            if (_dragging) Selection.Rectangle(_session, start, PointerCell(mouse.Position));
        }
        if (mouse.LeftButton != ButtonState.Released) return;
        _dragStart = null;
        if (_dragging)
        {
            _notice = Selection.TooLarge ? "Select at most 256 tiles." : $"{Selection.Cells.Count} observed tiles selected. Right-click the selection to choose an action.";
            _dragging = false;
        }
        else if (_map.Contains(mouse.Position)) ClickMap(mouse);
    }
    private void OpenContext(Point point)
    {
        StopTravel(); _dragStart = null;
        var cell = PointerCell(point);
        if (!_session.Chunks.Seen(cell)) { Selection.Clear(); _contextCells = null; _notice = "Explore this ground before selecting actions."; return; }
        if (!Selection.Cells.Contains(cell)) Selection.Single(cell);
        _contextCells = Selection.Cells.ToArray(); _contextAnchor = point; _contextBuilding = false;
    }
    private void BuildContextControls()
    {
        if (_contextCells is not { Length: > 0 } cells) return;
        var options = WorldContextActions.Options(_session, cells, _contextBuilding);
        int width = Math.Min(388, _map.Width - 16);
        var explained = options.FirstOrDefault(o => o.Id == "harvest") ?? options.FirstOrDefault(o => o.Id == "dig") ?? options.FirstOrDefault(o => o.Reason.Length > 0);
        string hint = explained == null ? "" : explained.Reason.Length > 0 ? explained.Reason +
            (explained.Reason.StartsWith("Excavation needs", StringComparison.Ordinal) ? " Gather food or trade in town, then retry." : "") : explained.Tip;
        _contextHint = FeedbackLines(hint, width - 20);
        int hintLines = Math.Min(3, _contextHint.Length);
        int height = 44 + options.Count * 34 + (hintLines > 0 ? hintLines * 19 + 8 : 0);
        _contextBox = new(Math.Clamp(_contextAnchor.X + 8, _map.Left + 4, _map.Right - width - 4),
            Math.Clamp(_contextAnchor.Y + 8, _map.Top + 4, Math.Max(_map.Top + 4, _map.Bottom - height - 4)), width, height);
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            _controls.Add(new("context:" + option.Id, option.Label, new(_contextBox.X + 8, _contextBox.Y + 36 + i * 34, width - 16, 30), option.Reason.Length == 0, Tip: option.Tip, DisabledReason: option.Reason));
        }
    }
    private void ActivateContext(string action)
    {
        if (_contextCells is not { Length: > 0 } cells) return;
        var p = cells[0]; var actor = _session.OccupantAt(p, observedOnly: true);
        if (action is "build" or "back") { _contextBuilding = action == "build"; return; }
        if (action == "forage")
        {
            _contextCells = null;
            if (Execute(new(WorldCommandKind.Forage))) _session.Paused = false;
            return;
        }
        if (action is "dig" or "repair" or "harvest" || action.StartsWith("construct:", StringComparison.Ordinal))
        {
            var kind = action == "harvest" ? WorldCommandKind.Harvest : action == "dig" ? WorldCommandKind.Dig : action == "repair" ? WorldCommandKind.Repair : WorldCommandKind.Build;
            var material = kind == WorldCommandKind.Build ? Enum.Parse<Material>(action[10..]) : Material.Wall;
            var result = _session.DesignateWork(kind, cells, material); _notice = result.Message;
            if (result.Queued > 0) { ClearWorldSelection(); _session.Paused = false; }
            return;
        }
        ClearWorldSelection();
        switch (action)
        {
            case "travel": case "interact": BeginApproach(WorldMouseNavigation.Plan(_session, p)); break;
            case "party": if (actor != null) { _packMember = _session.State.Party.IndexOf(actor.Id); _panel = "inventory"; } break;
            case "lead": if (actor != null) Execute(new(WorldCommandKind.LeadParty, ActorId: actor.Id)); break;
            case "rest": case "craft": case "camp":
                var route = _session.RouteToAdjacent(p); bool reachable = route.Count > 0 || _session.State.Position.Distance(p) <= 1;
                BeginApproach(new(route, new(action, p), reachable, "No free approach reaches this camp.")); break;
            case "work":
                _selectedWorkId = _session.State.Work.FirstOrDefault(w => cells.Contains(w.Target))?.Id ?? 0; _panel = "work"; break;
            case "suspend":
                foreach (var order in _session.State.Work.Where(w => cells.Contains(w.Target))) _session.Submit(new(WorldCommandKind.ConfigureWork, WorkId: order.Id, Item: "suspend"));
                _notice = "Selected work suspended; supplies and progress retained."; break;
            case "resume":
                foreach (var order in _session.State.Work.Where(w => cells.Contains(w.Target))) _session.Submit(new(WorldCommandKind.ConfigureWork, WorkId: order.Id, Item: "resume"));
                _session.Paused = false; _notice = "Resuming the selected work."; break;
            case "cancel":
                int count = 0;
                foreach (var order in _session.State.Work.Where(w => cells.Contains(w.Target)).ToArray()) if (Execute(new(WorldCommandKind.CancelWork, WorkId: order.Id))) count++;
                _notice = $"Cancelled {count} order(s); reserved supplies returned."; break;
            case "evidence": Execute(new(WorldCommandKind.ResolveArc)); break;
            case "delivery": Execute(new(WorldCommandKind.CompleteDelivery)); break;
        }
    }
    private void BeginApproach(WorldClickPlan plan)
    {
        StopTravel();
        if (!plan.Reachable) { _notice = plan.Reason; return; }
        _pendingInteraction = plan.Interaction;
        foreach (var cell in plan.Path) _route.Enqueue(cell);
        if (_route.Count > 0) { _session.Paused = false; _notice = "Travelling. Pause, Stop or right-click to stop."; }
        else FinishApproach();
    }
    private void DrawSelection()
    {
        foreach (var cell in Selection.Cells)
        {
            var rect = TileRect(cell); FillClipped(rect, new Color(113, 187, 195) * .22f);
            FillClipped(new(rect.X, rect.Y, rect.Width, 2), Gold); FillClipped(new(rect.X, rect.Bottom - 2, rect.Width, 2), Gold);
            FillClipped(new(rect.X, rect.Y, 2, rect.Height), Gold); FillClipped(new(rect.Right - 2, rect.Y, 2, rect.Height), Gold);
        }
    }
    private void DrawContext()
    {
        if (_contextCells is not { Length: > 0 } cells) return;
        Fill(_contextBox, new(22, 31, 37));
        string title = _contextBuilding ? "BUILD ON SELECTED GROUND" : cells.Length > 1 ? cells.Length + " TILES SELECTED" : WorldInspection.Inspect(_session, cells[0]).Title;
        Text(Short(title, (_contextBox.Width - 20) / 8), _contextBox.X + 10, _contextBox.Y + 12, Gold);
        int hintLines = Math.Min(3, _contextHint.Length);
        for (int i = 0; i < hintLines; i++) Text(_contextHint[i], _contextBox.X + 10, _contextBox.Bottom - hintLines * 19 - 8 + i * 19, Gold);
    }
}
