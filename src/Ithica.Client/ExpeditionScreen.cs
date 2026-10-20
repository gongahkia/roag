using Ithica.Core;
using Ithica.Core.Colony;
using Ithica.Core.Expeditions;
using Ithica.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Ithica.Client;

public sealed partial class IthicaGame
{
    private SpriteLibrary _fieldSprites = null!;
    private readonly MapCamera _fieldCamera = new();
    private readonly List<Cell> _fieldRoute = [];
    private string? _fieldRouteObject;
    private readonly HashSet<int> _departureIds = [];
    private ExpeditionPack _departurePack = new();
    private bool _atlas;
    private int _fieldSkill, _fieldResident;
    private double _fieldMoveAt;
    private long _fieldNoticeTick;
    private string _fieldNotice = "";

    private Rectangle HomeNoticeBounds() => new(_camera.Viewport.X + 12, _camera.Viewport.Y + 12, _camera.Viewport.Width - 24, 68);

    private void ResetFieldCamera()
    {
        _fieldRoute.Clear(); _fieldRouteObject = null; _fieldSkill = 0;
        _fieldNoticeTick = _campaign.CampaignTick; _fieldNotice = "";
        _fieldCamera.Viewport = _camera.Viewport;
        _fieldResident = _campaign.PartyIds.FirstOrDefault();
        _fieldCamera.Reset(28);
        if (_campaign.ActiveField is { } f) _fieldCamera.Center = new(f.Position.X + .5f, f.Position.Y + .5f);
    }

    private void SyncCampaignUI()
    {
        if (_atlas) { _modal = ""; return; }
        if (_campaign.Phase == CampaignPhase.Preparation) _modal = "departure";
        else if (_campaign.Phase == CampaignPhase.Arrival) _modal = "arrival";
        else if (_modal is "departure" or "arrival") _modal = "";
    }

    private void PrepareRescue(int contact) => PrepareSite("rescue-" + contact);
    private void PrepareSite(string id)
    {
        var result = _campaign.Prepare(id); Message(result);
        if (!result.Success) return;
        _departureIds.Clear();
        var ready = _sim.Crew.FirstOrDefault(c => c.Alive && c.Location == ResidentLocation.Home && !c.Rest && c.Health >= 25 && c.Energy >= 15 && c.Suit >= 25);
        if (ready is not null) _departureIds.Add(ready.Id);
        _departurePack = new(); _modal = "departure";
    }

    private void ConfirmDeparture()
    {
        var result = _campaign.Depart(_departureIds.Order(), _departurePack); Message(result);
        if (result.Success) { _modal = ""; ResetFieldCamera(); }
    }

