using System.Text.Json;
using Roag.Client;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    [Fact]
    public void SecretPublicCommandsMatchOriginalDiscoveryGoldAndFreeCacheReceipts()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-secrets.json")));
        int count = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input");
            using var world = Floor(ClassCatalog.Get("core:actor/" + input.GetProperty("class").GetString()).Kind);
            var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
            world.State.Inventory["gold"] = 0;
            var secret = PrepareSecret(world, input.GetProperty("kind").GetString() == "cracked");
            if (input.TryGetProperty("bump", out _))
            {
                var enemy = ReplaceRegular(world, "core:enemy/mossbound_guard"); enemy.Position = origin.Offset(1, 0);
                enemy.Tactics!.InnateStats = enemy.Tactics.InnateStats! with { MaxHealth = 1000 }; enemy.Health = enemy.MaxHealth;
                combat.State.Queue.CancelActor(enemy.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 100000, enemy.Id);
            }
            combat.State.RandomState = 37; world.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                var action = command.GetString() switch { "move" => TacticalAction.Move, "dash" => TacticalAction.Dash,
                    "sweeping_slash" => TacticalAction.Special, _ => TacticalAction.BasicAttack };
                var intent = new TacticalIntent(action, Facing.East); var expected = sample.GetProperty("expected")[index++]; long first = combat.State.NextEvent;
                string before = JsonSerializer.Serialize(world.State); var preview = combat.Preview(intent);
                Assert.Equal(before, JsonSerializer.Serialize(world.State)); Assert.Equal(expected.GetProperty("valid").GetBoolean(), preview.Valid);
                Assert.Equal(expected.GetProperty("recovery").GetInt64(), preview.Valid ? preview.Recovery : 0);
                Assert.Equal(preview.Valid, world.SubmitDungeon(intent).Success);
                Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), 0), hero.Position);
                Assert.Equal(expected.GetProperty("gold").GetInt32(), world.Count("gold"));
                Assert.Equal(expected.GetProperty("revealed").GetBoolean(), combat.State.Secrets.Nodes.Single(n => n.Id == secret.Id).Revealed);
                Assert.Equal(expected.GetProperty("wall").GetString() == "#", world.Chunks.At(origin.Offset(1, 0)) == Material.Wall);
                Assert.Equal(expected.GetProperty("cache").GetString() == "#", world.Chunks.At(origin.Offset(2, 0)) == Material.Wall);
                foreach (var (key, kind) in new[] { ("discovered", "SecretDiscovered"), ("rewarded", "SecretRewardSpawned") })
                    Assert.Equal(expected.GetProperty(key).GetInt32(), combat.State.Events.Count(e => e.Sequence >= first && e.Kind == kind));
                TacticalValidation.Validate(world.Plan, world.State); count++;
            }
        }
        Assert.Equal(9, count);
    }

    private static DungeonSecret PrepareSecret(WorldSession world, bool cracked)
    {
        var origin = world.Party.Single().Position; var combat = world.DungeonCombat;
        if (!cracked) return combat.SpawnSecret(DungeonSecretKind.MossSigil, "test:sigil", origin.Offset(1, 0));
        world.Chunks.Edit(origin.Offset(1, 0), Material.Wall); world.Chunks.Edit(origin.Offset(2, 0), Material.Wall);
        return combat.SpawnSecret(DungeonSecretKind.CrackedCache, "test:cache", origin, origin.Offset(1, 0), origin.Offset(2, 0));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SecretDiscoveryAndRewardReceiptsContinueExactlyThroughActualSaves(bool cracked, bool reveal)
    {
        using var world = Floor(); var secret = PrepareSecret(world, cracked); world.Reveal();
        var intent = new TacticalIntent(cracked ? TacticalAction.BasicAttack : TacticalAction.Move, Facing.East);
        if (reveal) Assert.True(world.SubmitDungeon(intent).Success);
        string root = Path.Combine(Path.GetTempPath(), "roag-secret-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(world.DungeonCombat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            for (int i = 0; i < 2; i++)
            {
                Assert.True(world.SubmitDungeon(intent).Success); Assert.True(loaded.SubmitDungeon(intent).Success);
                Assert.Equal(JsonSerializer.Serialize(world.DungeonCombat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
                Assert.Equal(world.Count("gold"), loaded.Count("gold"));
            }
            var receipt = loaded.DungeonCombat.State.Secrets.Nodes.Single(n => n.Id == secret.Id);
            Assert.True(receipt.Revealed); Assert.True(receipt.Reward > 0);
            if (cracked) Assert.Single(loaded.DungeonCombat.State.Chests.Chests, c => c.Marker == "secret:" + secret.Marker);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void SecretClueInspectionIsFreePrivateAndNeverExposesTheHiddenCache()
    {
        using var world = Floor(); var secret = PrepareSecret(world, true); world.Reveal();
        Assert.Contains(secret.Clue, world.Visible); Assert.DoesNotContain(secret.Cache!.Value, world.Visible);
        string before = JsonSerializer.Serialize(world.State);
        var info = WorldInspection.Inspect(world, secret.Clue);
        Assert.Equal("Cracked moss wall", info.Title); Assert.Contains("explicit Adventurer", info.Detail);
        Assert.Null(world.ObjectAt(secret.Cache.Value));
        world.Visible.Remove(secret.Clue);
        Assert.Contains("remembered", WorldInspection.Inspect(world, secret.Clue).Title);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.Throws<ArgumentException>(() => world.DungeonCombat.PlaceOptionalTerrain(OptionalTerrainKind.HeatVent, "overlap", secret.Clue));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void ForgedSecretRewardAndHiddenAlcoveStateAreRejected()
    {
        using var world = Floor(); var secret = PrepareSecret(world, true); var secrets = world.DungeonCombat.State.Secrets;
        int index = secrets.Nodes.FindIndex(n => n.Id == secret.Id);
        secrets.Nodes[index] = secret with { Revealed = true, Reward = 9999 };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        secrets.Nodes[index] = secret; world.Chunks.Edit(secret.Cache!.Value, Material.Floor);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        world.Chunks.Edit(secret.Cache.Value, Material.Wall); TacticalValidation.Validate(world.Plan, world.State);
    }
}
