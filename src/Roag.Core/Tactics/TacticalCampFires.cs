using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record NativeCampFire(long Owner, WorldPosition Position, long Until);
public enum CampFireResponse { Indifferent, Avoid, Approach }

public static class CampFireResponses
{
    public static CampFireResponse For(string definition) => definition switch
    {
        "core:enemy/ruin_skitter" or "core:enemy/gulp_toad" or "core:enemy/gloom_strider" or "core:enemy/web_weaver" => CampFireResponse.Avoid,
        "core:enemy/ember_moth" => CampFireResponse.Approach,
        _ => CampFireResponse.Indifferent
    };
    public static string Description(string definition) => For(definition) switch
    {
        CampFireResponse.Avoid => "Uninjured, avoids nearby visible campfires; injury overrides this caution.",
        CampFireResponse.Approach => "Approaches visible campfire light when no prey is detected.",
        _ => "Campfires do not alter its hunting behavior."
    };
}

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewCampFire(Actor actor, bool extinguish)
    {
        if (!Friendly(actor) || !Tuning.EnableSurvival) return Invalid("Campfires require an expedition member.");
        var fire = State.CampFires.FirstOrDefault(f => f.Position == actor.Position && f.Until > Clock);
        if (extinguish ? fire == null : fire != null)
            return Invalid(extinguish ? "Stand on a lit campfire to extinguish it." : "This tile already has a lit campfire.");
        if (!extinguish && (_world.Count("wood") < 1 || State.CampFires.Count(f => f.Until > Clock) >= 8
            || !_world.GroundOpen(actor.Position) || _world.ObjectAt(actor.Position) != null
            || _world.Chunks.At(actor.Position) is Material.StairsDown or Material.StairsUp or Material.Water))
            return Invalid("One wood and clear dry ground are needed; at most eight fires can burn.");
        return new(true, "", ActionTiming.Recovery(Tuning.WaitCost, EffectiveSpeed(actor)), [actor.Position]);
    }

    private void CommitCampFire(Actor actor, bool extinguish, long action)
    {
        State.CampFires.RemoveAll(f => f.Position == actor.Position || f.Until <= Clock);
        if (!extinguish)
        {
            _world.State.Inventory["wood"]--; _world.RecordNativeConsumption(1);
            State.CampFires.Add(new(actor.Id, actor.Position, checked(Clock + (long)State.Camp.FireMinutes * 60 * Tuning.UnitsPerSecond)));
        }
        Emit(extinguish ? "CampFireExtinguished" : "CampFireLit", actor.Id, actor.Id, action, actor.Position);
    }

    private EnemyIntent? CampFireIntent(Actor enemy)
    {
        var response = CampFireResponses.For(enemy.Tactics!.DefinitionId);
        if (response == CampFireResponse.Indifferent || response == CampFireResponse.Avoid && enemy.Health < enemy.MaxHealth) return null;
        int radius = response == CampFireResponse.Avoid ? State.Camp.FireDeterrenceRange : State.Camp.FireAttractionRange;
        var nearby = State.CampFires.Where(f => f.Until > Clock && f.Position.Distance(enemy.Position) <= radius)
            .OrderBy(f => f.Position.Distance(enemy.Position)).ThenBy(f => f.Position.Y).ThenBy(f => f.Position.X).ToArray();
        if (nearby.Length == 0) return null;
        var sight = _world.VisionFrom(enemy.Position); var fire = nearby.FirstOrDefault(f => sight.Contains(f.Position));
        if (fire == null) return null;
        if (response == CampFireResponse.Approach)
        {
            if (ObservedEnemyTarget(enemy) != null) return null;
            if (enemy.Position.Distance(fire.Position) <= 1) return new(EnemyIntentKind.Wait);
            var goals = new HashSet<WorldPosition>();
            foreach (var facing in new[] { Facing.North, Facing.East, Facing.South, Facing.West })
            {
                var (dx, dy) = AttackGeometry.Offset(facing); var p = fire.Position.Offset(dx, dy);
                if (_world.CanEnter(p, enemy.Id)) goals.Add(p);
            }
            var step = _world.Navigate(enemy, goals, p => PursuitAllowed(enemy, p));
            return step == null ? new(EnemyIntentKind.Wait) : new(step.Action == TacticalAction.UseStairs ? EnemyIntentKind.UseStairs : EnemyIntentKind.Move, step.Facing);
        }
        foreach (var facing in new[] { Facing.North, Facing.East, Facing.South, Facing.West })
        {
            var (dx, dy) = AttackGeometry.Offset(facing); var p = enemy.Position.Offset(dx, dy);
            if (p.Distance(fire.Position) > enemy.Position.Distance(fire.Position) && _world.CanEnter(p, enemy.Id)
                && PursuitAllowed(enemy, p) && _world.Chunks.At(p) is not (Material.StairsDown or Material.StairsUp))
                return new(EnemyIntentKind.Move, facing);
        }
        // Cornering removes the escape option; ordinary authored combat remains possible.
        return null;
    }
}
