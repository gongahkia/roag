using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private TitleBackdrop? _titleBackdrop;
    private bool HasTitleContinuation => _session.State.Tactics!.NextAction > 1 || !_options.NewGame && File.Exists(_options.Save);
    private Rectangle TitleCard(int w, int h) => new(Math.Max(48, w * 8 / 100), (h - 548) / 2, 440, 548);

    private void OpenTitleScreen()
    {
        _titleBackdrop ??= new(WorldStartup.FreshSeed());
        _panel = "title"; _controllerPanel = ""; _controllerFocus = null;
        _session.ModalOpen = true; _session.Focused = false;
        _dungeonAudio?.Stop(); _audioContext = null; ClearWorldSelection();
    }

    private void LeaveTitleScreen()
    {
        _titleBackdrop?.Dispose(); _titleBackdrop = null;
        ClosePanel(); _controllerFocus = null; _controllerPanel = "";
        _session.ModalOpen = false; _session.Focused = InputFocused;
        _panel = _session.State.Tactics!.NextAction == 1 ? "dungeon-class" : "";
        _notice = "WASD / arrows move one tile. F attacks, R special, X movement. Enter interacts.";
        _dungeonAudio?.Stop(); _audioContext = null; _dungeonPresentationStamp = 0;
        UpdateMotions(TimeSpan.Zero); UpdateView();
    }

    private void BuildTitleControls(int w, int h)
    {
        var card = TitleCard(w, h);
        void Add(string id, string label, int row) => ControlCache.Add("title:" + id, label,
            new(card.X + 34, card.Y + 198 + row * 58, card.Width - 68, 46), true, false, "", "", null, "", -1, null, false);
        if (_panel == "title-new-confirm")
        {
            Add("cancel", "Keep this run", 3);
            Add("new-go", "Begin a fresh run", 2);
        }
        else
        {
            Add("continue", HasTitleContinuation ? _session.State.AwaitingContinuation ? "View ended run" : "Continue" : "Begin Run", 0);
            Add("new", "New Run", 1); Add("settings", "Settings", 2); Add("quit", "Quit", 3);
        }
    }

    private bool ActivateTitleControl(string id)
    {
        if (_titleBackdrop == null || !id.StartsWith("title:", StringComparison.Ordinal)) return false;
        PlayDungeonSound("menu");
        switch (id)
        {
            case "title:continue": LeaveTitleScreen(); break;
            case "title:new":
                if (HasTitleContinuation) _panel = "title-new-confirm";
                else { LeaveTitleScreen(); NewWorld(); }
                break;
            case "title:new-go": LeaveTitleScreen(); NewWorld(); break;
            case "title:cancel": _panel = "title"; break;
            case "title:settings": _panel = "dungeon-settings"; break;
            case "title:quit": Exit(); break;
        }
        return true;
    }

    private void UpdateTitleScreen(GameTime gameTime)
    {
        int w = GraphicsDevice.Viewport.Width, h = GraphicsDevice.Viewport.Height;
        BuildMouseControls(w, h);
        var mouse = _playtest?.Next(gameTime.ElapsedGameTime.TotalSeconds, ResolvePlaytest, MarkMoment) ?? Mouse.GetState();
        if (_playtest is { StopRequested: true, Holding: true }) { Exit(); return; }
        var keyboard = Keyboard.GetState(); var pad = GamePad.GetState(PlayerIndex.One);
        if (InputFocused)
        {
            bool Press(Keys key) => keyboard.IsKeyDown(key) && _roagKeyboard.IsKeyUp(key);
            bool Pad(Buttons button) => DungeonController.Press(pad, _dungeonPad, button);
            var choices = _controls.Where(c => c.Enabled).ToArray(); EnsureDungeonMenuFocus(choices);
            int delta = Press(Keys.Up) || Press(Keys.Left) || Pad(Buttons.DPadUp) || Pad(Buttons.DPadLeft) ? -1
                : Press(Keys.Down) || Press(Keys.Right) || Press(Keys.Tab) || Pad(Buttons.DPadDown) || Pad(Buttons.DPadRight) ? 1 : 0;
            if (delta != 0) StepDungeonMenu(choices, delta);
            if (Press(Keys.Escape) || Pad(Buttons.B)) { if (_panel != "title") ClosePanel(); }
            else if ((Press(Keys.Enter) || Pad(Buttons.A) || Pad(Buttons.Start)) && _controllerFocus is { } selected) ActivateControl(selected);
            else if (mouse.LeftButton == ButtonState.Pressed && _mouse.LeftButton == ButtonState.Released)
            {
                var control = WorldMouseControl.Hit(_controls, mouse.Position);
                _lastMousePress = new((_lastMousePress?.Sequence ?? 0) + 1, 1, mouse.X, mouse.Y, control?.Id);
                if (control is { Enabled: true }) ActivateControl(control.Id);
            }
            if (mouse.RightButton == ButtonState.Pressed && _mouse.RightButton == ButtonState.Released) ClosePanel();
        }
        _roagKeyboard = keyboard; _dungeonPad = pad; _mouse = mouse;
        if (_titleBackdrop == null) return;
        _titleBackdrop.Resize(new(0, 0, w, h));
        bool running = _playtest == null ? IsActive : _playtest.AdvancesWorldPresentation;
        _titleBackdrop.Update(gameTime.ElapsedGameTime.TotalSeconds, running, _preferences.ReducedMotion);
        _dungeonAudio ??= new();
        _dungeonAudio.Update("explore/" + _titleBackdrop.World.State.Dungeon!.Floors[0].Biome,
            _preferences.MusicVolume, _preferences.EffectsVolume, !InputFocused, gameTime.ElapsedGameTime.TotalSeconds);
        _session.ModalOpen = true; _session.Focused = false;
    }

    private void DrawTitleScreen(int w, int h)
    {
        _titleBackdrop!.Draw(_batch, _sprites, _pixel, _uiFonts!, new(0, 0, w, h), _preferences);
        Fill(new(0, 0, w, h), new Color(8, 13, 19) * .43f);
        if (_panel == "dungeon-settings") DrawDungeonPanel(w, h);
        else
        {
            var card = TitleCard(w, h);
            Fill(card, new Color(13, 20, 26) * .9f);
            Fill(new(card.X, card.Y, card.Width, 3), Gold);
            Fill(new(card.X, card.Bottom - 3, card.Width, 3), Gold * .5f);
            Text("ROAG", card.X + 34, card.Y + 34, Gold, 7);
            Text("TURN-BASED DUNGEON RUNS", card.X + 34, card.Y + 142, Ink);
            if (_panel == "title-new-confirm")
            {
                Text("START A NEW RUN?", card.X + 34, card.Y + 192, Gold);
                Wrap("Your current character, build and dungeon will be replaced in the continuing save slot.", card.X + 34, card.Y + 220, card.Width - 68, Muted);
            }
            else
            {
                var hero = _session.DungeonMember;
                int members = _session.Party.Count(a => a.Life != LifeState.Dead);
                string place = _session.State.Dungeon is { } run ? "stage " + run.Stage : "day " + (_session.State.Tick / WorldSession.TicksPerDay + 1);
                string detail = HasTitleContinuation ? members > 1 ? $"{members} members / {place}"
                    : $"{ClassCatalog.Get(hero.Tactics!.Build!.Class).Name} / level {hero.Level} / {place}"
                    : "Fifteen classes. Find your next build.";
                Text(Short(detail, (card.Width - 68) / (_uiFonts?.Advance ?? 8)), card.X + 34, card.Y + 450, Muted);
            }
            Text("Arrows select / Enter confirms", card.X + 34, card.Bottom - 34, Muted);
        }
        DrawMouseControls(w, h);
    }
}
