using Microsoft.Xna.Framework;
using Regolith.Core;

namespace Regolith.Client;

public sealed partial class ColonyGame
{
    private string? _tooltip;
    private bool Button(Rectangle bounds, string label, string? icon = null, bool selected = false, bool enabled = true, bool modal = false, string? tooltip = null)
    {
        bool hot = bounds.Contains(_mouse.Position) && (_modal.Length == 0 || modal);
        var surface = bounds;
        if (hot && _mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
        {
            int dx = Math.Max(1, (int)(surface.Width * .02f)), dy = Math.Max(1, (int)(surface.Height * .02f));
            surface.Inflate(-dx, -dy);
        }
        Fill(surface, selected ? new Color(35, 65, 66) : hot ? new Color(39, 52, 59) : Raised);
        Outline(surface, selected ? Accent * .65f : Color.White * (hot ? .22f : .08f));
        if (selected)
        {
            Fill(new(surface.X, surface.Y, 2, surface.Height), Accent);
        }

        Color text = enabled ? (selected ? Accent : Ink) : Muted * .5f;
        if (icon is not null)
        {
            _sprites.Fit(_batch, icon, new(surface.X + 10, surface.Center.Y - 10, 20, 20), text);
        }

        if (label.Length > 0)
        {
            float x = icon is null ? surface.Center.X - SpriteLibrary.Measure(label) / 2 : surface.X + 40;
            Text(label, x, surface.Center.Y - 6, text);
        }
        if (hot && tooltip is not null)
        {
            _tooltip = tooltip;
        }

        if (hot && _leftPressed && enabled)
        {
            _leftPressed = false;
            return true;
        }
        return false;
    }
    private void Bar(string label, float value, float x, float y, float width, Color color, string? display = null)
    {
        Text(label, x, y, Muted, .85f);
        string number = display ?? $"{value * 100:0}%";
        Text(number, x + width - SpriteLibrary.Measure(number, .85f), y, color, .85f);
        Fill(new((int)x, (int)y + 17, (int)width, 5), Raised);
        Fill(new((int)x, (int)y + 17, (int)(width * Math.Clamp(value, 0, 1)), 5), color);
    }
    private static string Short(string text, int length) => text.Length <= length ? text : text[..Math.Max(0, length - 3)] + "...";
    private static string Cost(Stock cost)
    {
        string text = "";
        if (cost.Metal > 0)
        {
            text = $"{cost.Metal:0} metal";
        }

        if (cost.Crystal > 0)
        {
            text += (text.Length > 0 ? " + " : "") + $"{cost.Crystal:0} crystal";
        }

        if (cost.Water > 0)
        {
            text += (text.Length > 0 ? " + " : "") + $"{cost.Water:0} water";
        }

        return text;
    }
    private void StatCard(Rectangle r, string title, string value, string icon, Color color)
    {
        Fill(r, Surface);
        _sprites.Fit(_batch, icon, new(r.X + 12, r.Y + 11, 24, 24), color);
        Text(title, r.X + 48, r.Y + 5, Muted, .8f);
        Text(value, r.X + 48, r.Y + 22, Ink, 1.15f);
    }
    private void DrawInterface(int width, int height)
    {
        _tooltip = null;
        Text("REGOLITH", 20, 16, Ink, 2);
        Text("TRANQUILITY OUTPOST", 184, 21, Muted);
        if (width > 1160)
        {
            Text($"CYCLE {_sim.Cycle:00} / {(_sim.Storm ? "PARTICLE STORM" : _sim.Daylight > .1f ? "LUNAR DAY" : "LUNAR NIGHT")}", width - 618, 21, _sim.Storm ? Danger : Muted, .85f);
        }

        if (Button(new(width - 268, 8, 44, 40), "", "icon-save", tooltip: "Save colony [S]"))
        {
            SaveColony();
        }

        if (Button(new(width - 216, 8, 64, 40), "", _paused ? "icon-play" : "icon-pause", !_paused, tooltip: "Pause / resume [Space]"))
        {
            _paused = !_paused;
        }

        foreach (var (index, label) in new[] { (0, "1x"), (1, "3x"), (2, "8x") })
        {
            if (Button(new(width - 144 + index * 44, 8, 40, 40), label, selected: _speed == index, tooltip: "Simulation speed [1 / 2 / 3]"))
            {
                _speed = index;
            }
        }

        int cardWidth = (width - 88) / 6;
        StatCard(new(76, 62, cardWidth - 8, 44), "METAL", $"{_sim.Resources.Metal:0}", "icon-metal", Amber);
        StatCard(new(76 + cardWidth, 62, cardWidth - 8, 44), "WATER", $"{_sim.Resources.Water:0.0}", "icon-water", Blue);
        StatCard(new(76 + cardWidth * 2, 62, cardWidth - 8, 44), "FOOD", $"{_sim.Resources.Food:0}", "icon-food", Accent);
        StatCard(new(76 + cardWidth * 3, 62, cardWidth - 8, 44), "CRYSTAL", $"{_sim.Resources.Crystal:0}", "icon-crystal", Purple);
        StatCard(new(76 + cardWidth * 4, 62, cardWidth - 8, 44), "HABITAT O2", $"{_sim.Pressure * 100:0}%", "icon-shield", _sim.Pressure > .4f ? Accent : Danger);
        StatCard(new(76 + cardWidth * 5, 62, cardWidth - 8, 44), "CREW / SAVED", $"{_sim.LivingCount:00} / {_sim.RescuedCount:00}", "icon-crew", Ink);

        (Tool Tool, string Icon, string Key, string Tip)[] tools = [(Tool.Inspect,"icon-inspect","ESC","Inspect / select"),(Tool.Dig,"icon-mine","D","Mine or salvage [D]"),
            (Tool.Build,"icon-build","B","Choose a blueprint [B]"),(Tool.Survey,"icon-survey","P","Survey open ground [P]"),(Tool.Cancel,"icon-cancel","X","Cancel a work order [X]")];
        for (int i = 0; i < tools.Length; i++)
        {
            var tool = tools[i];
            var r = new Rectangle(12, 116 + i * 64, 52, 52);
            if (Button(r, "", tool.Icon, _tool == tool.Tool, tooltip: tool.Tip))
            {
                if (tool.Tool == Tool.Build)
                {
                    _modal = "build";
                }
                else
                {
                    SelectTool(tool.Tool);
                }
            }
            Text(tool.Key, r.Center.X - SpriteLibrary.Measure(tool.Key, .65f) / 2, r.Bottom + 3, Muted, .65f);
        }
        if (Button(new(12, 464, 52, 44), "", "icon-shield", _overlay > 0, tooltip: "Cycle oxygen, temperature, and power overlays [O]"))
        {
            _overlay = (_overlay + 1) % 4;
        }

        if (Button(new(12, 516, 52, 44), "", "icon-center", tooltip: "Return to habitat [G]"))
        {
            CenterHabitat();
        }

        if (Button(new(12, 568, 52, 44), "", "icon-signal", tooltip: "Locate next distress contact [R]"))
        {
            FocusDistress();
        }

        if (Button(new(12, 620, 52, 44), "", "icon-help", tooltip: "Open the field manual [T]"))
        {
            _modal = "tutorial";
            _lesson = 0;
        }
        Outline(_camera.Viewport, Color.White * .08f);
        int zoomY = _camera.Viewport.Bottom - 50, zoomX = _camera.Viewport.Right - 156;
        if (Button(new(zoomX, zoomY, 40, 40), "-", tooltip: "Zoom out at cursor [-]"))
        {
            _camera.ZoomAt(_camera.WorldToScreen(CellCenter(_cursor)), -1);
        }

        if (Button(new(zoomX + 44, zoomY, 64, 40), $"{_camera.CellSize / 24 * 100:0}%", tooltip: "Reset zoom [0]"))
        {
            _camera.Reset();
        }

        if (Button(new(zoomX + 112, zoomY, 40, 40), "+", tooltip: "Zoom in at cursor [+]"))
        {
            _camera.ZoomAt(_camera.WorldToScreen(CellCenter(_cursor)), 1);
        }

        DrawSidebar();
        DrawEvents();
        string footer = _elapsed < _messageUntil ? _message : $"{_tool.ToString().ToUpperInvariant()} / ARROWS MOVE   SPACE PAUSE   R DISTRESS   ? HELP";
        Text(Short(footer, Math.Max(20, (width - 420) / 8)), 76, height - 22, _elapsed < _messageUntil ? LevelColor(_messageLevel) : Muted, .85f);
        string performance = $"{_fps:0} FPS / {_tickMilliseconds:0.00} MS / {_sim.World.X(_cursor)},{_sim.World.Y(_cursor)}";
        Text(performance, width - 16 - SpriteLibrary.Measure(performance, .75f), height - 21, Muted, .75f);
        if (_tooltip is not null)
        {
            int w = Math.Min(340, (int)SpriteLibrary.Measure(_tooltip) + 24), x = Math.Clamp(_mouse.X + 18, 4, width - w - 4), y = Math.Min(height - 70, _mouse.Y + 20);
            Fill(new(x, y, w, 60), Background * .98f);
            Outline(new(x, y, w, 60), Color.White * .16f);
            Wrap(_tooltip, x + 12, y + 10, w - 24, Ink, .9f);
        }
    }
    private void DrawEvents()
    {
        Fill(_eventArea, Surface);
        Text("MISSION LOG", _eventArea.X + 14, _eventArea.Y + 10, Muted, .8f);
        if (Button(new(_eventArea.Right - 110, _eventArea.Y + 4, 102, 40), "OPEN LOG", tooltip: "Open the full event log [E]"))
        {
            _panel = "log";
            _panelScroll = 0;
        }
        for (int n = 0; n < 3 && n < _sim.Log.Count; n++)
        {
            var entry = _sim.Log[_sim.Log.Count - 1 - n];
            int y = _eventArea.Y + 38 + n * 22;
            Text($"{entry.Tick / Simulation.TicksPerCycle + 1:00}:{entry.Tick % Simulation.TicksPerCycle:000}", _eventArea.X + 14, y, Muted, .8f);
            Text(Short(entry.Text, Math.Max(10, (_eventArea.Width - 112) / 8)), _eventArea.X + 94, y, LevelColor(entry.Level), .9f);
        }
    }
    private void DrawSidebar()
    {
        Fill(_sidebar, Surface);
        int tabWidth = (_sidebar.Width - 16) / 3;
        for (int i = 0; i < 3; i++)
        {
            string tab = i == 0 ? "colony" : i == 1 ? "crew" : "log";
            if (Button(new(_sidebar.X + 8 + i * tabWidth, _sidebar.Y + 8, tabWidth - 4, 40), tab.ToUpperInvariant(), selected: _panel == tab))
            {
                _panel = tab;
                _panelScroll = 0;
            }
        }
        float x = _sidebar.X + 18, y = _sidebar.Y + 70, w = _sidebar.Width - 36;
        if (_panel == "crew")
        {
            Text("PEOPLE OF TRANQUILITY", x, y, Muted, .85f);
            y += 28;
            int rows = Math.Max(1, (int)((_sidebar.Bottom - y - 16) / 76));
            for (int n = _panelScroll; n < Math.Min(_sim.Crew.Count, _panelScroll + rows); n++)
            {
                var c = _sim.Crew[n];
                var r = new Rectangle((int)x, (int)y, (int)w, 68);
                if (Button(r, "", selected: c.Id == _selectedCrew, tooltip: "Select " + c.Name))
                {
                    _selectedCrew = c.Id;
                    _selectedSurvivor = null;
                    _cursor = c.Cell;
                    _camera.Center = CellCenter(c.Cell);
                    _panel = "colony";
                }
                _sprites.Fit(_batch, c.Role == "Engineer" ? "engineer" : c.Role == "Miner" ? "miner" : "crew", new(r.X + 8, r.Y + 14, 30, 30), c.Alive ? RoleColor(c.Role) : Muted * .4f);
                Text(c.Name.ToUpperInvariant(), x + 50, y + 8, c.Alive ? Ink : Muted);
                Text(c.Role, x + 50, y + 26, RoleColor(c.Role), .8f);
                Text(Short(c.Activity, 27), x + 10, y + 49, Muted, .75f);
                y += 76;
            }
            if (_sim.Crew.Count == 0)
            {
                Wrap("The sleeping pods are empty.", x, y, w, Muted);
            }

            return;
        }
        if (_panel == "log")
        {
            Text("OUTPOST TIMELINE", x, y, Muted, .85f);
            y += 30;
            for (int n = _panelScroll; n < _sim.Log.Count && y < _sidebar.Bottom - 90; n++)
            {
                var entry = _sim.Log[_sim.Log.Count - 1 - n];
                Text($"CYCLE {entry.Tick / 480 + 1:00} / {entry.Tick % 480:000}", x, y, LevelColor(entry.Level), .75f);
                y += 20;
                y += Wrap(entry.Text, x, y, w, Ink, .9f) + 14;
                Fill(new((int)x, (int)y - 6, (int)w, 1), Color.White * .06f);
            }
            return;
        }
        if (_selectedSurvivor is int contact && contact < _sim.Survivors.Count)
        {
            var s = _sim.Survivors[contact];
            Color color = s.State == ContactState.Stranded ? Danger : Amber;
            Text(s.State == ContactState.Stranded ? "DISTRESS CONTACT" : "RESCUE CONTACT", x, y, color, .8f);
            y += 26;
            Text(s.Name.ToUpperInvariant(), x, y, Ink, 1.7f);
            y += 30;
            Text(s.Role + " / " + s.Trait, x, y, Muted, .85f);
            y += 30;
            Bar("HEALTH", s.Health / 100, x, y, w, Danger);
            y += 36;
            Bar("SUIT OXYGEN", s.Suit / 100, x, y, w, s.Suit < 25 ? Danger : Blue);
            y += 40;
            y += Wrap(s.State switch
            {
                ContactState.Stranded => "A damaged suit in the wasteland. Send a ready colonist with emergency supplies.",
                ContactState.Escorted => "Contact reached. Emergency oxygen shared; returning to pressurized shelter.",
                ContactState.Rescued => "Home safely. This survivor is now a member of your crew.",
                _ => "The signal has gone silent."
            }, x, y, w, Ink, .95f) + 16;
            if (s.State == ContactState.Stranded && _sim.ExpeditionFor(s.Id) is null)
            {
                Text("RESCUE KIT / 2 FOOD + 2 WATER", x, y, Muted, .75f);
                y += 24;
                if (Button(new((int)x, (int)y, (int)w, 44), "SEND RESCUE", "icon-rescue", enabled: _sim.Resources.Covers(Simulation.RescueCost)))
                {
                    Message(_sim.Dispatch(s.Id));
                }

                y += 62;
            }
            else if (_sim.ExpeditionFor(s.Id) is { } expedition)
            {
                var rescuer = _sim.Crew[expedition.CrewId];
                Text("ASSIGNED / " + rescuer.Name.ToUpperInvariant(), x, y, Accent, .85f);
                y += 24;
                if (Button(new((int)x, (int)y, (int)w, 44), "FOLLOW RESCUER", "icon-follow", _follow == rescuer.Id))
                {
                    _follow = _follow == rescuer.Id ? null : rescuer.Id;
                }

                y += 62;
            }
        }
        else if (_selectedCrew is int selected && selected < _sim.Crew.Count)
        {
            var c = _sim.Crew[selected];
            Text("CREW / " + c.Role.ToUpperInvariant(), x, y, RoleColor(c.Role), .8f);
            y += 26;
            Text(c.Name.ToUpperInvariant(), x, y, Ink, 1.7f);
            y += 32;
            Text(c.Trait, x, y, Muted, .9f);
            y += 28;
            Bar("HEALTH", c.Health / 100, x, y, w, Danger);
            y += 34;
            Bar("SUIT OXYGEN", c.Suit / 100, x, y, w, Blue);
            y += 34;
            Bar("ENERGY", c.Energy / 100, x, y, w, Amber);
            y += 34;
            Bar("NOURISHMENT", 1 - c.Hunger / 100, x, y, w, Accent);
            y += 34;
            y += Wrap(c.Activity, x, y, w, Ink, .95f) + 12;
            if (Button(new((int)x, (int)y, (int)w, 42), _follow == c.Id ? "STOP FOLLOWING" : "FOLLOW COLONIST", "icon-follow", _follow == c.Id))
            {
                _follow = _follow == c.Id ? null : c.Id;
            }

            y += 60;
        }
        else
        {
            int cell = _cursor;
            var world = _sim.World;
            Text("SECTOR / " + world.X(cell) + ", " + world.Y(cell), x, y, Muted, .8f);
            y += 26;
            string title = world.Discovered[cell] == 0 ? "UNSURVEYED" : _sim.MachineAt(cell) is { } machine ? Blueprint.Get(machine.Kind).Name.ToUpperInvariant() : Simulation.TerrainName(world.Tiles[cell]).ToUpperInvariant();
            y += Wrap(title, x, y, w, Ink, 1.35f) + 14;
            if (world.Discovered[cell] > 0)
            {
                Bar("LOCAL PRESSURE", world.Oxygen[cell], x, y, w, Accent);
                y += 36;
                Text($"TEMPERATURE / {world.Temperature(cell):0.0} C", x, y, Muted, .85f);
                y += 30;
                if (_sim.MachineAt(cell) is { } m)
                {
                    Text(m.Powered ? "POWER / CONNECTED" : "POWER / DISCONNECTED", x, y, m.Powered ? Accent : Danger, .85f);
                    y += 25;
                    y += Wrap(Blueprint.Get(m.Kind).Description, x, y, w, Ink, .9f) + 12;
                }
                if (_sim.JobAt(cell) is { } job)
                {
                    Bar("WORK ORDER", job.Progress / job.Required, x, y, w, Amber);
                    y += 38;
                }
            }
            else
            {
                y += Wrap("Survey nearby open terrain to reveal this part of the Moon.", x, y, w, Muted) + 16;
            }
        }
        if (y < _sidebar.Bottom - 230)
        {
            Fill(new((int)x, (int)y, (int)w, 1), Color.White * .08f);
            y += 20;
            Text("LIFE AT TRANQUILITY", x, y, Muted, .8f);
            y += 25;
            Bar("HABITAT PRESSURE", _sim.Pressure, x, y, w, _sim.Pressure > .4f ? Accent : Danger);
            y += 36;
            Bar("BATTERY RESERVE", _sim.Capacity > 0 ? _sim.Charge / _sim.Capacity : 0, x, y, w, Amber, $"{_sim.Charge:0}/{_sim.Capacity:0}");
            y += 36;
            Text($"POWER / {_sim.Generated:0.0} IN : {_sim.Demand:0.0} KW", x, y, _sim.Generated >= _sim.Demand ? Muted : Amber, .85f);
            y += 34;
        }
        foreach (var s in _sim.Survivors)
        {
            if (!s.Active || y > _sidebar.Bottom - 84)
            {
                continue;
            }

            var box = new Rectangle((int)x, (int)y, (int)w, 64);
            if (Button(box, "", tooltip: "Locate " + s.Name + "'s distress contact"))
            {
                _selectedSurvivor = s.Id;
                _selectedCrew = null;
                _cursor = s.Cell;
                _camera.Center = CellCenter(s.Cell);
            }
            _sprites.Fit(_batch, "icon-signal", new(box.X + 9, box.Y + 12, 22, 22), s.State == ContactState.Stranded ? Danger : Amber);
            Text(s.Name.ToUpperInvariant() + " / " + s.State.ToString().ToUpperInvariant(), x + 42, y + 9, s.State == ContactState.Stranded ? Danger : Amber, .8f);
            Text($"SUIT O2 {s.Suit:0}% / HEALTH {s.Health:0}%", x + 12, y + 38, Muted, .8f);
            y += 74;
        }
        // Pin the charter to the bottom when there is room, without covering the inspector.
        if (y < _sidebar.Bottom - 126)
        {
            y = Math.Max(y + 12, _sidebar.Bottom - 128);
            Text(_sim.Victory ? "CHARTER GRANTED" : "FOUNDING CHARTER", x, y, _sim.Victory ? Accent : Muted, .8f);
            y += 25;
            CheckLine(_sim.RescuedCount >= 3, $"Rescue 3 people / {_sim.RescuedCount}", x, y);
            y += 23;
            CheckLine(_sim.Cycle >= 3 && _sim.LivingCount >= 4, "Cycle 3 + 4 living crew", x, y);
            y += 23;
            CheckLine(_sim.Resources.Food >= 20 && _sim.Machines.Any(m => m.Kind == ModuleKind.Beacon && m.Powered && _sim.World.Oxygen[m.Cell] >= .4f), "20 food + powered Earth uplink", x, y);
        }
    }
    private void CheckLine(bool done, string label, float x, float y)
    {
        var box = new Rectangle((int)x, (int)y + 2, 9, 9);
        Outline(box, done ? Accent : Muted * .5f);
        if (done)
        {
            box.Inflate(-2, -2);
            Fill(box, Accent);
        }
        Text(label, x + 18, y, done ? Accent : Muted, .8f);
    }

    private sealed record Lesson(string Title, string Story, string Instructions, string Objective, string Key);
    private static readonly Lesson[] Lessons = [
        new("ONE LIGHT IN THE DARK","Only Ada reached Tranquility. The generator hums, the air is good, and the other sleeping pods are empty.",
            "Space runs or pauses time. Move the cursor with arrows or H/J/K/L. Scroll over the map to zoom at your pointer; +/- zooms at the cursor. Drag with the right or middle mouse button to pan. Ada works, eats, and sleeps on her own.",
            "The colony starts with one person. The field manual holds time.","key-space"),
        new("A VOICE IN THE WASTELAND","Mika's damaged suit is losing oxygen outside the outpost. A red distress contact appears on the map, even beyond explored ground.",
            "Click the stranded person directly on the map to send a ready colonist. Each rescue kit costs 2 food and 2 water. You can also place the cursor on them and press Enter. R locates the next contact.",
            "First job: click Mika's distress marker, then resume time.","key-mouse"),
        new("BRING THEM HOME","Reaching someone is only half the rescue. Ada shares emergency oxygen, then escorts the injured survivor home at half walking speed.",
            "Survivors join only after reaching pressurized shelter. They arrive hungry and exhausted, then recover, eat, sleep, and work. More distress signals arrive over time. Leave someone too long and their oxygen, then health, runs out.",
            "Grow through rescues. Open CREW or press C to meet your people.","key-r"),
        new("LIVE OFF THE MOON","Every new arrival needs air, water, food, and a place to recover. The supplies in this abandoned outpost will not last forever.",
            "Choose the mining tool with D, then click cyan ice, amber ore, or purple crystal to queue work. V starts a rectangular selection; move to the opposite corner and confirm. P surveys open ground and uncovers more terrain.",
            "Mine ice for water, ore for metal, and crystal for equipment.","key-d"),
        new("BUILD A PLACE WORTH SAVING","Air spreads through rooms one cell at a time. A single missing hull tile lets the room breathe into the wasteland.",
            "B opens the blueprint catalog. Choose a module, then click to place it. Modules need deck and connected power. Enclose rooms with hull and an airlock. O cycles pressure, heat, and power overlays. Repair meteor breaches with pressure hull.",
            "Expand solar power and storage before adding more consumers.","key-b"),
        new("A FUTURE AT TRANQUILITY","Earth is listening. Give the people you bring home a future here, then send a signal that Tranquility is alive.",
            "Rescue at least 3 people. Reach cycle 3 with 4 living crew and 20 food. Build an Earth uplink for 60 metal and 20 crystal, then keep it powered and pressurized. Founding the colony is a milestone; you can keep building afterward.",
            "S saves. Shift+L loads. T reopens these lessons. Q saves and quits.","key-s")
    ];
    private void DrawModal(int width, int height)
    {
        Fill(new(0, 0, width, height), Color.Black * .72f);
        int w = Math.Min(780, width - 100), h = Math.Min(_modal == "build" ? 650 : 580, height - 70);
        var r = new Rectangle((width - w) / 2, (height - h) / 2, w, h);
        Fill(r, Surface);
        Outline(r, Color.White * .14f);
        Fill(new(r.X, r.Y, 3, r.Height), Accent);
        if (_modal == "tutorial")
        {
            var lesson = Lessons[_lesson];
            Text($"FIELD MANUAL / {_lesson + 1:00} OF {Lessons.Length:00}", r.X + 30, r.Y + 25, Accent, .9f);
            _sprites.Fit(_batch, "engineer", new(r.Right - 115, r.Y + 58, 55, 55), Amber);
            Text(lesson.Title, r.X + 30, r.Y + 69, Ink, 1.65f);
            float y = r.Y + 126;
            y += Wrap(lesson.Story, r.X + 30, y, w - 60, Muted, 1.05f) + 22;
            Fill(new(r.X + 30, (int)y, w - 60, 1), Color.White * .08f);
            y += 24;
            Text("YOUR NEXT STEP", r.X + 30, y, Accent, .85f);
            y += 27;
            y += Wrap(lesson.Instructions, r.X + 30, y, w - 60, Ink, 1.05f) + 18;
            Wrap(lesson.Objective, r.X + 30, Math.Max(y, r.Bottom - 174), w - 60, Amber, .95f);
            _sprites.Fit(_batch, lesson.Key, new(r.X + 30, r.Bottom - 108, 86, 27), Color.White, true);
            Text("LEARN AT YOUR OWN PACE / TIME IS HELD", r.X + 135, r.Bottom - 101, Muted, .8f);
            for (int n = 0; n < Lessons.Length; n++)
            {
                Fill(new(r.X + 30 + n * 21, r.Bottom - 133, 14, 3), n <= _lesson ? Accent : Raised);
            }

            if (Button(new(r.X + 30, r.Bottom - 65, 124, 42), "PREVIOUS", enabled: _lesson > 0, modal: true))
            {
                _lesson--;
            }

            if (Button(new(r.Center.X - 75, r.Bottom - 65, 150, 42), "SKIP FOR NOW", modal: true))
            {
                _modal = "";
            }

            if (Button(new(r.Right - 202, r.Bottom - 65, 172, 42), _lesson == Lessons.Length - 1 ? "BEGIN SHIFT" : "NEXT LESSON", selected: true, modal: true))
            {
                NextLesson();
            }
        }
        else if (_modal == "build")
        {
            Text("BUILD A FUTURE", r.X + 30, r.Y + 26, Ink, 1.7f);
            Text("SELECT A BLUEPRINT / MATERIALS ARE RESERVED ON PLACEMENT", r.X + 30, r.Y + 66, Muted, .8f);
            int cardW = (w - 72) / 2;
            for (int n = 0; n < 10; n++)
            {
                var bp = Blueprint.All[n];
                var box = new Rectangle(r.X + 30 + (n % 2) * (cardW + 12), r.Y + 104 + (n / 2) * 88, cardW, 78);
                if (Button(box, "", selected: _tool == Tool.Build && _blueprint == bp.Kind, modal: true, tooltip: bp.Description))
                {
                    ChooseBlueprint(bp.Kind);
                }

                _sprites.Fit(_batch, bp.Sprite, new(box.X + 12, box.Y + 18, 34, 34), _sim.Resources.Covers(bp.Cost) ? Accent : Muted);
                Text($"{(n + 1) % 10}  {bp.Name.ToUpperInvariant()}", box.X + 58, box.Y + 12, Ink, .9f);
                Text(Cost(bp.Cost), box.X + 58, box.Y + 37, _sim.Resources.Covers(bp.Cost) ? Muted : Danger, .8f);
                Text(bp.Power > 0 ? $"{bp.Power:0} KW REQUIRED" : bp.Kind == ModuleKind.Solar ? "UP TO 12 KW" : "PASSIVE", box.X + 58, box.Y + 56, Muted, .7f);
            }
            Text("DECK CONNECTS POWER. HULL HOLDS ATMOSPHERE.", r.X + 30, r.Bottom - 74, Muted, .85f);
            if (Button(new(r.Right - 154, r.Bottom - 55, 124, 40), "CLOSE", modal: true))
            {
                _modal = "";
            }
        }
        else
        {
            Text("OUTPOST CONTROLS", r.X + 30, r.Y + 28, Ink, 1.7f);
            Text("THE SIMULATION IS PAUSED WHILE THIS PANEL IS OPEN", r.X + 30, r.Y + 69, Muted, .8f);
            (string Key, string Action)[] controls = [("ARROWS / HJKL","Move cursor"),("SPACE / 1 2 3","Pause / speed"),("CLICK CONTACT","Dispatch rescue"),("R / C / G","Distress / crew / home"),
                ("D / B / P / X","Mine / build / survey / cancel"),("V / ENTER / ESC","Region / confirm / inspect"),("WHEEL / + - / 0","Zoom at pointer / cursor / reset"),("RIGHT DRAG","Pan map"),
                ("O / F / E","Overlays / follow / log"),("S / SHIFT+L","Save / load colony"),("T / Q","Tutorial / save and quit"),("F11 / F5 / F9","Fullscreen / save / load")];
            float y = r.Y + 115;
            foreach (var (key, action) in controls)
            {
                Text(key, r.X + 30, y, Accent, .9f);
                Text(action, r.X + 238, y, Ink, .9f);
                y += 29;
            }
            if (Button(new(r.Right - 174, r.Bottom - 62, 144, 42), "BACK TO OUTPOST", selected: true, modal: true))
            {
                _modal = "";
            }
        }
    }
}
