using Microsoft.Xna.Framework;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed record DungeonEffect(WorldPosition Position, string Kind, string Label, double Born,
    long Source, long Target, Vector2 Direction, WorldPosition? From, bool PlayerHit, string Ability);
internal sealed record DungeonReward(string Boon, double Born);

/// <summary>Transient presentation state. Never enters the simulation or its save.</summary>
internal sealed class DungeonFeedback
{
    private readonly List<DungeonEffect> _effects = [];
    public IReadOnlyList<DungeonEffect> Effects => _effects;
    public double Time { get; private set; }
    public double LastHit { get; private set; } = -10;
    public float ShakeStrength { get; private set; }
    public double ChestOpened { get; private set; } = -10;
    public DungeonReward? Reward { get; private set; }

    public void Advance(double elapsed, bool focused, int floor)
    {
        if (focused) Time += Math.Clamp(elapsed, 0, .1);
        _effects.RemoveAll(e => Time - e.Born > 1.1 || e.Position.Floor != floor);
        if (Reward != null && Time - Reward.Born > 2) Reward = null;
    }

    public void OpenChest() => ChestOpened = Time;

    public void Observe(TacticalEvent e, string? label, Facing facing, WorldPosition? visibleSource, bool playerHit)
    {
        if (e.Kind == "DamageTaken" && e.Amount > 0)
        { LastHit = Time; ShakeStrength = playerHit ? 4 : 1.7f; }
        if (e.Kind == "BoonGranted" && e.Ability.StartsWith("core:boon/", StringComparison.Ordinal))
            Reward = new(e.Ability, Time);
        if (label == null && e.Kind is not ("AttackPerformed" or "ChestClaimed" or "VaultOpened")) return;
        var (x, y) = AttackGeometry.Offset(facing);
        _effects.Add(new(e.Position, e.Kind, label ?? "", Time, e.Source, e.Target,
            Vector2.Normalize(new(x, y)), visibleSource, playerHit, e.Ability));
        if (_effects.Count > 64) _effects.RemoveRange(0, _effects.Count - 64);
    }

    public Vector2 Shake(PresentationPreferences preferences)
    {
        double age = Time - LastHit;
        if (!preferences.Shake || preferences.ReducedMotion || age < 0 || age > .22) return Vector2.Zero;
        float strength = ShakeStrength * (float)Math.Pow(1 - age / .22, 2);
        return new(MathF.Sin((float)age * 117) * strength, MathF.Cos((float)age * 89) * strength);
    }

    public void Reset()
    { _effects.Clear(); Reward = null; Time = 0; LastHit = ChestOpened = -10; ShakeStrength = 0; }
}
