namespace Roag.Core.Tactics;

/// <summary>Semantic bindings authored in the retained universal-atlas mapping, never source-art indices.</summary>
public static class NativeAppearance
{
    public static string ForClass(ClassKind kind) => "roag-class-" + ClassCatalog.Get(kind).Key.Replace('_', '-');
    public static string ForEnemy(string definition)
    {
        var enemy = EnemyCatalog.Default.Get(definition);
        return "roag-enemy-" + enemy.Id["core:enemy/".Length..].Replace('_', '-');
    }

    public static string ForService(DungeonServiceKind kind) => kind switch
    {
        DungeonServiceKind.OfferingFont => "shrine", DungeonServiceKind.CharmWell => "well",
        DungeonServiceKind.AttunementForge => "anvil", DungeonServiceKind.TravellingTrader => "merchant",
        DungeonServiceKind.Memorial => "grave", DungeonServiceKind.Camp => "campfire",
        DungeonServiceKind.CleanseFlask => "potion", DungeonServiceKind.EscapeScroll or DungeonServiceKind.ArchiveContract => "book",
        DungeonServiceKind.ReturnToken or DungeonServiceKind.RouteSign => "banner",
        DungeonServiceKind.Bell => "roag-service-bell", DungeonServiceKind.BellClue => "book",
        DungeonServiceKind.FallingFixture => "boulder", _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
