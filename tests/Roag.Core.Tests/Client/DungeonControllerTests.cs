using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Roag.Client;
using Xunit;

namespace Roag.Core.Tests.Client;

public sealed class DungeonControllerTests
{
    private static GamePadState Pad(params Buttons[] buttons) => new(Vector2.Zero, Vector2.Zero, 0, 0, buttons);

    [Fact]
    public void HoldingDpadOrStickDoesNotRepeatPaidMovementAndReleaseAllowsAnotherPress()
    {
        var neutral = Pad(); var right = Pad(Buttons.DPadRight);
        Assert.Contains(Keys.D, DungeonController.GameplayKeys(right, neutral, false));
        Assert.Empty(DungeonController.GameplayKeys(right, right, false));
        Assert.Empty(DungeonController.GameplayKeys(neutral, right, false));
        Assert.Contains(Keys.D, DungeonController.GameplayKeys(right, neutral, false));
        var stick = new GamePadState(new Vector2(.8f, .1f), Vector2.Zero, 0, 0);
        Assert.Equal(new[] { Keys.D }, DungeonController.GameplayKeys(stick, neutral, false));
        Assert.Empty(DungeonController.GameplayKeys(stick, stick, false));
        Assert.Empty(DungeonController.GameplayKeys(new GamePadState(new Vector2(.1f, .2f), Vector2.Zero, 0, 0), neutral, false));
    }

    [Fact]
    public void OriginalActionButtonsAndNativeGuardUseTheSameValidatedKeyboardPath()
    {
        foreach (var (button, key) in new[] { (Buttons.A, Keys.Enter), (Buttons.B, Keys.Escape), (Buttons.X, Keys.F), (Buttons.Y, Keys.R),
            (Buttons.RightShoulder, Keys.X), (Buttons.LeftShoulder, Keys.G), (Buttons.RightStick, Keys.U), (Buttons.LeftStick, Keys.Enter) })
            Assert.Equal(new[] { key }, DungeonController.GameplayKeys(Pad(button), Pad(), false));
        Assert.Equal(new[] { Keys.P }, DungeonController.GameplayKeys(Pad(Buttons.LeftShoulder), Pad(), true));
        Assert.Equal(new[] { Keys.W, Keys.D }, DungeonController.GameplayKeys(Pad(Buttons.DPadUp, Buttons.DPadRight), Pad(), true));
        Assert.Empty(DungeonController.GameplayKeys(default, Pad(), false));
    }
}
