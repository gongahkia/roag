using Lantern.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Regolith.Graphics;

namespace Lantern.Client;

public sealed partial class LanternGame
{
    private bool Button(Rectangle r, string label, string? icon = null, bool selected = false, bool enabled = true, bool modal = false, string? tip = null)
    {
        bool hot = r.Contains(_mouse.Position) && (!_intro && _dialogTitle.Length == 0 || modal);
        var surface = r;
        if (hot && _mouse.LeftButton == ButtonState.Pressed)
        {
            surface.Inflate(-(int)Math.Max(1, r.Width * .02), -1);
        }
        Fill(surface, selected ? new(64, 57, 43) : hot ? new(51, 57, 60) : Raised);
        Outline(surface, selected ? Gold * .8f : Color.White * (hot ? .22f : .08f));
        Color color = !enabled ? Muted * .4f : selected ? Gold : Ink;
        if (icon is not null)
        {
            _sprites.Fit(_batch, icon, new(r.X + 10, r.Center.Y - 10, 20, 20), color);
        }

        if (label.Length > 0)
        {
            Text(label, icon is null ? r.Center.X - SpriteLibrary.Measure(label) / 2 : r.X + 40, r.Center.Y - 6, color);
        }

        if (hot && tip is not null)
        {
            _hover = tip;
        }

        if (hot && enabled && _click)
        {
            _click = false;
            return true;
        }
        return false;
    }
    private void Meter(Rectangle r, float value, Color color)
    {
        Fill(r, Color.Black * .35f);
        Fill(new(r.X, r.Y, (int)(r.Width * Math.Clamp(value, 0, 1)), r.Height), color);
    }
    private void DrawInterface(int width, int height)
    {
        Tile("lantern", new(20, 18, 38, 38), Gold);
        Text("LANTERN BELOW", 70, 20, Ink, 2);
        Text("AN URIZEN EXPEDITION", 72, 51, Muted);
        int sx = Math.Max(390, width - 750);
        foreach (var (icon, value, label) in new[] { ("coin", _campaign.Gold.ToString(), "GOLD"), ("potion", _campaign.Potions.ToString(), "POTIONS"), ("lantern", _campaign.Light.ToString(), "LIGHT"), ("ration", _campaign.Rations.ToString(), "RATIONS") })
        {
            Tile(icon, new(sx, 19, 24, 24), icon == "potion" ? Red : Gold);
            Text(label, sx + 34, 17, Muted);
            Text(value, sx + 34, 37, Ink);
            sx += 122;
        }
        if (Button(new(width - 154, 17, 42, 42), "", "save", tip: "Save [F5 / Ctrl+S]"))
        {
            Save();
        }

        if (Button(new(width - 104, 17, 42, 42), "", "atlas", tip: "Open the complete sprite atlas [F2]"))
        {
            _atlas = true;
            _route.Clear();
        }
        if (Button(new(width - 54, 17, 40, 42), "?", tip: "Field guide [F1]"))
        {
            _intro = true;
            _lesson = 0;
        }
        Outline(_map, Color.White * .12f);
        Fill(new(_map.X + 10, _map.Y + 10, Math.Min(_map.Width - 20, 420), 44), Background * .92f);
        Text(_campaign.Zone.Name, _map.X + 20, _map.Y + 18, Gold);
        Text(_campaign.Zone.Subtitle, _map.X + 20, _map.Y + 36, Muted);
        if (_campaign.Battle is { } battle)
        {
            int tx = _map.X + 12, ty = _map.Y + 66;
            for (int n = battle.TurnIndex; n < Math.Min(battle.Order.Count, battle.TurnIndex + 8); n++)
            {
                var u = battle.Units[battle.Order[n]];
                if (!u.Alive)
                {
                    continue;
                }

                Fill(new(tx, ty, 40, 40), Background * .92f);
                Tile(u.Sprite, new(tx + 6, ty + 5, 28, 28), u.Friendly ? HeroColors[u.HeroId] : Red);
                if (n == battle.TurnIndex)
                {
                    Outline(new(tx, ty, 40, 40), Gold);
                }

                tx += 46;
            }
        }
        string hint = _campaign.Battle is null ? "WASD / ARROWS   E INTERACT   . WAIT   G FOLLOW" : "1-6 ACTION   CLICK TARGET   ENTER CONFIRM";
        Fill(new(_map.X + 10, _map.Bottom - 30, Math.Min(_map.Width - 20, hint.Length * 8 + 20), 23), Background * .9f);
        Text(hint, _map.X + 20, _map.Bottom - 25, Muted);
        if (_campaign.ZoneId != "hearth" && _campaign.Battle is null)
        {
            int hx = _map.Right - 215, hy = _map.Y + 10;
            Fill(new(hx, hy, 205, 44), Background * .92f);
            Text("OFF-ROAD / T CLEARS", hx + 10, hy + 6, Muted);
            foreach (var (icon, label, color) in new[] { ("hazard-fire", "FIRE", Red), ("hazard-spores", "SPORE", Green), ("hazard-corruption", "BLIGHT", Purple) })
            {
                Tile(icon, new(hx + 8, hy + 23, 14, 14), color);
                Text(label, hx + 26, hy + 25, color, .65f);
                hx += 65;
            }
        }
        DrawParty(height);
        DrawActions(height);
        DrawSidebar();
        if (_time < _toastUntil)
        {
            Text(Short(_toast, Math.Max(20, (width - 390) / 8)), 20, height - 18, Gold);
        }
        else
        {
            Text("F2 ATLAS  /  F4 ORIGINAL COLORS  /  F5 SAVE  /  F9 LOAD", 20, height - 18, Muted);
        }

        if (_hover.Length > 0 && _map.Contains(_mouse.Position) && !_intro && _dialogTitle.Length == 0)
        {
            int w = Math.Min(420, _map.Width - 30), x = Math.Clamp(_mouse.X + 18, _map.X + 8, _map.Right - w - 8), y = Math.Min(_mouse.Y + 24, _map.Bottom - 110);
            Fill(new(x, y, w, 88), Panel);
            Outline(new(x, y, w, 88), Color.White * .18f);
            Wrap(Short(_hover, 160), x + 12, y + 12, w - 24, Ink);
        }
    }
    private void DrawParty(int height)
    {
        int card = (_map.Width - 24) / 4;
        int y = height - 204;
        foreach (var h in _campaign.Party)
        {
            var unit = _campaign.Battle?.Units.First(u => u.HeroId == h.Id);
            int health = unit?.Health ?? h.Health, mana = unit?.Mana ?? h.Mana, stress = unit?.Stress ?? h.Stress;
            var r = new Rectangle(_map.X + h.Id * (card + 8), y, card, 96);
            bool active = _campaign.Battle?.Active.HeroId == h.Id;
            bool chosen = _selectedHero == h.Id;
            if (Button(r, "", selected: active || chosen && _campaign.Battle is null, tip: h.Name + " / " + h.RoleName + " / " + (unit?.Conditions ?? "Click to select. H uses a potion outside combat.")))
            {
                _selectedHero = h.Id;
                if (_campaign.Battle is { } b && b.Active.Friendly && Skills.Get(_campaign.Party[b.Active.HeroId].Role, _skill).Target == TargetKind.Ally)
                {
                    _target = h.Id;
                    DoSkill(h.Id);
                }
            }
            Tile(health > 0 ? h.Sprite : "bones", new(r.X + 12, r.Y + 10, 30, 30), health > 0 ? HeroColors[h.Id] : Muted * .5f);
            Text(h.Name.ToUpperInvariant() + " L" + h.Level, r.X + 52, r.Y + 9, active ? Gold : Ink);
            Text(h.RoleName.ToUpperInvariant(), r.X + 52, r.Y + 28, Muted);
            int bar = r.Width - 20;
            Meter(new(r.X + 10, r.Y + 53, bar, 5), health / (float)h.MaxHealth, HeroColors[h.Id]);
            Meter(new(r.X + 10, r.Y + 64, bar, 4), mana / (float)h.MaxMana, Blue);
            Meter(new(r.X + 10, r.Y + 74, bar, 3), stress / 100f, stress >= 80 ? Red : Purple * .7f);
            Text($"{health}/{h.MaxHealth}", r.X + 10, r.Y + 82, health > 0 ? Ink : Red);
            string mp = $"{mana} MP";
            Text(mp, r.Right - 10 - SpriteLibrary.Measure(mp), r.Y + 82, Blue);
        }
    }
    private void DrawActions(int height)
    {
        int y = height - 98;
        if (_campaign.Battle is { } battle)
        {
            int w = (_map.Width - 30) / 6;
            for (int i = 0; i < 6; i++)
            {
                var active = battle.Active;
                var h = _campaign.Party[active.Friendly ? active.HeroId : 0];
                var skill = Skills.Get(h.Role, i);
                var r = new Rectangle(_map.X + i * (w + 6), y, w, 65);
                bool enabled = active.Friendly && active.Mana >= skill.Mana && (i != 4 || _campaign.Potions > 0);
                if (Button(r, "", selected: _skill == i && active.Friendly, enabled: enabled, tip: skill.Description))
                {
                    ChooseSkill(i);
                }

                Tile(skill.Icon, new(r.X + 9, r.Y + 7, 22, 22), enabled ? Gold : Muted * .4f);
                Text((i + 1).ToString(), r.Right - 17, r.Y + 10, Muted);
                Text(Short(skill.Name.ToUpperInvariant(), Math.Max(5, (w - 12) / 8)), r.X + 8, r.Y + 37, enabled ? Ink : Muted * .4f);
                if (skill.Mana > 0)
                {
                    Text(skill.Mana + "MP", r.X + 38, r.Y + 12, Blue);
                }
            }
        }
        else
        {
            (string Key, string Name, string Icon, Action Action)[] actions = [("E", "INTERACT", "key", () => Apply(_campaign.Interact())), ("T", "TORCH", "lantern", () => Apply(_campaign.UseTorch())), ("R", "CAMP", "campfire", () => Apply(_campaign.Camp())), ("I", "PACK", "potion", () => { _dialogTitle = "THE SHARED PACK"; _dialogText = $"Potions: {_campaign.Potions}. Select a companion, then H to heal 24 HP.\n\nTorches: {_campaign.Torches}. T restores 45 light.\n\nRations: {_campaign.Rations}. Camps cost 2.\n\nGold: {_campaign.Gold}. Inn 12g; expedition kit 10g.\n\n" + _campaign.Objective; }), ("F2", "ATLAS", "atlas", () => { _atlas = true; _route.Clear(); })];
            int w = (_map.Width - 24) / 5;
            for (int i = 0; i < actions.Length; i++)
            {
                var a = actions[i];
                var r = new Rectangle(_map.X + i * (w + 6), y, w, 65);
                if (Button(r, "", tip: a.Name + " [" + a.Key + "]"))
                {
                    a.Action();
                }

                Tile(a.Icon, new(r.X + 10, r.Y + 8, 22, 22), Gold);
                Text(a.Key, r.Right - 8 - SpriteLibrary.Measure(a.Key), r.Y + 12, Muted);
                Text(a.Name, r.X + 10, r.Y + 39, Ink);
            }
        }
    }
    private void DrawSidebar()
    {
        Fill(_side, Panel);
        int x = _side.X + 16, y = _side.Y + 18, w = _side.Width - 32;
        if (_campaign.Battle is { } b)
        {
            Text("ENCOUNTER / ROUND " + b.Round.ToString("00"), x, y, Muted);
            y += 27;
            y += (int)Wrap(b.Name.ToUpperInvariant(), x, y, w, Ink, 1.2f) + 16;
            Text(b.Active.Friendly ? b.Active.Name.ToUpperInvariant() + " IS READY" : "ENEMY TURN / " + b.Active.Name.ToUpperInvariant(), x, y, b.Active.Friendly ? Gold : Red);
            y += 26;
            if (b.Active.Friendly)
            {
                var skill = Skills.Get(_campaign.Party[b.Active.HeroId].Role, _skill);
                y += (int)Wrap(skill.Description, x, y, w, Ink) + 12;
                string target = skill.Target switch
                {
                    TargetKind.Enemy => "CLICK AN ENEMY TO CONFIRM",
                    TargetKind.Ally => "CLICK A COMPANION TO CONFIRM",
                    _ => "SELECT THE ACTION TO EXECUTE"
                };
                Text(target, x, y, Gold);
                y += 32;
            }
            else
            {
                y += (int)Wrap("Watch their intent. Your next companion will be ready shortly.", x, y, w, Muted) + 14;
            }
            foreach (var enemy in b.Units.Where(u => !u.Friendly))
            {
                if (y > _side.Bottom - 168)
                {
                    break;
                }

                var r = new Rectangle(x, y, w, 92);
                if (Button(r, "", selected: enemy.Id == _target, enabled: enemy.Alive, tip: enemy.Conditions))
                {
                    _target = enemy.Id;
                    DoSkill(enemy.Id);
                }
                Tile(enemy.Sprite, new(x + 8, y + 10, 26, 26), enemy.Alive ? Red : Muted * .35f);
                Text(Short(enemy.Name.ToUpperInvariant(), 26), x + 43, y + 8, enemy.Alive ? Ink : Muted);
                Text($"{enemy.Health}/{enemy.MaxHealth}", x + 43, y + 26, Muted);
                Meter(new(x + 10, y + 47, w - 20, 4), enemy.Health / (float)enemy.MaxHealth, Red);
                Text(Short(enemy.Alive ? _campaign.IntentText(enemy) : "DEFEATED", 31), x + 10, y + 62, enemy.Alive ? Gold : Muted);
                y += 102;
            }
        }
        else
        {
            Text(_campaign.Victory ? "EXPEDITION COMPLETE" : "THE EMBER BENEATH", x, y, Gold);
            y += 30;
            y += (int)Wrap(_campaign.Objective, x, y, w, Ink, 1.05f) + 22;
            foreach (var (done, label) in new[] { (_campaign.HasKey || _campaign.Completed.Contains("iron-gate"), "Find the iron key"), (_campaign.Completed.Contains("abbey-abbot"), "Silence the Hollow Abbot"), (_campaign.HasRelic, "Recover the ember relic"), (_campaign.Victory, "Bring the light home") })
            {
                Text(done ? "[+]" : "[ ]", x, y, done ? Green : Muted);
                Text(label, x + 35, y, done ? Green : Muted);
                y += 25;
            }
            y += 18;
            Text("THE LANTERN", x, y, Muted);
            y += 24;
            Meter(new(x, y, w, 7), _campaign.Light / 100f, _campaign.Light < 25 ? Red : Gold);
            y += 20;
            y += (int)Wrap(_campaign.Light < 25 ? "Darkness adds stress. T burns a torch for 45 light." : "A bright flame reaches farther into the abbey. Torches restore 45 light.", x, y, w, Muted) + 18;
            var nearby = _campaign.Zone.Objects.Where(_campaign.Present).Where(o => o.Kind != ObjectKind.Scenery && o.Position.Distance(_campaign.Position) <= 2).OrderBy(o => o.Position.Distance(_campaign.Position)).FirstOrDefault();
            if (nearby is not null && y < _side.Bottom - 200)
            {
                Fill(new(x, y, w, 1), Color.White * .1f);
                y += 18;
                Text("NEARBY / E TO INTERACT", x, y, Gold);
                y += 24;
                y += (int)Wrap(nearby.Name, x, y, w, Ink) + 10;
                y += (int)Wrap(nearby.Detail.Length > 0 ? nearby.Detail : "Click to walk closer and interact.", x, y, w, Muted) + 12;
            }
        }
        int bottom = Math.Max(y + 14, _side.Bottom - 153);
        if (bottom < _side.Bottom - 70)
        {
            Text("FIELD NOTES", x, bottom, Muted);
            bottom += 24;
            foreach (var entry in _campaign.Journal.TakeLast(3).Reverse())
            {
                if (bottom > _side.Bottom - 45)
                {
                    break;
                }

                int lines = (_side.Bottom - bottom - 12) / 19;
                if (lines < 1)
                    break;
                bottom += (int)Wrap(Short(entry.Text, Math.Min(110, lines * (w / 8 - 5))), x, bottom, w, Muted) + 12;
            }
        }
    }
    private void DrawDialog(int width, int height)
    {
        Fill(new(0, 0, width, height), Color.Black * .75f);
        int w = Math.Min(760, width - 100), h = Math.Min(590, height - 90);
        var r = new Rectangle((width - w) / 2, (height - h) / 2, w, h);
        Fill(r, Panel);
        Outline(r, Color.White * .15f);
        Fill(new(r.X, r.Y, 3, r.Height), Gold);
        string title = _dialogTitle, body = _dialogText;
        if (_intro)
        {
            string[] titles = ["FOUR COMPANIONS. ONE LANTERN.", "READ THE ENEMY. CHOOSE YOUR MOMENT.", "BRING THE LIGHT HOME.", "THE FOREST AND THE BLIGHT."];
            string[] pages = ["You are a party of four: Rowan the warden, Vesper the ranger, Iris the arcanist, and Sable the vestal.\n\nMove with WASD or arrows, or click a visible destination. E interacts. Leave Hearth by the eastern road. Search Ashwood's abandoned chest for the iron key.\n\nThe inn restores your party for 12 gold. The provisioner sells expedition kits for 10. The party shares potions, torches, and rations.", "Enemies are visible on the map. Step onto one to begin a turn-based encounter, with everyone still shown from above.\n\n1-3 select a class skill, 4 guards, 5 uses a potion, and 6 retreats. Click an enemy or companion to confirm a targeted skill; Enter confirms the selected target. Enemy intentions appear in the right panel.\n\nMarks add damage. Fire and bleeding persist across turns. Guard halves incoming damage. Stress above 80 weakens your attacks.", "Find the iron key, descend into the Sunken Abbey, defeat the Hollow Abbot, and carry the ember relic back to Warden Elowen.\n\nT relights the lantern. R camps at a marked fire for 2 rations. I shows supplies; J opens the journal. F5 saves and F9 loads, including during combat. Defeat returns you to town with a gold penalty.\n\nF2 opens every sprite in the three packs, with identity evidence and future-use notes. F4 toggles original artwork colors. Scroll zooms; right-drag pans; G follows the party.", "Ashwood grows differently with each expedition seed: forests and ponds surround a winding road. Abbey rooms assemble into connected cloisters, cisterns and collapsed vaults. Hearth remains your familiar home.\n\nFollow roads and the clear crosses through Abbey rooms for safe passage. Fire, spores and violet blight spread outside those routes every four successful steps or waits. Blocked moves, menus, resting and battle do not advance them; distant areas pause.\n\nFire and spores cost conscious companions 1 health and 1 stress per step. Blight costs 3 stress. Hazards cannot take the last point of health. T spends a torch to clear nearby hazards and their sources, even at full light. Period or Space waits one turn.\n\nThe same seed reproduces a new map. Saving preserves your actual terrain, discoveries and evolving hazards, including during battle."];
            title = titles[_lesson];
            body = pages[_lesson];
            Text($"FIELD GUIDE / {_lesson + 1:00} OF {titles.Length:00}", r.X + 28, r.Y + 24, Gold);
        }
        else
        {
            Text("A TALE FROM THE ROAD", r.X + 28, r.Y + 24, Gold);
        }

        Wrap(title, r.X + 28, r.Y + 66, w - 56, Ink, 1.5f);
        string dialogKey = title + body;
        if (dialogKey != _lastDialogKey)
        {
            _dialogScroll = 0;
            _lastDialogKey = dialogKey;
        }
        var clip = new Rectangle(r.X + 28, r.Y + 123, w - 56, r.Height - 214);
        _batch.End();
        GraphicsDevice.ScissorRectangle = clip;
        _batch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp, null, _scissor);
        float bodyHeight = Wrap(body, clip.X, clip.Y - _dialogScroll, clip.Width, Muted);
        int maxScroll = Math.Max(0, (int)bodyHeight - clip.Height);
        _dialogScroll = Math.Min(_dialogScroll, maxScroll);
        _batch.End();
        GraphicsDevice.ScissorRectangle = new(0, 0, width, height);
        _batch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp);
        if (maxScroll > 0)
            Text("SCROLL OR PAGE DOWN TO READ MORE", r.X + 28, r.Bottom - 88, Muted);
        if (_intro)
        {
            if (Button(new(r.X + 28, r.Bottom - 69, 125, 44), "PREVIOUS", enabled: _lesson > 0, modal: true))
            {
                _lesson--;
            }

            if (Button(new(r.Center.X - 75, r.Bottom - 69, 150, 44), "SKIP GUIDE", modal: true))
            {
                _intro = false;
            }

            if (Button(new(r.Right - 193, r.Bottom - 69, 165, 44), _lesson == GuidePageCount - 1 ? "BEGIN" : "CONTINUE", selected: true, modal: true))
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
        }
        else if (Button(new(r.Right - 193, r.Bottom - 69, 165, 44), "CONTINUE", selected: true, modal: true))
        {
            _dialogTitle = "";
        }
    }
}
