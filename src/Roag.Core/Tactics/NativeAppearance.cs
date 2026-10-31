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
}