    private void HandleCampaignInput()
    {
        if (Pressed(Keys.Q)) { Exit(); return; }
        if (Pressed(Keys.F11)) _graphics.ToggleFullScreen();
        if (Pressed(Keys.F2)) { _atlas = !_atlas; _campaign.Clock.Accumulator = 0; }
        if (_atlas) { _modal = ""; AtlasInput(); return; }
        SyncCampaignUI();
        if (Pressed(Keys.F5)) { _fieldRoute.Clear(); SaveColony(); return; }
        if (Pressed(Keys.F9)) { LoadColony(); return; }
        if (_campaign.Phase == CampaignPhase.Preparation)
        {
            if (_departureList.Contains(_mouse.Position)) _departureScroll = Math.Max(0, _departureScroll - Math.Sign(_mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue));
            if (Pressed(Keys.Escape)) { Message(_campaign.CancelPreparation()); SyncCampaignUI(); }
            if (Pressed(Keys.Enter)) ConfirmDeparture();
            return;
        }
        if (_campaign.Phase == CampaignPhase.Arrival)
        {
            if (Pressed(Keys.Enter) || Pressed(Keys.Escape) || Pressed(Keys.Space)) { Message(_campaign.DismissArrival()); SyncCampaignUI(); }
            return;
        }
        if (_modal.Length > 0) { HandleInput(); return; }
        if (_campaign.ActiveField is not { } f) return;
        _fieldCamera.Viewport = _camera.Viewport;
        if (Pressed(Keys.F1)) { _modal = "tutorial"; _lesson = 0; return; }
        if (Pressed(Keys.Escape)) { _fieldRoute.Clear(); _fieldRouteObject = null; _fieldSkill = 0; }
        int wheel = _mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
        if (_fieldCamera.Viewport.Contains(_mouse.Position) && (_fieldNotice.Length == 0 || !HomeNoticeBounds().Contains(_mouse.Position)))
        {
            if (wheel != 0) _fieldCamera.ZoomAt(MousePoint, Math.Sign(wheel));
            if ((_mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Pressed)
                || (_mouse.MiddleButton == ButtonState.Pressed && _previousMouse.MiddleButton == ButtonState.Pressed))
                _fieldCamera.Pan(MousePoint - new Vector2(_previousMouse.X, _previousMouse.Y));
            if (_leftPressed)
            {
                _leftPressed = false;
                var world = _fieldCamera.ScreenToWorld(MousePoint); var cell = new Cell((int)Math.Floor(world.X), (int)Math.Floor(world.Y));
                if (f.Battle is { } battle)
                {
                    var unit = battle.Units.OrderBy(u => u.Position.Distance(cell)).FirstOrDefault(u => u.Position.Distance(cell) <= 1);
                    if (unit is not null) PerformFieldAction(unit.Id);
                }
                else if (f.Zone.Inside(cell) && f.Zone.Seen[f.Zone.Index(cell)])
                {
                    _fieldRoute.Clear(); _fieldRouteObject = f.ObjectAt(cell)?.Id;
                    if (_fieldRouteObject is not null && cell.Distance(f.Position) <= 1) ApplyField(_campaign.Interact(_fieldRouteObject));
                    else _fieldRoute.AddRange(f.Route(cell));
                }
            }
        }
        var playerScreen = _fieldCamera.WorldToScreen(new(f.Position.X + .5f, f.Position.Y + .5f));
        if (Pressed(Keys.OemPlus) || Pressed(Keys.PageUp)) _fieldCamera.ZoomAt(playerScreen, 1);
        if (Pressed(Keys.OemMinus) || Pressed(Keys.PageDown)) _fieldCamera.ZoomAt(playerScreen, -1);
        if (Pressed(Keys.D0)) _fieldCamera.Reset(28);
        if (f.Battle is { } b)
        {
            for (int i = 0; i < 6; i++) if (Pressed(Keys.D1 + i)) SelectFieldSkill(i);
            if (Pressed(Keys.Enter))
            {
                var skill = Skills.FieldGet(f.Party.Single(h => h.Id == b.Active.HeroId).Role, _fieldSkill);
                int? target = skill.Target == TargetKind.Ally ? b.Units.First(u => u.Friendly && u.HeroId == _fieldResident).Id : b.Units.First(u => !u.Friendly && u.Alive).Id;
                PerformFieldAction(target);
            }
            return;
        }
        if (Pressed(Keys.E) || Pressed(Keys.Enter)) ApplyField(_campaign.Interact());
        if (Pressed(Keys.Space) || Pressed(Keys.OemPeriod)) ApplyField(_campaign.Wait());
        if (Pressed(Keys.R)) ApplyField(_campaign.ReturnHome());
        if (Pressed(Keys.C)) ApplyField(_campaign.Rest());
        if (Pressed(Keys.O)) ApplyField(_campaign.Refill());
        if (Pressed(Keys.H)) ApplyField(_campaign.Treat(_fieldResident));
        if (Pressed(Keys.T)) ApplyField(_campaign.ClearHazards());
        int dx = _keyboard.IsKeyDown(Keys.A) || _keyboard.IsKeyDown(Keys.Left) ? -1 : _keyboard.IsKeyDown(Keys.D) || _keyboard.IsKeyDown(Keys.Right) ? 1 : 0;
        int dy = _keyboard.IsKeyDown(Keys.W) || _keyboard.IsKeyDown(Keys.Up) ? -1 : _keyboard.IsKeyDown(Keys.S) || _keyboard.IsKeyDown(Keys.Down) ? 1 : 0;
        if (Math.Abs(dx) + Math.Abs(dy) == 1 && _elapsed >= _fieldMoveAt)
        { _fieldRoute.Clear(); ApplyField(_campaign.Move(dx, dy)); _fieldMoveAt = _elapsed + .13; }
        else if (_fieldRoute.Count > 0 && _elapsed >= _fieldMoveAt)
        {
            var next = _fieldRoute[0]; _fieldRoute.RemoveAt(0);
            var result = _campaign.Move(next.X - f.Position.X, next.Y - f.Position.Y); ApplyField(result);
            _fieldMoveAt = _elapsed + .11;
            if (!result.Success || f.Battle is not null || f.Zone.Environment.Cells[f.Zone.Index(f.Position)] != HazardKind.None) _fieldRoute.Clear();
            if (_fieldRoute.Count == 0 && _fieldRouteObject is { } id && f.Battle is null && f.Zone.Objects.Single(o => o.Id == id).Position.Distance(f.Position) <= 1)
            { _fieldRouteObject = null; ApplyField(_campaign.Interact(id)); }
        }
    }

