using System.Diagnostics;
using System.Text.Json;
using Ithica.Core;
using Ithica.Core.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ithica.Core.Colony;
using Ithica.Graphics;

namespace Ithica.Client;

public sealed partial class IthicaGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly ClientOptions _options;
    private Ithica.Core.Campaign _campaign;
    private Simulation _sim => _campaign.Home;
    private SpriteBatch _batch = null!;
    private SpriteLibrary _sprites = null!;
    private Texture2D _pixel = null!;
    private RenderTarget2D? _frame;
    private readonly RasterizerState _scissor = new() { ScissorTestEnable = true, CullMode = CullMode.None };
    private readonly MapCamera _camera = new();
    private MouseState _mouse, _previousMouse;
    private KeyboardState _keyboard, _previousKeyboard;
    private bool _leftPressed, _leftReleased;
    private bool _paused { get => _campaign.Clock.Paused; set => _campaign.Clock.Paused = value; }
    private int _speed { get => _campaign.Clock.Speed == 1 ? 0 : _campaign.Clock.Speed == 3 ? 1 : 2; set => _campaign.Clock.Speed = value == 0 ? 1 : value == 1 ? 3 : 8; }
    private int _cursor, _lesson, _panelScroll, _drawCount;
    private int? _anchor, _selectedCrew, _selectedSurvivor, _follow;
    private string _modal = "tutorial", _panel = "colony", _message = "Click a distress contact to send help.";
    private NoticeLevel _messageLevel = NoticeLevel.Info;
    private Tool _tool = Tool.Inspect;
    private ModuleKind _blueprint = ModuleKind.Wall;
    private double _elapsed, _keyRepeatAt, _messageUntil, _tickMilliseconds;
    private float _fps = 60;
    private Vector2[] _crewPositions = [];
    private bool _resizing;
    private Rectangle _sidebar, _eventArea;
    private readonly List<string> _smokeChecks = [];
    private readonly Queue<Action> _uiCommands = new();
    private void QueueUI(Action command) => _uiCommands.Enqueue(command);
    private static readonly Color Background = new(12, 17, 22), Surface = new(20, 28, 35), Raised = new(28, 38, 46), Ink = new(218, 225, 214), Muted = new(133, 154, 158),
        Accent = new(125, 204, 191), Amber = new(230, 179, 113), Danger = new(235, 118, 109), Blue = new(126, 178, 211), Purple = new(184, 153, 211);

    public IthicaGame(ClientOptions options)
    {
        _options = options;
        _campaign = options.ImportPath is not null ? Ithica.Core.Campaign.ImportColony(options.ImportPath) : options.Load ? Ithica.Core.Campaign.Load(options.SavePath) : new Ithica.Core.Campaign(options.Seed, options.Width, options.Height);
        if (!options.Load && options.ImportPath is null) CampaignScenarios.OpenScene(_campaign, options.Scene);
        _atlas = options.Scene == "atlas";
        if (_campaign.Phase == CampaignPhase.Preparation)
        { var ready = _sim.Crew.FirstOrDefault(c => c.Alive && !c.Rest); if (ready is not null) _departureIds.Add(ready.Id); }
        if (options.Demo)
        {
            _campaign.Clock.Paused = false;
        }

        _cursor = _sim.World.Center;
        _camera.Center = CellCenter(_cursor);
        if (options.SkipIntro || options.Load || options.Scene != "colony")
        {
            _modal = "";
        }

        _graphics = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = options.WindowWidth, PreferredBackBufferHeight = options.WindowHeight, SynchronizeWithVerticalRetrace = true };
        Window.Title = "Ithica";
        Window.TextInput += (_, e) => { if (_atlas && !char.IsControl(e.Character) && e.Character < 127) { _search += e.Character; FilterAtlas(); } };
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        Window.ClientSizeChanged += (_, _) =>
        {
            if (_resizing)
            {
                return;
            }

            var size = Window.ClientBounds;
            if (size.Width < 1024 || size.Height < 720)
            {
                _resizing = true;
                _graphics.PreferredBackBufferWidth = Math.Max(1024, size.Width);
                _graphics.PreferredBackBufferHeight = Math.Max(720, size.Height);
                _graphics.ApplyChanges();
                _resizing = false;
            }
        };
        Exiting += (_, _) =>
        {
            if (!options.SmokeTest && options.Frames == 0 && options.Scene == "colony" && !_sim.GameOver)
            {
                SaveColony();
            }
        };
    }
    protected override void Initialize()
    {
        base.Initialize();
        UpdateLayout();
        _mouse = _previousMouse = Mouse.GetState();
        _keyboard = _previousKeyboard = Keyboard.GetState();
    }
    protected override void LoadContent()
    {
        _batch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _sprites = new SpriteLibrary(GraphicsDevice, Path.Combine(AppContext.BaseDirectory, "assets"));
        _fieldSprites = new SpriteLibrary(GraphicsDevice, Path.Combine(AppContext.BaseDirectory, "assets"), "ithica-expedition-sprites.json");
        _entries = JsonSerializer.Deserialize<List<ArtworkEntry>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "assets", "atlas-index.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        FilterAtlas();
        ResetFieldCamera();
    }
    private void UpdateLayout()
    {
        int w = GraphicsDevice.PresentationParameters.BackBufferWidth, h = GraphicsDevice.PresentationParameters.BackBufferHeight;
        _camera.Viewport = new(76, 116, w - 76 - 332, h - 116 - 162);
        _sidebar = new(w - 320, 116, 308, h - 152);
        _eventArea = new(76, _camera.Viewport.Bottom + 12, _camera.Viewport.Width, 110);
    }
    private Vector2 CellCenter(int cell) => new(_sim.World.X(cell) + .5f, _sim.World.Y(cell) + .5f);
    private bool Pressed(Keys key) => _keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    private bool Shift => _keyboard.IsKeyDown(Keys.LeftShift) || _keyboard.IsKeyDown(Keys.RightShift);
    private Vector2 MousePoint => new(_mouse.X, _mouse.Y);
    private void Message(string text, NoticeLevel level = NoticeLevel.Info)
    {
        _message = text;
        _messageLevel = level;
        _messageUntil = _elapsed + 7;
    }
    private void Message(Result result) => Message(result.Message, result.Success ? NoticeLevel.Good : NoticeLevel.Warning);
    protected override void Update(GameTime gameTime)
    {
        double delta = Math.Clamp(gameTime.ElapsedGameTime.TotalSeconds, 0, .15);
        _elapsed += delta;
        _fps = MathHelper.Lerp(_fps, (float)(1 / Math.Max(.001, delta)), .05f);
        UpdateLayout();
        _mouse = Mouse.GetState();
        _keyboard = Keyboard.GetState();
        _leftPressed = _mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
        _leftReleased = _mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed;
        while (_uiCommands.TryDequeue(out var command)) command();
        SyncCampaignUI();
        if (IsActive && !_options.SmokeTest)
        {
            if (_campaign.Phase != CampaignPhase.Colony || _atlas) HandleCampaignInput();
            else if (Pressed(Keys.F2)) _atlas = true;
            else HandleInput();
        }

        SyncCampaignUI();
        if (_modal.Length == 0 && !_atlas && !_sim.GameOver)
        {
            long tickStarted = Stopwatch.GetTimestamp();
            long beforeTick = _campaign.CampaignTick;
            _campaign.UpdateRealtime(delta, maxTicks: 1);
            while (_campaign.Clock.Accumulator >= 1 && Stopwatch.GetElapsedTime(tickStarted).TotalMilliseconds < 12)
                _campaign.UpdateRealtime(0, maxTicks: 1);
            _tickMilliseconds = Stopwatch.GetElapsedTime(tickStarted).TotalMilliseconds / Math.Max(1, _campaign.CampaignTick - beforeTick);
        }
        else _campaign.Clock.Accumulator = 0;

        if (_crewPositions.Length < _sim.Crew.Count)
        {
            Array.Resize(ref _crewPositions, Math.Max(_sim.Crew.Count, _crewPositions.Length * 2 + 16));
        }

        foreach (var c in _sim.Crew)
        {
            var target = CellCenter(c.Cell);
            if (_crewPositions[c.Id] == Vector2.Zero || Vector2.DistanceSquared(_crewPositions[c.Id], target) > 25)
            {
                _crewPositions[c.Id] = target;
            }
            else
            {
                _crewPositions[c.Id] = Vector2.Lerp(_crewPositions[c.Id], target, Math.Min(1, (float)delta * 18));
            }
        }
        if (_follow is int id && id < _sim.Crew.Count && _sim.Crew[id].Alive)
        {
            _cursor = _sim.Crew[id].Cell;
            _camera.Center = _crewPositions[id];
        }
        if (_options.SmokeTest)
        {
            RunSmokeTest();
        }

        _previousMouse = _mouse;
        _previousKeyboard = _keyboard;
        base.Update(gameTime);
    }
    private void HandleInput()
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
        if (_modal.Length > 0)
        {
            if (Pressed(Keys.Escape))
            {
                _modal = "";
                return;
            }
            if (_modal == "tutorial")
            {
                if (Pressed(Keys.Left) || Pressed(Keys.P))
                {
                    _lesson = Math.Max(0, _lesson - 1);
                }

                if (Pressed(Keys.Right) || Pressed(Keys.N) || Pressed(Keys.Enter) || Pressed(Keys.Space))
                {
                    NextLesson();
                }
            }
            else if (_modal == "build")
            {
                if (Pressed(Keys.U)) { ChooseBlueprint(ModuleKind.SuitRack); return; }
                for (int i = 0; i < 10; i++)
                {
                    if (Pressed(i == 9 ? Keys.D0 : Keys.D1 + i))
                    {
                        ChooseBlueprint((ModuleKind)i);
                        return;
                    }
                }
            }
            return;
        }
        if (Pressed(Keys.Space))
        {
            _paused = !_paused;
        }

        if (Pressed(Keys.D1))
        {
            _speed = 0;
        }

        if (Pressed(Keys.D2))
        {
            _speed = 1;
        }

        if (Pressed(Keys.D3))
        {
            _speed = 2;
        }

        if (Pressed(Keys.OemPeriod))
        {
            _paused = true;
            _campaign.StepColony();
        }
        if (Pressed(Keys.U)) { ChooseBlueprint(ModuleKind.SuitRack); }
        if (Pressed(Keys.B))
        {
            _modal = "build";
        }

        if (Pressed(Keys.D))
        {
            SelectTool(Tool.Dig);
        }

        if (Pressed(Keys.P))
        {
            SelectTool(Tool.Survey);
        }

        if (Pressed(Keys.X))
        {
            SelectTool(Tool.Cancel);
        }

        if (Pressed(Keys.Escape))
        {
            SelectTool(Tool.Inspect);
            _selectedCrew = _selectedSurvivor = _follow = null;
        }
        if (Pressed(Keys.C))
        {
            _panel = "crew";
            _panelScroll = 0;
        }
        if (Pressed(Keys.E))
        {
            _panel = "log";
            _panelScroll = 0;
        }
        if (Pressed(Keys.G))
        {
            CenterHabitat();
        }

        if (Pressed(Keys.F))
        {
            _selectedCrew ??= _sim.CrewAt(_cursor)?.Id;
            _follow = _follow is null ? _selectedCrew : null;
        }
        if (Pressed(Keys.R))
        {
            FocusDistress();
        }

        if (Pressed(Keys.O))
        {
            _overlay = (_overlay + 1) % 4;
        }

        if (Pressed(Keys.T))
        {
            _modal = "tutorial";
            _lesson = 0;
        }
        if (Pressed(Keys.OemQuestion))
        {
            _modal = "help";
        }
        if (Pressed(Keys.S) || Pressed(Keys.F5))
        {
            SaveColony();
        }

        if ((Pressed(Keys.L) && Shift) || Pressed(Keys.F9))
        {
            LoadColony();
        }

        if (Pressed(Keys.N) && _sim.GameOver)
        {
            NewColony();
        }

        if (Pressed(Keys.V))
        {
            _anchor = _anchor is null ? _cursor : null;
        }

        if (Pressed(Keys.Enter))
        {
            PlaceAtCursor();
        }

        if (Pressed(Keys.OemPlus) || Pressed(Keys.PageUp))
        {
            _camera.ZoomAt(_camera.WorldToScreen(CellCenter(_cursor)), 1);
        }

        if (Pressed(Keys.OemMinus) || Pressed(Keys.PageDown))
        {
            _camera.ZoomAt(_camera.WorldToScreen(CellCenter(_cursor)), -1);
        }

        if (Pressed(Keys.D0))
        {
            _camera.Reset();
        }

        bool movementEdge = Pressed(Keys.Left) || Pressed(Keys.Right) || Pressed(Keys.Up) || Pressed(Keys.Down) || Pressed(Keys.H) || Pressed(Keys.J) || Pressed(Keys.K) || (!Shift && Pressed(Keys.L));
        if (movementEdge || _elapsed >= _keyRepeatAt)
        {
            int dx = 0, dy = 0;
            if (_keyboard.IsKeyDown(Keys.Left) || _keyboard.IsKeyDown(Keys.H))
            {
                dx--;
            }

            if (_keyboard.IsKeyDown(Keys.Right) || (!Shift && _keyboard.IsKeyDown(Keys.L)))
            {
                dx++;
            }

            if (_keyboard.IsKeyDown(Keys.Up) || _keyboard.IsKeyDown(Keys.K))
            {
                dy--;
            }

            if (_keyboard.IsKeyDown(Keys.Down) || _keyboard.IsKeyDown(Keys.J))
            {
                dy++;
            }

            if (dx != 0 || dy != 0)
            {
                int stride = Shift ? 5 : 1;
                int x = Math.Clamp(_sim.World.X(_cursor) + dx * stride, 0, _sim.World.Width - 1), y = Math.Clamp(_sim.World.Y(_cursor) + dy * stride, 0, _sim.World.Height - 1);
                _cursor = _sim.World.Index(x, y);
                _follow = null;
                _keyRepeatAt = _elapsed + (movementEdge ? .22 : .07);
                var screen = _camera.WorldToScreen(CellCenter(_cursor));
                var inset = _camera.Viewport;
                inset.Inflate(-36, -36);
                if (!inset.Contains(screen))
                {
                    _camera.Center = CellCenter(_cursor);
                }
            }
        }
        int wheel = _mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
        if (_camera.Viewport.Contains(_mouse.Position))
        {
            if (wheel != 0)
            {
                _camera.ZoomAt(MousePoint, Math.Sign(wheel));
            }

            bool dragging = (_mouse.MiddleButton == ButtonState.Pressed && _previousMouse.MiddleButton == ButtonState.Pressed) || (_mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Pressed);
            if (dragging)
            {
                _camera.Pan(MousePoint - new Vector2(_previousMouse.X, _previousMouse.Y));
                _follow = null;
            }
            var zoomControls = new Rectangle(_camera.Viewport.Right - 156, _camera.Viewport.Bottom - 50, 156, 40);
            if (_leftPressed && !zoomControls.Contains(_mouse.Position))
            {
                ClickMap(MousePoint);
            }
        }
        else if (_sidebar.Contains(_mouse.Position) && wheel != 0)
        {
            _panelScroll = Math.Clamp(_panelScroll - Math.Sign(wheel), 0, Math.Max(0, (_panel == "crew" ? _sim.Crew.Count : _sim.Log.Count) - 1));
        }
    }
    private void SelectTool(Tool tool)
    {
        _tool = tool;
        _anchor = null;
    }
    private void ChooseBlueprint(ModuleKind kind)
    {
        _blueprint = kind;
        SelectTool(Tool.Build);
        _modal = "";
        Message("Equipped " + Blueprint.Get(kind).Name.ToLowerInvariant() + ". Click the map to place.");
    }
    private void CenterHabitat()
    {
        _camera.Center = CellCenter(_sim.World.Center);
        _cursor = _sim.World.Center;
        _follow = null;
    }
    private void NextLesson()
    {
        if (_lesson < Lessons.Length - 1)
        {
            _lesson++;
        }
        else
        {
            _modal = "";
            _paused = false;
            Message("Ada is alone. Click Mika's distress marker to send help.");
        }
    }
    private void FocusDistress()
    {
        var contacts = _sim.Survivors.Where(s => s.Active).ToArray();
        if (contacts.Length == 0)
        {
            Message("No active distress contacts.");
            return;
        }
        int previous = Array.FindIndex(contacts, s => s.Id == _selectedSurvivor);
        var next = contacts[(previous + 1) % contacts.Length];
        _selectedSurvivor = next.Id;
        _selectedCrew = null;
        _cursor = next.Cell;
        _camera.Center = CellCenter(next.Cell);
        _follow = null;
        _panel = "colony";
    }
    private void ClickMap(Vector2 point)
    {
        // Distress hit targets retain a useful screen size at every zoom level.
        Survivor? closest = null;
        float distance = 22 * 22;
        foreach (var s in _sim.Survivors)
        {
            if (!s.Active)
            {
                continue;
            }

            float d = Vector2.DistanceSquared(_camera.WorldToScreen(CellCenter(s.Cell)), point);
            if (d < distance)
            {
                distance = d;
                closest = s;
            }
        }
        if (closest is not null)
        {
            _anchor = null;
            _cursor = closest.Cell;
            _selectedSurvivor = closest.Id;
            _selectedCrew = null;
            _panel = "colony";
            PrepareRescue(closest.Id);
            return;
        }
        var site = _campaign.Sites.Where(s => s.ContactId is null).OrderBy(s => Vector2.DistanceSquared(_camera.WorldToScreen(CellCenter(s.MapCell)), point)).FirstOrDefault();
        if (site is not null && Vector2.DistanceSquared(_camera.WorldToScreen(CellCenter(site.MapCell)), point) < 22 * 22)
        { PrepareSite(site.Id); return; }
        var pos = _camera.ScreenToWorld(point);
        int x = (int)Math.Floor(pos.X), y = (int)Math.Floor(pos.Y);
        if (!_sim.World.Inside(x, y))
        {
            return;
        }

        _cursor = _sim.World.Index(x, y);
        _follow = null;
        if (_tool != Tool.Inspect)
        {
            PlaceAtCursor();
            return;
        }
        _selectedCrew = _sim.CrewAt(_cursor)?.Id;
        _selectedSurvivor = null;
        _panel = "colony";
    }
    private void PlaceAtCursor()
    {
        if (_campaign.Sites.FirstOrDefault(s => s.MapCell == _cursor && s.ContactId is null) is { } site)
        { PrepareSite(site.Id); return; }
        if (_sim.SurvivorAt(_cursor) is { } contact)
        {
            _anchor = null;
            _selectedSurvivor = contact.Id;
            _selectedCrew = null;
            PrepareRescue(contact.Id);
            return;
        }
        if (_tool == Tool.Inspect)
        {
            _selectedCrew = _sim.CrewAt(_cursor)?.Id;
            _selectedSurvivor = null;
            _panel = "colony";
            return;
        }
        if (_anchor is int start)
        {
            int x1 = Math.Min(_sim.World.X(start), _sim.World.X(_cursor)), x2 = Math.Max(_sim.World.X(start), _sim.World.X(_cursor));
            int y1 = Math.Min(_sim.World.Y(start), _sim.World.Y(_cursor)), y2 = Math.Max(_sim.World.Y(start), _sim.World.Y(_cursor));
            if ((x2 - x1 + 1) * (y2 - y1 + 1) > 4096)
            {
                Message("Select a region of at most 4096 cells.", NoticeLevel.Warning);
                return;
            }
            int queued = 0;
            for (int y = y1; y <= y2; y++)
            {
                for (int x = x1; x <= x2; x++)
                {
                    if (_sim.Designate(x, y, _tool, _blueprint).Success)
                    {
                        queued++;
                    }
                }
            }

            _anchor = null;
            Message($"{queued} work orders accepted.", queued > 0 ? NoticeLevel.Good : NoticeLevel.Warning);
        }
        else
        {
            Message(_sim.Designate(_sim.World.X(_cursor), _sim.World.Y(_cursor), _tool, _blueprint));
        }
    }
    private void SaveColony()
    {
        try
        {
            _campaign.Save(_options.SavePath);
            Message("Campaign saved.", NoticeLevel.Good);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Message("Save failed: " + e.Message, NoticeLevel.Warning); }
    }
    private void LoadColony()
    {
        try
        {
            _campaign = Ithica.Core.Campaign.Load(_options.SavePath);
            _modal = ""; _fieldRoute.Clear(); ResetFieldCamera();
            _paused = true;
            _selectedCrew = _selectedSurvivor = _follow = _anchor = null;
            _crewPositions = [];
            CenterHabitat();
            Message("Colony restored. Time paused.", NoticeLevel.Good);
        }
        catch (Exception e) when (e is IOException or JsonException or ArgumentException) { Message("Load failed: " + e.Message, NoticeLevel.Warning); }
    }
    private void NewColony()
    {
        _campaign = new Ithica.Core.Campaign(_options.Seed, _options.Width, _options.Height);
        _paused = true;
        _modal = "tutorial";
        _lesson = 0;
        _selectedCrew = _selectedSurvivor = _follow = _anchor = null;
        _crewPositions = [];
        CenterHabitat();
    }

    protected override void Draw(GameTime gameTime)
    {
        int w = GraphicsDevice.PresentationParameters.BackBufferWidth, h = GraphicsDevice.PresentationParameters.BackBufferHeight;
        if (_frame is null || _frame.Width != w || _frame.Height != h)
        {
            _frame?.Dispose();
            _frame = new RenderTarget2D(GraphicsDevice, w, h, false, SurfaceFormat.Color, DepthFormat.None);
        }
        GraphicsDevice.SetRenderTarget(_frame);
        GraphicsDevice.Clear(Background);
        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, _scissor);
        GraphicsDevice.ScissorRectangle = _camera.Viewport;
        if (!_atlas) { if (_campaign.Phase == CampaignPhase.Expedition) DrawFieldMap(); else { DrawMap(); DrawCampaignMarkers(); } }
        _batch.End();
        GraphicsDevice.ScissorRectangle = new(0, 0, w, h);
        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        if (_atlas) DrawAtlas(w, h);
        else if (_campaign.Phase == CampaignPhase.Expedition) DrawFieldInterface(w, h);
        else { DrawInterface(w, h); if (_campaign.Phase == CampaignPhase.Preparation) DrawDeparture(w, h); else if (_campaign.Phase == CampaignPhase.Arrival) DrawArrival(w, h); }
        if (_modal is "tutorial" or "build" or "help" && !_atlas)
        {
            DrawModal(w, h);
        }

        _batch.End();
        GraphicsDevice.SetRenderTarget(null);
        _batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        _batch.Draw(_frame, new Rectangle(0, 0, w, h), Color.White);
        _batch.End();
        _drawCount++;
        int end = _options.SmokeTest ? 12 : _options.Frames;
        if (end > 0 && _drawCount >= end)
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
                    checks = _smokeChecks,
                    artworkQueries = ArtworkQuery.Diagnostics(_entries),
                    bindings = _sprites.BindingCount,
                    frames = _drawCount
                }));
            }

            Exit();
        }
        base.Draw(gameTime);
    }
    private void RunSmokeTest()
    {
        if (_drawCount != 2) return;
        void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException("Graphical smoke test failed: " + message);
            _smokeChecks.Add(message);
        }
        Check(_modal == "tutorial", "sectioned tutorial opens"); NextLesson(); Check(_lesson == 1, "tutorial navigation"); _modal = "";
        var contact = _sim.Survivors[0]; var stock = _sim.Resources;
        SelectTool(Tool.Dig); ClickMap(_camera.WorldToScreen(CellCenter(contact.Cell)));
        Check(_campaign.Phase == CampaignPhase.Preparation && _sim.Resources == stock, "map click opens preparation without spending supplies");
        ConfirmDeparture(); Check(_campaign.Phase == CampaignPhase.Expedition && _campaign.PartyIds.SequenceEqual(new[] { 0 }), "solo departure in the same window");
        string frozen = _campaign.StateJson(); _campaign.UpdateRealtime(600); Check(frozen == _campaign.StateJson(), "both sides freeze during decisions");
        long tick = _campaign.CampaignTick;
        _previousKeyboard = new(); _keyboard = new(Keys.Space); HandleCampaignInput(); _keyboard = new();
        Check(_campaign.CampaignTick == tick + 1, "keyboard wait spends exactly one shared tick");
        var pointer = new Vector2(_camera.Viewport.X + 120, _camera.Viewport.Y + 90);
        var anchor = _fieldCamera.ScreenToWorld(pointer); _fieldCamera.ZoomAt(pointer, 1);
        Check(Vector2.Distance(anchor, _fieldCamera.ScreenToWorld(pointer)) < .0001f, "field pointer zoom preserves anchor");
        for (int n = 0; n < 40; n++) _fieldCamera.ZoomAt(pointer, -1);
        Check(_fieldCamera.CellSize == 6, "field zoom lower bound");
        for (int n = 0; n < 40; n++) _fieldCamera.ZoomAt(pointer, 1);
        Check(_fieldCamera.CellSize == 72, "field zoom upper bound"); _fieldCamera.Reset(28);
        CampaignScenarios.Approach(_campaign, "survivor"); CampaignScenarios.Approach(_campaign, "return");
        Check(_campaign.Phase == CampaignPhase.Arrival && _sim.RescuedCount == 1 && _sim.Crew.Count == 2, "actual rescue recruits only after returning to pressure");
        CampaignScenarios.Require(_campaign.DismissArrival()); SyncCampaignUI(); CampaignScenarios.Require(_campaign.StepColony(80));
        PrepareSite("salvage"); ConfirmDeparture(); CampaignScenarios.Approach(_campaign, "presence", fight: false);
        Check(_campaign.ActiveField?.Battle is not null, "salvage encounter is top-down and playable");
        string file = Path.Combine(Path.GetTempPath(), "ithica-smoke-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            _campaign.Save(file); var loaded = Ithica.Core.Campaign.Load(file);
            Check(loaded.StateJson() == _campaign.StateJson(), "unified combat save resumes exact state");
        }
        finally { File.Delete(file); }
        CampaignScenarios.Fight(_campaign); CampaignScenarios.Approach(_campaign, "component"); CampaignScenarios.Approach(_campaign, "return");
        CampaignScenarios.Require(_campaign.DismissArrival()); SyncCampaignUI();
        Check(_sim.Resources.Components == 1 && _sim.Crew[0].Health < 100, "cargo and expedition injuries return to the colony");
        int cell = _sim.World.Floors.First(i => _sim.World.Breathable(i) && _sim.MachineAt(i) is null && _sim.JobAt(i) is null);
        _cursor = cell; ChooseBlueprint(ModuleKind.SuitRack); PlaceAtCursor();
        Check(_sim.Resources.Components == 0, "construction reserves the recovered component once");
        for (int i = 0; !_campaign.SupportUpgrade && i < 240; i++) CampaignScenarios.Require(_campaign.StepColony());
        Check(_campaign.SupportUpgrade && _campaign.Sites.Any(s => s.Id == "below"), "workers build the upgrade and reveal the next site");
        int cx = _sim.World.Width / 2, cy = _sim.World.Height / 2; var before = _sim.Resources;
        Check(_sim.Designate(cx, cy + 1, Tool.Build, ModuleKind.Solar).Success, "ordinary construction accepts work");
        Check(_sim.Designate(cx, cy + 1, Tool.Cancel).Success && _sim.Resources == before, "cancel refunds reserved stock");
        Check(_entries.Count == 7115 && _fieldSprites.HasBinding("resident"), "complete atlas and lunar actor binding load");
        string artworkState = _campaign.StateJson();
        CheckArtworkAtlas(Check);
        Check(_campaign.StateJson() == artworkState, "atlas filtering preserves exact campaign state");
        CenterHabitat(); _selectedSurvivor = null; _selectedCrew = 0; _paused = true; _modal = ""; SelectTool(Tool.Inspect);
    }
    protected override void UnloadContent()
    {
        _frame?.Dispose();
        _sprites.Dispose();
        _fieldSprites.Dispose();
        _pixel.Dispose();
        _batch.Dispose();
        _scissor.Dispose();
        base.UnloadContent();
    }
}
