using System.Text.Json;
using Ithica.Core.Expeditions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ithica.Graphics;

namespace Ithica.Client.Preview;

public sealed partial class PreviewGame : Game
{
    private const int GuidePageCount = 4;
    private readonly Options _options;
    private readonly GraphicsDeviceManager _graphics;
    private Campaign _campaign;
    private SpriteLibrary _sprites = null!;
    private SpriteBatch _batch = null!;
    private Texture2D _pixel = null!;
    private RenderTarget2D? _frame;
    private readonly RasterizerState _scissor = new() { ScissorTestEnable = true, CullMode = CullMode.None };
    private readonly MapCamera _camera = new();
    private Rectangle _map, _side;
    private KeyboardState _keys, _oldKeys;
    private MouseState _mouse, _oldMouse;
    private bool _click, _atlas, _original, _resizing;
    private bool _follow = true;
    private bool _autoSave;
    private double _time, _moveAt, _enemyAt;
    private int _draws, _selectedHero, _skill, _target = -1, _lastActive = -1, _lesson;
    private string _dialogTitle = "", _dialogText = "", _toast = "", _hover = "";
    private double _toastUntil;
    private bool _intro;
    private readonly List<Cell> _route = [];
    private int _routeCursor;
    private string? _routeObject;
    private Vector2[] _positions = new Vector2[4];
    private readonly List<string> _checks = [];
    private sealed record Popup(Cell Cell, string Text, Color Color, double Until);
    private readonly List<Popup> _popups = [];
    private int _eventWatermark;
    private int _dialogScroll;
    private string _lastDialogKey = "";
    private static readonly Color Background = new(18, 21, 25), Panel = new(28, 32, 37), Raised = new(37, 42, 46), Ink = new(229, 221, 199), Muted = new(154, 160, 155), Gold = new(218, 181, 117), Green = new(132, 183, 151), Blue = new(136, 165, 202), Red = new(218, 122, 111), Purple = new(177, 146, 193);
    private static readonly Color[] HeroColors = [Gold, Green, Blue, Purple];

