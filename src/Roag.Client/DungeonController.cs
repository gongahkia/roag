using Microsoft.Xna.Framework.Input;

namespace Roag.Client;

/// <summary>Edge-triggered controller input; a held direction never repeats a paid action.</summary>
internal static class DungeonController
{
    public static bool Press(GamePadState current, GamePadState previous, Buttons button)
        => current.IsConnected && current.IsButtonDown(button) && previous.IsButtonUp(button);

    public static Keys[] GameplayKeys(GamePadState current, GamePadState previous, bool attacking)
    {
        if (!current.IsConnected) return [];
        var keys = new List<Keys>();
        foreach (var (button, key) in new[] { (Buttons.A, Keys.Enter), (Buttons.B, Keys.Escape), (Buttons.X, Keys.F), (Buttons.Y, Keys.R),
            (Buttons.RightShoulder, Keys.X), (Buttons.LeftShoulder, attacking ? Keys.P : Keys.G),
            (Buttons.LeftStick, Keys.Enter), (Buttons.RightStick, Keys.U), (Buttons.RightTrigger, Keys.Space) })
            if (Press(current, previous, button)) keys.Add(key);
        var directions = new[] { (Buttons.DPadUp, Keys.W), (Buttons.DPadDown, Keys.S), (Buttons.DPadLeft, Keys.A), (Buttons.DPadRight, Keys.D) };
        if (directions.Any(d => Press(current, previous, d.Item1)))
            foreach (var (button, key) in directions) if (current.IsButtonDown(button)) keys.Add(key);
        Keys stick = Stick(current), oldStick = Stick(previous);
        if (stick != Keys.None && stick != oldStick) keys.Add(stick);
        return keys.Distinct().ToArray();
    }

    private static Keys Stick(GamePadState state)
    {
        if (!state.IsConnected) return Keys.None;
        var p = state.ThumbSticks.Left;
        if (Math.Max(Math.Abs(p.X), Math.Abs(p.Y)) < .6f) return Keys.None;
        return Math.Abs(p.X) >= Math.Abs(p.Y) ? p.X > 0 ? Keys.D : Keys.A : p.Y > 0 ? Keys.W : Keys.S;
    }
}