    private void SelectFieldSkill(int index)
    {
        _fieldSkill = index;
        if (_campaign.ActiveField?.Battle is not { } b) return;
        var hero = _campaign.ActiveField.Party.Single(h => h.Id == b.Active.HeroId);
        if (Skills.FieldGet(hero.Role, index).Target is TargetKind.Self or TargetKind.AllAllies or TargetKind.AllEnemies) PerformFieldAction(null);
    }

    private void PerformFieldAction(int? target)
    {
        ApplyField(_campaign.Act(_fieldSkill, target)); _fieldSkill = 0;
    }

    private void ApplyField(Result result)
    {
        if (result.Message.Length > 0) Message(result);
        if (_campaign.ActiveField is { } f)
        {
            var screen = _fieldCamera.WorldToScreen(new(f.Position.X + .5f, f.Position.Y + .5f));
            var margin = _fieldCamera.Viewport; margin.Inflate(-60, -60);
            if (!margin.Contains(screen)) _fieldCamera.Center = new(f.Position.X + .5f, f.Position.Y + .5f);
            if (f.Battle is not null) { _fieldRoute.Clear(); _fieldRouteObject = null; }
        }
        else { _fieldRoute.Clear(); _fieldRouteObject = null; }
        if (_campaign.ActiveField is not null && _sim.Log.LastOrDefault(n => n.Level == NoticeLevel.Danger && n.Tick > _fieldNoticeTick) is { } notice)
        {
            _fieldNotice = notice.Text; _fieldNoticeTick = notice.Tick;
            _fieldRoute.Clear(); _fieldRouteObject = null;
        }
        SyncCampaignUI();
    }

    private Rectangle FieldRect(Cell cell)
    {
        var p = _fieldCamera.WorldToScreen(new(cell.X, cell.Y)); int size = (int)Math.Ceiling(_fieldCamera.CellSize);
        return new((int)p.X, (int)p.Y, size, size);
    }

