using Roag.Core.World;

namespace Roag.Core.Tactics;

public static partial class EnemyPlanning
{
    private static EnemyIntent Weaver(EnemyMemory memory, EnemyPlanningContext world)
    {
        var webs = world.Webs ?? throw new ArgumentException("Weaver decisions need existing web facts.");
        var ground = world.GroundOpen ?? throw new ArgumentException("Weaver decisions need ground facts.");
        var reserved = world.ProtectedLanding ?? throw new ArgumentException("Weaver decisions need protected landing facts.");
        if (memory.WebCasts < 2 && webs.Count < EnemyWebRules.Cap && world.Source.Distance(world.Target) <= 5 && ClearLine(world))
            foreach (var span in new[] { new WebSpan(world.Target.Offset(-2, 0), world.Target.Offset(2, 0)),
                new WebSpan(world.Target.Offset(0, -2), world.Target.Offset(0, 2)) })
                if (EnemyWebRules.Fits(span, world.Source, ground, world.Free, reserved, webs))
                    return new(EnemyIntentKind.Windup, Ability: "core:ability/weaver_web", Cells: [.. span.Cells()],
                        Support: new(EnemySupportKind.Web, Web: span));
        if (world.Source.Distance(world.Target) == 1)
            return new(EnemyIntentKind.Windup, Ability: "core:ability/weaver_bite", Cells: [world.Target]);
        var goals = Cardinals.Select(d => world.Target.Offset(d.X, d.Y)).Where(world.Free).ToHashSet();
        return world.NextStep(goals) is { } step ? new(EnemyIntentKind.Move, step) : new(EnemyIntentKind.Wait);
    }
}
