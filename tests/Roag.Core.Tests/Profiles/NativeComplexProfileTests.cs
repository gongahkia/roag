using Roag.Core.Content;
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
    public void CanonicalRegentComplexVictoryRequiresItsOwnSettledReceiptAndAConsciousSurvivor(bool completed, bool survive)
    {
        string directory = Path.Combine(Path.GetTempPath(), "roag-complex-profile-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var world = WorldSession.CreateNativeCampaign(42);
            var parcel = DungeonParcelPlanner.Reserve(world.Plan, 1).Single();
            var complex = world.InstallDungeonComplexTerrain(DungeonComplexPlanner.GenerateGeometry(world.Plan, parcel));
            world.EnablePermanentProfile(Path.Combine(directory, "campaign.json"));
            var finale = complex.Run.Floors.Single(f => f.Stage == 4); var combat = world.DungeonCombat; var hero = world.Party.Single();
            var boss = ActorFactory.Create(world.Content, world.Content.Game.Raider, world.Seed, world.State.NextActorId++, finale.Exit, 0);
            boss.CombatTrait = ""; boss.Talents.Clear(); world.State.Actors.Add(boss);
            combat.RegisterEnemy(boss, "core:enemy/hollow_regent", combat.Clock + 100000); boss.Health = boss.MaxHealth;
            complex.Run.Floors[complex.Run.Floors.IndexOf(finale)] = finale with { Boss = boss.Id };
            Assert.NotEqual(boss.Id, world.State.Endgame.ActorId); Assert.False(world.State.Endgame.Victory);
            world.RecordComplexVictory(boss.Id); Assert.DoesNotContain(AchievementCatalog.Prefix + "regents_fall", combat.State.Profile!.Earned);
            void Hit(long source, WorldPosition position) => combat.ResolveAttack(new(combat.State.NextAction++, source,
                world.ActorById(source).Position, [position], "test:complex-profile", AttackDelivery.Area, DamageElement.Physical,
                10000, combat.Clock, IgnoresTerrain: true));
            if (!survive) Hit(boss.Id, hero.Position);
            Hit(hero.Id, boss.Position); // A dead/downed source cannot invent a kill; use the world's real environment below when downed.
            if (!survive)
                combat.ResolveAttack(new(combat.State.NextAction++, 0, boss.Position, [boss.Position], "test:complex-environment",
                    AttackDelivery.Area, DamageElement.Physical, 10000, combat.Clock, IgnoresTerrain: true));
            Assert.Equal(LifeState.Dead, boss.Life);
            if (completed)
            {
                complex.Run.BossExitReceipts.Add(boss.Id); complex.Run.Victory = true;
            }
            world.RecordComplexVictory(boss.Id);
            Assert.Equal(completed && survive, combat.State.Profile.Earned.Contains(AchievementCatalog.Prefix + "regents_fall"));
            Assert.False(world.State.Endgame.Victory);
            world.FlushPermanentProfile();
            Assert.Equal(completed && survive, world.PermanentAchievements.Contains(AchievementCatalog.Prefix + "regents_fall"));
            TacticalValidation.Validate(world.Plan, world.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