    private void DrawFieldMap()
    {
        var f = _campaign.ActiveField!; var z = f.Zone; _fieldCamera.Viewport = _camera.Viewport;
        Fill(_fieldCamera.Viewport, new(9, 13, 19));
        for (int y = 0; y < z.Height; y++) for (int x = 0; x < z.Width; x++)
        {
            var p = new Cell(x, y); int i = z.Index(p); var r = FieldRect(p);
            if (!r.Intersects(_fieldCamera.Viewport) || !z.Seen[i]) continue;
            float light = z.Visible[i] ? 1 : .32f;
            Fill(r, (z.Tiles[i] == Ground.Wall ? new Color(28, 35, 42) : new Color(46, 49, 51)) * light);
            _fieldSprites.Draw(_batch, z.GroundSprites[i] ?? "grit", r, (z.Tiles[i] == Ground.Wall ? new Color(115, 127, 137) : new Color(144, 144, 133)) * light);
            if (z.Environment.Cells[i] != HazardKind.None)
            { Fill(r, Purple * (.22f * light)); _fieldSprites.Fit(_batch, "hazard-corruption", r, Purple * light); }
        }
        foreach (var o in z.Objects.Where(f.Present))
        {
            if (!z.Seen[z.Index(o.Position)] || f.Battle is not null && o.Kind == ObjectKind.Encounter) continue;
            var rect = FieldRect(o.Position);
            _fieldSprites.Fit(_batch, o.Kind == ObjectKind.Gate && f.Completed.Contains(o.Id) ? "gate-open" : o.Sprite, rect, o.Kind == ObjectKind.Rescue ? Amber : o.Kind == ObjectKind.Return ? Accent : o.Kind == ObjectKind.Encounter ? Danger : Blue);
            if (z.Visible[z.Index(o.Position)] && _fieldCamera.CellSize >= 20)
            {
                string label = o.Kind == ObjectKind.Rescue && _campaign.SelectedSite!.ContactId is int id ? _sim.Survivors[id].Name : o.Name;
                Fill(new(rect.X - 2, rect.Y - 17, (int)SpriteLibrary.Measure(label, .75f) + 8, 15), Background * .92f);
                Text(label, rect.X + 2, rect.Y - 15, Ink, .75f);
            }
        }
        if (f.Battle is { } battle)
        {
            foreach (var unit in battle.Units)
            {
                var r = FieldRect(unit.Position); r.Inflate(3, 3);
                _fieldSprites.Fit(_batch, unit.Sprite, r, !unit.Alive ? Muted * .3f : unit.Friendly ? Accent : Danger);
                if (unit.Id == battle.Active.Id) Outline(r, Amber);
                Fill(new(r.X, r.Bottom + 2, (int)(r.Width * unit.Health / (float)unit.MaxHealth), 3), unit.Friendly ? Accent : Danger);
            }
        }
        else
        {
            var rect = FieldRect(f.Position); Outline(rect, Accent * .6f);
            _fieldSprites.Fit(_batch, "resident", rect, Ink);
            if (_sim.Survivors.Any(s => s.FieldEscort))
                _fieldSprites.Fit(_batch, "resident", new(rect.X - rect.Width / 2, rect.Y + rect.Height / 2, rect.Width, rect.Height), Amber * .85f);
        }
    }

