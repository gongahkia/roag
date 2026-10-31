using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private long _nativeChest, _nativeSeenEvent;

    private void BuildDungeonControls(int w, int h)
    {
        void Add(string id, string label, Rectangle bounds, bool enabled = true, bool selected = false, string reason = "")
            => ControlCache.Add(id, label, bounds, enabled, selected, "", reason, null, "", -1, null, false);
        Add("dungeon:save", "Save", new(w - 286, 16, 82, 34));
        Add("dungeon:boons", "Boons", new(w - 196, 16, 82, 34));
        Add("dungeon:menu", "Menu", new(w - 106, 16, 90, 34));
        var box = PanelBox(w, h);
        if (_panel.StartsWith("dungeon-", StringComparison.Ordinal))
        {
            Add("dungeon:close", "Close", new(box.Right - 100, box.Top + 12, 80, 34));
            if (_panel == "dungeon-class")
            {
                foreach (var kind in Enum.GetValues<ClassKind>())
                {
                    int i = (int)kind, width = (box.Width - 64) / 3;
                    Add("dungeon:class:" + i, ClassCatalog.Get(kind).Name,
                        new(box.X + 20 + i % 3 * (width + 12), box.Y + 92 + i / 3 * 56, width, 44),
                        _session.State.Tactics!.NextAction == 1, _session.Party.Single().Tactics!.Build!.Class == kind,
                        "The class is fixed after the first paid action.");
                }
            }
            else if (_panel == "dungeon-chest")
            {
                var actor = _session.Party.Single();
                var offers = _session.DungeonCombat.ChestOffers(_nativeChest, actor.Id);
                for (int i = 0; i < offers.Count; i++)
                {
                    var preview = _session.DungeonCombat.Preview(new(TacticalAction.ClaimChest, Target: _nativeChest, Choice: i + 1));
                    Add("dungeon:claim:" + (i + 1), "Choose " + (i + 1), new(box.Right - 166, box.Y + 100 + i * 108, 138, 38), preview.Valid, reason: preview.Reason);
                }
            }
            else if (_panel == "dungeon-menu")
            {
                string[] ids = ["classes", "guide", "motion", "volume", "quit"];
                string[] labels = ["Starting class", "Controls and help", _reducedMotion ? "Motion: Reduced" : "Motion: Full", $"Sound: {_combatVolume:P0}", "Save and quit"];
                for (int i = 0; i < ids.Length; i++) Add("dungeon:" + ids[i], labels[i], new(box.X + 28, box.Y + 80 + i * 54, 300, 40));
            }
            return;
        }
        if (_session.State.AwaitingContinuation)
        {
            Add("dungeon:new", "Begin a fresh run", new(_map.Center.X - 140, _map.Center.Y + 36, 280, 42)); return;
        }
        if (_session.State.Tactics!.NextAction == 1)
            Add("dungeon:classes", "Choose class", new(_side.X + 12, _side.Bottom - 54, _side.Width - 24, 36));
        string[] kinds = ["move", "basic_attack", "sweeping_slash", "dash", "guard", "wait"];
        string[] names = ["Move", "Basic / F", "Special / R", "Movement / X", "Guard / G", "Wait / Space"];
        int buttonWidth = (_map.Width - 16) / 3;
        for (int i = 0; i < kinds.Length; i++)
            Add("roag:" + kinds[i], names[i], new(_map.X + i % 3 * (buttonWidth + 8), h - 94 + i / 3 * 44, buttonWidth, 36), selected: _roagAim == kinds[i]);
    }

    private bool ActivateDungeonControl(string id)
    {
        if (!id.StartsWith("dungeon:", StringComparison.Ordinal)) return false;
        var parts = id.Split(':');
        switch (parts[1])
        {
            case "save": TrySave(); break;
            case "close": ClosePanel(); break;
            case "menu": _panel = "dungeon-menu"; break;
            case "guide": _panel = "dungeon-guide"; break;
            case "boons": _panel = "dungeon-boons"; break;
            case "classes": _panel = "dungeon-class"; break;
            case "class":
                var result = _session.ChooseDungeonClass((ClassKind)int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture));
                _notice = result.Message; if (result.Success) ClosePanel(); break;
            case "claim":
                CommitRoag(new("claim_chest", Target: _nativeChest, Choice: int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)));
                ClosePanel(); break;
            case "new": NewWorld(); _nativeSeenEvent = _session.State.Tactics!.NextEvent - 1; break;
            case "motion": _reducedMotion = !_reducedMotion; SaveConversationSettings(); break;
            case "volume": _combatVolume = _combatVolume >= .99f ? 0 : Math.Min(1, _combatVolume + .25f); SaveConversationSettings(); break;
            case "quit": TrySave(); if (_notice == "World saved.") Exit(); break;
        }
        return true;
    }

    private bool TryOpenDungeonChest(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var position = new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        var chest = _session.State.Tactics!.Chests.Chests.FirstOrDefault(c => !c.Claimed && c.Position == position);
        if (chest == null || _session.DungeonCombat.ChestOffers(chest.Id, _session.State.Party[0]).Count == 0) return false;
        _nativeChest = chest.Id; _panel = "dungeon-chest"; return true;
    }

    private void DungeonInteract()
    {
        var actor = _session.Party.Single();
        var chest = _session.State.Tactics!.Chests.Chests.FirstOrDefault(c => !c.Claimed && c.Position.Distance(actor.Position) == 1);
        if (chest != null) { _nativeChest = chest.Id; _panel = "dungeon-chest"; return; }
        CommitRoag(new("use_stairs"));
    }

    private void DrawDungeonHud(int w, int h)
    {
        var combat = _session.DungeonCombat;
        Text("ROAG", 20, 16, Gold, 2);
        Text($"{combat.Clock / (double)combat.Tuning.UnitsPerSecond:0.00}s / your action", 160, 22, Muted);
        Text($"Floor {_session.State.Position.Floor} / seed {_session.Seed}", 20, 57, Muted);
        Text(Short(_notice, Math.Max(20, (w - 310) / 8)), 300, 57, Ink);
        Fill(_side, Panel);
    }

    private void DrawDungeonTactics()
    {
        var combat = _session.DungeonCombat; var hero = _session.Party.Single(); var build = hero.Tactics!.Build!;
        foreach (var drop in combat.State.Loot.Drops.Where(d => d.CarriedBy == 0 && _session.Visible.Contains(d.Position)))
        {
            var tile = TileRect(drop.Position);
            _sprites.Fit(_batch, drop.Kind == GroundLootKind.Currency ? "coin" : drop.Kind == GroundLootKind.Health ? "heart" : "ration", tile, Gold);
        }
        foreach (var warning in _session.RoagTelegraphs()) foreach (var cell in warning.Cells)
        {
            var r = TileRect(cell); var color = new Color(240, 112, 78);
            FillClipped(new(r.X, r.Y, r.Width, 2), color); FillClipped(new(r.X, r.Bottom - 2, r.Width, 2), color);
            FillClipped(new(r.X, r.Y, 2, r.Height), color); FillClipped(new(r.Right - 2, r.Y, 2, r.Height), color);
        }
        int x = _side.X + 14, y = _side.Y + 14;
        Text(ClassCatalog.Get(build.Class).Name.ToUpperInvariant(), x, y, Gold); y += 26;
        Text($"HP {hero.Health}/{hero.MaxHealth} / level {build.Level}", x, y); y += 24;
        Text($"XP {build.Experience}/{CharacterBuild.NextThreshold(build.Level)}", x, y, Muted); y += 24;
        Text($"Gold {_session.Count("gold")}", x, y, Gold); y += 30;
        Text("TURN TIMELINE", x, y, Gold); y += 24;
        Text(_session.State.AwaitingContinuation ? "Run ended" : "You: ready now", x, y); y += 22;
        foreach (var entry in combat.VisibleTimeline().Take(7))
        {
            var actor = _session.State.Actors.FirstOrDefault(a => a.Id == entry.ActorId);
            string name = actor == null || !actor.Recruited && !_session.Visible.Contains(actor.Position) ? "Ground effect" : actor.Name;
            string kind = entry.Kind == ScheduledKind.Impact ? "impact" : entry.Kind == ScheduledKind.ActorReady ? "turn" : "effect";
            Text(Short($"{Math.Max(0, entry.Due - combat.Clock) / 100.0:0.00}s {name} {kind}", 34), x, y, entry.Kind == ScheduledKind.Impact ? new Color(240, 112, 78) : Muted);
            y += 20;
        }
        y += 14; Text("RECENT EVENTS", x, y, Gold); y += 22;
        foreach (var e in combat.State.Events.Where(e => _session.Visible.Contains(e.Position)
                     && e.Kind is "DamageTaken" or "Downed" or "Died" or "BoonGranted" or "CurrencyCollected" or "FloorChanged" or "GuardRaised").TakeLast(6))
        {
            Text(Short(e.Kind + (e.Amount > 0 ? " " + e.Amount : ""), 34), x, y, Muted); y += 20;
        }
        if (_session.State.AwaitingContinuation)
        {
            Fill(new(_map.Center.X - 210, _map.Center.Y - 56, 420, 130), Panel);
            Text("YOU HAVE FALLEN", _map.Center.X - 170, _map.Center.Y - 30, new Color(240, 112, 78), 2);
            return;
        }
        if (_panel.Length > 0 || _roagAim == "move") return;
        var mouse = Mouse.GetState(); if (!_map.Contains(mouse.Position)) return;
        var point = _camera.ScreenToWorld(new(mouse.X, mouse.Y));
        int dx = Math.Sign((int)Math.Floor(point.X) - hero.Position.X), dy = Math.Sign((int)Math.Floor(point.Y) - hero.Position.Y);
        var previewCells = _roagAim == "guard" && AttackGeometry.TryFacing(dx, dy, out var guardFacing)
            ? AttackGeometry.Arc(hero.Position, guardFacing) : _session.RoagPreview(new(_roagAim, dx, dy));
        foreach (var cell in previewCells)
            if (_session.Visible.Contains(cell)) FillClipped(TileRect(cell), new Color(113, 187, 195) * .25f);
    }

    private void DrawDungeonPanel(int w, int h)
    {
        var box = PanelBox(w, h); Fill(box, Panel); int x = box.X + 24, y = box.Y + 20;
        string title = _panel switch { "dungeon-class" => "CHOOSE A FIXED CLASS", "dungeon-chest" => "CHOOSE A BOON", "dungeon-boons" => "YOUR BOONS", "dungeon-guide" => "CONTROLS", _ => "MENU" };
        Text(title, x, y, Gold, 1.3f); y += 48;
        if (_panel == "dungeon-chest")
        {
            var chest = _session.State.Tactics!.Chests.Chests.Single(c => c.Id == _nativeChest);
            var definition = BoonChestDefinition.Get(chest.Definition);
            Text($"{definition.Name} / {definition.Price} gold / purse {_session.Count("gold")}", x, y, Gold); y += 34;
            var offers = _session.DungeonCombat.ChestOffers(chest.Id, _session.State.Party[0]);
            for (int i = 0; i < offers.Count; i++)
            {
                var offer = offers[i]; var boon = BoonCatalog.Default.Get(offer.Boon);
                Text($"{i + 1}. {boon.Name} / {offer.Rarity}", x, y + i * 108, Gold);
                Wrap(boon.Description, x, y + 26 + i * 108, box.Width - 220, Muted);
            }
        }
        else if (_panel == "dungeon-guide")
            Wrap("WASD / arrows: one tile per press. F: aim basic attack. R: aim special. X: movement ability. G: directional Guard. Choose a direction with the keyboard, or click a target for attacks. Q/E/Z/C give diagonal aim. Space or period waits. Enter opens an adjacent boon chest or uses the stair beneath you. Escape cancels aim. P toggles Pilgrim's Vow. F5 saves. Menus and inspection consume no game time. Close and relaunch to resume the continuing run.", x, y, box.Width - 48);
        else if (_panel == "dungeon-boons")
        {
            var owned = _session.Party.Single().Tactics!.Boons.Inventory.Stacks;
            if (owned.Count == 0) Text("Find boon chests to develop your build.", x, y, Muted);
            foreach (var pair in owned.OrderBy(p => p.Key).Take(12))
            {
                var boon = BoonCatalog.Default.Get(pair.Key);
                Text(boon.Name + " / " + BoonCatalog.Rarities.Sum(r => pair.Value[r]) + " stacks", x, y, Gold); y += 24;
                y += Wrap(boon.Description, x, y, box.Width - 48, Muted) + 10;
                if (y > box.Bottom - 70) break;
            }
        }
        else if (_panel == "dungeon-class")
            Text("Choose before taking your first action. Each class has its own attacks and movement.", x, y, Muted);
    }

    private void CollectDungeonFeedback()
    {
        if (_session.State.Tactics is not { } state) return;
        var events = state.Events.Where(e => e.Sequence > _nativeSeenEvent && _session.Visible.Contains(e.Position)).ToArray();
        _nativeSeenEvent = state.NextEvent - 1;
        if (events.Any(e => e.Kind == "DamageTaken") && _combatVolume > 0)
        {
            _combatAudio ??= new(); _combatAudio.Play("slash", _combatVolume);
        }
    }
}
