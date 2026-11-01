using Microsoft.Xna.Framework;
using Roag.Core.Tactics;

namespace Roag.Client;

internal static class DungeonClassCards
{
    public static Rectangle Bounds(Rectangle panel, int index)
    {
        int width = (panel.Width - 64) / 3;
        int height = (panel.Height - 156) / 5;
        return new(panel.X + 20 + index % 3 * (width + 12), panel.Y + 100 + index / 3 * (height + 8), width, height);
    }

    public static string Description(ClassKind kind) => kind switch
    {
        ClassKind.Adventurer => "Versatile swordfighter.\nSweep nearby crowds.",
        ClassKind.Ranger => "Fight at bow range.\nPierce lines; dash away.",
        ClassKind.Arcanist => "Cast bolts and sigils.\nBurn groups from afar.",
        ClassKind.Berserker => "Slow, crushing blows.\nCleave through crowds.",
        ClassKind.Sentinel => "Armored frontliner.\nStrike surrounding foes.",
        ClassKind.Alchemist => "Throw volatile vials.\nBlast clustered foes.",
        ClassKind.Shade => "Distract with a decoy.\nSwap into its shadow.",
        ClassKind.Hookrunner => "Hook enemies closer.\nStrike and sidestep.",
        ClassKind.Trapper => "Set a snare for foes.\nControl their approach.",
        ClassKind.Duelist => "Parry a coming blow.\nAnswer with a riposte.",
        ClassKind.Geomancer => "Raise a stone bulwark.\nShape cover and shots.",
        ClassKind.Spiritbinder => "Command a bound spirit.\nFight from two places.",
        ClassKind.Tinkerer => "Deploy an armed sentry.\nLet it cover your moves.",
        ClassKind.Stormcaller => "Build lightning charge.\nDischarge; ground it.",
        ClassKind.Wayfinder => "Plant a return beacon.\nScout, strike, return.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