    private void DrawFieldInterface(int width, int height)
    {
        var f = _campaign.ActiveField!; _tooltip = null;
        Text("ITHICA", 20, 16, Ink, 2); Text(f.Zone.Name.ToUpperInvariant(), 184, 21, Muted);
        Text($"TICK {_campaign.CampaignTick:000000} / " + (_fieldRoute.Count > 0 ? "FOLLOWING ROUTE / ESC STOPS" : "WAITING FOR YOU"), 76, 66, Accent);
        Text($"HOME / O2 {_sim.Pressure * 100:0}%  FOOD {_sim.Resources.Food:0}  WATER {_sim.Resources.Water:0}", 76, 89, _sim.Pressure < .4f || _sim.Resources.Water < 15 || _sim.Resources.Food < 15 ? Danger : Muted, .85f);
        if (Button(new(width - 262, 8, 76, 40), "SAVE")) QueueUI(SaveColony);
        if (Button(new(width - 178, 8, 76, 40), "ATLAS")) _atlas = true;
        if (Button(new(width - 94, 8, 76, 40), "GUIDE")) { _modal = "tutorial"; _lesson = 0; }
        var side = _sidebar; Fill(side, Surface); float y = side.Y + 14, x = side.X + 12; int w = side.Width - 24;
        Text($"PACK / {_campaign.Pack.Food} FOOD  {_campaign.Pack.Water} WATER", x, y, Amber, .85f); y += 22;
        Text($"{_campaign.Pack.Medicine} MEDICAL  {_campaign.Pack.Filters} FILTERS", x, y, Muted, .8f); y += 26;
        foreach (var c in _campaign.Party.ToArray())
        {
            var card = new Rectangle((int)x, (int)y, w, 72);
            if (Button(card, "", selected: c.Id == _fieldResident))
            {
                _fieldResident = c.Id;
                if (f.Battle is { } b) { int target = b.Units.Single(u => u.HeroId == c.Id).Id; QueueUI(() => PerformFieldAction(target)); }
            }
            string conditions = f.Battle?.Units.SingleOrDefault(u => u.HeroId == c.Id)?.Conditions ?? "";
            Text(Short(c.Name.ToUpperInvariant() + (conditions.Length > 0 ? " / " + conditions : ""), (w - 20) / 8), x + 10, y + 8, Ink);
            Text($"HP {c.Health:0}   O2 {c.Suit:0}%", x + 10, y + 29, c.Health < 35 || c.Suit < 25 ? Danger : Accent, .85f);
            Text($"CHARGE {c.Charge:00}  ENERGY {c.Energy:0}  STRESS {100 - c.Morale:0}", x + 10, y + 50, Muted, .75f);
            y += 80;
        }
        if (f.Battle is { } battle)
        {
            Text($"ROUND {battle.Round} / {battle.Active.Name}", x, y, Amber, .85f); y += 24;
            foreach (var enemy in battle.Units.Where(u => !u.Friendly && u.Alive).ToArray())
            {
                if (Button(new((int)x, (int)y, w, 58), "", tooltip: f.IntentText(enemy))) QueueUI(() => PerformFieldAction(enemy.Id));
                Text(enemy.Name + $" / {enemy.Health} HP", x + 8, y + 5, Danger, .85f);
                Text(Short(f.IntentText(enemy), 34), x + 8, y + 25, Muted, .7f);
                Text(Short(enemy.Conditions, 34), x + 8, y + 42, Amber, .7f); y += 64;
            }
        }
        else
        {
            if (Button(new((int)x, side.Bottom - 56, w, 44), "RETURN APPROACH [R]"))
            {
                var exit = f.Zone.Objects.Single(o => o.Kind == ObjectKind.Return);
                if (f.Position.Distance(exit.Position) <= 1) QueueUI(() => ApplyField(_campaign.ReturnHome()));
                else { _fieldRoute.Clear(); _fieldRoute.AddRange(f.Route(exit.Position)); _fieldRouteObject = exit.Id; }
            }
        }
        int actionWidth = (_eventArea.Width - 16) / 3;
        for (int i = 0; i < 6; i++)
        {
            var box = new Rectangle(_eventArea.X + (i % 3) * (actionWidth + 8), _eventArea.Y + (i / 3) * 48, actionWidth, 42);
            if (f.Battle is { } b)
            {
                var hero = f.Party.Single(h => h.Id == b.Active.HeroId); var skill = Skills.FieldGet(hero.Role, i);
                if (Button(box, $"{i + 1} {skill.Name}" + (skill.Mana > 0 ? $" ({skill.Mana})" : ""), selected: i == _fieldSkill, enabled: b.Active.Mana >= skill.Mana, tooltip: skill.Description)) { int selected = i; QueueUI(() => SelectFieldSkill(selected)); }
            }
            else
            {
                string[] labels = ["INTERACT [E]", "WAIT [SPACE]", "REFILL SUITS [O]", "TREAT [H]", "REST [C]", "SUPPRESS [T]"];
                if (Button(box, labels[i])) { int action = i; QueueUI(() => ApplyField(action switch { 0 => _campaign.Interact(), 1 => _campaign.Wait(), 2 => _campaign.Refill(), 3 => _campaign.Treat(_fieldResident), 4 => _campaign.Rest(), _ => _campaign.ClearHazards() })); }
            }
        }
        if (_fieldNotice.Length > 0)
        {
            var banner = HomeNoticeBounds();
            Fill(banner, Background * .98f); Outline(banner, Danger * .6f);
            Wrap("HOME / " + _fieldNotice, banner.X + 12, banner.Y + 12, banner.Width - 110, Danger, .85f);
            if (Button(new(banner.Right - 86, banner.Y + 14, 74, 40), "OK")) _fieldNotice = "";
        }
        if (_elapsed < _messageUntil) Text(Short(_message, Math.Max(20, (width - 100) / 8)), 76, height - 28, LevelColor(_messageLevel), .85f);
    }

    private void DrawCampaignMarkers()
    {
        foreach (var site in _campaign.Sites.Where(s => s.ContactId is null))
        {
            var rect = CellRect(site.MapCell); rect.Inflate(4, 4); Outline(rect, site.Structure ? Purple : Blue);
            _sprites.Fit(_batch, site.Structure ? "beacon" : "icon-mine", rect, site.Structure ? Purple : Blue);
            if (_camera.CellSize >= 14) Text(site.Name.ToUpperInvariant(), rect.X, rect.Y - 17, Ink, .75f);
        }
    }
}
