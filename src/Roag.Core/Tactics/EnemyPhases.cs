using Roag.Core.World;

namespace Roag.Core.Tactics;

internal static class EnemyPhases
{
    public const int HealthPercent = 50, SpeedBonus = 25;

    public static bool Supported(string definition) => EnemyCatalog.Default.Contains(definition)
        && EnemyCatalog.Default.Get(definition).Ai is EnemyBrainKind.Warden or EnemyBrainKind.BiomeElite or EnemyBrainKind.HollowRegent;
}

public sealed partial class TacticalCombat
{
    /// <summary>Original stage/finale reaction, owned by each persistent boss rather than the viewed floor.</summary>
    private void ResolveEnemyPhase(BoonEvent e)
    {
        if (e.Kind != BoonTrigger.DamageTaken || e.TargetId is not { } id
            || Actor(id) is not { Life: LifeState.Alive, Health: > 0, Tactics: { Enemy: { PhaseTwo: false } memory } tactics } boss
            || !EnemyPhases.Supported(tactics.DefinitionId)
            || (long)boss.Health * 100 > (long)boss.MaxHealth * EnemyPhases.HealthPercent) return;
        memory.PhaseTwo = true;
        Emit(tactics.DefinitionId == "core:enemy/hollow_regent" ? "RegentPhaseChanged" : "ElitePhaseChanged",
            boss.Id, boss.Id, e.RootActionId ?? 0, boss.Position, 2);
    }
}
