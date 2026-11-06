using Roag.Core.Dungeons;
using Roag.Core.Profiles;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Profiles;

public sealed class NativeComplexProfileTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void CanonicalRegentComplexVictoryRequiresItsOwnDefeatAndAConsciousSurvivor(bool defeated, bool survive)
    {
        string directory = Path.Combine(Path.GetTempPath(), "roag-complex-profile-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var world = WorldSession.CreateNativeCampaign(42);
            var parcel = DungeonParcelPlanner.Reserve(world.Plan, 1).Single();
            var complex = world.InstallDungeonComplex(DungeonComplexPlanner.Generate(world.Plan, parcel));
            world.EnablePermanentProfile(Path.Combine(directory, "campaign.json"));
            var finale = complex.Run.Floors.Single(f => f.Stage == 4); var combat = world.DungeonCombat; var hero = world.Party.Single();
            var boss = world.ActorById(finale.Boss);
            Assert.NotEqual(boss.Id, world.State.Endgame.ActorId); Assert.False(world.State.Endgame.Victory);
            world.RecordComplexVictory(boss.Id); Assert.DoesNotContain(AchievementCatalog.Prefix + "regents_fall", combat.State.Profile!.Earned);
            void Hit(long source, WorldPosition position) => combat.ResolveAttack(new(combat.State.NextAction++, source,
                world.ActorById(source).Position, [position], "test:complex-profile", AttackDelivery.Area, DamageElement.Physical,
                10000, combat.Clock, IgnoresTerrain: true));
            if (!survive) Hit(boss.Id, hero.Position);
            if (defeated)
            {
                // Real boss-death settlement supplies the owning receipt. When
                // no conscious member survives, only environmental damage acts.
                if (survive) Hit(hero.Id, boss.Position);
                else combat.ResolveAttack(new(combat.State.NextAction++, 0, boss.Position, [boss.Position], "test:complex-environment",
                    AttackDelivery.Area, DamageElement.Physical, 10000, combat.Clock, IgnoresTerrain: true));
            }
            Assert.Equal(defeated ? LifeState.Dead : LifeState.Alive, boss.Life);
            world.RecordComplexVictory(boss.Id);
            Assert.Equal(defeated && survive, combat.State.Profile.Earned.Contains(AchievementCatalog.Prefix + "regents_fall"));
            Assert.False(world.State.Endgame.Victory);
            world.FlushPermanentProfile();
            Assert.Equal(defeated && survive, world.PermanentAchievements.Contains(AchievementCatalog.Prefix + "regents_fall"));
            TacticalValidation.Validate(world.Plan, world.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
