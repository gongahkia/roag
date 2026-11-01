using System.Text.Json;
using Roag.Client;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    [Fact]
    public void VaultCommandsMatchAllOriginalSeededOutcomesPaidKeysAndPhysicalRewards()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-vaults.json")));
        int count = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input");
            using var world = Floor(input.TryGetProperty("class", out var kind) ? ClassCatalog.Get("core:actor/" + kind.GetString()).Kind : ClassKind.Adventurer);
            var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
            combat.State.Secrets.Nodes.RemoveAll(s => s.Position.Floor == origin.Floor);
            combat.SpawnSecret(DungeonSecretKind.MossSigil, "test:clue", origin.Offset(-2, 3));
            var vault = combat.SpawnVault("test:original-vault", origin.Offset(4, 0), origin.Offset(-4, 0));
            int vaultIndex = combat.State.Vaults.Nodes.FindIndex(v => v.Id == vault.Id);
            combat.State.Vaults.Nodes[vaultIndex] = vault with { Seed = sample.GetProperty("seed").GetInt32(), KeyCollected = input.TryGetProperty("held", out _) };
            combat.State.RandomState = 37; world.State.Inventory["gold"] = 0; world.Reveal(); int index = 0;
            long nextActor = world.State.NextActorId;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                var intent = new TacticalIntent(TacticalAction.Move, command.GetInt32() > 0 ? Facing.East : Facing.West);
                var expected = sample.GetProperty("expected")[index++]; long first = combat.State.NextEvent;
                string before = JsonSerializer.Serialize(world.State); var preview = combat.Preview(intent);
                Assert.Equal(before, JsonSerializer.Serialize(world.State)); Assert.Equal(expected.GetProperty("valid").GetBoolean(), preview.Valid);
                Assert.Equal(expected.GetProperty("recovery").GetInt64(), preview.Valid ? preview.Recovery : 0);
                Assert.Equal(preview.Valid, world.SubmitDungeon(intent).Success);
                if (!preview.Valid) Assert.Equal(before, JsonSerializer.Serialize(world.State));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), 0), hero.Position);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), hero.Health); Assert.Equal(expected.GetProperty("gold").GetInt32(), world.Count("gold"));
                var current = combat.State.Vaults.Nodes.Single(v => v.Id == vault.Id);
                Assert.Equal(expected.GetProperty("opened").GetBoolean(), current.Opened);
                Assert.Equal(expected.GetProperty("key").GetBoolean(), current.KeyCollected);
                Assert.Equal(expected.GetProperty("held").GetBoolean(), current.KeyCollected && !current.Opened);
                Assert.Equal(expected.GetProperty("outcome").GetString(), current.Outcome?.ToString().ToLowerInvariant() ?? "");
                Assert.Equal(expected.GetProperty("clue").GetString(), current.Clue?.ToString().ToLowerInvariant() ?? "");
                Assert.Equal(expected.GetProperty("currency").GetInt32(), combat.State.Loot.Drops.Where(d => d.Kind == GroundLootKind.Currency).Sum(d => d.Amount));
                Assert.Equal(expected.GetProperty("health").GetInt32(), combat.State.Loot.Drops.Where(d => d.Kind == GroundLootKind.Health).Sum(d => d.Amount));
                Assert.Equal(expected.GetProperty("chests").GetInt32(), combat.State.Chests.Chests.Count(c => c.Marker.StartsWith("review:vault:", StringComparison.Ordinal)));
                Assert.Equal(expected.GetProperty("ambush").GetInt32(), world.State.Actors.Count(a => a.Id >= nextActor && a.Tactics?.Enemy != null));
                Assert.Equal(expected.GetProperty("rng").GetInt32(), combat.State.RandomState);
                foreach (var (key, name) in new[] { ("openings", "VaultOpened"), ("picked", "VaultKeyCollected") })
                    Assert.Equal(expected.GetProperty(key).GetInt32(), combat.State.Events.Count(e => e.Sequence >= first && e.Kind == name));
                TacticalValidation.Validate(world.Plan, world.State); count++;
            }
        }
        Assert.Equal(43, count);
    }

    private static DungeonVault PreparedVault(WorldSession world, DungeonVaultOutcome outcome, bool key = true)
    {
        var combat = world.DungeonCombat; var origin = world.Party.Single().Position;
        var vault = combat.SpawnVault("test:prepared-vault", origin.Offset(4, 0), origin.Offset(-4, 0));
        int index = combat.State.Vaults.Nodes.FindIndex(v => v.Id == vault.Id);
        vault = vault with { Seed = 4 + (int)outcome, KeyCollected = key };
        combat.State.Vaults.Nodes[index] = vault;
        for (int i = 0; i < 3; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        return vault;
    }

    [Theory]
    [InlineData(DungeonVaultOutcome.Cache, false)]
    [InlineData(DungeonVaultOutcome.Cache, true)]
    [InlineData(DungeonVaultOutcome.Clue, true)]
    [InlineData(DungeonVaultOutcome.Boon, true)]
    [InlineData(DungeonVaultOutcome.Ambush, true)]
    public void LockedAndOpenedVaultReceiptsContinueThroughActualSaves(DungeonVaultOutcome outcome, bool opened)
    {
        using var world = Floor(); var combat = world.DungeonCombat; var vault = PreparedVault(world, outcome);
        if (opened) Assert.True(world.SubmitDungeon(new(TacticalAction.OpenVault, Target: vault.Id)).Success);
        string root = Path.Combine(Path.GetTempPath(), "roag-vault-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            for (int i = 0; i < 2; i++)
            {
                var intent = new TacticalIntent(TacticalAction.Wait);
                Assert.True(world.SubmitDungeon(intent).Success); Assert.True(loaded.SubmitDungeon(intent).Success);
                Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            }
            if (opened)
            {
                string before = JsonSerializer.Serialize(loaded.State);
                Assert.False(loaded.SubmitDungeon(new(TacticalAction.OpenVault, Target: vault.Id)).Success);
                Assert.Equal(before, JsonSerializer.Serialize(loaded.State));
            }
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void FreeVaultChestUsesTheOpenedContainerOnceAndHasARealPaidBoonClaim()
    {
        using var world = Floor(); var combat = world.DungeonCombat; var hero = world.Party.Single();
        var vault = PreparedVault(world, DungeonVaultOutcome.Boon);
        Assert.True(world.SubmitDungeon(new(TacticalAction.OpenVault, Target: vault.Id)).Success);
        var current = combat.State.Vaults.Nodes.Single(v => v.Id == vault.Id);
        var chest = Assert.Single(combat.State.Chests.Chests, c => c.Id == current.Chest);
        Assert.Equal(vault.Position, chest.Position); Assert.Equal("core:chest/free_elite", chest.Definition);
        Assert.Equal("boon-chest", world.ObjectAt(chest.Position)!.Kind); Assert.False(world.CanEnter(chest.Position));
        var offers = combat.ChestOffers(chest.Id, hero.Id); Assert.Equal(3, offers.Count); long clock = combat.Clock;
        Assert.True(world.SubmitDungeon(new(TacticalAction.ClaimChest, Target: chest.Id, Choice: 1)).Success);
        Assert.Equal(clock + 100, combat.Clock); Assert.Equal(0, world.Count("gold"));
        Assert.True(hero.Tactics!.Boons.Inventory.Stacks.ContainsKey(offers[0].Boon));
        Assert.True(world.CanEnter(vault.Position)); Assert.Empty(combat.ChestOffers(chest.Id, hero.Id));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void AClosedVaultHasNoHealthAndPassesPrivateSightAndProjectiles()
    {
        using var world = Floor(ClassKind.Ranger); var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
        var vault = combat.SpawnVault("test:projectile-vault", origin.Offset(2, 0), origin.Offset(-4, -4));
        var target = ReplaceRegular(world, "core:enemy/mossbound_guard"); target.Position = origin.Offset(3, 0);
        target.Tactics!.InnateStats = target.Tactics.InnateStats! with { Defense = 0 };
        combat.State.Queue.CancelActor(target.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 10000, target.Id);
        world.Reveal(); Assert.Contains(target.Position, world.Visible); int hp = target.Health;
        Assert.True(world.SubmitDungeon(new(TacticalAction.BasicAttack, Facing.East)).Success); Assert.Equal(hp - hero.Power, target.Health);
        Assert.False(world.CanEnter(vault.Position)); Assert.Null(world.OccupantAt(vault.Position));
        Assert.Contains("projectiles pass", WorldInspection.Inspect(world, vault.Position).Detail);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void VaultPlacementCannotConsumeTheOnlyAlternateRouteAroundATimedGate()
    {
        using var world = Floor(); var origin = world.Party.Single().Position; var combat = world.DungeonCombat;
        for (int y = -4; y <= 4; y++) for (int x = -4; x <= 4; x++) world.Chunks.Edit(origin.Offset(x, y), Material.Wall);
        for (int x = 0; x <= 4; x++)
        { world.Chunks.Edit(origin.Offset(x, 0), Material.Floor); world.Chunks.Edit(origin.Offset(x, 2), Material.Floor); }
        world.Chunks.Edit(origin.Offset(0, 1), Material.Floor); world.Chunks.Edit(origin.Offset(4, 1), Material.Floor);
        var key = origin.Offset(-4, -4); world.Chunks.Edit(key, Material.Floor);
        combat.SpawnTimedGate("test:vault-loop-gate", origin.Offset(2, 2), origin.Offset(1, 2));
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(combat.CanPlaceVault("test:sealing-vault", origin.Offset(2, 0), key));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void ABlockedAmbushConsumesTheVaultOnceWithoutInventingALandingOrActor()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var vault = PreparedVault(world, DungeonVaultOutcome.Ambush);
        var blockers = world.State.Actors.Where(a => a.Tactics?.Enemy is { Squad: null, OathPartner: 0, Dormant: false }).Take(3).ToArray();
        int index = 0;
        foreach (var (dx, dy) in new[] { (0, -1), (1, 0), (0, 1) })
        {
            var p = vault.Position.Offset(dx, dy); if (!world.GroundOpen(p)) continue;
            var actor = blockers[index++]; actor.Position = p;
            combat.State.Queue.CancelActor(actor.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 100000, actor.Id);
        }
        int actors = world.State.Actors.Count;
        Assert.True(world.SubmitDungeon(new(TacticalAction.OpenVault, Target: vault.Id)).Success);
        Assert.Equal(actors, world.State.Actors.Count); Assert.Equal(0, combat.State.Vaults.Nodes.Single(v => v.Id == vault.Id).Ambush);
        Assert.False(world.SubmitDungeon(new(TacticalAction.OpenVault, Target: vault.Id)).Success);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void VaultValidationRejectsSpentKeysAndForgedSeededRewardReceipts()
    {
        using var world = Floor(); var combat = world.DungeonCombat; var vault = PreparedVault(world, DungeonVaultOutcome.Boon);
        Assert.True(world.SubmitDungeon(new(TacticalAction.OpenVault, Target: vault.Id)).Success);
        int index = combat.State.Vaults.Nodes.FindIndex(v => v.Id == vault.Id); var opened = combat.State.Vaults.Nodes[index];
        foreach (var invalid in new[] { opened with { KeyCollected = false }, opened with { Chest = 99999 }, opened with { Seed = opened.Seed + 1 } })
        {
            combat.State.Vaults.Nodes[index] = invalid;
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        }
        combat.State.Vaults.Nodes[index] = opened; TacticalValidation.Validate(world.Plan, world.State);
    }
}
