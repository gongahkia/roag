using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void OwnedFormsPatternsDiceAndVowMatchOriginalPublicPreviewAndCommit()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Tactics/Fixtures/original-attack-boons.json")));
        int decisions = 0; var faces = new HashSet<int>();
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(ClassCatalog.Get("core:actor/" + input.GetProperty("class").GetString()).Kind);
            a.Hero.Health -= 5; a.Friend.Position = new(110, 110);
            var actors = new List<Actor> { a.Hero };
            foreach (var enemy in input.GetProperty("enemies").EnumerateArray())
            {
                var actor = actors.Count == 1 ? a.Enemy : new Actor
                { Id = a.World.State.NextActorId++, Kind = ActorKind.Raider, Archetype = "bandit", Health = 1 };
                actor.Position = a.Hero.Position.Offset(enemy.GetProperty("dx").GetInt32(), enemy.GetProperty("dy").GetInt32());
                var tactics = new ActorTactics { InnateStats = new(1000, 6, 6,
                    enemy.TryGetProperty("armor", out var armor) ? armor.GetInt32() : 0, 100, 0, 150),
                    DefinitionId = enemy.TryGetProperty("definition", out var definition) ? definition.GetString()! : "core:enemy/mossbound_guard", RegularEnemy = true };
                if (actors.Count == 1) actor.Tactics = tactics;
                else { a.World.State.Actors.Add(actor); a.Combat.Register(actor, tactics, 100000); }
                actor.Health = 1000; actors.Add(actor);
            }
            foreach (var boon in input.GetProperty("boons").EnumerateArray()) Grant(a.Hero, "core:boon/" + boon.GetString());
            a.Combat.State.RandomState = input.GetProperty("seed").GetInt32(); a.World.Reveal();
            int step = 0;
            int Index(long id) => actors.FindIndex(actor => actor.Id == id) + 1;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                var expected = sample.GetProperty("expected")[step++];
                var intent = new TacticalIntent(command.GetProperty("kind").GetString() switch
                {
                    "basic_attack" => TacticalAction.BasicAttack, "sweeping_slash" => TacticalAction.Special,
                    "move" => TacticalAction.Move, "dash" => TacticalAction.Dash, _ => TacticalAction.Wait
                }, Facing.East, Vow: command.TryGetProperty("vow", out var vow) && vow.GetBoolean());
                string before = JsonSerializer.Serialize(a.Combat.State);
                var preview = a.Combat.Preview(intent);
                Assert.True(preview.Valid, name + ": " + preview.Reason);
                Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
                var cells = expected.GetProperty("cells").EnumerateArray().Select(c => new WorldPosition(100 + c.GetProperty("x").GetInt32(), 100 + c.GetProperty("y").GetInt32()));
                Assert.True(cells.SequenceEqual(preview.Cells), $"{name}, decision {step}: preview cells differ");
                var patterns = preview.BoonCells.Select(c => $"{c.BoonId}:{c.Position.X - 100}:{c.Position.Y - 100}:{c.Condition.ToString().ToLowerInvariant()}:{c.Delay ?? 0}");
                Assert.Equal(expected.GetProperty("patterns").EnumerateArray().Select(c => c.GetString()), patterns);
                long? Optional(string key) => expected.GetProperty(key).ValueKind == JsonValueKind.Null ? null : expected.GetProperty(key).GetInt64();
                Assert.Equal(Optional("damage"), preview.Damage); Assert.Equal(Optional("fate_power"), preview.FatePower);
                Assert.Equal(Optional("vow_hp"), preview.Vow?.HealthCost); Assert.Equal(expected.GetProperty("cost").GetInt64(), preview.Recovery);
                long first = a.Combat.State.NextEvent;
                Assert.True(a.Combat.Commit(intent).Success); a.Next();
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first && e.Kind is
                    "BoonActivated" or "DamageTaken" or "DamageBlocked" or "HitConfirmed" or "Healed" or "FateRolled" or "VowPaid")
                    .Select(e => e.Kind switch
                    {
                        "BoonActivated" => $"{e.Kind}:{Index(e.Source)}:{e.Ability}",
                        "FateRolled" or "VowPaid" => $"{e.Kind}:{Index(e.Source)}:{e.Amount}",
                        _ => $"{e.Kind}:{Index(e.Source)}:{Index(e.Target)}:{e.Amount}"
                    }).ToArray();
                var original = expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()!).ToArray();
                Assert.True(original.SequenceEqual(trace), $"{name}, decision {step}\nOriginal: {string.Join(", ", original)}\nNative: {string.Join(", ", trace)}");
                Assert.Equal(expected.GetProperty("health").EnumerateArray().Select(e => e.GetInt32()), actors.Select(actor => actor.Health));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.True(expected.GetProperty("random").GetInt32() == a.Combat.State.RandomState, $"{name}, decision {step}: RNG differs");
                foreach (var e in a.Combat.State.Events.Where(e => e.Kind == "FateRolled")) faces.Add(e.Amount);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(59, decisions); Assert.Equal(Enumerable.Range(1, 8), faces.Order());
    }

    [Fact]
    public void VowIsExplicitLeavesOneHpAndInvalidCommandsSpendNothing()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Position = new(101, 100);
        var vowed = new TacticalIntent(TacticalAction.BasicAttack, Facing.East, Vow: true);
        Assert.False(a.Combat.Preview(vowed).Valid);
        Grant(a.Hero, "core:boon/pilgrims_vow");
        int hp = a.Hero.Health; a.Act(TacticalAction.BasicAttack); Assert.Equal(hp, a.Hero.Health); a.Next();
        a.Hero.Health = 2;
        string before = JsonSerializer.Serialize(a.Combat.State);
        Assert.False(a.Combat.Commit(vowed).Success);
        Assert.False(a.Combat.Commit(new(TacticalAction.Move, Facing.East, Vow: true)).Success);
        Assert.False(a.Combat.Commit(new(TacticalAction.Guard, Facing.East, Vow: true)).Success);
        Assert.Equal(2, a.Hero.Health); Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
        a.Hero.Health = 3;
        var preview = a.Combat.Preview(vowed); int enemyHp = a.Enemy.Health;
        Assert.Equal(2, preview.Vow!.HealthCost); Assert.Equal(a.Hero.Power + preview.Vow.DamageBonus, (long)preview.Damage!.Value);
        Assert.True(a.Combat.Commit(vowed).Success);
        Assert.Equal(1, a.Hero.Health); Assert.Equal(enemyHp - preview.Damage, a.Enemy.Health);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "DamageTaken" && e.Target == a.Hero.Id);
    }

    [Theory]
    [InlineData(ClassKind.Shade)]
    [InlineData(ClassKind.Trapper)]
    [InlineData(ClassKind.Duelist)]
    [InlineData(ClassKind.Tinkerer)]
    [InlineData(ClassKind.Stormcaller)]
    [InlineData(ClassKind.Spiritbinder)]
    public void PlacementParryAndDischargeNeitherRollDiceNorAcceptVow(ClassKind kind)
    {
        using var a = new Arena(kind);
        Grant(a.Hero, "core:boon/fateful_dice"); Grant(a.Hero, "core:boon/pilgrims_vow");
        if (kind == ClassKind.Stormcaller) a.Hero.Tactics!.Resources.StormCharge = 1;
        var intent = new TacticalIntent(TacticalAction.Special, Facing.East);
        Assert.True(a.Combat.Preview(intent).Valid);
        Assert.Null(a.Combat.Preview(intent).FatePower); Assert.False(a.Combat.Preview(intent with { Vow = true }).Valid);
        int random = a.Combat.State.RandomState, hp = a.Hero.Health;
        Assert.True(a.Combat.Commit(intent).Success);
        Assert.Equal(random, a.Combat.State.RandomState); Assert.Equal(hp, a.Hero.Health);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind is "FateRolled" or "VowPaid");
    }

    [Fact]
    public void DiceMissKeepsRecoveryAndVowPaymentButSkipsCriticalRngProcsAndHookPull()
    {
        using var a = new Arena(ClassKind.Hookrunner);
        Grant(a.Hero, "core:boon/fateful_dice"); Grant(a.Hero, "core:boon/pilgrims_vow"); Grant(a.Hero, "core:boon/temporal_echo");
        a.Hero.Tactics!.Build = a.Hero.Tactics.Build! with
        { Level = 21, Experience = 1050, Stats = a.Hero.Tactics.Stats with { CriticalChancePartsPerMillion = 1_000_000 } };
        int seed = Enumerable.Range(1, 1000).First(s => new RoagRandom(s).NextOneBased(8) == 8);
        var random = new RoagRandom(seed); Assert.Equal(8, random.NextOneBased(8)); a.Combat.State.RandomState = seed;
        var at = a.Enemy.Position; int hp = a.Enemy.Health;
        var intent = new TacticalIntent(TacticalAction.Special, Facing.East, Vow: true);
        long due = a.Combat.Preview(intent).Recovery;
        Assert.True(a.Combat.Commit(intent).Success);
        Assert.Equal(random.State, a.Combat.State.RandomState); Assert.Equal(a.Hero.MaxHealth - 2, a.Hero.Health);
        Assert.Equal(hp, a.Enemy.Health); Assert.Equal(at, a.Enemy.Position);
        Assert.Empty(a.Combat.State.BoonEffects.Echoes);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind is "BoonActivated" or "HitConfirmed" or "Displaced");
        Assert.Equal(due, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Hero.Id).Due);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("ordinary", 0)]
    [InlineData("guard", 100)]
    [InlineData("ward", 5)]
    [InlineData("special", 196)]
    [InlineData("exempt", 196)]
    [InlineData("invulnerable", 200)]
    public void SkullExecutionRespectsEnemyEligibilityAndExistingDamageProtection(string protection, int health)
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Position = new(101, 100); a.Enemy.Tactics!.RegularEnemy = protection != "special";
        a.Enemy.Tactics.RewardExempt = protection == "exempt";
        a.Enemy.Tactics.Invulnerable = protection == "invulnerable";
        a.Enemy.Tactics.Guarding = protection == "guard"; a.Enemy.Tactics.Facing = Facing.West;
        if (protection == "ward")
        {
            Grant(a.Enemy, "core:boon/beggars_cap");
            a.Enemy.Tactics.Boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 5, 100, 0);
        }
        Grant(a.Hero, "core:boon/fateful_dice"); a.Combat.State.RandomState = 1; a.Act(TacticalAction.BasicAttack);
        Assert.Equal(7, a.Hero.Tactics!.Boons.Memory["core:boon/fateful_dice"].Face);
        Assert.Equal(health, a.Enemy.Health);
        Assert.Equal(health == 0 ? LifeState.Dead : LifeState.Alive, a.Enemy.Life);
        Assert.Equal(protection != "ordinary", a.Combat.State.Events.Any(e => e.Kind == "FateSkullResisted"));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void DuplicateClassMembersKeepTheirOwnAttackFormsDiceAndVowEligibility()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Position = new(101, 100); a.Friend.Position = new(100, 101);
        Grant(a.Hero, "core:boon/longblade_geometry"); Grant(a.Hero, "core:boon/fateful_dice"); Grant(a.Hero, "core:boon/pilgrims_vow");
        var intent = new TacticalIntent(TacticalAction.BasicAttack, Facing.East);
        Assert.Equal(2, a.Combat.Preview(intent).Cells.Count);
        a.Combat.State.RandomState = 8; a.Act(TacticalAction.BasicAttack);
        var memory = a.Hero.Tactics!.Boons.Memory["core:boon/fateful_dice"];
        a.Next(a.Friend.Id);
        var preview = a.Combat.Preview(intent);
        Assert.Single(preview.Cells); Assert.Null(preview.FatePower); Assert.Null(preview.Vow);
        Assert.False(a.Combat.Preview(intent with { Vow = true }).Valid);
        int random = a.Combat.State.RandomState; a.Act(TacticalAction.BasicAttack);
        Assert.Equal(random, a.Combat.State.RandomState); Assert.Empty(a.Friend.Tactics!.Boons.Memory);
        Assert.Equal(memory, a.Hero.Tactics.Boons.Memory["core:boon/fateful_dice"]);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "FateRolled");
    }

    [Fact]
    public void HeavyFaceAndVowBonusesApplyBeforeTheCriticalMultiplier()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Position = new(101, 100);
        Grant(a.Hero, "core:boon/fateful_dice"); Grant(a.Hero, "core:boon/pilgrims_vow");
        a.Hero.Tactics!.Build = a.Hero.Tactics.Build! with
        { Level = 21, Experience = 1050, Stats = a.Hero.Tactics.Stats with { CriticalChancePartsPerMillion = 1_000_000 } };
        a.Combat.State.RandomState = 6;
        Assert.True(a.Combat.Commit(new(TacticalAction.BasicAttack, Facing.East, Vow: true)).Success);
        Assert.Equal(2, a.Hero.Tactics.Boons.Memory["core:boon/fateful_dice"].Face);
        Assert.Equal(188, a.Enemy.Health); // (4 base + 1 heavy + 3 vow) * 150% = 12.
        Assert.Equal(a.Hero.MaxHealth - 2, a.Hero.Health);
        Assert.Equal(1095634764, a.Combat.State.RandomState); // One die draw, then one critical draw.
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void PaidDiceVowAndModifiedEchoesContinueExactlyAfterActualWorldSave()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Position = new(101, 100);
        foreach (string id in new[] { "fateful_dice", "pilgrims_vow", "longblade_geometry", "temporal_echo" }) Grant(a.Hero, "core:boon/" + id);
        Grant(a.Friend, "core:boon/fateful_dice");
        a.Combat.State.RandomState = 5; // Burning face, leaving both a status and a modified echo pending.
        Assert.True(a.Combat.Commit(new(TacticalAction.BasicAttack, Facing.East, Vow: true)).Success);
        Assert.NotNull(a.Enemy.Tactics!.Burning);
        Assert.Equal(2, Assert.Single(a.Combat.State.BoonEffects.Echoes).Value.Activation.Event.TargetCells.Length);
        string directory = Path.Combine(Path.GetTempPath(), "roag-attack-boons-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json"); a.World.Save(slot);
            using var resumed = WorldSession.Load(slot); var combat = new TacticalCombat(resumed, _ => { });
            for (int i = 0; i < 8; i++)
            {
                var owner = a.Combat.AdvanceToDecision()!; Assert.Equal(owner.Id, combat.AdvanceToDecision()!.Id);
                var intent = new TacticalIntent(TacticalAction.BasicAttack, Facing.East,
                    Vow: owner.Id == a.Hero.Id && owner.Health > 2);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.Preview(intent)), JsonSerializer.Serialize(combat.Preview(intent)));
                Assert.True(a.Combat.Commit(intent).Success); Assert.True(combat.Commit(intent).Success);
            }
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(combat.State));
            foreach (var actor in new[] { a.Hero, a.Friend, a.Enemy })
            {
                var other = resumed.State.Actors.Single(a => a.Id == actor.Id);
                Assert.Equal(actor.Health, other.Health); Assert.Equal(JsonSerializer.Serialize(actor.Tactics), JsonSerializer.Serialize(other.Tactics));
            }
            TacticalValidation.Validate(resumed.Plan, resumed.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
