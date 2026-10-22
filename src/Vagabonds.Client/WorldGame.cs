using System.Diagnostics;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.World;
using Vagabonds.Graphics;

namespace Vagabonds.Client;

internal sealed class WorldGame : Game
{
    private readonly Options _options;
    private readonly GraphicsDeviceManager _graphics;
    private WorldSession _session;
    private bool _autoSave;
    private SpriteLibrary _sprites = null!;
    private SpriteBatch _batch = null!;
    private Texture2D _pixel = null!;
    private RenderTarget2D? _frame;
    private readonly MapCamera _camera = new();
    private Rectangle _map, _side;
    private KeyboardState _keyboard;
    private MouseState _mouse;
    private string _panel = "", _tool = "", _notice = "Space resumes the world. E speaks to someone nearby.";
    private long _person;
    private Material _build = Material.Wall;
    private int _skill, _combatTarget = -1, _journalOffset;
    private int _inspectStage;
    private readonly Queue<WorldPosition> _route = [];
    private readonly WorldPointerPreview _pointer = new();
    private readonly GridMotion _partyMotion = new();
    private readonly Dictionary<long, GridMotion> _actorMotions = [];
    private WorldSession? _visualSession;
    private long _visualLeader;
    private const double MoveDuration = .14;
    private double _moveTimer, _enemyTimer;
    private int _draws;
    private readonly Stopwatch _runtime = new();
    private readonly List<double> _updates = [];
    private readonly List<double> _frames = [];
    private long _lastDraw;
    private static readonly Color Ink = new(222, 214, 189), Muted = new(151, 154, 150), Gold = new(234, 187, 94), Panel = new(27, 33, 38);
    private static readonly Material[] BuildChoices = [Material.Wall, Material.Floor, Material.Door, Material.Bridge, Material.Storage, Material.Camp];
    public WorldGame(Options options, WorldSession session)
    {
        _options = options; _session = session; _autoSave = !options.Preview;
        if (_autoSave) _session.SavePath = options.Save;
        if (!options.Load && options.Scene == "world-inspector") _panel = "inspector";
        if (!options.Load && options.Scene == "world-site")
        { _session.State.Position = _session.Plan.Sites[0].Center; foreach (var a in _session.Party) a.Position = _session.State.Position; _session.Chunks.Prefetch(_session.State.Position); _session.Reveal(); }
        if (!options.Load && options.Scene == "world-battle")
        {
            foreach (var person in _session.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(3).ToArray())
            { person.Position = _session.State.Position; _session.Submit(new(WorldCommandKind.Recruit, ActorId: person.Id)); }
            _session.StartBattle(_session.State.Actors.First(a => a.Kind == ActorKind.Beast).Id);
        }
        _graphics = new(this) { PreferredBackBufferWidth = options.Width, PreferredBackBufferHeight = options.Height, SynchronizeWithVerticalRetrace = true };
        Window.Title = "Vagabonds - the living world"; Window.AllowUserResizing = true;
        Window.ClientSizeChanged += (_, _) =>
        {
            if (Window.ClientBounds.Width >= 1024 && Window.ClientBounds.Height >= 720) return;
            _graphics.PreferredBackBufferWidth = Math.Max(1024, Window.ClientBounds.Width);
            _graphics.PreferredBackBufferHeight = Math.Max(720, Window.ClientBounds.Height); _graphics.ApplyChanges();
        };
        IsMouseVisible = true; IsFixedTimeStep = false;
        Exiting += (_, _) => { if (_autoSave) TrySave(); Capture(); };
    }
    protected override void LoadContent()
    {
        _batch = new(GraphicsDevice); _pixel = new(GraphicsDevice, 1, 1); _pixel.SetData(new[] { Color.White });
        _sprites = new(GraphicsDevice, Path.Combine(AppContext.BaseDirectory, "assets"));
        foreach (Material material in Enum.GetValues<Material>()) foreach (Biome biome in Enum.GetValues<Biome>())
            foreach (var culture in SampleLibrary.Default.Cultures)
                if (!_sprites.HasBinding(Materials.Sprite(material, biome, culture))) throw new InvalidDataException("Missing world material sprite.");
        _camera.Reset(32); UpdateMotions(TimeSpan.Zero); _runtime.Start();
    }
    private void Layout()
    {
        int w = GraphicsDevice.Viewport.Width, h = GraphicsDevice.Viewport.Height;
        _map = new(16, 84, Math.Max(300, w - 350), Math.Max(260, h - 230));
        _side = new(w - 318, 84, 302, h - 100);
        _camera.Viewport = _map;
    }
    protected override void Update(GameTime gameTime)
    {
        var timer = Stopwatch.StartNew(); Layout();
        var keys = Keyboard.GetState(); var mouse = Mouse.GetState();
        bool Press(Keys key) => keys.IsKeyDown(key) && !_keyboard.IsKeyDown(key);
        _session.Focused = IsActive;
        if (IsActive && !_options.WorldSmoke)
        {
            if (Press(Keys.Q) && (keys.IsKeyDown(Keys.LeftControl) || keys.IsKeyDown(Keys.RightControl))) Exit();
            if (Press(Keys.F11)) _graphics.ToggleFullScreen();
            if (Press(Keys.F5)) TrySave();
            if (Press(Keys.F9)) TryLoad();
            if (Press(Keys.Escape)) { _panel = ""; _tool = ""; _route.Clear(); }
            if (Press(Keys.F1)) Toggle("guide");
            if (Press(Keys.F3)) Toggle("inspector");
            if (Press(Keys.I)) Toggle("inventory");
            if (Press(Keys.J)) Toggle("journal");
            if (Press(Keys.M)) Toggle("map");
            if (_session.State.AwaitingContinuation)
            {
                if (Press(Keys.N))
                {
                    if (_session.DeathPending) { _notice = "F5 must persist the deaths before starting another expedition."; _keyboard = keys; _mouse = mouse; return; }
                    if (keys.IsKeyDown(Keys.LeftShift) || keys.IsKeyDown(Keys.RightShift))
                    {
                        _session = WorldSession.Create(unchecked(_session.Seed + 1)); if (_autoSave) _session.SavePath = _options.Save;
                        _notice = "A fresh world begins. Space resumes.";
                    }
                    else Execute(new(WorldCommandKind.Continue));
                    _camera.Center = Vector2.Zero; _route.Clear();
                }
            }
            else if (_session.IsInCombat && _panel.Length == 0) CombatInput(keys, mouse, Press);
            else if (_panel == "person")
            {
                if (Press(Keys.R)) { Execute(new(WorldCommandKind.Recruit, ActorId: _person)); _panel = ""; }
                if (Press(Keys.T)) _panel = "trade";
            }
            else if (_panel == "trade")
            {
                string[] items = ["food", "wood", "stone", "medicine"];
                for (int i = 0; i < 4; i++) if (Press(Keys.D1 + i)) Execute(new(keys.IsKeyDown(Keys.LeftShift) || keys.IsKeyDown(Keys.RightShift) ? WorldCommandKind.Sell : WorldCommandKind.Buy, Item: items[i], Amount: 1));
            }
            else if (_panel == "inventory")
            {
                var party = _session.Party.ToArray();
                for (int i = 0; i < party.Length; i++) if (Press(Keys.D1 + i))
                    Execute(new(keys.IsKeyDown(Keys.LeftShift) || keys.IsKeyDown(Keys.RightShift) ? WorldCommandKind.Abandon : WorldCommandKind.Revive, ActorId: party[i].Id));
            }
            else if (_panel == "journal")
            {
                if (Press(Keys.PageUp)) _journalOffset += 6;
                if (Press(Keys.PageDown)) _journalOffset = Math.Max(0, _journalOffset - 6);
            }
            else if (_panel == "inspector" && Press(Keys.Tab)) _inspectStage = (_inspectStage + 1) % 3;
            else if (_panel.Length == 0)
            {
                if (Press(Keys.Space)) { _session.Paused = !_session.Paused; if (_session.Paused) _route.Clear(); }
                if (Press(Keys.B)) _tool = _tool == "build" ? "" : "build";
                if (Press(Keys.X)) _tool = _tool == "dig" ? "" : "dig";
                if (_tool == "build") for (int i = 0; i < 6; i++) if (Press(Keys.D1 + i)) _build = BuildChoices[i];
                if (Press(Keys.E) || Press(Keys.Enter)) Interact();
                if (Press(Keys.R)) Execute(new(WorldCommandKind.Camp));
                if (Press(Keys.F)) Execute(new(WorldCommandKind.ResolveArc));
                if (Press(Keys.V)) Execute(new(WorldCommandKind.CompleteDelivery));
                if (Press(Keys.G) || Press(Keys.H))
                {
                    var storage = WorldSession.Directions.Select(d => _session.State.Position.Offset(d.X, d.Y)).FirstOrDefault(p => _session.Chunks.At(p) == Material.Storage, _session.State.Position);
                    Execute(new(Press(Keys.G) ? WorldCommandKind.Store : WorldCommandKind.Take, storage, Item: "wood", Amount: 1));
                }
                if (mouse.ScrollWheelValue != _mouse.ScrollWheelValue && _map.Contains(mouse.Position)) _camera.ZoomAt(new(mouse.X, mouse.Y), Math.Sign(mouse.ScrollWheelValue - _mouse.ScrollWheelValue));
                if (mouse.LeftButton == ButtonState.Pressed && _mouse.LeftButton == ButtonState.Released && _map.Contains(mouse.Position)) ClickMap(mouse);
                if (mouse.RightButton == ButtonState.Pressed) { _tool = ""; _route.Clear(); }
                _moveTimer += gameTime.ElapsedGameTime.TotalSeconds;
                if (!_session.Frozen && _moveTimer >= MoveDuration)
                {
                    int dx = keys.IsKeyDown(Keys.A) || keys.IsKeyDown(Keys.Left) ? -1 : keys.IsKeyDown(Keys.D) || keys.IsKeyDown(Keys.Right) ? 1 : 0;
                    int dy = keys.IsKeyDown(Keys.W) || keys.IsKeyDown(Keys.Up) ? -1 : keys.IsKeyDown(Keys.S) || keys.IsKeyDown(Keys.Down) ? 1 : 0;
                    if (dx != 0 || dy != 0) { _route.Clear(); Execute(new(WorldCommandKind.Move, _session.State.Position.Offset(dx, dx != 0 ? 0 : dy))); _moveTimer = 0; }
                    else if (_route.TryDequeue(out var next)) { if (!Execute(new(WorldCommandKind.Move, next))) _route.Clear(); _moveTimer = 0; }
                }
            }
        }
        _session.ModalOpen = _panel.Length > 0;
        // Long stalls/focus changes discard elapsed time rather than simulating an offline catch-up.
        _session.AdvanceFrame(gameTime.ElapsedGameTime <= TimeSpan.FromSeconds(5) ? gameTime.ElapsedGameTime : TimeSpan.Zero);
        if (_session.IsInCombat)
        {
            _route.Clear(); _enemyTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (!_session.State.Battle!.Active.Friendly && _enemyTimer > .65 && _panel.Length == 0 && IsActive) { _notice = _session.AdvanceEnemy().Message; _enemyTimer = 0; }
        }
        else _enemyTimer = 0;
        UpdateMotions(gameTime.ElapsedGameTime);
        if (_updates.Count < 10000) _updates.Add(timer.Elapsed.TotalMilliseconds);
        _keyboard = keys; _mouse = mouse; base.Update(gameTime);
    }
    private void Toggle(string panel) { _panel = _panel == panel ? "" : panel; _route.Clear(); }
    private bool Execute(WorldCommand command)
    {
        try { var result = _session.Submit(command); _notice = result.Message; return result.Success; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { _notice = "Save failed: " + e.Message + " F5 retries."; return false; }
    }
    private void Interact()
    {
        var person = _session.NearbyPerson();
        if (person != null)
        {
            _person = person.Id; Execute(new(WorldCommandKind.Interact, ActorId: person.Id)); _panel = "person"; _route.Clear(); return;
        }
        var cells = new[] { _session.State.Position }.Concat(WorldSession.Directions.Select(d => _session.State.Position.Offset(d.X, d.Y)));
        var stair = cells.FirstOrDefault(p => _session.Chunks.At(p) is Material.StairsDown or Material.StairsUp, _session.State.Position);
        Execute(new(WorldCommandKind.Interact, stair)); _camera.Center = Vector2.Zero; _route.Clear();
    }
    private void ClickMap(MouseState mouse)
    {
        UpdatePointer(mouse.Position);
        if (_pointer.Cell is not { } p) return;
        if (!_session.Chunks.Seen(p)) { _notice = "Explore this ground first."; return; }
        if (_tool == "build") { Execute(new(WorldCommandKind.Build, p, Material: _build)); return; }
        if (_tool == "dig") { Execute(new(WorldCommandKind.Dig, p)); return; }
        var person = _session.State.Actors.FirstOrDefault(a => a.Position == p && !a.Recruited && a.Kind == ActorKind.Person && a.Life == LifeState.Alive);
        if (person != null && _session.Visible.Contains(p) && p.Distance(_session.State.Position) <= 2) { _person = person.Id; Execute(new(WorldCommandKind.Interact, ActorId: person.Id)); _panel = "person"; return; }
        _route.Clear(); foreach (var cell in _pointer.Path) _route.Enqueue(cell);
        _notice = _route.Count == 0 ? "No known route. Explore manually or change the terrain." : _session.Paused ? "Route ready. Space resumes travel." : "Following known ground.";
    }
    private void CombatInput(KeyboardState keys, MouseState mouse, Func<Keys, bool> press)
    {
        _ = keys;
        var battle = _session.State.Battle!;
        if (!battle.Active.Friendly) return;
        for (int i = 0; i < 6; i++) if (press(Keys.D1 + i)) { _skill = i; UseSkill(); }
        if (mouse.LeftButton != ButtonState.Pressed || _mouse.LeftButton != ButtonState.Released) return;
        foreach (var unit in battle.Units) if (UnitRect(unit).Contains(mouse.Position)) { _combatTarget = unit.Id; UseSkill(); return; }
    }
    private void UseSkill()
    {
        var battle = _session.State.Battle!; var skill = _session.ActiveSkills()[_skill];
        var selected = battle.Units.FirstOrDefault(u => u.Id == _combatTarget && (skill.Target == TargetKind.Ally ? u.Friendly : !u.Friendly && u.Alive));
        selected ??= skill.Target == TargetKind.Ally ? battle.Units.Where(u => u.Friendly).MinBy(u => u.Health / (float)u.MaxHealth) : battle.Units.FirstOrDefault(u => !u.Friendly && u.Alive);
        try { _notice = _session.Act(_skill, selected?.Id ?? -1).Message; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { _notice = "Death save failed: " + e.Message + " F5 retries."; }
        _enemyTimer = 0;
    }
    private void TrySave()
    {
        try { _session.Save(_options.Save); _autoSave = true; _notice = "World saved."; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { _notice = "Save failed: " + e.Message; Console.Error.WriteLine(_notice); }
    }
    private void TryLoad()
    {
        try
        {
            var replacement = WorldSession.Load(_options.Save);
            _session = replacement; _autoSave = true; _route.Clear(); _panel = ""; _person = 0; _camera.Center = Vector2.Zero;
            _tool = ""; _skill = 0; _combatTarget = -1; _moveTimer = _enemyTimer = 0;
            _notice = "World restored and paused. Space resumes.";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or JsonException or ArgumentException)
        {
            _notice = e is IOException or UnauthorizedAccessException
                ? "Load failed: the selected save could not be read. The current world is preserved."
                : "Load failed: incompatible, damaged or incomplete world save. The current world is preserved.";
        }
    }
    private void Fill(Rectangle r, Color color) => _batch.Draw(_pixel, r, color);
    private void Text(string text, int x, int y, Color? color = null, float scale = 1) => _sprites.Text(_batch, text, new(x, y), color ?? Ink, scale);
    private int Wrap(string text, int x, int y, int width, Color? color = null) => (int)_sprites.Wrapped(_batch, text, new(x, y), width, color ?? Ink) + 6;
    private Rectangle TileRect(WorldPosition p)
    {
        var pos = _camera.WorldToScreen(new(p.X, p.Y));
        return new((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y), (int)Math.Ceiling(_camera.CellSize), (int)Math.Ceiling(_camera.CellSize));
    }
    private void UpdateMotions(TimeSpan elapsed)
    {
        long leader = _session.State.Party[0];
        if (!ReferenceEquals(_visualSession, _session) || _visualLeader != leader)
        {
            _visualSession = _session; _visualLeader = leader; _actorMotions.Clear();
            _partyMotion.Reset(_session.State.Position, resetFacing: true);
        }
        _partyMotion.Observe(_session.State.Position, MoveDuration);
        _partyMotion.Advance(elapsed);
        _camera.Center = _partyMotion.Position;
        foreach (var actor in _session.State.Actors)
        {
            if (actor.Recruited || actor.Life == LifeState.Dead) { _actorMotions.Remove(actor.Id); continue; }
            if (!_actorMotions.TryGetValue(actor.Id, out var motion)) _actorMotions[actor.Id] = motion = new();
            motion.Observe(actor.Position, 1); motion.Advance(elapsed);
        }
    }
    private Rectangle ActorRect(Vector2 position)
    {
        var center = _camera.WorldToScreen(position); int size = (int)Math.Ceiling(_camera.CellSize);
        return new((int)Math.Round(center.X - size / 2f), (int)Math.Round(center.Y - size / 2f), size, size);
    }
    private void UpdatePointer(Point point) => _pointer.Update(_session, _camera, point,
        enabled: _panel.Length == 0 && !_session.IsInCombat && !_session.State.AwaitingContinuation,
        navigationEnabled: _tool.Length == 0);
    protected override void Draw(GameTime gameTime)
    {
        Layout(); int w = GraphicsDevice.Viewport.Width, h = GraphicsDevice.Viewport.Height;
        if (_frame == null || _frame.Width != w || _frame.Height != h) { _frame?.Dispose(); _frame = new(GraphicsDevice, w, h); }
        GraphicsDevice.SetRenderTarget(_frame); GraphicsDevice.Clear(new Color(16, 22, 27));
        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        DrawTerrain(); DrawHud(w, h);
        if (_session.IsInCombat) DrawCombat();
        if (_panel.Length > 0) DrawPanel(w, h);
        if (_session.State.AwaitingContinuation) DrawWipe(w, h);
        _batch.End(); GraphicsDevice.SetRenderTarget(null);
        _batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp); _batch.Draw(_frame, new Rectangle(0, 0, w, h), Color.White); _batch.End();
        if (_lastDraw > 0 && _frames.Count < 10000) _frames.Add(Stopwatch.GetElapsedTime(_lastDraw).TotalMilliseconds); _lastDraw = Stopwatch.GetTimestamp();
        _draws++;
        int end = _options.WorldSmoke ? Math.Max(120, _options.Frames) : _options.Frames;
        if (end > 0 && _draws >= end)
        {
            Capture();
            if (_options.WorldSmoke)
            {
                var checks = new Dictionary<string, bool> { ["expectedGeneratedParty"] = _session.State.Party.Count == (_options.Scene == "world-battle" ? 4 : 1), ["finiteWorld"] = _session.Plan.Towns.Count == 24 && _session.Plan.Sites.Count == 48, ["startsPaused"] = _session.Paused && _session.State.Tick == 0, ["discovery"] = _session.Chunks.Seen(_session.State.Position), ["boundedCache"] = _session.Chunks.ResidentCount <= 25, ["materialBindings"] = true, ["renderTarget"] = _frame.Width == w && _frame.Height == h };
                Console.WriteLine(JsonSerializer.Serialize(new { worldSmoke = checks.Values.All(c => c) ? "passed" : "failed", checks, frames = _draws, updateP95Ms = Percentile(_updates), frameP95Ms = Percentile(_frames), averageFramesPerSecond = _draws / _runtime.Elapsed.TotalSeconds, residentChunks = _session.Chunks.ResidentCount }));
                if (checks.Values.Any(c => !c)) throw new InvalidDataException("Native world smoke check failed.");
            }
            Exit();
        }
        base.Draw(gameTime);
    }
    private void Capture()
    {
        if (_frame == null || _options.Screenshot is not { } path) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); using var file = File.Create(path); _frame.SaveAsPng(file, _frame.Width, _frame.Height);
    }
    private static double Percentile(List<double> values) => values.Count == 0 ? 0 : values.Order().ElementAt((int)((values.Count - 1) * .95));
    private void DrawTerrain()
    {
        Fill(_map, new(9, 16, 21));
        var left = _camera.ScreenToWorld(new(_map.Left, _map.Top)); var right = _camera.ScreenToWorld(new(_map.Right, _map.Bottom));
        for (int y = (int)Math.Floor(left.Y); y <= (int)right.Y; y++) for (int x = (int)Math.Floor(left.X); x <= (int)right.X; x++)
        {
            var p = new WorldPosition(x, y, _session.State.Position.Floor);
            if (!_session.Chunks.Seen(p)) continue;
            var rect = TileRect(p); if (!_map.Contains(rect)) continue;
            var chunk = _session.Chunks.Get(p.Chunk); var biome = chunk.Biomes[p.LocalIndex]; int culture = chunk.Cultures[p.LocalIndex];
            var material = _session.Chunks.At(p);
            Color tint = material switch { Material.Water => new(72, 132, 158), Material.Tree => new(104, 141, 104), Material.StairsDown or Material.StairsUp => Gold, Material.Camp => new(241, 159, 88), _ => biome switch { Biome.Forest => new(127, 150, 112), Biome.Marsh => new(127, 144, 110), Biome.Badland => new(187, 144, 103), Biome.Snow => new(199, 211, 217), Biome.Highland => new(160, 163, 159), _ => new(156, 170, 123) } };
            if (material is Material.Wall or Material.Floor or Material.Door) tint = new(191, 184, 163);
            bool bright = _session.Visible.Contains(p);
            float light = bright ? 1 : .35f;
            bool ground = material is Material.Grass or Material.Earth or Material.Floor;
            var background = material switch { Material.Grass => new Color(29, 42, 33), Material.Floor => new Color(51, 45, 36), Material.Earth => new Color(49, 43, 33), Material.Water => new Color(19, 38, 47), _ => new Color(32, 36, 34) };
            Fill(rect, background * light);
            if (material != Material.Grass || WorldRandom.Hash(_session.Seed, x, y) % 3 == 0)
                _sprites.Draw(_batch, Materials.Sprite(material, biome, culture < 0 ? null : SampleLibrary.Default.Cultures[culture]), rect, tint * light * (ground ? .32f : 1));
            if (ground && WorldRandom.Hash(_session.Seed, x, y, 999) % (culture < 0 ? 33 : 61) == 0)
            {
                string prop = culture < 0 ? "shrub" : _session.State.Position.Floor > 0 ? "bones" : "barrel";
                _sprites.Draw(_batch, prop, rect, new Color(154, 141, 105) * light);
            }
            if (material == Material.Door) Fill(new(rect.Center.X - 2, rect.Top + rect.Height / 3, 4, rect.Height / 2), Gold * .7f);
        }
        UpdatePointer(_mouse.Position);
        if (_panel.Length == 0 && !_session.IsInCombat && !_session.State.AwaitingContinuation)
        {
            if (_pointer.Path.Count > 0 && (_route.Count == 0 || _route.Last() != _pointer.Path[^1])) DrawRoute(_pointer.Path, new(113, 187, 195), 2);
            DrawRoute(_route, Gold, 3);
        }
        foreach (var actor in _session.State.Actors.Where(a => !a.Recruited && a.Life != LifeState.Dead && _session.Visible.Contains(a.Position)))
        {
            var motion = _actorMotions[actor.Id]; var rect = ActorRect(motion.Position); if (!_map.Contains(rect)) continue;
            _sprites.Draw(_batch, actor.Sprite, rect, actor.Kind is ActorKind.Beast or ActorKind.Raider ? new(224, 119, 103) : new(212, 204, 176), effects: motion.FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        }
        int slot = 0;
        foreach (var actor in _session.Party.Where(a => a.Life != LifeState.Dead))
        {
            var rect = ActorRect(_partyMotion.Position); rect.Offset(slot % 2 * 8 - 4, slot / 2 * 8 - 4);
            _sprites.Draw(_batch, actor.Sprite, rect, actor.Life == LifeState.Downed ? new(139, 100, 100) : Ink, effects: _partyMotion.FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None); slot++;
        }
        if (_pointer.Cell is { } cell)
        {
            var rect = TileRect(cell); FillClipped(rect, Gold * .1f);
            int edge = Math.Clamp(rect.Width / 3, 3, 12), stroke = rect.Width >= 16 ? 2 : 1;
            foreach (int x in new[] { rect.Left, rect.Right - edge })
                foreach (int y in new[] { rect.Top, rect.Bottom - stroke }) FillClipped(new(x, y, edge, stroke), Gold);
            foreach (int x in new[] { rect.Left, rect.Right - stroke })
                foreach (int y in new[] { rect.Top, rect.Bottom - edge }) FillClipped(new(x, y, stroke, edge), Gold);
        }
    }
    private void FillClipped(Rectangle rectangle, Color color)
    {
        var clipped = Rectangle.Intersect(_map, rectangle);
        if (clipped.Width > 0 && clipped.Height > 0) Fill(clipped, color);
    }
    private void DrawRoute(IEnumerable<WorldPosition> cells, Color color, int thickness)
    {
        var previous = _camera.WorldToScreen(new(_session.State.Position.X + .5f, _session.State.Position.Y + .5f));
        foreach (var cell in cells)
        {
            var center = _camera.WorldToScreen(new(cell.X + .5f, cell.Y + .5f));
            int x = (int)Math.Round(center.X), y = (int)Math.Round(center.Y), px = (int)Math.Round(previous.X), py = (int)Math.Round(previous.Y);
            FillClipped(new(Math.Min(px, x) - thickness / 2, Math.Min(py, y) - thickness / 2, Math.Abs(x - px) + thickness, Math.Abs(y - py) + thickness), color * .75f);
            FillClipped(new(x - thickness, y - thickness, thickness * 2 + 1, thickness * 2 + 1), color);
            previous = center;
        }
    }
    private void DrawHud(int w, int h)
    {
        Text("VAGABONDS", 20, 16, Gold, 2);
        long tick = _session.State.Tick;
        Text($"Day {tick / 7200 + 1}  {tick / 300 % 24:00}:{tick / 5 % 60:00}    {(_session.Frozen ? "PAUSED" : "LIVING WORLD")}", 300, 22);
        Text($"{_session.State.Position.X}, {_session.State.Position.Y}  / floor {_session.State.Position.Floor}    seed {_session.Seed}", 20, 57, Muted);
        Fill(_side, Panel); int x = _side.X + 14, y = _side.Y + 14;
        Text("THE EXPEDITION", x, y, Gold); y += 30;
        foreach (var a in _session.Party)
        {
            _sprites.Fit(_batch, a.Sprite, new(x, y, 38, 38), a.Life == LifeState.Alive ? Ink : new(166, 113, 113));
            Text(a.Name, x + 46, y); Text($"{new Hero { Role = a.Role }.RoleName}  L{a.Level}", x + 46, y + 18, Muted);
            y += 42; Text(a.Life == LifeState.Alive ? $"HP {a.Health}/{a.MaxHealth}  MP {a.Mana}/{a.MaxMana}" : a.Life.ToString(), x, y); y += 27;
        }
        y += 8;
        Text($"Food {_session.Count("food")}  Gold {_session.Count("gold")}", x, y, Gold); y += 22;
        Text($"Wood {_session.Count("wood")}  Stone {_session.Count("stone")}", x, y, Muted); y += 22;
        Text($"Medicine {_session.Count("medicine")}", x, y, Muted); y += 34;
        if (y < _side.Bottom - 100) { Text(_session.State.Arc.Status == ArcStatus.Active ? "A CHANGING WORLD" : _session.State.Arc.Status.ToString().ToUpperInvariant(), x, y, Gold); y += 26; Wrap(_session.State.Arc.Objective, x, y, _side.Width - 28); }
        int bottom = _map.Bottom + 12;
        Wrap(_notice.Length > 150 ? _notice[..147] + "..." : _notice, 20, bottom, w - 370); bottom += 42;
        Text("WASD move  E speak/stairs  Space pause  I pack  J journal  M map", 20, Math.Min(bottom, h - 48), Muted);
        Text(_tool == "build" ? $"BUILD {_build} | 1 wall 2 floor 3 door 4 bridge 5 store 6 camp | click adjacent" : _tool == "dig" ? "DIG | click adjacent rock, wall or tree | food pays for the work" : "B build  X dig  R rest  F resolve arc  F1 guide  F3 inspect  F5 save  F9 load", 20, h - 24, _tool.Length > 0 ? Gold : Muted);
    }
    private Rectangle UnitRect(CombatUnit unit)
    {
        var b = _session.State.Battle!; int index = b.Units.Take(unit.Id).Count(u => u.Friendly == unit.Friendly);
        int row = Math.Min(90, Math.Max(64, (_map.Height - 180) / 4));
        return new(unit.Friendly ? _map.X + 24 : _map.Right - 280, _map.Y + 70 + index * row, 252, Math.Min(78, row - 6));
    }
    private void DrawCombat()
    {
        Fill(_map, new Color(14, 19, 25) * .96f); var b = _session.State.Battle!;
        Text($"ENCOUNTER  /  ROUND {b.Round}", _map.X + 24, _map.Y + 18, Gold);
        Text(b.Active.Name + "'s turn", _map.X + 24, _map.Y + 42);
        foreach (var unit in b.Units)
        {
            var r = UnitRect(unit); Fill(r, unit.Id == b.Active.Id ? new(66, 65, 49) : Panel);
            _sprites.Fit(_batch, unit.Sprite, new(r.X + 8, r.Y + 8, 48, 48), unit.Alive ? Ink : Muted * .4f);
            Text(unit.Name, r.X + 64, r.Y + 8); Text($"HP {unit.Health}/{unit.MaxHealth}", r.X + 64, r.Y + 28, unit.Friendly ? Ink : new(222, 138, 125));
            Text(unit.Friendly ? $"MP {unit.Mana} {unit.Conditions}" : $"{unit.Intent} {unit.Conditions}", r.X + 64, r.Y + 48, Muted);
        }
        if (b.Active.Friendly)
        {
            int y = _map.Bottom - 102; var skills = _session.ActiveSkills();
            for (int i = 0; i < skills.Count; i++) Text($"{i + 1} {skills[i].Name} ({skills[i].Mana} MP)", _map.X + 24 + i % 2 * 320, y + i / 2 * 26, i == _skill ? Gold : Ink);
        }
    }
    private void DrawPanel(int w, int h)
    {
        var box = new Rectangle(48, 70, w - 96, h - 130); Fill(box, new(24, 31, 37));
        int x = box.X + 24, y = box.Y + 20, width = box.Width - 48;
        Text(_panel.ToUpperInvariant(), x, y, Gold, 2); y += 42;
        if (_panel == "guide")
        {
            string[] lines = ["You arrive alone in a finite world of towns, ruins and competing compacts.", "Space resumes or pauses time. Explore with WASD/arrows or click known ground.", "E speaks to nearby people or takes stairs. In conversation, R recruits and T trades.", "I opens your pack: 1-4 revives a downed companion; Shift+1-4 abandons them permanently.", "B selects building; 1-6 chooses wall/floor/door/bridge/storage/camp. Click adjacent ground.", "X digs adjacent walls, rock and trees; food funds work and materials are recovered.", "R rests at a camp. G stores one wood; H retrieves it from adjacent storage.", "J shows the main arc and recent history. M shows places you have discovered.", "F presents evidence at the arc's town. A dead patron's townspeople can accept it.", "Combat pauses the world: 1-6 uses a skill; click a fighter to choose its target.", "A wipe kills the party permanently. N starts a new traveller in this changed world.", "Shift+N after a wipe creates a fresh world. F5 saves, F9 loads, Ctrl+Q saves/quits.", "Escape closes a panel or tool. F3 opens generation inspection."];
            foreach (string line in lines) y += Wrap(line, x, y, width);
        }
        else if (_panel == "person")
        {
            var a = _session.State.Actors.Single(a => a.Id == _person);
            _sprites.Fit(_batch, a.Sprite, new(x, y, 96, 96), Ink); y += 106;
            y += Wrap(_notice, x, y, width); y += 20;
            Text("R  Invite them to travel with you", x, y, Gold); y += 28;
            Text("T  Trade with the town", x, y, Gold); y += 28;
            Text("Their former job will be vacant if they join.", x, y, Muted);
        }
        else if (_panel == "trade")
        {
            var town = _session.Plan.Towns.FirstOrDefault(t => _session.State.Position.Distance(t.Center) <= 28);
            if (town == null) Text("Reach a town to trade.", x, y);
            else
            {
                Text(town.Name, x, y); y += 32; int i = 1;
                foreach (var (item, stock) in _session.State.Towns[town.Id].Stocks) { Text($"{i++}  {item,-10} stock {stock,-5} price {_session.State.Towns[town.Id].Price(item)} gold", x, y); y += 30; }
                Text("1-4 buys one. Shift+1-4 sells one at half the asking price.", x, y + 12, Muted);
            }
        }
        else if (_panel == "inventory")
        {
            foreach (var (item, amount) in _session.State.Inventory) { Text($"{item}: {amount}", x, y); y += 26; }
            y += 20; int i = 1;
            foreach (var a in _session.Party) { Text($"{i++}  {a.Name}: {a.Life}", x, y); y += 28; }
            y += Wrap("Press a downed companion's number to spend one medicine and revive them. Shift plus their number deliberately abandons them and records permanent death immediately.", x, y + 12, width, Muted);
        }
        else if (_panel == "journal")
        {
            y += Wrap($"Main arc: {_session.State.Arc.Status}. {_session.State.Arc.Objective}", x, y, width, Gold); y += 14;
            foreach (var task in _session.State.Tasks.Where(t => !t.Complete).Take(3)) y += Wrap(task.Description + (task.RoadId == null ? " V delivers at the town." : ""), x, y, width, Muted);
            foreach (string line in _session.State.Journal.AsEnumerable().Reverse().Skip(_journalOffset).Take(10)) { y += Wrap(line, x, y, width); if (y > box.Bottom - 55) break; }
            Text("PageUp / PageDown scroll history", x, box.Bottom - 50, Muted);
        }
        else if (_panel == "map")
        {
            Text("Known places / surface", x, y, Muted); y += 30;
            var region = new Rectangle(x, y, Math.Min(width, 900), box.Bottom - y - 45);
            Fill(region, new(12, 22, 27));
            foreach (var d in _session.State.Chunks.Where(c => c.Key.Floor == 0 && c.Discovered.Count > 0))
                Fill(new(region.X + d.Key.X * region.Width / 64, region.Y + d.Key.Y * region.Height / 64, Math.Max(2, region.Width / 64), Math.Max(2, region.Height / 64)), new(70, 86, 74));
            foreach (var town in _session.Plan.Towns.Where(t => _session.Chunks.Seen(t.Center))) Text(town.Name, region.X + town.Center.X * region.Width / WorldPlan.Size, region.Y + town.Center.Y * region.Height / WorldPlan.Size, Gold);
            var p = _session.State.Position; Fill(new(region.X + p.X * region.Width / WorldPlan.Size, region.Y + p.Y * region.Height / WorldPlan.Size, 5, 5), Ink);
        }
        else if (_panel == "inspector") DrawInspector(x, y, width);
        Text("Escape closes | world paused", x, box.Bottom - 25, Muted);
    }
    private void DrawInspector(int x, int y, int width)
    {
        Text($"Generation v{WorldPlan.GenerationVersion} | {_session.Chunks.ResidentCount}/25 chunks | {_session.Chunks.Generator.CachedFootprints}/8 footprints", x, y); y += 26;
        Text($"Samples {SampleLibrary.Default.Hash[..20]} | pattern size {SampleLibrary.Default.PatternSize}", x, y, Muted); y += 26;
        int index = 0;
        foreach (var sample in SampleLibrary.Default.Samples)
        {
            int sx = x + index * 180; Text(sample.Id, sx, y, Gold); int sy = y + 24;
            foreach (string row in sample.Rows) { Text(row, sx, sy, Muted); sy += 16; } index++;
        }
        y += 270;
        var p = _session.State.Position with { Floor = 0 };
        var town = _session.Plan.Towns.FirstOrDefault(t => Math.Abs(t.Center.X - p.X) < 32 && Math.Abs(t.Center.Y - p.Y) < 32 && _session.State.Position.Floor == 0);
        var site = _session.Plan.Sites.FirstOrDefault(t => Math.Abs(t.Center.X - p.X) < 32 && Math.Abs(t.Center.Y - p.Y) < 32);
        if (town == null && site == null) { Wrap("Natural field: coordinate noise + river + biome; public road constraints override terrain. No worldwide WFC pass.", x, y, width); return; }
        var footprint = _session.Chunks.Generator.GenerateFootprint(town != null, town?.Id ?? site!.Id, _session.State.Position.Floor);
        var trace = footprint.Trace;
        int mx = x + 560, my = y - 270;
        Text(_inspectStage == 0 ? "Raw structure" : _inspectStage == 1 ? "Final layout" : "Repairs in gold", mx, my, Gold); my += 24;
        for (int cell = 0; cell < 4096; cell++)
        {
            Material m = _inspectStage == 0 ? footprint.RawCells[cell] : footprint.Cells[cell];
            var color = m == Material.Wall ? new Color(149, 145, 131) : m == Material.Door ? new Color(131, 100, 63) : new Color(46, 56, 58);
            if (_inspectStage == 2 && footprint.RawCells[cell] != footprint.Cells[cell]) color = Gold;
            Fill(new(mx + cell % 64 * 3, my + cell / 64 * 3, 3, 3), color);
        }
        Text("Tab changes stage", mx, my + 200, Muted);
        Wrap($"{town?.Name ?? site!.Name}: {trace.Sample}, {trace.PatternCount} patterns. Attempts {trace.Attempts}/32; observations {trace.Observations}; propagations {trace.Propagations}. Fallback regions {trace.FallbackRegions}/4. Repaired cells {trace.RepairedCells}. Pinned borders around four structural regions; central roads/plaza and stair links are protected.", x, y, width);
    }
    private void DrawWipe(int w, int h)
    {
        var box = new Rectangle(70, h / 2 - 100, w - 140, 230); Fill(box, new(44, 27, 31));
        Text("THE EXPEDITION HAS ENDED", box.X + 24, box.Y + 22, Gold, 2);
        Wrap(_session.DeathPending ? "Deaths await a successful save. F5 retries before continuation is available." : "Your companions are permanently dead. Their belongings remain where they fell; the world remembers them.", box.X + 24, box.Y + 72, box.Width - 48);
        Text("N  New traveller in this world", box.X + 24, box.Y + 142);
        Text("Shift+N  Fresh world", box.X + 24, box.Y + 174, Muted);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _sprites?.Dispose(); _batch?.Dispose(); _pixel?.Dispose(); _frame?.Dispose(); }
        base.Dispose(disposing);
    }
}
