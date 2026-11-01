using Roag.Core.World;

namespace Roag.Reference;

/// <summary>Explicit opt-in for archived prototype tests. Never used by Roag.Client.</summary>
public static class PrototypeReference
{
    public static void Install() => WorldSession.PrototypeCombatFactory = world => new PrototypeRoagCombat(world);
}
