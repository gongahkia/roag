using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private long _nativeChest, _nativeSeenEvent;
    private int _nativeBoonPage;

    private void BuildDungeonControls(int w, int h)
    {
        void Add(string id, string label, Rectangle bounds, bool enabled = true, bool selected = false, string reason = "", string tip = "")
            => ControlCache.Add(id, label, bounds, enabled, selected, tip, reason, null, "", -1, null, false);
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
                    var definition = ClassCatalog.Get(kind);
                    Add("dungeon:class:" + i, definition.Name,
                        new(box.X + 20 + i % 3 * (width + 12), box.Y + 92 + i / 3 * 56, width, 44),
                        _session.State.Tactics!.NextAction == 1, _session.Party.Single().Tactics!.Build!.Class == kind,
                        "The class is fixed after the first paid action.",
                        $"HP {definition.Health}, damage {definition.Damage}, defense {definition.Defense}, speed {definition.Speed}. {definition.BasicName}; {definition.SpecialName}; {definition.DashName}.");
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
            else if (_panel == "dungeon-routes")
            {
                var routes = _session.AvailableDungeonRoutes(_session.State.Party[0]);
                for (int i = 0; i < routes.Count; i++) Add("dungeon:route:" + (i + 1), routes[i].Name,
                    new(box.X + 28, box.Y + 92 + i * 100, box.Width - 56, 42));
            }
            else if (_panel == "dungeon-boons")
            {
                int count = _session.Party.Single().Tactics!.Boons.Inventory.Stacks.Count;
                _nativeBoonPage = Math.Clamp(_nativeBoonPage, 0, Math.Max(0, (count - 1) / 3));
                Add("dungeon:boon-prev", "Previous", new(box.X + 24, box.Bottom - 54, 130, 34), _nativeBoonPage > 0);
                Add("dungeon:boon-next", "Next", new(box.Right - 154, box.Bottom - 54, 130, 34), (_nativeBoonPage + 1) * 3 < count);
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
        var activeClass = ClassCatalog.Get(_session.Party.Single().Tactics!.Build!.Class);
        string[] names = ["Move", activeClass.BasicName + " / F", activeClass.SpecialName + " / R", activeClass.DashName + " / X", "Guard / G", "Wait / Space"];
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
            case "boon-prev": _nativeBoonPage--; break;
            case "boon-next": _nativeBoonPage++; break;
            case "classes": _panel = "dungeon-class"; break;
            case "class":
                var result = _session.ChooseDungeonClass((ClassKind)int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture));
                _notice = result.Message; if (result.Success) ClosePanel(); break;
            case "route":
                CommitRoag(new("dungeon_route", Choice: int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)));
                ClosePanel(); break;
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
        if (_session.AvailableDungeonRoutes(actor.Id).Count > 0) { _panel = "dungeon-routes"; return; }
        var chest = _session.State.Tactics!.Chests.Chests.FirstOrDefault(c => !c.Claimed && c.Position.Distance(actor.Position) == 1);
        if (chest != null) { _nativeChest = chest.Id; _panel = "dungeon-chest"; return; }
        CommitRoag(new("use_stairs"));
    }

    private void DrawDungeonHud(int w, int h)
    {
        var combat = _session.DungeonCombat;
        Text("ROAG", 20, 16, Gold, 2);
        Text($"{combat.Clock / (double)combat.Tuning.UnitsPerSecond:0.00}s / your action", 160, 22, Muted);
        Text($"Stage {_session.State.Dungeon!.Stage} / floor {_session.State.Position.Floor} / seed {_session.Seed}", 20, 57, Muted);
        Text(Short(_notice, Math.Max(20, (w - 310) / 8)), 300, 57, Ink);
        Fill(_side, Panel);
    }

    private void DrawDungeonTactics()
    {
        var combat = _session.DungeonCombat; var hero = _session.Party.Single(); var build = hero.Tactics!.Build!;
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
        if (_session.State.Dungeon!.Victory) { Text("REGENT DEFEATED", x, y, Gold); y += 24; }
        Text("Aim: " + _roagAim.Replace('_', ' '), x, y, Muted); y += 24;
        var resources = hero.Tactics.Resources;
        string resource = build.Class switch { ClassKind.Stormcaller => $"Charge {resources.StormCharge}/3 / ward {(resources.StormWard ? "ready" : "spent")}",
            ClassKind.Duelist => resources.Riposte ? "Riposte ready" : resources.Parry != null ? "Parry raised" : "Parry available",
            ClassKind.Tinkerer when resources.Sentry is { } sentry => $"Sentry: {sentry.Charges} shots / {sentry.Phase}", _ => "" };
        if (resource.Length > 0) { Text(resource, x, y, Gold); y += 24; }
        Text("TURN TIMELINE", x, y, Gold); y += 24;
        Text(_session.State.AwaitingContinuation ? "Run ended" : "You: ready now", x, y); y += 22;
        foreach (var entry in combat.VisibleTimeline().Take(5))
        {
            var actor = _session.State.Actors.FirstOrDefault(a => a.Id == entry.ActorId);
            string name = actor == null || !actor.Recruited && !_session.Visible.Contains(actor.Position) ? "Ground effect" : actor.Name;
            string kind = entry.Kind == ScheduledKind.Impact ? "impact" : entry.Kind == ScheduledKind.ActorReady ? "turn" : "effect";
            Text(Short($"{Math.Max(0, entry.Due - combat.Clock) / 100.0:0.00}s {name} {kind}", 34), x, y, entry.Kind == ScheduledKind.Impact ? new Color(240, 112, 78) : Muted);
            y += 20;
        }
        y += 14; Text("RECENT EVENTS", x, y, Gold); y += 22;
        foreach (var e in combat.State.Events.Where(e => _session.Visible.Contains(e.Position)
                     && e.Kind is "DamageTaken" or "Downed" or "Died" or "BoonGranted" or "CurrencyCollected" or "FloorChanged" or "GuardRaised").TakeLast(4))
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
        var preview = _session.PreviewDungeonAim(new(_roagAim, dx, dy, Vow: _roagVow));
        var previewCells = _roagAim == "guard" && AttackGeometry.TryFacing(dx, dy, out var guardFacing)
            ? AttackGeometry.Arc(hero.Position, guardFacing) : preview.Cells;
        foreach (var cell in previewCells)
            if (_session.Visible.Contains(cell)) FillClipped(TileRect(cell), new Color(113, 187, 195) * .25f);
        Text(preview.Valid ? $"Recovery {preview.Recovery / 100.0:0.00}s" + (preview.Damage is { } damage ? $" / base damage {damage}" : "") : Short(preview.Reason, Math.Max(30, _map.Width / 8)),
            _map.X + 4, _map.Bottom - 24, preview.Valid ? Gold : new Color(240, 112, 78));
    }

    private void DrawDungeonPanel(int w, int h)
    {
        var box = PanelBox(w, h); Fill(box, Panel); int x = box.X + 24, y = box.Y + 20;
        string title = _panel switch { "dungeon-class" => "CHOOSE A FIXED CLASS", "dungeon-chest" => "CHOOSE A BOON", "dungeon-boons" => "YOUR BOONS", "dungeon-guide" => "CONTROLS", "dungeon-routes" => "OPEN A PHYSICAL ROUTE", _ => "MENU" };
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
        else if (_panel == "dungeon-routes")
        {
            var routes = _session.AvailableDungeonRoutes(_session.State.Party[0]);
            Text("Opening a route spends one action. Then walk down its stairs; cleared floors remain.", x, y, Muted);
            for (int i = 0; i < routes.Count; i++) Wrap(routes[i].Description, x + 6, box.Y + 142 + i * 100, box.Width - 60, Muted);
        }
        else if (_panel == "dungeon-guide")
            Wrap("WASD / arrows: one tile per press. F: aim basic attack. R: aim special. X: movement ability. G: directional Guard. Choose a direction with the keyboard, or click a target for attacks. Q/E/Z/C give diagonal aim. Space or period waits. Enter opens an adjacent boon chest or uses the stair beneath you. Escape cancels aim. P toggles Pilgrim's Vow. F5 saves. Menus and inspection consume no game time. Close and relaunch to resume the continuing run.", x, y, box.Width - 48);
        else if (_panel == "dungeon-boons")
        {
            var owned = _session.Party.Single().Tactics!.Boons.Inventory.Stacks;
            if (owned.Count == 0) Text("Find boon chests to develop your build.", x, y, Muted);
            foreach (var pair in owned.OrderBy(p => p.Key).Skip(_nativeBoonPage * 3).Take(3))
            {
                var boon = BoonCatalog.Default.Get(pair.Key);
                Text(boon.Name + " / " + BoonCatalog.Rarities.Sum(r => pair.Value[r]) + " stacks", x, y, Gold); y += 24;
                Text(string.Join(" / ", BoonCatalog.Rarities.Where(r => pair.Value[r] > 0).Select(r => r + " " + pair.Value[r])), x, y, Muted); y += 22;
                y += Wrap(boon.Description, x, y, box.Width - 48, Muted) + 10;
            }
            Text($"Page {_nativeBoonPage + 1}/{Math.Max(1, (owned.Count + 2) / 3)}", box.Center.X - 50, box.Bottom - 42, Muted);
        }
        else if (_panel == "dungeon-class")
            Text("Choose before taking your first action. Each class has its own attacks and movement.", x, y, Muted);
    }

    private void DrawDungeonObjects()
    {
        var state = _session.State.Tactics!;
        void Icon(WorldPosition p, string sprite, Color tint)
        { if (_session.Visible.Contains(p) && _map.Intersects(TileRect(p))) _sprites.Fit(_batch, sprite, TileRect(p), tint); }
        foreach (var chest in state.Chests.Chests)
            if (_session.Chunks.Seen(chest.Position) && chest.Position.Floor == _session.State.Position.Floor)
                _sprites.Fit(_batch, chest.Claimed ? "chest-open" : "chest", TileRect(chest.Position), Gold * (_session.Visible.Contains(chest.Position) ? 1 : .35f));
        foreach (var drop in state.Loot.Drops.Where(d => d.CarriedBy == 0))
            Icon(drop.Position, drop.Kind == GroundLootKind.Currency ? "coin" : drop.Kind == GroundLootKind.Health ? "heart" : "ration", Gold);
        var hostile = new Color(240, 112, 78); var friendly = new Color(113, 207, 218);
        foreach (var web in state.Webs) foreach (var p in web.Span.Cells()) Icon(p, "web", hostile);
        foreach (var trail in state.Silt) Icon(trail.Position, "water", new(122, 162, 96));
        foreach (var ember in state.Embers) Icon(ember.Position, "hazard-fire", hostile);
        foreach (var mirror in state.Mirrors) Icon(mirror.Position, "crystal", new(210, 153, 224));
        foreach (var snare in state.EnemySnares) Icon(snare.Position, "trap", hostile);
        foreach (var actor in _session.State.Actors)
        {
            if (actor.Tactics is not { } tactics) continue;
            var r = tactics.Resources;
            if (r.Decoy is { } decoy) Icon(decoy, "resident", friendly * .6f);
            if (r.Snare is { } snare) Icon(snare, "trap", friendly);
            if (r.Spirit is { } spirit) Icon(spirit, "wisp", friendly);
            if (r.Beacon is { } beacon) Icon(beacon.Position, "banner", friendly);
            if (r.Bulwark is { } bulwark) Icon(bulwark.Position, "boulder", friendly);
            if (r.Sentry is { } sentry) Icon(sentry.Position, "bow", sentry.Phase == SentryPhase.Spent ? Muted : friendly);
        }
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
