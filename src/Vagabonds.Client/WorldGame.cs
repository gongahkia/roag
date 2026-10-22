using System.Diagnostics;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.World;
using Vagabonds.Core.Content;
using Vagabonds.Graphics;

namespace Vagabonds.Client;

internal sealed partial class WorldGame : Game
{
    private readonly Options _options;
    private readonly GraphicsDeviceManager _graphics;
    private WorldSession _session;
    private bool _autoSave;
    private SpriteLibrary _sprites = null!;
    private SpriteBatch _batch = null!;
    private Texture2D _pixel = null!;
    private readonly RasterizerState _mapRasterizer = new() { ScissorTestEnable = true };
    private RenderTarget2D? _frame;
    private readonly MapCamera _camera = new();
    private Rectangle _map, _side;
    private MouseState _mouse;
    private string _panel = "", _notice = "Click the ground to travel. Click people or objects to approach and interact.";
    private long _person;
    private int _skill, _combatTarget = -1, _cooperativePartner = -1, _journalOffset;
    private int _inspectStage;
    private int _packMember, _packItem;
    private readonly Queue<WorldPosition> _route = [];
    private WorldClickIntent? _pendingInteraction;
    private readonly WorldPointerPreview _pointer = new();
    private readonly GridMotion _partyMotion = new();
    private readonly Dictionary<long, GridMotion> _actorMotions = [];
    private readonly List<(Rectangle Rect, string? Sprite, Color Tint, string? Prop, float Light, bool Door)> _terrain = [];
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
    public WorldGame(Options options, WorldSession session)
    {
        _options = options; _session = session; LoadConversationSettings(); _autoSave = !options.Preview;
        if (_autoSave) _session.SavePath = options.Save;
        if (!options.Load && options.Scene == "world-inspector") _panel = "inspector";
        if (!options.Load && options.Scene == "world-camp") { _campPosition = CampPreview.Prepare(_session); _campWorker = _session.State.Party[0]; _session.Paused = true; }
        if (!options.Load && options.Scene == "world-resources") { ResourcePreview.Prepare(_session, "resource.apple-tree"); _session.Paused = true; }
        if (!options.Load && options.Scene == "world-wildlife") WildlifePreview.Prepare(_session, "character.bee");
        if (!options.Load && options.Scene == "world-situation") { _discoveryId = SituationPreview.Prepare(_session, "discovery.trapped-storeroom").Id; _panel = "discovery"; }
        if (!options.Load && options.Scene == "world-cooperation") CooperationPreview.Prepare(_session);
        if (!options.Load && options.Scene == "world-site")
        { _session.State.Position = _session.Plan.Sites[0].Center; foreach (var a in _session.Party) a.Position = _session.State.Position; _session.Chunks.Prefetch(_session.State.Position); _session.Reveal(); }
        if (!options.Load && options.Scene == "world-discovery")
        {
            var d = _session.State.Discoveries.First(d => d.Context == "settlement" && _session.Content.Get<DiscoveryDefinition>(d.Definition).Action == "cache");
            _session.State.Position = d.Position.Offset(0, 1); foreach (var a in _session.Party) a.Position = _session.State.Position;
            _session.Reveal(); _session.Submit(new(WorldCommandKind.Discover, d.Position)); _discoveryId = d.Id; _panel = "discovery";
        }
        if (!options.Load && options.Scene is "world-battle" or "world-boss")
        {
            foreach (var person in _session.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(3).ToArray())
            { person.Position = _session.State.Position; _session.Submit(new(WorldCommandKind.Recruit, ActorId: person.Id)); }
            _session.StartBattle(options.Scene == "world-boss" ? _session.FinalBoss.Id : _session.State.Actors.First(a => a.Kind == ActorKind.Beast).Id);
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
        Layout(); WorldCameraScale.Apply(_camera, reset: true); UpdateMotions(TimeSpan.Zero); UpdateView(); _runtime.Start();
    }
    private void Layout()
    {
        int w = GraphicsDevice.Viewport.Width, h = GraphicsDevice.Viewport.Height;
        _map = new(16, 84, Math.Max(300, w - 350), Math.Max(260, h - 230));
        _side = new(w - 318, 84, 302, h - 100);
        _camera.Viewport = _map; WorldCameraScale.Apply(_camera);
    }
    private void UpdateView()
    {
        WorldCameraScale.Apply(_camera);
        var left = _camera.ScreenToWorld(new(_map.Left, _map.Top));
        var right = _camera.ScreenToWorld(new(_map.Right, _map.Bottom));
        _session.RevealSurface((int)Math.Floor(left.X), (int)Math.Floor(left.Y), (int)Math.Ceiling(right.X), (int)Math.Ceiling(right.Y));
    }
    protected override void Update(GameTime gameTime)
    {
        var timer = Stopwatch.StartNew(); Layout(); UpdateView();
        _presentationTime += Math.Min(.2, gameTime.ElapsedGameTime.TotalSeconds); CollectCombatFeedback();
        var mouse = Mouse.GetState();
        _session.Focused = IsActive;
        BuildMouseControls(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        if (IsActive && !_options.WorldSmoke)
        {
            bool press = mouse.LeftButton == ButtonState.Pressed && _mouse.LeftButton == ButtonState.Released;
            if (press)
            {
                var control = WorldMouseControl.Hit(_controls, mouse.Position);
                if (control != null) { if (control.Enabled) ActivateControl(control.Id); else _notice = control.DisabledReason; }
                else if (_panel == "person" && SpeechBox(PanelBox(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height)).Contains(mouse.Position)) AdvanceDialogue();
                else if (_contextCells != null) ClearWorldSelection(); // Dismiss consumes the click.
                else if (_panel.Length == 0 && !_session.State.Endgame.Victory && !_session.State.AwaitingContinuation)
                {
                    if (_session.IsInCombat) CombatClick(mouse.Position);
                    else if (_map.Contains(mouse.Position)) BeginSelection(mouse.Position);
                }
            }
            UpdateSelection(mouse);
            if (mouse.RightButton == ButtonState.Pressed && _mouse.RightButton == ButtonState.Released)
            {
                if (_session.IsInCombat && _panel.Length == 0) { _skill = 0; _cooperativePartner = _combatTarget = -1; _notice = "Ability selection cancelled."; }
                if (_panel.Length > 0 || _session.IsInCombat || !_map.Contains(mouse.Position)) { ClosePanel(); ClearWorldSelection(); }
                else OpenContext(mouse.Position);
            }
            if (mouse.ScrollWheelValue != _mouse.ScrollWheelValue && _panel.Length == 0 && !PointerModal && !_session.IsInCombat && _map.Contains(mouse.Position))
                Zoom(Math.Sign(mouse.ScrollWheelValue - _mouse.ScrollWheelValue));
        }
        else if (!IsActive) { _dragStart = null; _dragging = false; }
        if (_session.IsInCombat || _session.State.Endgame.Victory || _session.State.AwaitingContinuation) ClearWorldSelection();
        CollectCombatFeedback();
        _session.ModalOpen = PanelPausesTime(_panel) || PointerModal;
        _moveTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (!_session.Frozen && _moveTimer >= MoveDuration && _route.TryDequeue(out var next))
        {
            if (!Execute(new(WorldCommandKind.Move, next))) { StopTravel(); _notice += " Click again to route around the obstruction."; }
            _moveTimer = 0;
            if (!_session.IsInCombat) FinishApproach();
        }
        _session.ModalOpen = PanelPausesTime(_panel) || PointerModal;
        _session.PartyTravelling = _route.Count > 0 || _pendingInteraction != null;
        bool wasInCombat = _session.IsInCombat;
        // Long stalls/focus changes discard elapsed time rather than simulating an offline catch-up.
        _session.AdvanceFrame(gameTime.ElapsedGameTime <= TimeSpan.FromSeconds(5) ? gameTime.ElapsedGameTime : TimeSpan.Zero);
        if (_session.IsInCombat)
        {
            if (!wasInCombat && _panel is "work" or "inventory" or "camp") ClosePanel();
            StopTravel(); _enemyTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (!_session.State.Battle!.Active.Friendly && _enemyTimer > .65 && _panel.Length == 0 && IsActive) { _notice = _session.AdvanceEnemy().Message; _enemyTimer = 0; }
        }
        else _enemyTimer = 0;
        UpdateConversation(gameTime.ElapsedGameTime.TotalSeconds);
        CollectCombatFeedback(); _session.ModalOpen = PanelPausesTime(_panel) || PointerModal;
        UpdateMotions(gameTime.ElapsedGameTime);
        UpdateView();
        if (_updates.Count < 10000) _updates.Add(timer.Elapsed.TotalMilliseconds);
        _mouse = mouse; base.Update(gameTime);
    }
    internal static bool PanelPausesTime(string panel) => panel.Length > 0 && panel is not ("work" or "inventory" or "camp");
    private void Toggle(string panel) { string next = _panel == panel ? "" : panel; ClosePanel(); _panel = next; }
    private Actor[] WorkCandidates() => _session.State.Actors.Where(a => a.Kind == ActorKind.Person && a.Life == LifeState.Alive && a.Position.Distance(_session.State.Position) <= 100).OrderByDescending(a => a.Recruited).ThenBy(a => a.Id).ToArray();
    private RecipeDefinition[] CraftRecipes() => _session.Content.Game.Recipes.Select(_session.Content.Get<RecipeDefinition>).Where(r => r.Output != null).ToArray();
    private bool Execute(WorldCommand command)
    {
        try { var result = _session.Submit(command); _notice = result.Message; return result.Success; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { _notice = "Save failed: " + e.Message + " Use Save to retry."; return false; }
    }
    private void ClickMap(MouseState mouse)
    {
        UpdatePointer(mouse.Position);
        if (_pointer.Cell is not { } p) return;
        if (!_session.Chunks.Seen(p)) { _notice = "Explore this ground first."; return; }
        var plan = WorldMouseNavigation.Plan(_session, _pointer.NavigationCell ?? p);
        if (!plan.Reachable) { _notice = plan.Reason; OpenContext(mouse.Position); return; }
        BeginApproach(plan);
    }

    private void CombatClick(Point pointer)
    {
        var battle = _session.State.Battle!;
        if (!battle.Active.Friendly) return;
        foreach (var unit in battle.Units)
            if (UnitRect(unit).Contains(pointer))
            {
                var skill = _session.ActiveSkills().ElementAtOrDefault(_skill); if (skill == null) return;
                if (skill.Cooperative && (_cooperativePartner < 0 || unit.Friendly))
                {
                    string reason = _session.CooperationPartnerReason(_skill, unit.Id);
                    if (reason.Length > 0) { _notice = reason; return; }
                    _cooperativePartner = unit.Id;
                    if (skill.Target == TargetKind.AllAllies) UseSkill();
                    else _notice = skill.Name + " with " + unit.Name + ": choose an enemy. Both actions will be spent.";
                    return;
                }
                _combatTarget = unit.Id; UseSkill(); return;
            }
    }
    private void UseSkill()
    {
        if (_session.State.Battle == null || _session.ActiveSkills().ElementAtOrDefault(_skill) == null) return;
        try
        {
            var result = _session.Act(_skill, _combatTarget, _cooperativePartner); _notice = result.Message;
            if (result.Success) { _skill = 0; _cooperativePartner = _combatTarget = -1; }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { _notice = "Death save failed: " + e.Message + " Use Save to retry."; }
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
            _session = replacement; _dialogue = null; _voice?.Stop(); _afterDialogue = ""; ClearWorldSelection(); _autoSave = true; StopTravel(); _panel = ""; _person = 0; _camera.Center = Vector2.Zero;
            _skill = 0; _cooperativePartner = _combatTarget = -1; _moveTimer = _enemyTimer = 0;
            _waypoint = null; _selectedWorkId = _selectedWorkerId = 0;
            _notice = "World restored and paused. Click a destination or Play to continue.";
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
            if (actor.Life == LifeState.Dead) { _actorMotions.Remove(actor.Id); continue; }
            if (!_actorMotions.TryGetValue(actor.Id, out var motion)) _actorMotions[actor.Id] = motion = new();
            motion.Observe(actor.Position, actor.Recruited || actor.Kind == ActorKind.Caravan ? MoveDuration : 1); motion.Advance(elapsed);
        }
    }
    private Rectangle ActorRect(Vector2 position)
    {
        var center = _camera.WorldToScreen(position); int size = (int)Math.Ceiling(_camera.CellSize);
        return new((int)Math.Round(center.X - size / 2f), (int)Math.Round(center.Y - size / 2f), size, size);
    }
    private void UpdatePointer(Point point)
    {
        var actor = _session.State.Actors.LastOrDefault(a => !a.Recruited && a.Life == LifeState.Alive && _session.Visible.Contains(a.Position)
            && _actorMotions.TryGetValue(a.Id, out var motion)
            && _session.Visible.Contains(new((int)Math.Floor(motion.Position.X), (int)Math.Floor(motion.Position.Y), a.Position.Floor))
            && ActorRect(motion.Position).Contains(point));
        _pointer.Update(_session, _camera, point,
            enabled: _panel.Length == 0 && !PointerModal && !_session.IsInCombat && !_session.State.AwaitingContinuation && !_session.State.Endgame.Victory && WorldMouseControl.Hit(_controls, point) == null,
            interactionCell: actor?.Position);
    }
    protected override void Draw(GameTime gameTime)
    {
        Layout(); UpdateView(); int w = GraphicsDevice.Viewport.Width, h = GraphicsDevice.Viewport.Height;
        if (_frame == null || _frame.Width != w || _frame.Height != h) { _frame?.Dispose(); _frame = new(GraphicsDevice, w, h); }
        GraphicsDevice.SetRenderTarget(_frame); GraphicsDevice.Clear(new Color(16, 22, 27));
        GraphicsDevice.ScissorRectangle = _map;
        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: _mapRasterizer);
        DrawTerrain(); _batch.End();
        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        DrawHud(w, h);
        if (_session.IsInCombat) DrawCombat();
        if (_panel.Length > 0) DrawPanel(w, h);
        DrawContext();
        DrawMouseControls(w, h);
        DrawHoverFeedback();
        WriteUiReport(w, h);
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
                var checks = new Dictionary<string, bool> { ["expectedGeneratedParty"] = _session.State.Party.Count == (_options.Scene is "world-battle" or "world-boss" or "world-cooperation" or "world-camp" ? 4 : 1), ["finiteWorld"] = _session.Plan.Towns.Count == 24 && _session.Plan.Sites.Count == 48, ["startsPaused"] = _session.Paused && _session.State.Tick == 0, ["discovery"] = _session.Chunks.Seen(_session.State.Position), ["boundedCache"] = _session.Chunks.ResidentCount <= 25, ["materialBindings"] = true, ["renderTarget"] = _frame.Width == w && _frame.Height == h };
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
    private readonly List<(Rectangle Rect, string Sprite, float Light)> _resourceIcons = [];
    private void DrawTerrain()
    {
        Fill(_map, new(9, 16, 21)); _terrain.Clear(); _resourceIcons.Clear();
        var left = _camera.ScreenToWorld(new(_map.Left, _map.Top)); var right = _camera.ScreenToWorld(new(_map.Right, _map.Bottom));
        for (int y = (int)Math.Floor(left.Y); y <= (int)right.Y; y++) for (int x = (int)Math.Floor(left.X); x <= (int)right.X; x++)
        {
            var p = new WorldPosition(x, y, _session.State.Position.Floor);
            if (!_session.Chunks.Seen(p)) continue;
            var rect = TileRect(p); if (!_map.Intersects(rect)) continue;
            var chunk = _session.Chunks.Get(p.Chunk); var biome = chunk.Biomes[p.LocalIndex]; int culture = chunk.Cultures[p.LocalIndex];
            var material = _session.Chunks.At(p);
            Color tint = material switch { Material.Water => new(72, 132, 158), Material.Tree => new(104, 141, 104), Material.StairsDown or Material.StairsUp => Gold, Material.Camp => new(241, 159, 88), _ => biome switch { Biome.Forest => new(127, 150, 112), Biome.Marsh => new(127, 144, 110), Biome.Badland => new(187, 144, 103), Biome.Snow => new(199, 211, 217), Biome.Highland => new(160, 163, 159), _ => new(156, 170, 123) } };
            if (material is Material.Wall or Material.Floor or Material.Door) tint = new(191, 184, 163);
            bool bright = _session.Visible.Contains(p);
            float light = bright ? 1 : .35f;
            bool ground = material is Material.Grass or Material.Earth or Material.Floor;
            var background = material switch { Material.Grass => new Color(29, 42, 33), Material.Floor => new Color(51, 45, 36), Material.Earth => new Color(49, 43, 33), Material.Water => new Color(19, 38, 47), _ => new Color(32, 36, 34) };
            Fill(rect, background * light);
            string? sprite = material != Material.Grass || WorldRandom.Hash(_session.Seed, x, y) % 3 == 0
                ? Materials.Sprite(material, biome, culture < 0 ? null : SampleLibrary.Default.Cultures[culture]) : null;
            var resource = _session.ResourceAt(p);
            if (resource != null)
            {
                if (material is Material.Tree or Material.Rock or Material.Water)
                {
                    if (resource.Available > 0) _resourceIcons.Add((new(rect.Right - rect.Width / 2, rect.Top, rect.Width / 2, rect.Height / 2), resource.Sprite, light));
                    else { sprite = resource.Definition.DepletedSprite; tint = Muted; }
                }
                else { sprite = resource.Sprite; tint = resource.Available > 0 ? Gold : Muted; ground = false; }
            }
            string? prop = resource == null && _session.ObjectAt(p) is { Kind: "decoration" } decoration ? decoration.Sprite : null;
            _terrain.Add((rect, sprite, tint * light * (ground ? .32f : 1), prop, light, material == Material.Door));
        }
        // Backgrounds, terrain and props are separate layers so texture sorting preserves their order.
        _batch.End(); _batch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: _mapRasterizer);
        foreach (var tile in _terrain)
            if (tile.Sprite is { } sprite) _sprites.Draw(_batch, sprite, tile.Rect, tile.Tint);
        _batch.End(); _batch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: _mapRasterizer);
        foreach (var tile in _terrain)
        {
            if (tile.Prop is { } prop) _sprites.Draw(_batch, prop, tile.Rect, new Color(154, 141, 105) * tile.Light);
            if (tile.Door) Fill(new(tile.Rect.Center.X - 2, tile.Rect.Top + tile.Rect.Height / 3, 4, tile.Rect.Height / 2), Gold * .7f);
        }
        _batch.End(); _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: _mapRasterizer);
        foreach (var resource in _resourceIcons) _sprites.Draw(_batch, resource.Sprite, resource.Rect, Gold * resource.Light);
        UpdatePointer(_mouse.Position);
        if (_panel.Length == 0 && !_session.IsInCombat && !_session.State.AwaitingContinuation)
        {
            if (_pointer.Path.Count > 0 && (_route.Count == 0 || _route.Last() != _pointer.Path[^1])) DrawRoute(_pointer.Path, new(113, 187, 195), 2);
            DrawRoute(_route, Gold, 3);
        }
        foreach (var d in _session.State.Discoveries.Where(d => _session.Visible.Contains(d.Position)))
            _sprites.Draw(_batch, _session.DiscoverySprite(d), TileRect(d.Position), _session.DiscoveryComplete(d) ? Muted : d.Outcome.Length > 0 ? new Color(129, 200, 204) : Gold);
        foreach (var cache in _session.State.Loot.Where(c => !c.Taken && _session.Visible.Contains(c.Position) && !_session.State.Discoveries.Any(d => d.Id == c.Id)))
            _sprites.Draw(_batch, WorldSession.LootSprite(cache), TileRect(cache.Position), Gold);
        foreach (var actor in _session.State.Actors.Where(a => !a.Recruited && a.Life != LifeState.Dead && _session.Visible.Contains(a.Position)))
        {
            var motion = _actorMotions[actor.Id];
            if (!_session.Visible.Contains(new((int)Math.Floor(motion.Position.X), (int)Math.Floor(motion.Position.Y), actor.Position.Floor))) continue;
            var rect = ActorRect(motion.Position); if (!_map.Intersects(rect)) continue;
            _sprites.Draw(_batch, actor.Sprite, rect, _session.IsHostile(actor) ? new(224, 119, 103) : _session.Ecology(actor) != null ? new(164, 200, 144) : new(212, 204, 176), effects: motion.FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        }
        foreach (var order in _session.State.Work.Where(o => _session.Visible.Contains(o.Target)))
        {
            var rect = TileRect(order.Target); FillClipped(new(rect.Left + 2, rect.Top + 2, Math.Max(2, (int)((rect.Width - 4) * order.Progress / order.Required)), 3), Gold);
            FillClipped(new(rect.Left + 2, rect.Bottom - 5, Math.Max(2, rect.Width - 4), 2), Gold * .5f);
        }
        if (_waypoint is { } mark && _session.State.Position.Floor == mark.Floor)
        {
            var point = _camera.WorldToScreen(new(mark.X + .5f, mark.Y + .5f));
            var banner = new Rectangle(Math.Clamp((int)point.X - 8, _map.Left + 4, _map.Right - 20), Math.Clamp((int)point.Y - 8, _map.Top + 48, _map.Bottom - 20), 16, 16);
            _sprites.Fit(_batch, "banner", banner, Gold);
        }
        foreach (var actor in _session.Party.Where(a => a.Life != LifeState.Dead))
        {
            var motion = _actorMotions[actor.Id]; var rect = ActorRect(motion.Position);
            _sprites.Draw(_batch, actor.Sprite, rect, actor.Life == LifeState.Downed ? new(139, 100, 100) : Ink, effects: motion.FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        }
        if (_panel.Length == 0 && !_session.IsInCombat && _session.State.WorkRally is { } rally)
        {
            var tint = new Color(113, 187, 195);
            for (int dx = -WorldSession.WorkRallyRadius; dx <= WorldSession.WorkRallyRadius; dx++)
                foreach (int dy in new[] { WorldSession.WorkRallyRadius - Math.Abs(dx), -WorldSession.WorkRallyRadius + Math.Abs(dx) }.Distinct())
                {
                    var edge = rally.Offset(dx, dy); if (!_session.Chunks.Seen(edge)) continue;
                    var r = TileRect(edge); FillClipped(new(r.X, r.Y, r.Width, 2), tint * .4f);
                }
            if (_session.Chunks.Seen(rally))
            {
                var r = TileRect(rally); FillClipped(new(r.Center.X - 3, r.Center.Y - 3, 6, 6), tint);
                Text("RALLY", r.Right + 2, r.Center.Y - 4, tint);
            }
        }
        DrawSelection();
        if (_pointer.Cell is { } cell)
        {
            var rect = TileRect(cell);
            bool blocked = _pointer.Plan is { Reachable: false };
            Color selector = blocked ? new(225, 127, 110) : Gold; FillClipped(rect, selector * .1f);
            int edge = Math.Clamp(rect.Width / 3, 3, 12), stroke = rect.Width >= 16 ? 2 : 1;
            foreach (int x in new[] { rect.Left, rect.Right - edge })
                foreach (int y in new[] { rect.Top, rect.Bottom - stroke }) FillClipped(new(x, y, edge, stroke), selector);
            foreach (int x in new[] { rect.Left, rect.Right - stroke })
                foreach (int y in new[] { rect.Top, rect.Bottom - edge }) FillClipped(new(x, y, stroke, edge), selector);
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
            Text(a.Name, x + 46, y); Text($"{_session.Class(a).Name}  L{a.Level}", x + 46, y + 18, Muted);
            y += 42; Text(a.Life == LifeState.Alive ? $"HP {a.Health}/{a.MaxHealth}  MP {a.Mana}/{a.MaxMana}" : a.Life.ToString(), x, y); y += 27;
        }
        y += 8;
        Text($"Food {_session.Count("food")}  Gold {_session.Count("gold")}", x, y, Gold); y += 22;
        Text($"Wood {_session.Count("wood")}  Stone {_session.Count("stone")}", x, y, Muted); y += 22;
        Text($"Medicine {_session.Count("medicine")}", x, y, Muted); y += 34;
        if (y < _side.Bottom - 100) { Text("THE FINAL THREAT", x, y, Gold); y += 26; Wrap(_session.FinalObjective, x, y, _side.Width - 28); }
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
            var r = UnitRect(unit);
            long actorId = b.ActorIds[unit.Id];
            var latest = _combatFloats.LastOrDefault(e => e.Target == actorId && _presentationTime - e.Born < .25 && e.Kind is "damage" or "heal");
            Fill(r, latest != null ? latest.Kind == "damage" ? new(88, 45, 41) : new(39, 76, 52) : unit.Id == b.Active.Id ? new(66, 65, 49) : Panel);
            bool choosingHelper = b.Active.Friendly && _session.ActiveSkills().ElementAtOrDefault(_skill)?.Cooperative == true && _cooperativePartner < 0;
            if (unit.Id == _cooperativePartner || b.Active.Friendly && r.Contains(_mouse.Position) && (choosingHelper
                    ? _session.CooperationPartnerReason(_skill, unit.Id).Length == 0 : _session.ActionPreview(_skill, unit.Id, _cooperativePartner).Reason.Length == 0))
                Fill(new(r.X, r.Y, 3, r.Height), new(127, 208, 212));
            _sprites.Fit(_batch, unit.Sprite, new(r.X + 8, r.Y + 8, 48, 48), unit.Alive ? Ink : Muted * .4f);
            Text(unit.Name.Length > 25 ? unit.Name[..22] + "..." : unit.Name, r.X + 64, r.Y + 8); Text($"HP {unit.Health}/{unit.MaxHealth}", r.X + 64, r.Y + 28, unit.Friendly ? Ink : new(222, 138, 125));
            string conditions = string.Join(" ", new[] { unit.Guarding ? "G" : "", unit.Burn > 0 ? "B" + unit.Burn : "", unit.Bleed > 0 ? "L" + unit.Bleed : "", unit.Mark > 0 ? "M" + unit.Mark : "", unit.Stun > 0 ? "S" + unit.Stun : "" }.Where(c => c.Length > 0));
            Text(conditions, r.X + 160, r.Y + 29, Gold, .75f);
            Fill(new(r.X + 64, r.Y + 43, r.Width - 76, 3), new(57, 63, 64));
            Fill(new(r.X + 64, r.Y + 43, Math.Max(0, (r.Width - 76) * unit.Health / Math.Max(1, unit.MaxHealth)), 3), unit.Friendly ? new(129, 196, 150) : new(205, 126, 111));
            string detail = unit.Friendly ? b.Assisting.Contains(unit.Id) ? $"MP {unit.Mana} / action spent" : $"MP {unit.Mana} Stress {unit.Stress}" : _session.EnemyIntent(unit.Id);
            Text(detail.Length > 27 ? detail[..24] + "..." : detail, r.X + 64, r.Y + 48, Muted);
        }
        DrawCombatFeedback();
    }
    private void DrawInspector(int x, int y, int width)
    {
        Text($"Generation v{WorldPlan.GenerationVersion} / {_session.Chunks.ResidentCount}/25 chunks / {_session.Chunks.Generator.CachedFootprints}/8 footprints", x, y); y += 24;
        Text($"Samples {SampleLibrary.Default.Hash[..20]} / learned 3x3 patterns", x, y, Muted); y += 28;
        int column = 0;
        foreach (var sample in SampleLibrary.Default.Samples)
        {
            int sx = x + column++ * 144; Text(sample.Id, sx, y, Gold, .8f); int sy = y + 22;
            foreach (string row in sample.Rows) { Text(row, sx, sy, Muted, .65f); sy += 11; }
        }
        var p = _session.State.Position with { Floor = 0 };
        var town = _session.Plan.Towns.FirstOrDefault(t => Math.Abs(t.Center.X - p.X) < 32 && Math.Abs(t.Center.Y - p.Y) < 32 && _session.State.Position.Floor == 0);
        var site = _session.Plan.Sites.FirstOrDefault(t => Math.Abs(t.Center.X - p.X) < 32 && Math.Abs(t.Center.Y - p.Y) < 32);
        if (town == null && site == null) { Wrap("Natural field: coherent noise, river and biome, overridden by public roads. Use World map for the overview.", x, y + 190, width); return; }
        var footprint = _session.Chunks.Generator.GenerateFootprint(town != null, town?.Id ?? site!.Id, _session.State.Position.Floor); var trace = footprint.Trace;
        int mx = x + width - 260, my = y + 24;
        Text(new[] { "Raw structure", "Final layout", "Repairs in gold" }[_inspectStage], mx, y, Gold);
        for (int cell = 0; cell < 4096; cell++)
        {
            var material = _inspectStage == 0 ? footprint.RawCells[cell] : footprint.Cells[cell];
            var color = material == Material.Wall ? new Color(149, 145, 131) : material == Material.Door ? new Color(131, 100, 63) : new Color(46, 56, 58);
            if (_inspectStage == 2 && footprint.RawCells[cell] != footprint.Cells[cell]) color = Gold;
            Fill(new(mx + cell % 64 * 4, my + cell / 64 * 4, 4, 4), color);
        }
        Wrap($"{town?.Name ?? site!.Name}: {trace.Layout}. Sample {trace.Sample}; {trace.PatternCount} patterns. WFC attempts {trace.Attempts}; observations {trace.Observations}; fallback regions {trace.FallbackRegions}; repaired cells {trace.RepairedCells}. Connected floors and stair links are protected.", x, y + 194, Math.Max(280, width - 300));
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _voice?.Dispose(); _sprites?.Dispose(); _batch?.Dispose(); _pixel?.Dispose(); _frame?.Dispose(); _mapRasterizer?.Dispose(); }
        base.Dispose(disposing);
    }
}
