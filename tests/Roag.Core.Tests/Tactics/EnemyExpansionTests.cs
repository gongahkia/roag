using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class EnemyExpansionPlanningTests
{
    [Fact]
    public void AllExpansionDecisionsMatchOriginalSupportMemoryTerrainAndNavigation()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Tactics/Fixtures/original-expansion-decisions.json")));
        var origin = new WorldPosition(1000, 1000, 2); int count = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); var expected = sample.GetProperty("expected");
            var target = origin.Offset(input.GetProperty("dx").GetInt32(), input.GetProperty("dy").GetInt32());
            HashSet<WorldPosition> Cells(string key) => input.GetProperty(key).EnumerateArray()
                .Select(p => origin.Offset(p[0].GetInt32(), p[1].GetInt32())).ToHashSet();
            var walls = Cells("walls"); var mist = Cells("mist"); var barricades = Cells("barricades");
            var neighbors = input.GetProperty("neighbors").EnumerateArray().Select(n => new EnemyNeighbor(n.GetProperty("id").GetInt64(),
                origin.Offset(n.GetProperty("dx").GetInt32(), n.GetProperty("dy").GetInt32()), n.GetProperty("hp").GetInt32(),
                n.GetProperty("max_hp").GetInt32(), n.GetProperty("ally").GetBoolean())).Append(new(1, target, 30, 32, false)).ToArray();
            bool Open(WorldPosition p) => p.Floor == origin.Floor && Math.Abs(p.X - origin.X) <= 8 && Math.Abs(p.Y - origin.Y) <= 8
                && !walls.Contains(p) && !barricades.Contains(p);
            bool Free(WorldPosition p) => Open(p) && !neighbors.Any(n => n.Position == p);
            var memory = new EnemyMemory { Alerted = input.GetProperty("alerted").GetBoolean(), Retaliate = input.GetProperty("retaliate").GetBoolean(), Summons = input.GetProperty("summons").GetInt32() };
            var aim = input.GetProperty("decoy").GetBoolean() ? origin.Offset(-4, 0) : target;
            string before = JsonSerializer.Serialize(memory);
            var intent = EnemyPlanning.Choose(EnemyCatalog.Default.Get(input.GetProperty("id").GetString()!), memory,
                new(origin, aim, Free, Open, mist.Contains, goals => EnemyPlanningTests.FirstStep(origin, goals, Free), barricades.Contains, neighbors));
            Assert.True(expected.GetProperty("kind").GetString() == intent.Kind.ToString().ToLowerInvariant(), $"Case {count}: {input}");
            if (intent.Kind == EnemyIntentKind.Move)
                Assert.Equal((expected.GetProperty("dx").GetInt32(), expected.GetProperty("dy").GetInt32()), AttackGeometry.Offset(intent.Facing));
            if (intent.Kind == EnemyIntentKind.Windup)
            {
                Assert.Equal(expected.GetProperty("ability").GetString(), intent.Ability);
                Assert.Equal(expected.GetProperty("delivery").GetString(), JsonNamingPolicy.SnakeCaseLower.ConvertName(intent.Delivery.ToString()));
                Assert.Equal(expected.GetProperty("cells").EnumerateArray().Select(p => origin.Offset(p.GetProperty("x").GetInt32(), p.GetProperty("y").GetInt32())), intent.Cells);
            }
            Assert.Equal(expected.GetProperty("bonus").GetInt32(), intent.DamageBonus);
            Assert.Equal(expected.GetProperty("alert").GetBoolean(), intent.Alert);
            Assert.Equal(expected.GetProperty("clear_retaliation").GetBoolean(), intent.ClearRetaliation);
            var support = expected.GetProperty("support"); var advance = expected.GetProperty("advance");
            if (support.ValueKind == JsonValueKind.Null) Assert.Null(intent.Support);
            else
            {
                int Number(string key) => support.GetProperty(key).ValueKind == JsonValueKind.Null ? 0 : support.GetProperty(key).GetInt32();
                Assert.Equal(new EnemySupport(support.GetProperty("kind").GetString() == "heal" ? EnemySupportKind.Heal : EnemySupportKind.Summon,
                    Number("power"), support.GetProperty("definition").GetString() ?? "", Number("maximum_active")), intent.Support);
            }
            if (advance.ValueKind == JsonValueKind.Null) Assert.Null(intent.Advance);
            else
            {
                AttackGeometry.TryFacing(advance.GetProperty("dx").GetInt32(), advance.GetProperty("dy").GetInt32(), out var facing);
                Assert.Equal(new EnemyAdvance(facing, advance.GetProperty("steps").GetInt32()), intent.Advance);
            }
            Assert.Equal(before, JsonSerializer.Serialize(memory)); count++;
        }
        Assert.Equal(3840, count);
    }
}

