using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Client;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Theory]
    [InlineData("mossbound_guard", false)]
    [InlineData("thornspitter", false)]
    [InlineData("gulp_toad", true)]
    [InlineData("vault_lurker", true)]
    public void EnemyTargetingUsesRoleSpecificDownedPreferences(string definition, bool prefersDowned)
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Ranger);
        EnemyProfile(a, definition); a.Friend.Position = new(102, 100); a.Hit(a.Friend, 1000);
        Assert.Equal(LifeState.Downed, a.Friend.Life);
        Assert.Equal(prefersDowned ? a.Friend.Id : a.Hero.Id, a.Combat.SelectEnemyTarget(a.Enemy.Id));
        a.Hit(a.Hero, 1000);
        Assert.Equal(a.Friend.Id, a.Combat.SelectEnemyTarget(a.Enemy.Id));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void EnemyTargetingUsesHealthPercentageForPreyAndStableIdentityForTies()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Ranger);
        EnemyProfile(a, "ruin_skitter"); a.Hero.Position = new(100, 100); a.Friend.Position = new(102, 100);
        a.Hero.Health = 12; a.Friend.Health = 11; // 50% versus 55%; the nearer Ranger has fewer absolute HP.
        Assert.Equal(a.Hero.Id, a.Combat.SelectEnemyTarget(a.Enemy.Id));
        a.Hero.Position = new(102, 100); a.Friend.Position = new(104, 100);
        a.Hero.Health = a.Hero.MaxHealth; a.Friend.Health = a.Friend.MaxHealth;
        long target = Math.Min(a.Hero.Id, a.Friend.Id);
        Assert.Equal(target, a.Combat.SelectEnemyTarget(a.Enemy.Id));
        a.World.State.Party.Reverse();
        Assert.Equal(target, a.Combat.SelectEnemyTarget(a.Enemy.Id));
    }

    [Fact]
    public void EnemyTargetingClearShotsAccountForOriginalAttackGeometryAndBlockingBodies()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        EnemyProfile(a, "thornspitter"); a.Hero.Position = new(102, 99); a.Friend.Position = new(103, 104);
        Assert.Equal(a.Friend.Id, a.Combat.SelectEnemyTarget(a.Enemy.Id));
        var blocker = RevivalTestEnemy(a, "mossbound_guard", new(103, 102));
        Assert.Equal(a.Hero.Id, a.Combat.SelectEnemyTarget(a.Enemy.Id));
        blocker.Position = new(110, 110);
        Assert.Equal(a.Friend.Id, a.Combat.SelectEnemyTarget(a.Enemy.Id));
        string before = JsonSerializer.Serialize(a.World.State);
        for (int i = 0; i < 20; i++) a.Combat.SelectEnemyTarget(a.Enemy.Id);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void EnemyTargetingUsesEnemyPerceptionAndRejectsUnknownDeadAndOtherFloorMembers()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        EnemyProfile(a, "gulp_toad"); a.Hero.Position = new(100, 100, 1); a.Friend.Position = new(103, 103);
        a.World.Visible.Clear();
        Assert.Equal(a.Friend.Id, a.Combat.SelectEnemyTarget(a.Enemy.Id));
        a.World.Chunks.Edit(new(103, 102), Material.Wall);
        Assert.Null(a.Combat.SelectEnemyTarget(a.Enemy.Id));
        a.World.Chunks.Edit(new(103, 102), Material.Floor);
        a.Hit(a.Friend, 1000);
        Assert.Equal(a.Friend.Id, a.Combat.SelectEnemyTarget(a.Enemy.Id));
        for (int i = 0; i < 5; i++) a.Hit(a.Friend, 1);
        Assert.Equal(LifeState.Dead, a.Friend.Life); Assert.Null(a.Combat.SelectEnemyTarget(a.Enemy.Id));
    }

    [Fact]
    public void EnemyTargetingCommitsTheChosenMemberAndKeepsItsTilesAfterThatMemberMoves()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 50);
        EnemyProfile(a, "thornspitter"); a.Hero.Position = new(102, 99); a.Friend.Position = new(103, 104);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        Assert.Equal(EnemyIntentKind.Windup, a.Combat.CommitEnemyTurn((_, _) => null).Kind);
        var attack = Assert.Single(a.Combat.State.Attacks); Assert.Equal(a.Friend.Id, attack.IntendedTarget);
        Assert.Contains(a.Friend.Position, attack.Cells);
        a.Next(a.Friend.Id); a.Act(TacticalAction.Move, Facing.East);
        Assert.Equal(JsonSerializer.Serialize(attack), JsonSerializer.Serialize(Assert.Single(a.Combat.State.Attacks)));
        int health = a.Friend.Health;
        for (int i = 0; i < 6 && a.Combat.State.Attacks.Count > 0; i++) { a.Next(); a.Act(TacticalAction.Wait); }
        Assert.Empty(a.Combat.State.Attacks); Assert.Equal(health, a.Friend.Health);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void EnemyTargetingWithoutALocalTargetDoesNotInventOrCommitAFallbackAction()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0); EnemyProfile(a, "mossbound_guard");
        a.Hero.Position = new(100, 100, 1); a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<InvalidOperationException>(() => a.Combat.CommitEnemyTurn((_, _) => null));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void EnemyTargetingInspectionExposesTendenciesImmediatelyButHidesUnseenTargetsAndCells()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "thornspitter"); a.Enemy.Position = new(109, 100); a.Friend.Position = new(113, 100);
        for (int y = 98; y <= 102; y++) for (int x = 108; x <= 115; x++) a.World.Chunks.Edit(new(x, y), Material.Floor);
        var friendReady = a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Friend.Id);
        a.Combat.State.Queue.Delay(friendReady.Sequence, 1000);
        a.World.Reveal(); a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        var preferred = a.Combat.InspectEnemy(a.Hero.Id, a.Enemy.Id)!;
        Assert.Contains("usable shot", preferred.Tendency); Assert.False(preferred.Committed);
        Assert.Null(preferred.TargetId); Assert.Null(preferred.TargetName);
        a.Combat.CommitEnemyTurn((_, _) => null);
        Assert.Equal(a.Friend.Id, Assert.Single(a.Combat.State.Attacks).IntendedTarget);
        string before = JsonSerializer.Serialize(a.World.State);
        var inspection = a.World.InspectTacticalEnemy(a.Enemy.Id)!;
        Assert.True(inspection.Committed); Assert.Null(inspection.TargetId); Assert.Null(inspection.TargetName);
        Assert.Equal(new[] { new WorldPosition(110, 100), new(111, 100) }, inspection.Cells);
        var hover = WorldInspection.Inspect(a.World, a.Enemy.Position);
        Assert.Contains("usable shot", hover.Detail); Assert.Contains("Attack locked", hover.Detail);
        Assert.DoesNotContain(a.Friend.Name, hover.Detail);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Hero.Position = new(100, 100, 1);
        Assert.Null(a.Combat.InspectEnemy(a.Hero.Id, a.Enemy.Id));
    }

    [Fact]
    public void EnemyTargetingPreferencesAndCommittedChoiceContinueThroughActualWorldSave()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "thornspitter"); a.Hero.Position = new(102, 99); a.Friend.Position = new(103, 104);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id); a.Combat.CommitEnemyTurn((_, _) => null);
        string directory = Path.Combine(Path.GetTempPath(), "roag-targets-" + Guid.NewGuid().ToString("N"));
        try
        {
            a.World.Save(directory);
            using var loaded = WorldSession.Load(directory); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(a.Combat.SelectEnemyTarget(a.Enemy.Id), resumed.SelectEnemyTarget(a.Enemy.Id));
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.Equal(a.Friend.Id, Assert.Single(resumed.State.Attacks).IntendedTarget);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.InspectEnemy(a.Friend.Id, a.Enemy.Id)),
                JsonSerializer.Serialize(resumed.InspectEnemy(a.Friend.Id, a.Enemy.Id)));
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void EnemyTargetingCatalogRequiresAnExplicitValidPolicyForEveryDefinition()
    {
        Assert.Equal(50, EnemyCatalog.Default.Definitions.Length);
        Assert.All(EnemyCatalog.Default.Definitions, e => Assert.NotEmpty(EnemyTargeting.Description(e.TargetPriority)));
        using var stream = typeof(EnemyCatalog).Assembly.GetManifestResourceStream("Roag.Tactics.Enemies.json")!;
        using var reader = new StreamReader(stream); var root = JsonNode.Parse(reader.ReadToEnd())!;
        var first = root["definitions"]![0]!.AsObject(); first.Remove("target_priority");
        Assert.Throws<InvalidDataException>(() => EnemyCatalog.Load(root.ToJsonString()));
        first["target_priority"] = "unrecognised";
        Assert.Throws<InvalidDataException>(() => EnemyCatalog.Load(root.ToJsonString()));
    }

    [Theory]
    [InlineData("character.rabbit")]
    [InlineData("character.bee")]
    public void EnemyTargetingRespectsRetainedWildlifeHostilityAndNoticeLeashRules(string species)
    {
        using var world = WorldSession.Create(42);
        var animal = WildlifePreview.Prepare(world, species); var home = animal.Habitat!.Value;
        var hero = world.Party.Single(); var friend = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        friend.Recruited = true; world.State.Party.Add(friend.Id);
        var rules = world.Ecology(animal)!;
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= rules.Leash + 2; x++)
            if (x != 0 || y != 0) world.Chunks.Edit(home.Offset(x, y), Material.Floor);
        animal.Position = home.Offset(1, 0); hero.Position = world.State.Position = home.Offset(0, 2);
        friend.Position = home.Offset(rules.Notice + 1, 0);
        var combat = new TacticalCombat(world, _ => { });
        combat.Register(hero, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42) });
        combat.Register(friend, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 43) }); friend.Health = 1;
        combat.RegisterEnemy(animal, "core:enemy/ruin_skitter");
        if (species == "character.rabbit")
        {
            Assert.False(world.IsHostile(animal)); Assert.Null(combat.SelectEnemyTarget(animal.Id));
        }
        else
        {
            Assert.True(world.IsHostile(animal)); Assert.Equal(hero.Id, combat.SelectEnemyTarget(animal.Id));
            animal.ProvokedUntil = world.State.Tick + 75;
            Assert.Equal(friend.Id, combat.SelectEnemyTarget(animal.Id));
            hero.Position = home.Offset(-rules.Leash - 2, 0); friend.Position = home.Offset(rules.Leash + 2, 0);
            Assert.Null(combat.SelectEnemyTarget(animal.Id));
        }
    }
}