    public PreviewGame(Options options)
    {
        _options = options;
        _autoSave = options.Load || options.Scene == "town";
        _campaign = options.Load ? Campaign.Load(options.Save) : new Campaign(options.Seed);
        _intro = !options.SkipIntro && !options.Load;
        if (!options.Load)
        {
            switch (options.Scene)
            {
                case "forest":
                    _campaign.TravelTo("to-wilds");
                    break;
                case "crypt":
                    _campaign.TravelTo("to-wilds");
                    _campaign.TravelTo("iron-cache");
                    _campaign.TravelTo("to-crypt");
                    break;
                case "battle":
                    _campaign.TravelTo("to-wilds");
                    _campaign.TravelTo("wilds-hounds");
                    break;
                case "victory":
                    _campaign.RunDemoExpedition();
                    break;
                case "atlas":
                    _atlas = true;
                    break;
            }
        }

        _eventWatermark = _campaign.CombatEvents.LastOrDefault()?.Sequence ?? 0;
        _graphics = new(this)
        {
            PreferredBackBufferWidth = options.Width,
            PreferredBackBufferHeight = options.Height,
            SynchronizeWithVerticalRetrace = true
        };
        Window.Title = "ITHICA / EXPEDITION PREVIEW / An Urizen Expedition";
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        Window.ClientSizeChanged += (_, _) => { if (_resizing) { return; } var b = Window.ClientBounds; if (b.Width < 1024 || b.Height < 720) { _resizing = true; _graphics.PreferredBackBufferWidth = Math.Max(1024, b.Width); _graphics.PreferredBackBufferHeight = Math.Max(720, b.Height); _graphics.ApplyChanges(); _resizing = false; } };
        Window.TextInput += (_, e) => { if (_atlas && !char.IsControl(e.Character) && e.Character < 127) { _search += e.Character; FilterAtlas(); } };
        Exiting += (_, _) =>
        {
            if (_autoSave && options.Frames == 0 && !options.SmokeTest)
            {
                Save();
            }
        };
    }
    protected override void LoadContent()
    {
        _batch = new(GraphicsDevice);
        _pixel = new(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        string root = Path.Combine(AppContext.BaseDirectory, "assets");
        _sprites = new(GraphicsDevice, root, "ithica-expedition-sprites.json");
        foreach (var sprite in _campaign.Zones.SelectMany(z => z.Objects).Select(o => o.Sprite).Concat(GenerationRules.Default.SpriteAliases).Concat(_campaign.Party.Select(h => h.Sprite)).Concat(Skills.ByRole.SelectMany(s => s).Select(s => s.Icon)).Concat(new[] { Skills.Guard.Icon, Skills.Potion.Icon, Skills.Flee.Icon }))
        {
            if (!_sprites.HasBinding(sprite))
            {
                throw new InvalidDataException("Unbound game sprite: " + sprite);
            }
        }

        _entries = JsonSerializer.Deserialize<List<AtlasEntry>>(File.ReadAllText(Path.Combine(root, "atlas-index.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        FilterAtlas();
        _camera.Reset(32);
        Recenter(true);
    }
    private Vector2 Point(Cell p) => new(p.X + .5f, p.Y + .5f);
    private bool Pressed(Keys key) => _keys.IsKeyDown(key) && _oldKeys.IsKeyUp(key);
    private void Toast(string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        _toast = text;
        _toastUntil = _time + 5;
    }
    private void Apply(ActionResult r)
    {
        Toast(r.Message);
        if (r.Title is not null)
        {
            _dialogScroll = 0;
            _dialogTitle = r.Title;
            _dialogText = r.Message;
        }
        if (_campaign.Battle is not null)
        {
            _route.Clear();
        }
    }
    private void Layout()
    {
        int w = GraphicsDevice.Viewport.Width, h = GraphicsDevice.Viewport.Height;
        _map = new(18, 94, w - 352, h - 318);
        _side = new(w - 318, 94, 300, h - 112);
        _camera.Viewport = _map;
    }
    private void Recenter(bool snap = false)
    {
        Vector2 center = _campaign.Battle is { } b ? Point(_campaign.Zone.Objects.Single(o => o.Id == b.EncounterId).Position) : Point(_campaign.Position) + new Vector2(0, -1.5f);
        Vector2 half = new(_camera.Viewport.Width / _camera.CellSize / 2, _camera.Viewport.Height / _camera.CellSize / 2);
        center.X = half.X * 2 >= _campaign.Zone.Width ? _campaign.Zone.Width / 2f : Math.Clamp(center.X, half.X, _campaign.Zone.Width - half.X);
        center.Y = half.Y * 2 >= _campaign.Zone.Height ? _campaign.Zone.Height / 2f : Math.Clamp(center.Y, half.Y, _campaign.Zone.Height - half.Y);
        if (snap)
        {
            _camera.Center = center;
        }
        else if (_follow)
        {
            _camera.Center = Vector2.Lerp(_camera.Center, center, .16f);
        }
    }
    protected override void Update(GameTime gameTime)
    {
        double delta = Math.Min(.1, gameTime.ElapsedGameTime.TotalSeconds);
        _time += delta;
        Layout();
        _keys = Keyboard.GetState();
        _mouse = Mouse.GetState();
        _click = _mouse.LeftButton == ButtonState.Pressed && _oldMouse.LeftButton == ButtonState.Released;
        if (IsActive && !_options.SmokeTest)
        {
            Input();
        }

        if (!_atlas && !_intro && _dialogTitle.Length == 0)
        {
            if (_campaign.Battle is { } battle)
            {
                if (battle.Active.Id != _lastActive)
                {
                    _lastActive = battle.Active.Id;
                    _skill = 0;
                    _target = battle.Units.FirstOrDefault(u => !u.Friendly && u.Alive)?.Id ?? -1;
                    _enemyAt = _time + .65;
                }
                if (!battle.Active.Friendly && _time >= _enemyAt)
                {
                    Apply(_campaign.AdvanceEnemy());
                    _enemyAt = _time + .65;
                }
            }
            else if (_routeCursor < _route.Count && _time >= _moveAt)
            {
                Cell next = _route[_routeCursor++];
                Move(next.X - _campaign.Position.X, next.Y - _campaign.Position.Y);
                _moveAt = _time + .11;
                if (_routeCursor >= _route.Count && _routeObject is { } id)
                {
                    _routeObject = null;
                    if (_campaign.Battle is null)
                    {
                        Apply(_campaign.Interact(id));
                    }
                }
            }
        }
        Recenter();
        foreach (var e in _campaign.CombatEvents.Where(e => e.Sequence > _eventWatermark))
        {
            _eventWatermark = e.Sequence;
            if (_campaign.Battle is { } active && e.Target >= 0 && e.Target < active.Units.Count)
            {
                _popups.Add(new(active.Units[e.Target].Position, (e.Kind == "damage" ? "-" : "+") + e.Amount, e.Kind == "damage" ? Red : Green, _time + .8));
            }
        }
        _popups.RemoveAll(p => p.Until <= _time);
        for (int i = 0; i < 4; i++)
        {
            var p = Point(_campaign.Position) + new Vector2(-i % 2, i / 2);
            if (_positions[i] == Vector2.Zero || Vector2.Distance(_positions[i], p) > 6)
            {
                _positions[i] = p;
            }
            else
            {
                _positions[i] = Vector2.Lerp(_positions[i], p, (float)Math.Min(1, delta * 16));
            }
        }
        if (_options.SmokeTest && _draws == 2)
        {
            Smoke();
        }

        _oldMouse = _mouse;
        _oldKeys = _keys;
        base.Update(gameTime);
    }
    private void Input()
    {
        if (Pressed(Keys.Q))
        {
            Exit();
            return;
        }
        if (Pressed(Keys.F11))
        {
            _graphics.ToggleFullScreen();
            return;
        }
        if (Pressed(Keys.F2))
        {
            _atlas = !_atlas;
            _intro = false;
            _dialogTitle = "";
            _route.Clear();
            return;
        }
        if (_atlas)
        {
            AtlasInput();
            return;
        }
        if (_intro || _dialogTitle.Length > 0)
        {
            int dialogWheel = _mouse.ScrollWheelValue - _oldMouse.ScrollWheelValue;
            if (dialogWheel != 0) _dialogScroll = Math.Max(0, _dialogScroll - Math.Sign(dialogWheel) * 48);
            if (Pressed(Keys.PageDown) || Pressed(Keys.Down)) _dialogScroll += 96;
            if (Pressed(Keys.PageUp) || Pressed(Keys.Up)) _dialogScroll = Math.Max(0, _dialogScroll - 96);
        }
        if (_intro)
        {
            if (Pressed(Keys.Escape))
            {
                _intro = false;
            }

            if (Pressed(Keys.Left))
            {
                _lesson = Math.Max(0, _lesson - 1);
            }

            if (Pressed(Keys.Enter) || Pressed(Keys.Space) || Pressed(Keys.Right))
            {
                if (_lesson < GuidePageCount - 1)
                {
                    _lesson++;
                }
                else
                {
                    _intro = false;
                }
            }
            return;
        }
        if (_dialogTitle.Length > 0)
        {
            if (Pressed(Keys.Escape) || Pressed(Keys.Enter) || Pressed(Keys.Space))
            {
                _dialogTitle = "";
            }

            return;
        }
        if (Pressed(Keys.F1))
        {
            _intro = true;
            _lesson = 0;
        }
        if (Pressed(Keys.F4))
        {
            _original = !_original;
        }

        if (Pressed(Keys.S) && (_keys.IsKeyDown(Keys.LeftControl) || _keys.IsKeyDown(Keys.RightControl)))
        {
            Save();
            return;
        }
        if (Pressed(Keys.F5))
        {
            Save();
        }

        if (Pressed(Keys.F9))
        {
            Load();
        }

        if (Pressed(Keys.J))
        {
            _dialogTitle = "EXPEDITION JOURNAL";
            _dialogText = string.Join("\n\n", _campaign.Journal.TakeLast(8).Select(e => e.Text));
        }
        if (Pressed(Keys.I))
        {
            _dialogTitle = "THE SHARED PACK";
            _dialogText = $"{_campaign.Potions} potions / restore 24 health or revive. Click a party card, then H outside combat.\n\n{_campaign.Torches} torches / restore 45 light with T.\n\n{_campaign.Rations} rations / camps cost 2. Press R at a campsite.\n\n{_campaign.Gold} gold / inn 12, expedition kit 10.\n\n" + (_campaign.HasKey ? "An iron key. " : "") + (_campaign.HasRelic ? "The ember relic. " : "") + "Equipment is defined by class in this prototype.";
        }
        int wheel = _mouse.ScrollWheelValue - _oldMouse.ScrollWheelValue;
        if (_map.Contains(_mouse.Position))
        {
            if (wheel != 0)
            {
                _follow = false;
                _camera.ZoomAt(_mouse.Position.ToVector2(), Math.Sign(wheel));
            }
            if ((_mouse.MiddleButton == ButtonState.Pressed && _oldMouse.MiddleButton == ButtonState.Pressed) || (_mouse.RightButton == ButtonState.Pressed && _oldMouse.RightButton == ButtonState.Pressed))
            {
                _camera.Pan((_mouse.Position - _oldMouse.Position).ToVector2());
                _follow = false;
            }
            if (_click)
            {
                ClickMap(_mouse.Position.ToVector2());
            }
        }
        if (Pressed(Keys.G))
        {
            _follow = true;
            Recenter(true);
        }
        if (Pressed(Keys.D0))
        {
            _camera.Reset(32);
            _follow = true;
        }
        if (Pressed(Keys.OemPlus) || Pressed(Keys.PageUp))
        {
            _follow = false;
            _camera.ZoomAt(_camera.WorldToScreen(Point(_campaign.Position)), 1);
        }
        if (Pressed(Keys.OemMinus) || Pressed(Keys.PageDown))
        {
            _follow = false;
            _camera.ZoomAt(_camera.WorldToScreen(Point(_campaign.Position)), -1);
        }
        if (_campaign.Battle is { } b)
        {
            for (int i = 0; i < 6; i++)
            {
                if (Pressed(Keys.D1 + i))
                {
                    ChooseSkill(i);
                }
            }

            if (Pressed(Keys.Tab) || Pressed(Keys.Right))
            {
                var ids = b.Units.Where(u => Skills.Get(_campaign.Party[b.Active.HeroId >= 0 ? b.Active.HeroId : 0].Role, _skill).Target == TargetKind.Ally ? u.Friendly : !u.Friendly && u.Alive).Select(u => u.Id).ToArray();
                if (ids.Length > 0)
                {
                    _target = ids[(Array.IndexOf(ids, _target) + 1) % ids.Length];
                }
            }
            if (Pressed(Keys.Enter) || Pressed(Keys.Space))
            {
                DoSkill(_target);
            }

            return;
        }
        if (Pressed(Keys.E) || Pressed(Keys.Enter))
        {
            Apply(_campaign.Interact());
        }
        if (Pressed(Keys.OemPeriod) || Pressed(Keys.Space))
        {
            _route.Clear();
            Apply(_campaign.Wait());
        }

        if (Pressed(Keys.T))
        {
            Apply(_campaign.UseTorch());
        }

        if (Pressed(Keys.R))
        {
            Apply(_campaign.Camp());
        }

        if (Pressed(Keys.H))
        {
            Apply(_campaign.UsePotion(_selectedHero));
        }

        if (_time >= _moveAt)
        {
            int dx = 0, dy = 0;
            if (_keys.IsKeyDown(Keys.Left) || _keys.IsKeyDown(Keys.A))
            {
                dx = -1;
            }
            else if (_keys.IsKeyDown(Keys.Right) || _keys.IsKeyDown(Keys.D))
            {
                dx = 1;
            }
            else if (_keys.IsKeyDown(Keys.Up) || _keys.IsKeyDown(Keys.W))
            {
                dy = -1;
            }
            else if (_keys.IsKeyDown(Keys.Down) || _keys.IsKeyDown(Keys.S))
            {
                dy = 1;
            }

            if (dx != 0 || dy != 0)
            {
                _route.Clear();
                _routeObject = null;
                Move(dx, dy);
                _moveAt = _time + .13;
            }
        }
    }
    private void Move(int dx, int dy)
    {
        string zone = _campaign.ZoneId;
        var r = _campaign.Move(dx, dy);
        if (r.Message.Length > 0)
        {
            Apply(r);
        }

        if (_campaign.Battle is not null || _campaign.ZoneId != zone)
        {
            _follow = true;
            _route.Clear();
            _routeObject = null;
            _lastActive = -1;
            Recenter(true);
        }
    }
    private void ClickMap(Vector2 point)
    {
        if (_campaign.Battle is { } b)
        {
            var unit = b.Units.OrderBy(u => Vector2.DistanceSquared(_camera.WorldToScreen(Point(u.Position)), point)).FirstOrDefault();
            if (unit is not null && Vector2.DistanceSquared(_camera.WorldToScreen(Point(unit.Position)), point) < 28 * 28)
            {
                _target = unit.Id;
                DoSkill(unit.Id);
            }
            return;
        }
        var p = _camera.ScreenToWorld(point);
        Cell cell = new((int)Math.Floor(p.X), (int)Math.Floor(p.Y));
        if (!_campaign.Zone.Inside(cell) || !_campaign.Zone.Seen[_campaign.Zone.Index(cell)])
        {
            return;
        }

        var o = _campaign.ObjectAt(cell);
        if (o is not null && cell.Distance(_campaign.Position) <= 1)
        {
            Apply(_campaign.Interact(o.Id));
            return;
        }
        _route.Clear();
        _route.AddRange(_campaign.Route(cell));
        _routeCursor = 0;
        _routeObject = o?.Kind is ObjectKind.Portal or ObjectKind.Encounter ? null : o?.Id;
        if (_route.Count == 0)
        {
            Toast("No visible route. Use WASD or approach from another side.");
        }
    }
    private void ChooseSkill(int skill)
    {
        if (_campaign.Battle is not { } b || !b.Active.Friendly)
        {
            return;
        }

        _skill = skill;
        var definition = Skills.Get(_campaign.Party[b.Active.HeroId].Role, skill);
        if (definition.Target is TargetKind.AllEnemies or TargetKind.AllAllies or TargetKind.Self)
        {
            DoSkill(null);
        }
        else if (definition.Target == TargetKind.Ally)
        {
            _target = b.Units.Where(u => u.Friendly).OrderBy(u => u.Health / (float)u.MaxHealth).First().Id;
        }
        else
        {
            _target = b.Units.First(u => !u.Friendly && u.Alive).Id;
        }
    }
    private void DoSkill(int? target)
    {
        if (_campaign.Battle is not { } b || !b.Active.Friendly)
        {
            return;
        }

        var result = _campaign.Act(_skill, target);
        Apply(result);
        if (result.Success)
        {
            _lastActive = -1;
            _enemyAt = _time + .65;
        }
    }
    private void Save()
    {
        try
        {
            _campaign.Save(_options.Save);
            Toast("Expedition saved.");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Toast("Save failed: " + e.Message); }
    }
    private void Load()
    {
        try
        {
            _campaign = Campaign.Load(_options.Save);
            _autoSave = true;
            _route.Clear();
            _routeObject = null;
            _positions = new Vector2[4];
            _lastActive = -1;
            _eventWatermark = _campaign.CombatEvents.LastOrDefault()?.Sequence ?? 0;
            _popups.Clear();
            Recenter(true);
            Toast("Expedition restored.");
        }
        catch (Exception e) when (e is IOException or JsonException or ArgumentException) { Toast("Load failed: " + e.Message); }
    }
    protected override void Draw(GameTime gameTime)
    {
        int w = GraphicsDevice.Viewport.Width, h = GraphicsDevice.Viewport.Height;
        if (_frame is null || _frame.Width != w || _frame.Height != h)
        {
            _frame?.Dispose();
            _frame = new(GraphicsDevice, w, h, false, SurfaceFormat.Color, DepthFormat.None);
        }
        GraphicsDevice.SetRenderTarget(_frame);
        GraphicsDevice.Clear(Background);
        _hover = "";
        if (_atlas)
        {
            _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            DrawAtlas(w, h);
            _batch.End();
        }
        else
        {
            GraphicsDevice.ScissorRectangle = _map;
            _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, _scissor);
            DrawWorld();
            _batch.End();
            _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            DrawInterface(w, h);
            if (_intro || _dialogTitle.Length > 0)
            {
                DrawDialog(w, h);
            }

            _batch.End();
        }
        GraphicsDevice.SetRenderTarget(null);
        _batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        _batch.Draw(_frame, new Rectangle(0, 0, w, h), Color.White);
        _batch.End();
        _draws++;
        int end = _options.SmokeTest ? 12 : _options.Frames;
        if (end > 0 && _draws >= end)
        {
            if (_options.Screenshot is { } file)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                using var stream = File.Create(file);
                _frame.SaveAsPng(stream, w, h);
            }
            if (_options.SmokeTest)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    graphicalSmokeTest = "passed",
                    checks = _checks,
                    sprites = _entries.Count
                }));
            }