public sealed partial class ClassActionTests
{
    private static LockedAttack BeginExpansion(Arena a, string definition)
    {
        EnemyProfile(a, definition); a.Act(TacticalAction.Wait);
        Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        return Assert.Single(a.Combat.State.Attacks);
    }
    private static void FinishExpansion(Arena a, LockedAttack attack)
    {
        a.Next();
        for (int i = 0; a.Combat.Clock < attack.Due; i++)
        { Assert.True(i < 20); a.Act(TacticalAction.Wait); a.Next(); }
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ExpansionMenderHealsTheLockedTileAndNeverDamagesAnEnemyOrPartyMember()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Friend.Position = new(102, 100);
        a.Combat.RegisterEnemy(a.Friend, "core:enemy/mossbound_guard", 10000); a.Friend.Health = 2;
        var attack = BeginExpansion(a, "cinder_mender");
        Assert.Equal(EnemySupportKind.Heal, attack.Support!.Kind); Assert.Equal(new[] { a.Friend.Position }, attack.Cells);
        FinishExpansion(a, attack);
        Assert.Equal(5, a.Friend.Health); Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "Healed" && e.Source == a.Enemy.Id && e.Target == a.Friend.Id && e.Amount == 3);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "DamageTaken");
    }

    [Fact]
    public void ExpansionHealingDoesNotTrackAMovingAllyOrHealTheParty()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Friend.Position = new(102, 100);
        a.Combat.RegisterEnemy(a.Friend, "core:enemy/mossbound_guard", 10000); a.Friend.Health = 2;
        var attack = BeginExpansion(a, "cinder_mender");
        a.Friend.Position = new(102, 102); a.Hero.Position = new(102, 100); a.Hero.Health = 10;
        FinishExpansion(a, attack);
        Assert.Equal(2, a.Friend.Health); Assert.Equal(10, a.Hero.Health);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "Healed");
    }

    [Fact]
    public void ExpansionConjurerCreatesPersistentNativeMinionsWithAuthoredStatsAndRewards()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        var attack = BeginExpansion(a, "forge_conjurer");
        Assert.Equal(EnemySupportKind.Summon, attack.Support!.Kind);
        FinishExpansion(a, attack);
        var child = Assert.Single(a.World.State.Actors, x => x.Tactics?.Enemy?.SummonedBy == a.Enemy.Id);
        Assert.Equal("core:enemy/ruin_skitter", child.Tactics!.DefinitionId);
        Assert.Equal(4, child.MaxHealth); Assert.Equal(160, child.Speed); Assert.Equal("spider", child.SpriteId);
        Assert.Equal(1, a.Enemy.Tactics!.Enemy!.Summons);
        Assert.Contains(child.Id, a.Combat.State.Participants);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "EnemySummoned" && e.Target == child.Id);
        a.Hit(child, 1000, source: a.Hero);
        Assert.Equal(3, a.Hero.Tactics!.Build!.Experience);
        Assert.Contains(a.Combat.State.Loot.Drops, d => d.DefeatedActor == child.Id && d.Amount == 2);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ExpansionSummoningRespectsOccupiedLandingsAndLifetimeLimit()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        var attack = BeginExpansion(a, "forge_conjurer");
        a.Friend.Position = attack.Cells[0]; FinishExpansion(a, attack);
        Assert.Equal(0, a.Enemy.Tactics!.Enemy!.Summons);
        Assert.DoesNotContain(a.World.State.Actors, x => x.Tactics?.Enemy?.SummonedBy == a.Enemy.Id);
        a.Enemy.Tactics.Enemy.Summons = 3;
        a.Next(a.Enemy.Id);
        Assert.Null(a.Combat.PreviewEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals)).Support);
    }

    [Fact]
    public void ExpansionChargeUsesLockedDamageTilesThenAdvancesThroughVacatedGround()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Friend.Position = new(110, 110);
        var attack = BeginExpansion(a, "glass_charger");
        Assert.Equal(new EnemyAdvance(Facing.West, 3), attack.Advance);
        a.Hero.Position = new(100, 99); FinishExpansion(a, attack);
        Assert.Equal(new(100, 100), a.Enemy.Position);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.Equal(3, a.Combat.State.Events.Count(e => e.Kind == "TileEntered" && e.Source == a.Enemy.Id));
    }

    [Fact]
    public void ExpansionBreakerDestroysPhysicalBarricadesAtImpactWithPersistentTerrain()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        var wall = new WorldPosition(102, 100); a.World.Chunks.Edit(wall, Material.Barricade);
        Assert.False(a.World.GroundOpen(wall)); Assert.True(Materials.Opaque(a.World.Chunks.At(wall)));
        var attack = BeginExpansion(a, "mossbreaker");
        Assert.Equal("core:ability/mossbreaker_crack", attack.Ability);
        Assert.Equal(Material.Barricade, a.World.Chunks.At(wall));
        FinishExpansion(a, attack);
        Assert.Equal(Material.Floor, a.World.Chunks.At(wall)); Assert.True(a.World.GroundOpen(wall));
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "TerrainBroken" && e.Source == a.Enemy.Id && e.Position == wall);
    }

    [Fact]
    public void ExpansionRetaliationRequiresHostileDamageAndIsConsumedWhenAttackCommits()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "bastion_bulwark"); a.Enemy.Position = new(101, 100);
        a.Hit(a.Enemy, 1, source: a.Hero); Assert.False(a.Enemy.Tactics!.Enemy!.Retaliate);
        a.Hit(a.Enemy, 5, source: a.Hero); Assert.True(a.Enemy.Tactics.Enemy.Retaliate);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        string before = JsonSerializer.Serialize(a.Combat.State);
        Assert.Equal("core:ability/bulwark_retort", a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null).Ability);
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State)); Assert.True(a.Enemy.Tactics.Enemy.Retaliate);
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null); Assert.False(a.Enemy.Tactics.Enemy.Retaliate);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }
}
