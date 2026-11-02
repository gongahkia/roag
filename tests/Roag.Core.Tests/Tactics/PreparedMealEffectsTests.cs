using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void PreparedStewReducesActualNativeDamageAndExpiresOnTheWorldClock()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Hero.Preparations.Add(new("camp.mushroom-stew", "armor", 2, 1200));
        int hp = a.Hero.Health; a.Hit(a.Hero, 5);
        Assert.Equal(hp - 3, a.Hero.Health);
        a.World.State.Tick = 1200;
        hp = a.Hero.Health; a.Hit(a.Hero, 5);
        Assert.Equal(hp - 5, a.Hero.Health);
    }

    [Fact]
    public void PreparedBrothReducesOngoingBurnButNotDirectFireAndKeepsItsCommittedSchedule()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Hero.Preparations.Add(new("camp.apple-broth", "burn-resist", 2, 1800));
        var burn = Activation(a, a.Enemy, "core:boon/emberbrand", 3, a.Hero);
        Apply(a, burn, BoonEffectKind.ApplyStatus);
        long due = a.Hero.Tactics!.Burning!.Due; int hp = a.Hero.Health;
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(hp - 1, a.Hero.Health);
        Assert.Equal(due + 100, a.Hero.Tactics.Burning!.Due);
        hp = a.Hero.Health;
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Enemy.Id, a.Enemy.Position, [a.Hero.Position],
            "test:direct-fire", AttackDelivery.Melee, DamageElement.Fire, 3, a.Combat.Clock));
        Assert.Equal(hp - 3, a.Hero.Health);
    }

    [Fact]
    public void AutomaticEatingUsesProvisionsBeforePreparedServingsAndCanEatGloballySharedMealsWhenTheyRunOut()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Position = a.Enemy.Position with { Floor = 1 };
        a.Hero.Body.Hunger = 70; a.World.State.Inventory["food"] = 1;
        a.Combat.State.PreparedMeals["camp.mushroom-stew"] = 1;
        a.Combat.State.PreparedMeals["camp.apple-broth"] = 1;
        Assert.Same(a.Hero, a.Combat.AdvanceToPartyDecision());
        Assert.Equal(200, a.Combat.Clock); Assert.Equal(0, a.Hero.Body.Hunger);
        Assert.Equal(0, a.World.Count("food"));
        Assert.False(a.Combat.State.PreparedMeals.ContainsKey("camp.apple-broth"));
        Assert.Equal(1, a.Combat.State.PreparedMeals["camp.mushroom-stew"]);
        Assert.Equal(2, a.World.PreparationPower(a.Hero, "burn-resist"));
        Assert.Equal(0, a.World.PreparationPower(a.Hero, "armor"));
    }
}
