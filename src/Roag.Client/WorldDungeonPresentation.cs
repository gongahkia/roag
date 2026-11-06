using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private long _nativeChest, _nativeSeenEvent;
    private int _nativeBoonPage;
    private int _nativeUtilitySteps = 3;
    private long _nativeService;
    private int _nativeServicePage, _nativeReplacementChoice;

    private void BuildDungeonControls(int w, int h)
    {
        void Add(string id, string label, Rectangle bounds, bool enabled = true, bool selected = false, string reason = "", string tip = "", string? sprite = null, string detail = "")
            => ControlCache.Add(id, label, bounds, enabled, selected, tip, reason, sprite, detail, -1, null, false);
        if (_titleBackdrop == null)
        {
            Add("dungeon:save", "Save", new(w - 286, 16, 82, 34));
            Add("dungeon:boons", "Boons", new(w - 196, 16, 82, 34));
            Add("dungeon:menu", "Menu", new(w - 106, 16, 90, 34));
            Add("dungeon:map", "Map / Tab", new(w - 520, 16, 108, 34), selected: _nativeMapVisible);
            Add("dungeon:utility", "Utility / U", new(w - 404, 16, 110, 34));
            Add("dungeon:party", "Party / B", new(w - 632, 16, 104, 34));
        }
        var box = PanelBox(w, h);
        if (_panel.StartsWith("dungeon-", StringComparison.Ordinal))
        {
            Add("dungeon:close", "Close", new(box.Right - 100, box.Top + 12, 80, 34));
            if (_panel == "dungeon-party") BuildDungeonPartyPanelControls(box);
            else if (_panel == "dungeon-gear") BuildDungeonGearControls(box);
            else if (_panel == "dungeon-gear-detail") BuildDungeonGearDetailControls(box);
            else if (_panel == "dungeon-cooperate") BuildDungeonCooperationControls(box);
            else if (_panel == "dungeon-skills") BuildDungeonSkillControls(box);
            else if (_panel == "dungeon-trade") BuildDungeonTradeControls(box);
            else if (_panel == "dungeon-world-loot") BuildWorldCacheControls(box);
            else if (_panel == "dungeon-social") BuildNativeSocialControls(box);
            else if (_panel == "dungeon-corpse") BuildDungeonCorpseControls(box);
            else if (_panel == "dungeon-work") BuildDungeonWorkControls(box);
            else if (_panel == "dungeon-camp") BuildDungeonCampControls(box);
            else if (_panel == "dungeon-needs") BuildDungeonNeedsControls(box);
            else if (_panel == "dungeon-abandon-confirm") BuildDungeonAbandonControls(box);
            else if (_panel == "dungeon-recruit") BuildDungeonRecruitControls(box);
            else if (_panel == "dungeon-chest-recipient") BuildDungeonChestRecipientControls(box);
            else if (_panel == "dungeon-class")
            {
                foreach (var kind in Enum.GetValues<ClassKind>())
                {
                    int i = (int)kind;
                    var definition = ClassCatalog.Get(kind);
                    Add("dungeon:class:" + i, definition.Name,
                        DungeonClassCards.Bounds(box, i),
                        _session.State.Tactics!.NextAction == 1, _session.DungeonMember.Tactics!.Build!.Class == kind,
                        "The class is fixed after the first paid action.",
                        $"HP {definition.Health}, damage {definition.Damage}, defense {definition.Defense}, speed {definition.Speed}. {definition.BasicName}; {definition.SpecialName}; {definition.DashName}.",
                        sprite: NativeAppearance.ForClass(kind), detail: DungeonClassCards.Description(kind));
                }
            }
            else if (_panel == "dungeon-chest")
            {
                var actor = _session.DungeonMember;
                var offers = _session.DungeonCombat.ChestOffers(_nativeChest, actor.Id);
                for (int i = 0; i < offers.Count; i++)
                {
                    var preview = _session.DungeonCombat.Preview(new(TacticalAction.ClaimChest, Target: _nativeChest, Choice: i + 1));
                    Add("dungeon:claim:" + (i + 1), "Choose " + (i + 1), new(box.Right - 166, box.Y + 100 + i * 108, 138, 38), preview.Valid, reason: preview.Reason);
                }
            }
            else if (_panel == "dungeon-routes")
            {
                var routes = _session.AvailableDungeonRoutes(_session.DungeonMember.Id);
                for (int i = 0; i < routes.Count; i++) Add("dungeon:route:" + (i + 1), routes[i].Name,
                    new(box.X + 28, box.Y + 92 + i * 100, box.Width - 56, 42));
            }
            else if (_panel == "dungeon-utility")
            {
                var utility = _session.DungeonMember.Tactics!.Supplies.Utility;
                if (utility != null)
                {
                    bool aimed = utility.Kind == UtilityKind.EscapeScroll;
                    if (aimed) for (int i = 1; i <= 3; i++) Add("dungeon:utility-steps:" + i, i + (i == 1 ? " tile" : " tiles"),
                        new(box.X + 28 + (i - 1) * 140, box.Y + 170, 126, 38), selected: _nativeUtilitySteps == i);
                    var preview = _session.DungeonCombat.Preview(new(TacticalAction.UseUtility));
                    Add("dungeon:utility-use", aimed ? "Choose direction" : utility.Marker == null && utility.Kind == UtilityKind.ReturnToken ? "Plant token" : "Use utility",
                        new(box.X + 28, box.Y + 240, 240, 42), aimed || preview.Valid, reason: preview.Reason);
                }
            }
            else if (_panel == "dungeon-service")
            {
                var options = _session.DungeonCombat.DungeonServiceChoices(_nativeService, _session.DungeonMember.Id);
                _nativeServicePage = Math.Clamp(_nativeServicePage, 0, Math.Max(0, (options.Count - 1) / 2));
                foreach (var option in options.Skip(_nativeServicePage * 2).Take(2))
                    Add("dungeon:service-choice:" + option.Choice, option.Replacement ? "Review exchange"
                        : _session.State.Tactics!.Services.Nodes.Single(n => n.Id == _nativeService).Fixture != null ? "Preview cross" : "Confirm choice",
                        new(box.Right - 194, box.Y + 100 + (option.Choice - 1) % 2 * 160, 166, 42), option.Enabled, reason: option.Reason);
                Add("dungeon:service-prev", "Previous", new(box.X + 24, box.Bottom - 54, 130, 34), _nativeServicePage > 0);
                Add("dungeon:service-next", "Next", new(box.Right - 154, box.Bottom - 54, 130, 34), (_nativeServicePage + 1) * 2 < options.Count);
            }
            else if (_panel == "dungeon-service-replace")
            {
                Add("dungeon:service-confirm", "Replace carried utility", new(box.X + 28, box.Y + 210, 290, 42));
                Add("dungeon:service-back", "Keep current utility", new(box.X + 334, box.Y + 210, 260, 42));
            }
            else if (_panel == "dungeon-profile") BuildPermanentProfileControls(box);
            else if (_panel == "dungeon-boons")
            {
                int count = _session.DungeonMember.Tactics!.Boons.Inventory.Stacks.Count;
                _nativeBoonPage = Math.Clamp(_nativeBoonPage, 0, Math.Max(0, (count - 1) / 3));
                Add("dungeon:boon-prev", "Previous", new(box.X + 24, box.Bottom - 54, 130, 34), _nativeBoonPage > 0);
                Add("dungeon:boon-next", "Next", new(box.Right - 154, box.Bottom - 54, 130, 34), (_nativeBoonPage + 1) * 3 < count);
            }
            else if (_panel == "dungeon-menu")
            {
                string[] ids = ["classes", "guide", "settings", "new-question", "title", "quit", "party", "gear", "cooperate", "needs"];
                string[] labels = ["Starting class", "Controls and help", "Presentation settings", "Start a new run", "Return to title", "Save and quit", "Party and rescue", "Equipment / K", "Cooperation / O", "Survival / N"];
                int columns = (box.Width - 80) / 2;
                for (int i = 0; i < ids.Length; i++) Add("dungeon:" + ids[i], labels[i], new(box.X + 28 + i / 5 * (columns + 24), box.Y + 80 + i % 5 * 60, columns, 44),
                    ids[i] != "new-question" || !_session.DeathPending, reason: "Save the recorded death first.");
                Add("dungeon:work", "Work / J", new(box.X + 28, box.Y + 392, columns, 44));
                Add("dungeon:profile", "Achievements and unlocks", new(box.X + 52 + columns, box.Y + 392, columns, 44));
                Add("dungeon:skills", "Unlocked skills / L", new(box.X + 28, box.Y + 446, columns, 44));
                Add("dungeon:trade", "Town trade", new(box.X + 52 + columns, box.Y + 446, columns, 44));
            }
            else if (_panel == "dungeon-new-confirm")
            {
                Add("dungeon:new-go", "Begin a fresh run", new(box.X + 28, box.Y + 190, 280, 42), !_session.DeathPending, reason: "Save the recorded death first.");
                Add("dungeon:new-cancel", "Keep this run", new(box.X + 328, box.Y + 190, 240, 42));
            }
            else if (_panel == "dungeon-settings")
            {
                static string On(bool value) => value ? "On" : "Off";
                string[] ids = ["master", "music", "effects", "shake", "flash", "combat-effects", "numbers", "font", "text-size", "contrast", "motion"];
                string[] labels = [$"Master: {_preferences.Master}%", $"Music: {_preferences.Music}%", $"Effects: {_preferences.Effects}%",
                    "Screen shake: " + On(_preferences.Shake), "Hit flash: " + On(_preferences.Flash), "Combat effects: " + On(_preferences.CombatEffects),
                    "Damage numbers: " + On(_preferences.DamageNumbers), "Font: " + _preferences.Font, "Text size: " + _preferences.TextSize,
                    "High contrast: " + On(_preferences.HighContrast), "Reduced motion: " + On(_preferences.ReducedMotion)];
                int width = (box.Width - 72) / 2;
                for (int i = 0; i < ids.Length; i++) Add("dungeon:setting:" + ids[i], labels[i],
                    new(box.X + 24 + i % 2 * (width + 24), box.Y + 86 + i / 2 * 56, width, 42));
            }
            return;
        }
        if (_session.State.AwaitingContinuation)
        {
            Add("dungeon:new", "Begin a fresh run", new(_map.Center.X - 140, _map.Center.Y + 36, 280, 42), !_session.DeathPending,
                reason: "Save the recorded death before starting a fresh run.");
            if (_session.DeathPending) Add("dungeon:death-save", "Retry saving the death", new(_map.Center.X - 140, _map.Center.Y + 84, 280, 34));
            return;
        }
        if (_session.State.Tactics!.NextAction == 1)
            Add("dungeon:classes", "Choose class", new(_side.X + 12, _side.Bottom - 54, _side.Width - 24, 36));
        if (_panel.Length == 0 && _session.Party.Count(a => a.Life != LifeState.Dead) > 1)
            BuildDungeonMemberControls();
    }

    private bool ActivateDungeonControl(string id)
    {
        if (!id.StartsWith("dungeon:", StringComparison.Ordinal)) return false;
        PlayDungeonSound("menu");
        var parts = id.Split(':');
        if (parts[1] == "social-open") { OpenNativeSocial(_nativeRecruit); return true; }
        if (parts[1] == "social-next") { AdvanceDialogue(); return true; }
        if (parts[1] == "social") { ChooseNativeSocial(parts[2]); return true; }
        if (parts[1] == "world-loot") { ClaimWorldCache(int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)); return true; }
        if (parts[1] == "work" || parts[1].StartsWith("work-", StringComparison.Ordinal))
        { ActivateDungeonWorkControl(parts); return true; }
        if (parts[1] == "skills" || parts[1].StartsWith("skill-", StringComparison.Ordinal))
        { ActivateDungeonSkillControl(parts); return true; }
        if (parts[1] == "trade" || parts[1].StartsWith("trade-", StringComparison.Ordinal))
        { ActivateDungeonTradeControl(parts); return true; }
        switch (parts[1])
        {
            case "save":
            case "death-save": TrySave(); break;
            case "close": ClosePanel(); break;
            case "menu": _panel = "dungeon-menu"; break;
            case "map": ToggleDungeonMap(); break;
            case "guide": _panel = "dungeon-guide"; break;
            case "settings": _panel = "dungeon-settings"; break;
            case "profile": _profilePage = 0; _panel = "dungeon-profile"; break;
            case "profile-prev": _profilePage = Math.Max(0, _profilePage - 1); break;
            case "profile-next": _profilePage = Math.Min(7, _profilePage + 1); break;
            case "title": TrySave(); if (_notice == "World saved.") OpenTitleScreen(); break;
            case "setting": ChangeDungeonPreference(parts[2]); break;
            case "new-question": _panel = "dungeon-new-confirm"; break;
            case "new-cancel": ClosePanel(); break;
            case "boons": _panel = "dungeon-boons"; break;
            case "utility": _panel = "dungeon-utility"; break;
            case "party": _panel = "dungeon-party"; break;
            case "gear": _panel = "dungeon-gear"; break;
            case "gear-item": OpenDungeonGearDetail(parts[2]); break;
            case "gear-slot":
                if (_session.DungeonMember.Equipment.TryGetValue(parts[2], out var gear)) OpenDungeonGearDetail(gear.Item, parts[2]);
                break;
            case "gear-prev": _nativeGearPage--; break;
            case "gear-next": _nativeGearPage++; break;
            case "gear-confirm": ConfirmDungeonGear(); break;
            case "cooperate": _panel = "dungeon-cooperate"; break;
            case "cooperate-choice": ChooseDungeonCooperation(long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture)); break;
            case "recover-equipment": CommitRoag(new("recover_equipment", Target: _nativeCorpse)); ClosePanel(); break;
            case "campfire": CommitRoag(new(parts[2])); ClosePanel(); break;
            case "camp": _panel = "dungeon-camp"; break;
            case "activity": CommitRoag(new(parts[2], Choice: int.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture))); ClosePanel(); break;
            case "activity-cancel": _notice = _session.CancelDungeonActivity(_session.DungeonMember.Id).Message; break;
            case "needs": _panel = "dungeon-needs"; break;
            case "eat":
                _notice = _session.SubmitDungeon(new(TacticalAction.Eat, Item: "food")).Message;
                CollectDungeonFeedback(); ClosePanel(); break;
            case "medicine": CommitRoag(new("use_medicine", Target: long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture))); ClosePanel(); break;
            case "auto-eat": ToggleDungeonEating(combat: false); break;
            case "auto-eat-combat": ToggleDungeonEating(combat: true); break;
            case "member": SelectDungeonMember(long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)); break;
            case "ready": SelectDungeonMember(_session.State.Tactics!.ActiveActor); break;
            case "revive": CommitRoag(new("revive", Target: long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture))); ClosePanel(); break;
            case "abandon": OpenDungeonAbandon(long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)); break;
            case "abandon-cancel": _panel = "dungeon-party"; break;
            case "abandon-confirm": ConfirmDungeonAbandon(); break;
            case "recruit": CommitRoag(new("recruit", Target: _nativeRecruit)); ClosePanel(); break;
            case "lead-selected": _notice = _session.ChooseDungeonLeader(_session.DungeonMember.Id).Message; break;
            case "regroup": _notice = _session.RegroupDungeonParty(_session.DungeonMember.Id).Message; break;
            case "split": _notice = _session.SplitDungeonMember(_session.DungeonMember.Id).Message; break;
            case "chest-recipient":
                long recipient = long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                if (recipient == _session.DungeonMember.Id && DungeonSelectionReady
                    && _session.DungeonCombat.ChestOffers(_nativeChest, recipient).Count == 3) _panel = "dungeon-chest";
                else _notice = "The chosen recipient must physically open this chest on their own turn.";
                break;
            case "utility-steps": _nativeUtilitySteps = int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture); break;
            case "utility-use":
                if (_session.DungeonMember.Tactics!.Supplies.Utility?.Kind == UtilityKind.EscapeScroll)
                { ClosePanel(); _roagAim = "use_utility"; }
                else { CommitRoag(new("use_utility")); ClosePanel(); }
                break;
            case "service-prev": _nativeServicePage--; break;
            case "service-next": _nativeServicePage++; break;
            case "service-back": _panel = "dungeon-service"; break;
            case "service-choice":
                int selected = int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                var option = _session.DungeonCombat.DungeonServiceChoices(_nativeService, _session.DungeonMember.Id).Single(o => o.Choice == selected);
                if (option.Replacement) { _nativeReplacementChoice = selected; _panel = "dungeon-service-replace"; }
                else if (_session.State.Tactics!.Services.Nodes.Single(n => n.Id == _nativeService).Fixture != null)
                { ClosePanel(); _roagAim = "fixture"; _notice = "Fixture preview: four physical damage in +1.00s. Enter/click confirms; Escape cancels. No guaranteed reaction turn."; }
                else { CommitRoag(new("interact", Target: _nativeService, Choice: selected)); ClosePanel(); }
                break;
            case "service-confirm":
                CommitRoag(new("interact", Target: _nativeService, Choice: _nativeReplacementChoice, ReplaceUtility: true)); ClosePanel(); break;
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
            case "new-go":
            case "new": NewWorld(); _nativeSeenEvent = _session.State.Tactics!.NextEvent - 1; break;
            case "quit": TrySave(); if (_notice == "World saved.") Exit(); break;
        }
        return true;
    }

    private bool TryOpenDungeonChest(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var position = new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        var chest = _session.State.Tactics!.Chests.Chests.FirstOrDefault(c => !c.Claimed && c.Position == position);
        if (chest == null || _session.DungeonCombat.ChestOffers(chest.Id, _session.DungeonMember.Id).Count == 0) return false;
        OpenDungeonChest(chest.Id); return true;
    }

    private void OpenDungeonChest(long chest)
    {
        _nativeChest = chest; _panel = DungeonRoster.Length > 1 ? "dungeon-chest-recipient" : "dungeon-chest";
        _dungeonFeedback.OpenChest(); PlayDungeonSound("chest-open");
    }

    private bool TryOpenDungeonRoute(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var position = new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        var floor = _session.DungeonFloorAt(position);
        if (floor is not { Stage: < 4, Boss: > 0 } || floor.Exit != position || _session.DungeonRouteChosen(position)
            || _session.Chunks.At(position) == Material.StairsDown || !_session.Visible.Contains(position)) return false;
        if (_session.AvailableDungeonRoutes(_session.DungeonMember.Id).Count > 0) _panel = "dungeon-routes";
        else _notice = position.Distance(_session.State.Position) > 1 ? "Stand beside the marked stage exit to choose a route."
            : "This route remains sealed until this floor's elite falls.";
        return true;
    }

    private bool TryOpenDungeonService(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var position = new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        var node = _session.State.Tactics!.Services.Nodes.FirstOrDefault(n => n.Position == position);
        if (node == null || position.Distance(_session.State.Position) > 1 || !_session.Visible.Contains(position)) return false;
        _nativeService = node.Id; _nativeServicePage = 0; _panel = "dungeon-service"; return true;
    }

    private bool TryOpenDungeonVault(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var position = new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        if (_session.DungeonVaultAt(position) is not { Opened: false } vault || position.Distance(_session.State.Position) != 1
            || !_session.Visible.Contains(position)) return false;
        CommitRoag(new("open_vault", Target: vault.Id)); return true;
    }

    private void DungeonInteract()
    {
        var actor = _session.DungeonMember;
        var downed = _session.Party.FirstOrDefault(a => a.Life == LifeState.Downed && a.Position.Distance(actor.Position) == 1);
        if (downed != null) { _panel = "dungeon-party"; return; }
        var corpse = _session.Party.FirstOrDefault(a => a.Life == LifeState.Dead && a.Equipment.Count > 0
            && a.Position.Distance(actor.Position) <= 1 && _session.Visible.Contains(a.Position));
        if (corpse != null) { _nativeCorpse = corpse.Id; _panel = "dungeon-corpse"; return; }
        var recruit = _session.NativeAdventurers.Select(r => _session.State.Actors.Single(a => a.Id == r.Actor))
            .FirstOrDefault(a => !a.Recruited && a.Life == LifeState.Alive && a.Position.Distance(actor.Position) == 1 && _session.Visible.Contains(a.Position));
        if (recruit != null) { _nativeRecruit = recruit.Id; _panel = "dungeon-recruit"; return; }
        if (_session.AvailableDungeonRoutes(actor.Id).Count > 0) { _panel = "dungeon-routes"; return; }
        var chest = _session.State.Tactics!.Chests.Chests.FirstOrDefault(c => !c.Claimed && c.Position.Distance(actor.Position) == 1);
        if (chest != null) { OpenDungeonChest(chest.Id); return; }
        var service = _session.State.Tactics!.Services.Nodes.Where(n => n.Position.Distance(actor.Position) <= 1 && _session.Visible.Contains(n.Position))
            .OrderBy(n => n.UsedBy != 0 || n.Contract?.Status is DungeonContractStatus.Failed or DungeonContractStatus.Completed).ThenBy(n => n.Id).FirstOrDefault();
        if (service != null) { _nativeService = service.Id; _nativeServicePage = 0; _panel = "dungeon-service"; return; }
        var vault = _session.State.Tactics.Vaults.Nodes.FirstOrDefault(v => !v.Opened && v.Position.Distance(actor.Position) == 1 && _session.Visible.Contains(v.Position));
        if (vault != null) { CommitRoag(new("open_vault", Target: vault.Id)); return; }
        var door = _session.State.Tactics.Doors.Nodes.FirstOrDefault(d => d.Position.Distance(actor.Position) == 1
            && _session.Chunks.At(d.Position) == Material.ClosedDoor && _session.Visible.Contains(d.Position));
        if (door != null) { CommitRoag(new("open_door", Target: door.Id)); return; }
        var worldCache = _session.State.Loot.FirstOrDefault(c => !c.Taken && c.Position.Distance(actor.Position) <= 1 && _session.Visible.Contains(c.Position));
        if (worldCache != null) { OpenWorldCache(worldCache); return; }
        if (_session.Chunks.At(actor.Position) is not (Material.StairsUp or Material.StairsDown))
        {
            var person = _session.State.Actors.Where(a => !a.Recruited && a.Life == LifeState.Alive && a.Kind == ActorKind.Person
                && a.Position.Distance(actor.Position) <= 2 && _session.Visible.Contains(a.Position) && _session.Interactions(a.Id).Count > 0)
                .OrderBy(a => a.Position.Distance(actor.Position)).ThenBy(a => a.Id).FirstOrDefault();
            if (person != null) { OpenNativeSocial(person.Id); return; }
        }
        CommitRoag(new("use_stairs"));
    }

    private void DrawDungeonHud(int w, int h)
    {
        var combat = _session.DungeonCombat;
        Text("ROAG", 20, 16, Gold, 2);
        Text($"{_session.NativeElapsedTime / (double)combat.Tuning.UnitsPerSecond:0.00}s / {(_session.DungeonMember.Id == combat.State.ActiveActor ? "your action" : "viewing member")}", 160, 22, Muted);
        string identity = _session.State.Dungeon is { } run ? $"Stage {run.Stage} / floor {_session.State.Position.Floor} / seed {_session.Seed}"
            : $"{_session.CalendarDescription} / {(_session.State.Position.Floor == 0 ? _session.WeatherAt(_session.State.Position).Weather.ToString() : "depth " + _session.State.Position.Floor)}";
        Text(identity, 20, 57, Muted);
        Fill(_side, Panel);
    }

    private void DrawDungeonTactics()
    {
        var combat = _session.DungeonCombat; var hero = _session.DungeonMember; var build = hero.Tactics!.Build!;
        DrawDungeonFeedback();
        foreach (var warning in _session.RoagTelegraphs()) foreach (var cell in warning.Cells)
        {
            var r = TileRect(cell); var color = new Color(240, 112, 78);
            FillClipped(new(r.X, r.Y, r.Width, 2), color); FillClipped(new(r.X, r.Bottom - 2, r.Width, 2), color);
            FillClipped(new(r.X, r.Y, 2, r.Height), color); FillClipped(new(r.Right - 2, r.Y, 2, r.Height), color);
        }
        foreach (var actor in _session.State.Actors.Where(a => a.Life != LifeState.Dead && a.Tactics != null && _session.Visible.Contains(a.Position)))
        {
            if (actor.Tactics is { DefinitionId: "core:enemy/haunted_cache", Enemy.Alerted: false } && actor.Health == actor.MaxHealth) continue;
            var tile = TileRect(actor.Position); var bar = new Rectangle(tile.X + 4, tile.Bottom - 5, tile.Width - 8, 3);
            FillClipped(bar, new Color(16, 22, 27));
            FillClipped(bar with { Width = Math.Max(0, bar.Width * actor.Health / actor.MaxHealth) }, _session.IsHostile(actor) ? new Color(240, 112, 78) : Gold);
            if (actor.Life == LifeState.Downed) Text($"DOWN {actor.Tactics!.BleedTurns}", tile.X, tile.Y - 12, new Color(240, 112, 78));
            if (combat.IsContractTarget(actor.Id)) FillClipped(new(tile.X + 5, tile.Y + 3, tile.Width - 10, 2), Gold);
            if (actor.Tactics!.Guarding)
                foreach (var cell in AttackGeometry.Arc(actor.Position, actor.Tactics.Facing))
                    if (_session.Visible.Contains(cell)) FillClipped(TileRect(cell), Gold * .08f);
        }
        int x = _side.X + 14, y = _side.Y + 14;
        if (_session.Party.Count(a => a.Life != LifeState.Dead) > 1) y += _session.Party.Count(a => a.Life != LifeState.Dead) * 48 + 8;
        Text(ClassCatalog.Get(build.Class).Name.ToUpperInvariant(), x, y, Gold); y += 26;
        Text($"HP {hero.Health}/{hero.MaxHealth} / level {build.Level}", x, y); y += 24;
        Text($"XP {build.Experience}/{CharacterBuild.NextThreshold(build.Level)}", x, y, Muted); y += 24;
        Text($"Gold {_session.Count("gold")} / medicine {_session.Count("medicine")}", x, y, Gold); y += 30;
        if (combat.Tuning.EnableSurvival)
        {
            Text($"Food {_session.Count("food")} / hunger {hero.Body.Hunger:0.0} / N", x, y,
                hero.Body.Hunger >= _session.Simulation.StarveAt ? new Color(240, 112, 78) : Muted);
            y += 22;
        }
        if (_session.State.Dungeon?.Victory == true || _session.State.Endgame.Victory) { Text("REGENT DEFEATED", x, y, Gold); y += 24; }
        Text("Aim: " + _roagAim.Replace('_', ' '), x, y, Muted); y += 24;
        var supplies = hero.Tactics.Supplies;
        if (supplies.Utility != null) { Text(UtilityName(supplies.Utility.Kind) + " / U", x, y, Gold); y += 22; }
        if (supplies.FocusActions > 0) { Text($"Focus +2 / {supplies.FocusActions} actions", x, y, Gold); y += 22; }
        if (supplies.WardActions > 0) { Text($"One-hit ward / {supplies.WardActions} actions", x, y, Gold); y += 22; }
        if (hero.Tactics.Burning is { } burning)
        { Text($"Burning: {burning.Power} x {burning.Remaining} pulses", x, y, new Color(240, 112, 78)); y += 24; }
        string resource = DungeonMemberResources.Summary(hero, combat.Clock);
        if (resource.Length > 0) { Text(resource, x, y, Gold); y += 24; }
        if (_notice.Length > 0 && !_notice.StartsWith("WASD /", StringComparison.Ordinal)
            && _notice is not ("Action committed." or "Choose your fixed starting class."))
        { y += Wrap(Short(_notice, 96), x, y, _side.Width - 28, Muted); y += 8; }
        if (y + 46 < _side.Bottom)
        {
            Text("TURN TIMELINE", x, y, Gold); y += 24;
            Text(_session.State.AwaitingContinuation ? "Run ended" : "Ready: " + Short(combat.Active?.Name ?? "none", 24), x, y); y += 22;
        }
        foreach (var entry in combat.VisibleTimeline().Take(Math.Clamp((_side.Bottom - y - 16) / 20, 0, 5)))
        {
            var actor = _session.State.Actors.FirstOrDefault(a => a.Id == entry.ActorId);
            string name = actor == null || !actor.Recruited && !_session.Visible.Contains(actor.Position) ? "Ground effect" : actor.Name;
            string kind = entry.Kind == ScheduledKind.Impact ? "impact" : entry.Kind == ScheduledKind.ActorReady ? "turn" : "effect";
            Text(Short($"{Math.Max(0, entry.Due - combat.Clock) / 100.0:0.00}s {name} {kind}", 34), x, y, entry.Kind == ScheduledKind.Impact ? new Color(240, 112, 78) : Muted);
            y += 20;
        }
        y += 14;
        if (y + 44 < _side.Bottom) { Text("RECENT EVENTS", x, y, Gold); y += 22; }
        foreach (var e in combat.State.Events.Where(e => _session.Visible.Contains(e.Position)
                     && e.Kind is "DamageTaken" or "Downed" or "Died" or "Abandoned" or "Revived" or "Recruited" or "BleedOut" or "BleedShortened" or "BoonGranted" or "CurrencyCollected" or "FloorChanged" or "GuardRaised" or "DoorOpened" or "SecretDiscovered" or "VaultOpened" or "VaultKeyCollected"
                     or "EliteEngaged" or "ElitePhaseChanged" or "RegentPhaseChanged" or "EliteDefeated" or "Victory").TakeLast(4))
        {
            if (y + 20 >= _side.Bottom) break;
            Text(Short(e.Kind + (e.Amount > 0 ? " " + e.Amount : ""), 34), x, y, Muted); y += 20;
        }
        if (_session.State.AwaitingContinuation)
        {
            Fill(new(_map.Center.X - 210, _map.Center.Y - 80, 420, _session.DeathPending ? 204 : 160), Panel);
            Text("YOU HAVE FALLEN", _map.Center.X - 170, _map.Center.Y - 30, new Color(240, 112, 78), 2);
            Text(_session.DeathPending ? "Use Save to record this death first." : "This run is over. Begin a fresh run.", _map.Center.X - 170, _map.Center.Y + 6, Muted);
            return;
        }
        if (_panel.Length > 0 || _roagAim == "move") return;
        var mouse = Mouse.GetState(); if (!_map.Contains(mouse.Position) && _roagAim != "fixture") return;
        var point = _camera.ScreenToWorld(new(mouse.X, mouse.Y));
        int dx = Math.Sign((int)Math.Floor(point.X) - hero.Position.X), dy = Math.Sign((int)Math.Floor(point.Y) - hero.Position.Y);
        if (_roagAim == "use_utility") { if (Math.Abs(point.X - hero.Position.X) >= Math.Abs(point.Y - hero.Position.Y)) dy = 0; else dx = 0; }
        var preview = _session.PreviewDungeonAim(SelectedRoagAim(dx, dy));
        if (preview.Valid && _roagAim is "basic_attack" or "sweeping_slash")
        {
            var allies = _session.Party.Where(a => a.Id != hero.Id && a.Life != LifeState.Dead
                && _session.Visible.Contains(a.Position) && preview.Cells.Contains(a.Position)).ToArray();
            if (allies.Length > 0) Text("FRIENDLY FIRE: " + string.Join(", ", allies.Select(a => ClassCatalog.Get(a.Tactics!.Build!.Class).Name)),
                _map.X + 4, _map.Bottom - 68, new Color(240, 112, 78));
        }
        var previewCells = _roagAim == "guard" && AttackGeometry.TryFacing(dx, dy, out var guardFacing)
            ? AttackGeometry.Arc(hero.Position, guardFacing) : preview.Cells;
        foreach (var cell in previewCells)
            if (_session.Visible.Contains(cell)) FillClipped(TileRect(cell), new Color(113, 187, 195) * .25f);
        foreach (var condition in preview.BoonCells)
        {
            if (!_session.Visible.Contains(condition.Position)) continue;
            var tile = TileRect(condition.Position); var tint = new Color(197, 153, 225);
            FillClipped(new(tile.X + 3, tile.Y + 3, 5, 2), tint);
            FillClipped(new(tile.Right - 8, tile.Bottom - 5, 5, 2), tint);
        }
        Text(preview.Valid ? $"Recovery {preview.Recovery / 100.0:0.00}s" + (preview.Damage is { } damage ? $" / base damage {damage}" : "") : Short(preview.Reason, Math.Max(30, _map.Width / 8)),
            _map.X + 4, _map.Bottom - 24, preview.Valid ? Gold : new Color(240, 112, 78));
        if (preview.BoonCells.Count > 0)
            Text("Purple corners: conditional boon tiles / " + string.Join(", ", preview.BoonCells.Select(c => c.Condition).Distinct()),
                _map.X + 4, _map.Bottom - 46, new Color(197, 153, 225));
        if (_roagAim == "fixture") Text("PREVIEW / impact +1.00s / Enter or click confirms / Escape cancels", _map.X + 4, _map.Bottom - 46, Gold);
    }

    private void DrawDungeonPanel(int w, int h)
    {
        var box = PanelBox(w, h); Fill(box, Panel); int x = box.X + 24, y = box.Y + 20;
        string title = _panel switch { "dungeon-class" => "CHOOSE A FIXED CLASS", "dungeon-chest" => "CHOOSE A BOON", "dungeon-boons" => "YOUR BOONS", "dungeon-guide" => "CONTROLS", "dungeon-routes" => "OPEN A PHYSICAL ROUTE", "dungeon-utility" => "CARRIED UTILITY", "dungeon-service" => "FINITE SERVICE", "dungeon-service-replace" => "EXCHANGE CARRIED UTILITY", "dungeon-settings" => "PRESENTATION SETTINGS", _ => "MENU" };
        if (_panel == "dungeon-profile") title = "PERMANENT ACHIEVEMENTS";
        if (_panel == "dungeon-party") title = "PARTY AND RESCUE";
        if (_panel is "dungeon-gear" or "dungeon-gear-detail") title = "EQUIPMENT AND SHARED PACK";
        if (_panel == "dungeon-cooperate") title = "COOPERATIVE ABILITIES";
        if (_panel == "dungeon-skills") title = "UNLOCKED SKILLS";
        if (_panel == "dungeon-trade") title = "TOWN TRADE";
        if (_panel == "dungeon-world-loot") title = "SUPPLIES AND TREASURE";
        if (_panel == "dungeon-social") title = "A MOMENT OFF THE ROAD";
        if (_panel == "dungeon-corpse") title = "RECOVER FALLEN EQUIPMENT";
        if (_panel == "dungeon-work") title = "WORK AND CRAFTING";
        if (_panel == "dungeon-camp") title = "REST AND WATCH";
        if (_panel == "dungeon-needs") title = "SURVIVAL AND PROVISIONS";
        if (_panel == "dungeon-abandon-confirm") title = "ABANDON THIS COMPANION?";
        if (_panel == "dungeon-recruit") title = "AN ADVENTURER";
        if (_panel == "dungeon-chest-recipient") title = "CHOOSE THE OPENER";
        Text(title, x, y, Gold, 1.3f); y += 48;
        if (_panel == "dungeon-profile") { DrawPermanentProfile(box, x, y); return; }
        if (_panel == "dungeon-party") { DrawDungeonPartyPanel(box, x, y); return; }
        if (_panel == "dungeon-gear") { DrawDungeonGearPanel(box, x, y); return; }
        if (_panel == "dungeon-gear-detail") { DrawDungeonGearDetail(box, x, y); return; }
        if (_panel == "dungeon-cooperate") { DrawDungeonCooperationPanel(box, x, y); return; }
        if (_panel == "dungeon-skills") { DrawDungeonSkillsPanel(box, x, y); return; }
        if (_panel == "dungeon-trade") { DrawDungeonTradePanel(box, x, y); return; }
        if (_panel == "dungeon-world-loot") { DrawWorldCachePanel(box, x, y); return; }
        if (_panel == "dungeon-social") { DrawNativeSocialPanel(box); return; }
        if (_panel == "dungeon-corpse") { DrawDungeonCorpsePanel(box, x, y); return; }
        if (_panel == "dungeon-work") { DrawDungeonWorkPanel(box, x, y); return; }
        if (_panel == "dungeon-camp") { DrawDungeonCampPanel(box, x, y); return; }
        if (_panel == "dungeon-needs") { DrawDungeonNeedsPanel(box, x, y); return; }
        if (_panel == "dungeon-abandon-confirm") { DrawDungeonAbandonPanel(box, x, y); return; }
        if (_panel == "dungeon-recruit") { DrawDungeonRecruitPanel(box, x, y); return; }
        if (_panel == "dungeon-chest-recipient")
        {
            Wrap("The member physically opening this chest receives its boon permanently. Bring your chosen member beside it on their own turn, then confirm them before seeing three eligible offers.", x, y, box.Width - 48, Muted);
            return;
        }
        if (_panel == "dungeon-new-confirm")
        {
            Text("START A NEW RUN?", x, y, Gold); y += 32;
            Wrap("Your current character, build and dungeon will be replaced in the continuing save slot. Permanent achievements, unlocked boons and presentation settings are kept.", x, y, box.Width - 48, Muted);
        }
        if (_panel == "dungeon-settings")
        {
            Text("Click a setting to cycle it. Changes save separately from your run.", x, y, Muted);
            Wrap("Reduced motion suppresses shake, travel and floating movement. Disabling combat effects keeps tactical previews and enemy warnings visible.", x, box.Bottom - 78, box.Width - 48, Muted);
        }
        if (_panel == "dungeon-chest")
        {
            var chest = _session.State.Tactics!.Chests.Chests.Single(c => c.Id == _nativeChest);
            var definition = BoonChestDefinition.Get(chest.Definition);
            double opening = _dungeonFeedback.Time - _dungeonFeedback.ChestOpened;
            int pop = _preferences.CombatEffects && !_preferences.ReducedMotion && opening < .4
                ? (int)(Math.Sin(Math.Clamp(opening / .4, 0, 1) * Math.PI) * 5) : 0;
            _sprites.Fit(_batch, "chest-open", new(x - pop, y - 6 - pop, 28 + pop * 2, 28 + pop * 2), Gold);
            Text($"{definition.Name} / {definition.Price} gold / purse {_session.Count("gold")}", x + 40, y, Gold);
            if (DungeonRoster.Length > 1) Text("Recipient: " + Short(_session.DungeonMember.Name, 30), x, box.Bottom - 62, Muted);
            y += 34;
            var offers = _session.DungeonCombat.ChestOffers(chest.Id, _session.DungeonMember.Id);
            for (int i = 0; i < offers.Count; i++)
            {
                var offer = offers[i]; var boon = BoonCatalog.Default.Get(offer.Boon);
                float reveal = !_preferences.CombatEffects || _preferences.ReducedMotion ? 1
                    : (float)Math.Clamp((opening - i * .055) / .18, 0, 1);
                DrawDungeonBoonIcon(boon.Id, offer.Rarity, new(x, y + i * 108, 64, 64), reveal);
                Text($"{i + 1}. {boon.Name} / {offer.Rarity}", x + 76, y + i * 108, Gold);
                Wrap(boon.Description, x + 76, y + 26 + i * 108, box.Width - 296, Muted);
            }
        }
        else if (_panel == "dungeon-routes")
        {
            var routes = _session.AvailableDungeonRoutes(_session.DungeonMember.Id);
            Text("Opening a route spends one action. Then walk down its stairs; cleared floors remain.", x, y, Muted);
            for (int i = 0; i < routes.Count; i++) Wrap(routes[i].Description, x + 6, box.Y + 142 + i * 100, box.Width - 60, Muted);
        }
        else if (_panel == "dungeon-guide")
        {
            y += Wrap("WASD / arrows: one tile per press. F: aim basic attack. R: aim special. X: movement ability. G: directional Guard. U: carried utility. Choose a direction with the keyboard, or click a target for attacks. Q/E/Z/C give diagonal aim. Space or period waits. Enter inspects an adjacent chest/service or uses the stair beneath you. Escape cancels aim. P toggles Pilgrim's Vow. B opens the party. K opens equipment; equipping/removing costs one action. O opens cooperation. J opens work and crafting; right-click a visible tile to choose a work site. N opens survival, eating and medicine. 1–4 select a member; T returns to the ready member. H aims adjacent medicine rescue. F5 saves. Menus and inspection consume no game time. Close and relaunch to resume the continuing run.", x, y, box.Width - 48);
            Wrap("Controller: D-pad / left stick moves one tile per press. X aims basic, Y special, right shoulder movement. Left shoulder aims Guard, or toggles Vow during attack aim. Right trigger waits. A interacts; left-stick press also interacts. Right-stick press opens utilities. Start opens the menu / cancels aim; Back toggles the map; left trigger opens boons. In menus, D-pad selects, A confirms, B closes. Keyboard menus use arrows, number keys and Enter. F1 opens help, I/V inspect boons, Tab toggles the explored map, Escape opens the menu.", x, y + 16, box.Width - 48, Muted);
        }
        else if (_panel == "dungeon-utility")
        {
            var utility = _session.DungeonMember.Tactics!.Supplies.Utility;
            if (utility == null) Text("Empty. Find a utility service or travelling trader.", x, y, Muted);
            else
            {
                Text(UtilityName(utility.Kind), x, y, Gold); y += 30;
                Wrap(utility.Kind switch { UtilityKind.CleanseFlask => "Remove Burning. One paid action; the charge is retained if you are not Burning.",
                    UtilityKind.EscapeScroll => "Travel one to three visible, clear cardinal tiles. Stairs block the path. Choose distance, then direction; confirmation spends one action.",
                    _ => utility.Marker == null ? "Plant in your room for one action, then return once for another action. Leaving the room forfeits the token."
                        : "Return to the visible empty marker by a known clear path of at most four steps. The return spends one action and consumes the token." },
                    x, y, box.Width - 48, Muted);
            }
        }
        else if (_panel == "dungeon-service")
        {
            var node = _session.State.Tactics!.Services.Nodes.Single(n => n.Id == _nativeService);
            Text(TacticalCombat.ServiceName(node.Kind) + (node.Bell is { Number: > 0 } bell ? " " + bell.Number : "") + $" / purse {_session.Count("gold")}", x, y, Gold);
            var choices = _session.DungeonCombat.DungeonServiceChoices(node.Id, _session.DungeonMember.Id);
            if (choices.Count == 0) Wrap(node.UsedBy == 0 && node.Kind is DungeonServiceKind.AttunementForge or DungeonServiceKind.Memorial
                ? "No eligible owned stack or replacement. Nothing is spent."
                : _session.DungeonCombat.DungeonServiceDescription(node.Id, _session.DungeonMember.Id), x, y + 40, box.Width - 48, Muted);
            foreach (var option in choices.Skip(_nativeServicePage * 2).Take(2))
            {
                int rowY = box.Y + 96 + (option.Choice - 1) % 2 * 160;
                int height = Wrap(option.Label + (option.Price > 0 ? $" / {option.Price} gold" : ""), x, rowY, box.Width - 240, Gold);
                Wrap(option.Detail, x, rowY + height + 12, box.Width - 240, Muted);
            }
            Text(node.Kind is DungeonServiceKind.BellClue or DungeonServiceKind.RouteSign ? "Reading costs no time."
                : "Inspection and cancellation are free. Confirming spends one action.", x, box.Bottom - 82, Muted);
        }
        else if (_panel == "dungeon-service-replace")
        {
            var current = _session.DungeonMember.Tactics!.Supplies.Utility;
            var option = _session.DungeonCombat.DungeonServiceChoices(_nativeService, _session.DungeonMember.Id).Single(o => o.Choice == _nativeReplacementChoice);
            Wrap($"Exchange {UtilityName(current!.Kind)} for {UtilityName(option.Utility!.Value)}? The old charge and any planted marker will be lost."
                + (option.Price > 0 ? $" Pay {option.Price} gold." : ""), x, y, box.Width - 48);
        }
        else if (_panel == "dungeon-boons")
        {
            var owned = _session.DungeonMember.Tactics!.Boons.Inventory.Stacks;
            if (owned.Count == 0) Text("Find boon chests to develop your build.", x, y, Muted);
            foreach (var pair in owned.OrderBy(p => p.Key).Skip(_nativeBoonPage * 3).Take(3))
            {
                var boon = BoonCatalog.Default.Get(pair.Key);
                int top = y;
                DrawDungeonBoonIcon(boon.Id, BoonCatalog.Rarities.Last(r => pair.Value[r] > 0), new(x, y, 64, 64));
                Text(boon.Name + " / " + BoonCatalog.Rarities.Sum(r => pair.Value[r]) + " stacks", x + 76, y, Gold); y += 24;
                Text(string.Join(" / ", BoonCatalog.Rarities.Where(r => pair.Value[r] > 0).Select(r => r + " " + pair.Value[r])), x + 76, y, Muted); y += 22;
                var retained = RetainedBoons.Find(boon.Id);
                string description = retained == null ? boon.Description : retained.Active ? boon.Description + " Open Skills / L to use it."
                    : "Strength " + _session.DungeonMember.Tactics!.Boons.Inventory.Magnitude(boon.Id).Power + ". " + boon.Description;
                y += Wrap(description, x + 76, y, box.Width - 124, Muted) + 10;
                y = Math.Max(y, top + 80);
            }
            Text($"Page {_nativeBoonPage + 1}/{Math.Max(1, (owned.Count + 2) / 3)}", box.Center.X - 50, box.Bottom - 42, Muted);
        }
        else if (_panel == "dungeon-class")
            Text("Choose before taking your first action. Each class has its own attacks and movement.", x, y, Muted);
    }

    private void DrawDungeonBoonIcon(string boon, BoonRarity rarity, Rectangle bounds, float reveal = 1)
    {
        Color color = rarity switch
        {
            BoonRarity.Common => new(196, 207, 199), BoonRarity.Uncommon => new(126, 211, 153),
            BoonRarity.Rare => new(126, 184, 240), BoonRarity.Legendary => Gold,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity))
        };
        Fill(bounds, color * .8f);
        Fill(new(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), new(12, 22, 24));
        int inset = 8 + (int)((1 - reveal) * 10);
        _sprites.Fit(_batch, NativeAppearance.ForBoon(boon), new(bounds.X + inset, bounds.Y + inset, bounds.Width - inset * 2, bounds.Height - inset * 2), color * (.4f + reveal * .6f));
    }

    private void DrawDungeonObjects()
    {
        var state = _session.State.Tactics!;
        void Icon(WorldPosition p, string sprite, Color tint)
        { if (_session.Visible.Contains(p) && _map.Intersects(TileRect(p))) _sprites.Fit(_batch, sprite, TileRect(p), tint); }
        foreach (var fire in state.CampFires.Where(f => f.Until > state.Queue.Clock))
            Icon(fire.Position, "campfire", new Color(255, 190, 112));
        foreach (var corpse in _session.Party.Where(a => a.Life == LifeState.Dead))
            Icon(corpse.Position, "bones", Gold);
        foreach (var floor in _session.DungeonFloors.Where(f => f.Stage < 4 && f.Boss > 0))
        {
            if (floor.Exit.Floor != _session.State.Position.Floor || !_session.Chunks.Seen(floor.Exit)
                || !_map.Intersects(TileRect(floor.Exit)) || _session.Chunks.At(floor.Exit) == Material.StairsDown
                || _session.DungeonRouteChosen(floor.Exit)) continue;
            bool visible = _session.Visible.Contains(floor.Exit);
            bool available = visible && _session.DungeonExitCleared(floor.Exit);
            var tile = TileRect(floor.Exit);
            _sprites.Fit(_batch, "stairs-down", tile, visible ? available ? Gold : Muted : Muted * .35f);
            if (visible) Text(available ? "ROUTE" : "SEALED", tile.X - 6, tile.Bottom + 2, available ? Gold : Muted);
        }
        foreach (var vault in state.Vaults.Nodes)
        {
            if (vault.Position.Floor != _session.State.Position.Floor) continue;
            if (_session.Chunks.Seen(vault.Position) && _map.Intersects(TileRect(vault.Position)))
                _sprites.Fit(_batch, _session.Visible.Contains(vault.Position) && vault.Opened ? "chest-open" : "chest", TileRect(vault.Position),
                    _session.Visible.Contains(vault.Position) ? Gold : Gold * .35f);
            if (!vault.KeyCollected) Icon(vault.KeyPosition, "key", Gold);
        }
        foreach (var quest in state.Quests.Nodes)
            foreach (var node in quest.Nodes)
            {
                var p = node.Position;
                if (node.Kind == Roag.Core.Dungeons.DungeonBranchNodeKind.Enemy || p.Floor != _session.State.Position.Floor
                    || !_session.Chunks.Seen(p) || !_map.Intersects(TileRect(p))) continue;
                bool visible = _session.Visible.Contains(p);
                if (visible && node.Activated && node.Kind is not (Roag.Core.Dungeons.DungeonBranchNodeKind.Gate or Roag.Core.Dungeons.DungeonBranchNodeKind.Brazier)) continue;
                string sprite = node.Kind switch
                {
                    Roag.Core.Dungeons.DungeonBranchNodeKind.Clue => "book", Roag.Core.Dungeons.DungeonBranchNodeKind.Key => "key",
                    Roag.Core.Dungeons.DungeonBranchNodeKind.Plate => "mark", Roag.Core.Dungeons.DungeonBranchNodeKind.Brazier => "brazier",
                    Roag.Core.Dungeons.DungeonBranchNodeKind.Gate => visible && quest.Opened ? "gate-open" : "gate", _ => "coin"
                };
                Color tint = visible ? node.Kind == Roag.Core.Dungeons.DungeonBranchNodeKind.Reward && !quest.Ready ? Muted : Gold : Gold * .35f;
                _sprites.Fit(_batch, sprite, TileRect(p), tint);
            }
        foreach (var chest in state.Chests.Chests)
            if (_session.Chunks.Seen(chest.Position) && chest.Position.Floor == _session.State.Position.Floor)
                _sprites.Fit(_batch, chest.Claimed ? "chest-open" : "chest", TileRect(chest.Position), Gold * (_session.Visible.Contains(chest.Position) ? 1 : .35f));
        foreach (var gate in state.Gates.Nodes)
            if (gate.Position.Floor == _session.State.Position.Floor && _session.Chunks.Seen(gate.Position) && _map.Intersects(TileRect(gate.Position)))
                _sprites.Fit(_batch, _session.Visible.Contains(gate.Position) && _session.Chunks.At(gate.Position) == Material.Door ? "gate-open" : "gate", TileRect(gate.Position),
                    _session.Visible.Contains(gate.Position) ? Gold : Gold * .35f);
        foreach (var node in state.Services.Nodes)
            if (_session.Chunks.Seen(node.Position) && node.Position.Floor == _session.State.Position.Floor && _map.Intersects(TileRect(node.Position)))
            {
                _sprites.Fit(_batch, NativeAppearance.ForService(node.Kind), TileRect(node.Position),
                    _session.Visible.Contains(node.Position) ? node.UsedBy == 0 ? Gold : Muted : Gold * .35f);
                if (node.Bell is { Number: > 0 } bell && _session.Visible.Contains(node.Position))
                { var tile = TileRect(node.Position); Text(bell.Number.ToString(System.Globalization.CultureInfo.InvariantCulture), tile.Right - 12, tile.Y + 2, Ink); }
            }
        foreach (var drop in state.Loot.Drops.Where(d => d.CarriedBy == 0))
            Icon(drop.Position, drop.Kind == GroundLootKind.Currency ? "coin" : drop.Kind == GroundLootKind.Health ? "heart" : "ration", Gold);
        foreach (var secret in state.Secrets.Nodes)
            if (!secret.Revealed && secret.Clue.Floor == _session.State.Position.Floor && _session.Chunks.Seen(secret.Clue)
                && _map.Intersects(TileRect(secret.Clue)))
                _sprites.Fit(_batch, secret.Kind == DungeonSecretKind.CrackedCache ? "cracked-wall" : "mark", TileRect(secret.Clue),
                    new Color(136, 202, 145) * (_session.Visible.Contains(secret.Clue) ? 1 : .35f));
        foreach (var feature in state.RoomFeatures.Nodes)
            if (!feature.Used && feature.Position.Floor == _session.State.Position.Floor && _session.Chunks.Seen(feature.Position)
                && _map.Intersects(TileRect(feature.Position)))
                _sprites.Fit(_batch, NativeAppearance.ForRoomFeature(feature.Kind), TileRect(feature.Position),
                    (feature.Kind == DungeonRoomFeatureKind.Brazier && _session.Visible.Contains(feature.Position) && feature.LitUntil > state.Queue.Clock ? Gold : Muted)
                    * (_session.Visible.Contains(feature.Position) ? 1 : .35f));
        var hostile = new Color(240, 112, 78); var friendly = new Color(113, 207, 218);
        foreach (var node in state.OptionalTerrain.Nodes)
        {
            if (!_session.Visible.Contains(node.Position)) continue;
            if (node.Kind == OptionalTerrainKind.HeatVent) Icon(node.Position,
                node.Phase == OptionalTerrainPhase.Warning ? "hazard-fire" : "brazier", node.Phase == OptionalTerrainPhase.Warning ? hostile : Muted);
            else if (node.Kind == OptionalTerrainKind.FragileCrossing) Icon(node.Position,
                node.Phase == OptionalTerrainPhase.Closed ? "water" : "bridge", node.Phase == OptionalTerrainPhase.Armed ? hostile : Gold);
            else
            {
                var tile = TileRect(node.Position);
                string arrow = node.Facing switch { Facing.North => "^", Facing.East => ">", Facing.South => "v", _ => "<" };
                Text(arrow, tile.Center.X - 8, tile.Center.Y - 8, Gold, 2);
            }
        }
        foreach (var web in state.Webs) foreach (var p in web.Span.Cells()) Icon(p, "web", hostile);
        foreach (var trail in state.Silt) Icon(trail.Position, "water", new(122, 162, 96));
        foreach (var ember in state.Embers) Icon(ember.Position, "hazard-fire", hostile);
        foreach (var mirror in state.Mirrors) Icon(mirror.Position, "crystal", new(210, 153, 224));
        foreach (var snare in state.EnemySnares) Icon(snare.Position, "trap", hostile);
        var members = DungeonRoster;
        foreach (var construct in DungeonMemberResources.Objects(_session))
        {
            Icon(construct.Position, construct.Sprite, construct.Spent ? Muted : friendly);
            if (members.Length <= 1 || !_session.Visible.Contains(construct.Position) || !_map.Intersects(TileRect(construct.Position))) continue;
            int owner = Array.FindIndex(members, a => a.Id == construct.Owner);
            var tile = TileRect(construct.Position);
            Text(owner < 0 ? "X" : (owner + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), tile.Right - 12, tile.Y + 2, Gold);
        }
    }

    private static string UtilityName(UtilityKind kind) => kind switch
    { UtilityKind.CleanseFlask => "Cleanse flask", UtilityKind.EscapeScroll => "Escape scroll", UtilityKind.ReturnToken => "Return token", _ => "Utility" };

    private void CollectDungeonFeedback()
    {
        if (_session.State.Tactics is not { } state) return;
        var events = state.Events.Where(e => e.Sequence > _nativeSeenEvent && _session.Visible.Contains(e.Position)).ToArray();
        _nativeSeenEvent = state.NextEvent - 1;
        if (events.Any(e => e.Kind == "EliteRewardDeferred")) _notice = "Elite defeated. Its chest waits for clear nearby ground.";
        else if (events.Any(e => e.Kind == "EliteRewardPlaced" && e.Action == 0)) _notice = "The elite chest appears on cleared nearby ground.";
        foreach (var e in events) ShowDungeonEvent(e);
    }
}