            Exit();
        }
        base.Draw(gameTime);
    }
    protected override void UnloadContent()
    {
        _frame?.Dispose();
        _sprites.Dispose();
        _batch.Dispose();
        _pixel.Dispose();
        _scissor.Dispose();
        base.UnloadContent();
    }
    private void Smoke()
    {
        void Check(bool ok, string name)
        {
            if (!ok)
            {
                throw new InvalidOperationException("RPG smoke: " + name);
            }

            _checks.Add(name);
        }
        Check(_entries.Count == 7115, "all occupied sprites available in viewer");
        Check(WorldBuilder.Validate(_campaign.Zones).Valid, "generated quest routes and sealed boss chamber validate");
        Check(_campaign.Zones[2].Generation.Rooms.Count == 35 && !_campaign.Zones[2].Generation.UsedFallback, "WFC assembled 35 matching rooms");
        Check(_intro, "tutorial opens");
        _intro = false;
        Check(_campaign.Move(1, 0).Success, "cardinal movement");
        Check(_campaign.TravelTo("to-wilds").Success, "town-to-forest transition");
        Check(_campaign.TravelTo("wilds-hounds").Success && _campaign.Battle is not null, "visible encounter starts on exploration map");
        var b = _campaign.Battle!;
        Check(b.Active.Friendly, "party initiative");
        int hp = b.Units[4].Health;
        _skill = 0;
        DoSkill(4);
        Check(b.Units[4].Health < hp, "targeted skill damages selected enemy");
        var loaded = Campaign.FromJson(_campaign.StateJson());
        Check(loaded.StateJson() == _campaign.StateJson(), "mid-combat save restores exactly");
        _campaign.AutoBattle();
        Check(_campaign.Completed.Contains("wilds-hounds"), "combat victory clears encounter");
        int environmentTick = _campaign.Zone.Environment.Ticks;
        for (int i = 0; i < 4; i++) _campaign.Wait();
        Check(_campaign.Zone.Environment.Ticks == environmentTick + 1, "four exploration turns advance cellular hazards");
        Check(Campaign.FromJson(_campaign.StateJson()).StateJson() == _campaign.StateJson(), "generated maps and evolving hazards save exactly");
        var pointer = new Vector2(_map.X + 150, _map.Y + 100);
        var anchor = _camera.ScreenToWorld(pointer);
        _camera.ZoomAt(pointer, 1);
        Check(Vector2.Distance(anchor, _camera.ScreenToWorld(pointer)) < .0001f, "pointer zoom anchor");
        _camera.Reset(32);
        _search = "potion";
        FilterAtlas();
        Check(_filtered.Count > 8, "atlas search finds cross-pack candidates");
        _search = "";
        FilterAtlas();
        _search = "runtime_rule";
        FilterAtlas();
        Check(_filtered.Count >= 40, "atlas searches per-sprite generation rules");
        _search = "";
        FilterAtlas();
        _campaign = new Campaign(_options.Seed);
        _campaign.RunDemoExpedition();
        Check(_campaign.Victory, "full expedition and return-to-town quest");
        _campaign = new Campaign(_options.Seed);
        _campaign.TravelTo("to-wilds");
        _campaign.TravelTo("wilds-hounds");
        _lastActive = -1;
        _dialogTitle = "";
        _route.Clear();
        _positions = new Vector2[4];
        Recenter(true);
    }
}
