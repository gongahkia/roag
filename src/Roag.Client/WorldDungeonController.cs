using Microsoft.Xna.Framework.Input;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private GamePadState _dungeonPad;
    private string? _controllerFocus;
    private string _controllerPanel = "";
    private string _menuFocusHint = "Arrows select / Enter confirms / Escape closes.";

    private WorldMouseControl[] DungeonMenuChoices() => _controls.Where(c => c.Enabled && c.Id.StartsWith("dungeon:", StringComparison.Ordinal)
        && (_session.State.AwaitingContinuation && _panel.Length == 0 ? c.Id is "dungeon:new" or "dungeon:death-save"
            : c.Id is not ("dungeon:save" or "dungeon:boons" or "dungeon:menu" or "dungeon:utility" or "dungeon:map" or "dungeon:party"))).ToArray();

    private void EnsureDungeonMenuFocus(WorldMouseControl[] choices)
    {
        if (_controllerPanel == _panel && choices.Any(c => c.Id == _controllerFocus)) return;
        _controllerPanel = _panel;
        _controllerFocus = (choices.FirstOrDefault(c => c.Id != "dungeon:close") ?? choices.FirstOrDefault())?.Id;
    }

    private void StepDungeonMenu(WorldMouseControl[] choices, int delta)
    {
        if (choices.Length == 0) return;
        int index = Array.FindIndex(choices, c => c.Id == _controllerFocus);
        _controllerFocus = choices[(index + delta + choices.Length) % choices.Length].Id;
    }

    private bool HandleDungeonMenuKeyboard(KeyboardState keyboard)
    {
        if (!_session.HasNativeParty) return false;
        if (!_panel.StartsWith("dungeon-", StringComparison.Ordinal) && !_session.State.AwaitingContinuation)
        { _controllerFocus = null; _controllerPanel = ""; return false; }
        bool Press(Keys key) => keyboard.IsKeyDown(key) && _roagKeyboard.IsKeyUp(key);
        var choices = DungeonMenuChoices(); EnsureDungeonMenuFocus(choices);
        int delta = Press(Keys.Up) || Press(Keys.Left) ? -1 : Press(Keys.Down) || Press(Keys.Right) || Press(Keys.Tab) ? 1 : 0;
        if (delta != 0) StepDungeonMenu(choices, delta);
        var numbered = choices.Where(c => c.Id != "dungeon:close").ToArray();
        bool numberedKey = false;
        for (int i = 0; i < Math.Min(9, numbered.Length); i++) if (Press(Keys.D1 + i)) { _controllerFocus = numbered[i].Id; numberedKey = true; }
        bool confirm = Press(Keys.Enter);
        if (confirm && _controllerFocus is { } selected) ActivateDungeonControl(selected);
        if (delta != 0 || confirm || numberedKey) _menuFocusHint = "Arrows select / Enter confirms / Escape closes.";
        return true;
    }

    private Keys[] DungeonControllerKeys(GamePadState pad, bool focused)
    {
        var previous = _dungeonPad; _dungeonPad = pad;
        if (!_session.HasNativeParty) { _controllerFocus = null; return []; }
        if (!focused) return [];
        if (!pad.IsConnected) return [];
        bool Press(Buttons button) => DungeonController.Press(pad, previous, button);
        if (_panel.Length > 0 || _session.State.AwaitingContinuation)
        {
            var choices = DungeonMenuChoices(); EnsureDungeonMenuFocus(choices);
            _menuFocusHint = "Controller: D-pad selects / A confirms / B closes.";
            if (Press(Buttons.B) || Press(Buttons.Start)) { ClosePanel(); _controllerFocus = null; return []; }
            int delta = Press(Buttons.DPadUp) || Press(Buttons.DPadLeft) || Press(Buttons.LeftShoulder) ? -1
                : Press(Buttons.DPadDown) || Press(Buttons.DPadRight) || Press(Buttons.RightShoulder) ? 1 : 0;
            if (delta != 0) StepDungeonMenu(choices, delta);
            if (Press(Buttons.A) && _controllerFocus is { } selected) ActivateDungeonControl(selected);
            return [];
        }
        _controllerFocus = null; _controllerPanel = "";
        if (Press(Buttons.Start)) { if (_roagAim == "move") _panel = "dungeon-menu"; else _roagAim = "move"; return []; }
        if (Press(Buttons.Back)) { ToggleDungeonMap(); return []; }
        return DungeonController.GameplayKeys(pad, previous, _roagAim is "basic_attack" or "sweeping_slash");
    }
}
