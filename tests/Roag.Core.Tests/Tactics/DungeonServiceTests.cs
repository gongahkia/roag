using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    [Fact]
    public void ServicePublicChoicesAndPaymentsMatchThePinnedOriginal()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-services.json")));
        int commands = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
            var input = sample.GetProperty("input"); var expected = sample.GetProperty("expected");
            var kind = input.GetProperty("kind").GetString() switch
            {
                "offering_font" => DungeonServiceKind.OfferingFont, "cleanse_flask" => DungeonServiceKind.CleanseFlask,
                "escape_scroll" => DungeonServiceKind.EscapeScroll, "return_token" => DungeonServiceKind.ReturnToken,
                "charm_well" => DungeonServiceKind.CharmWell, "camp" => DungeonServiceKind.Camp,
                "travelling_trader" => DungeonServiceKind.TravellingTrader, "attunement_forge" => DungeonServiceKind.AttunementForge,
                _ => DungeonServiceKind.Memorial
            };
            hero.Health -= input.TryGetProperty("missing", out var missing) ? missing.GetInt32() : 0;
            world.State.Inventory["gold"] = input.TryGetProperty("gold", out var gold) ? gold.GetInt32() : 0;
            if (input.TryGetProperty("owned", out _)) hero.Tactics!.Boons.Inventory = hero.Tactics.Boons.Inventory.Grant("core:boon/emberbrand", BoonRarity.Rare, ClassKind.Adventurer, 2);
            if (input.TryGetProperty("utility", out _)) hero.Tactics!.Supplies.Utility = new(UtilityKind.CleanseFlask);
            var node = combat.SpawnDungeonService(kind, "test:original-service", hero.Position.Offset(1, 0));
            combat.State.Services.Nodes[^1] = node with { Seed = sample.GetProperty("seed").GetInt32() };
            var options = combat.DungeonServiceChoices(node.Id, hero.Id);
            var originals = expected.GetProperty("options").EnumerateArray().ToArray(); Assert.Equal(originals.Length, options.Count);
            static string Utility(UtilityKind? utility) => utility == null ? "" : TacticalCombat.UtilityId(utility.Value)["core:utility/".Length..];
            for (int i = 0; i < options.Count; i++)
            {
                var actual = options[i]; var original = originals[i];
                Assert.Equal(original.GetProperty("price").GetInt32(), actual.Price);
                Assert.Equal(original.GetProperty("hp_cost").GetInt32(), actual.HealthCost);
                Assert.Equal(original.GetProperty("heal").GetInt32(), actual.Healing);
                Assert.Equal(original.GetProperty("ward").GetBoolean(), actual.Ward);
                Assert.Equal(original.GetProperty("focus").GetBoolean(), actual.Focus);
                Assert.Equal(original.GetProperty("utility").GetString(), Utility(actual.Utility));
                Assert.Equal(original.GetProperty("offer").GetString(), actual.Offer?.Boon ?? "");
                Assert.Equal(original.GetProperty("rarity").GetString(), actual.Offer?.Rarity.ToString().ToLowerInvariant() ?? "");
                Assert.Equal(original.GetProperty("enabled").GetBoolean(), actual.Enabled);
            }
            int choice = input.TryGetProperty("choice", out var selected) ? selected.GetInt32() : 1;
            bool replace = input.TryGetProperty("replace", out var confirmation) && confirmation.GetBoolean();
            Assert.Equal(expected.GetProperty("valid").GetBoolean(), world.SubmitDungeon(new(TacticalAction.Interact, Target: node.Id, Choice: choice, ReplaceUtility: replace)).Success);
            Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock);
            Assert.Equal(expected.GetProperty("hp").GetInt32(), hero.Health);
            Assert.Equal(expected.GetProperty("gold").GetInt32(), world.Count("gold"));
            Assert.Equal(expected.GetProperty("focus").GetInt32(), hero.Tactics!.Supplies.FocusActions);
            Assert.Equal(expected.GetProperty("ward").GetInt32(), hero.Tactics.Supplies.WardActions);
            Assert.Equal(expected.GetProperty("utility").GetString(), Utility(hero.Tactics.Supplies.Utility?.Kind));
            Assert.Equal(expected.GetProperty("spent").GetBoolean(), combat.State.Services.Nodes[^1].UsedBy != 0);
            Assert.Equal(expected.GetProperty("owned").EnumerateObject().Count(), hero.Tactics.Boons.Inventory.Stacks.Count);
            foreach (var owned in expected.GetProperty("owned").EnumerateObject()) foreach (var rarity in BoonCatalog.Rarities)
                Assert.Equal(owned.Value.GetProperty(rarity.ToString().ToLowerInvariant()).GetInt32(), hero.Tactics.Boons.Inventory.Stacks[owned.Name][rarity]);
            TacticalValidation.Validate(world.Plan, world.State); commands++;
        }
        Assert.Equal(15, commands);
    }

    [Fact]
    public void ServiceAcquisitionIsPaidFiniteAndRequiresExplicitUtilityReplacement()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var first = combat.SpawnDungeonService(DungeonServiceKind.CleanseFlask, "test:flask", hero.Position.Offset(1, 0));
        var second = combat.SpawnDungeonService(DungeonServiceKind.EscapeScroll, "test:scroll", hero.Position.Offset(0, 1));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: first.Id, Choice: 1)).Success);
        Assert.Equal(100, combat.Clock); Assert.Equal(UtilityKind.CleanseFlask, hero.Tactics!.Supplies.Utility!.Kind);
        Assert.Empty(combat.DungeonServiceChoices(first.Id, hero.Id));
        var replacement = Assert.Single(combat.DungeonServiceChoices(second.Id, hero.Id)); Assert.True(replacement.Replacement);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(combat.Commit(new(TacticalAction.Interact, Target: second.Id, Choice: 1)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: second.Id, Choice: 1, ReplaceUtility: true)).Success);
        Assert.Equal(200, combat.Clock); Assert.Equal(UtilityKind.EscapeScroll, hero.Tactics.Supplies.Utility!.Kind);
        Assert.All(combat.State.Services.Nodes.Where(n => n.Id == first.Id || n.Id == second.Id), n => Assert.Equal(hero.Id, n.UsedBy));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void ServicePreparedConditionsReplaceOneAnotherAndRetainThreeFutureActions()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var font = combat.SpawnDungeonService(DungeonServiceKind.CharmWell, "test:focus", hero.Position.Offset(1, 0));
        var camp = combat.SpawnDungeonService(DungeonServiceKind.Camp, "test:brace", hero.Position.Offset(0, 1));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: font.Id, Choice: 1)).Success);
        Assert.Equal(3, hero.Tactics!.Supplies.FocusActions); Assert.Equal(0, hero.Tactics.Supplies.WardActions);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: camp.Id, Choice: 2)).Success);
        Assert.Equal(0, hero.Tactics.Supplies.FocusActions); Assert.Equal(3, hero.Tactics.Supplies.WardActions);
        for (int i = 2; i >= 0; i--)
        { Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); Assert.Equal(i, hero.Tactics.Supplies.WardActions); }
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void FontOffersAreFixedAndEligibleAndCannotSpendTheRecipientsLastHP()
    {
        using var world = Floor(ClassKind.Tinkerer); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var font = combat.SpawnDungeonService(DungeonServiceKind.OfferingFont, "test:font", hero.Position.Offset(1, 0));
        int combatRandom = combat.State.RandomState, buildRandom = hero.Tactics!.Build!.RandomState;
        var offers = combat.DungeonServiceChoices(font.Id, hero.Id); Assert.Equal(3, offers.Count);
        Assert.All(offers, o => Assert.True(BoonCatalog.Default.Get(o.Offer!.Boon).Allows(ClassKind.Tinkerer)));
        Assert.Equal(JsonSerializer.Serialize(offers), JsonSerializer.Serialize(combat.DungeonServiceChoices(font.Id, hero.Id)));
        Assert.Equal(combatRandom, combat.State.RandomState); Assert.Equal(buildRandom, hero.Tactics.Build.RandomState);
        int cost = offers[0].HealthCost; hero.Health = cost;
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(combat.Commit(new(TacticalAction.Interact, Target: font.Id, Choice: 1)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        hero.Health = cost + 1;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: font.Id, Choice: 1)).Success);
        Assert.Equal(1, hero.Health); Assert.Equal(1, hero.Tactics.Boons.Inventory.Stacks[offers[0].Offer!.Boon][offers[0].Offer!.Rarity]);
        Assert.Empty(combat.DungeonServiceChoices(font.Id, hero.Id));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void ForgeTradesExactlyOneRarityCopyAndMemorialClearsOnlyUnownedMemory()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var boons = hero.Tactics!.Boons;
        boons.Inventory = boons.Inventory.Grant("core:boon/emberbrand", BoonRarity.Rare, ClassKind.Adventurer, 2);
        boons.Memory["core:boon/emberbrand"] = new(ExpiresAt: 1000, Charges: 1);
        world.State.Inventory["gold"] = 3;
        var forge = combat.SpawnDungeonService(DungeonServiceKind.AttunementForge, "test:forge", hero.Position.Offset(1, 0));
        var memorial = combat.SpawnDungeonService(DungeonServiceKind.Memorial, "test:memorial", hero.Position.Offset(0, 1));
        var options = combat.DungeonServiceChoices(forge.Id, hero.Id); Assert.Equal(3, options.Count);
        var trade = options.First(o => o.Enabled);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: forge.Id, Choice: trade.Choice)).Success);
        Assert.Equal(0, world.Count("gold")); Assert.Equal(1, boons.Inventory.Stacks["core:boon/emberbrand"].Rare);
        Assert.True(boons.Memory.ContainsKey("core:boon/emberbrand"));
        Assert.Equal(1, boons.Inventory.Stacks[trade.Offer!.Boon].Rare);
        var retirement = combat.DungeonServiceChoices(memorial.Id, hero.Id).Single(o => o.Retire!.Id == "core:boon/emberbrand");
        Assert.False(retirement.Enabled); hero.Health--;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: memorial.Id, Choice: retirement.Choice)).Success);
        Assert.Equal(hero.MaxHealth, hero.Health); Assert.False(boons.Inventory.Stacks.ContainsKey("core:boon/emberbrand"));
        Assert.False(boons.Memory.ContainsKey("core:boon/emberbrand"));
        Assert.True(boons.Inventory.Stacks.ContainsKey(trade.Offer.Boon));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void ServiceReceiptsOffersAndResourcesPersistWithoutRefillingOnContinue()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var used = combat.SpawnDungeonService(DungeonServiceKind.ReturnToken, "test:used", hero.Position.Offset(1, 0));
        var font = combat.SpawnDungeonService(DungeonServiceKind.OfferingFont, "test:unspent", hero.Position.Offset(0, 1));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: used.Id, Choice: 1)).Success);
        string root = Path.Combine(Path.GetTempPath(), "roag-service-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(combat.State.Services), JsonSerializer.Serialize(loaded.DungeonCombat.State.Services));
            Assert.Empty(loaded.DungeonCombat.DungeonServiceChoices(used.Id, hero.Id));
            Assert.Equal(JsonSerializer.Serialize(combat.DungeonServiceChoices(font.Id, hero.Id)),
                JsonSerializer.Serialize(loaded.DungeonCombat.DungeonServiceChoices(font.Id, hero.Id)));
            Assert.Equal(hero.Tactics!.Supplies.Utility, loaded.Party.Single().Tactics!.Supplies.Utility);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
