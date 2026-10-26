using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Tactics;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class BoonCatalogTests
{
    private static JsonDocument Original() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Tactics", "Fixtures", "original-boon-catalog.json")));

    [Fact]
    public void EveryRegisteredOriginalDefinitionAndPoolSurvivesTypedLoadingWithoutOldSpriteBindings()
    {
        using var original = Original();
        var catalog = BoonCatalog.Default;
        var definitions = original.RootElement.GetProperty("definitions").EnumerateArray().ToArray();
        Assert.Equal(103, catalog.Definitions.Length);
        Assert.Equal(definitions.Select(d => d.GetProperty("id").GetString()), catalog.Ids);
        Assert.Equal(original.RootElement.GetProperty("starterPool").EnumerateArray().Select(e => e.GetString()), catalog.StarterPool);
        Assert.Equal(original.RootElement.GetProperty("normalPool").EnumerateArray().Select(e => e.GetString()), catalog.NormalPool);
        foreach (var definition in definitions)
        {
            var expected = JsonNode.Parse(definition.GetRawText())!.AsObject();
            expected.Remove("art"); // source indices are reference only; final sprites come from Vagabonds
            var actual = JsonSerializer.SerializeToNode(catalog.Get(definition.GetProperty("id").GetString()!), BoonCatalog.Json)!.AsObject();
            Normalize(expected); Normalize(actual);
            Assert.True(JsonNode.DeepEquals(expected, actual), $"Content mismatch: {definition.GetProperty("id").GetString()}\n{actual}");
            Assert.False(actual.ContainsKey("art"));
        }
        static void Normalize(JsonObject node)
        {
            foreach (string key in new[] { "modifiers", "incompatible", "eligible_classes" })
                if (node[key] is JsonArray { Count: 0 }) node.Remove(key);
        }
    }

    [Fact]
    public void NativeSchemaIncludesUnusedOriginalAuthoringPredicatesAndEveryWeaponForm()
    {
        using var original = Original();
        var schema = original.RootElement.GetProperty("supportedSchema");
        void Check<T>(string key, bool snake = true) where T : struct, Enum => Assert.Equal(
            schema.GetProperty(key).EnumerateArray().Select(e => e.GetString()).Order(),
            Enum.GetNames<T>().Select(n => snake ? JsonNamingPolicy.SnakeCaseLower.ConvertName(n) : n).Order());
        Check<BoonTrigger>("triggers", false); Check<BoonEffectKind>("effects");
        Check<BoonPredicateKind>("predicates"); Check<BoonPattern>("patterns");
        foreach (var form in Enum.GetValues<BoonWeaponForm>())
        {
            var expected = original.RootElement.GetProperty("weaponForms").GetProperty(JsonNamingPolicy.SnakeCaseLower.ConvertName(form.ToString()));
            var actual = BoonCatalog.WeaponForm(form);
            Assert.Equal(expected.GetProperty("class_id").GetString(), ClassCatalog.Get(actual.Class).Id);
            Assert.Equal(expected.GetProperty("ability_id").GetString(), actual.AbilityId);
        }
    }

    [Fact]
    public void SubscriptionOrderAndClassEligibilityMatchTheAuthoredCatalog()
    {
        var catalog = BoonCatalog.Default;
        foreach (var trigger in Enum.GetValues<BoonTrigger>())
            Assert.Equal(catalog.Definitions.Where(d => d.Triggers.Contains(trigger)).Select(d => d.Id), catalog.Subscribers(trigger).Select(d => d.Id));
        var geometry = catalog.Get("core:boon/longblade_geometry");
        Assert.True(geometry.Allows(ClassKind.Adventurer));
        Assert.False(geometry.Allows(ClassKind.Ranger)); Assert.False(geometry.Allows(null));
        Assert.True(catalog.Get("core:boon/storm_conductor").Allows(null));
    }

    [Theory]
    [InlineData("{\"op\":\"unknown\"}")]
    [InlineData("{\"op\":0}")]
    [InlineData("{\"all\":[]}")]
    [InlineData("{\"any\":[null]}")]
    [InlineData("{\"op\":\"source_owner\",\"all\":[{\"op\":\"positive\"}]}")]
    [InlineData("{\"op\":\"positive\",\"op\":\"source_owner\"}")]
    [InlineData("{\"op\":\"positive\",\"typo\":1}")]
    public void MalformedPredicateDocumentsCannotSilentlyChangeTheRule(string json)
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BoonPredicate>(json, BoonCatalog.Json));

    [Fact]
    public void InvalidNativeDeclarationsAreRejectedBeforeTheyCanBeRegistered()
    {
        var sample = BoonCatalog.Default.Get("core:boon/storm_conductor");
        BoonDefinition[] invalid = [
            sample with { Scope = new(BoonScopeKind.Radius) },
            sample with { Triggers = [BoonTrigger.HitConfirmed, BoonTrigger.HitConfirmed] },
            sample with { Parameters = sample.Parameters.Remove(BoonRarity.Rare) },
            sample with { Parameters = sample.Parameters.SetItem(BoonRarity.Legendary, new(0, 0)) },
            sample with { Predicate = new BoonCondition(BoonPredicateKind.RollFace, Number: 9) },
            sample with { Predicate = new BoonCondition(BoonPredicateKind.EventAmount, Min: 1, Field: BoonEventField.BoonId) },
            sample with { Effects = [new() { Kind = BoonEffectKind.StunTarget, Cap = 101, Cooldown = 400 }] },
            sample with { Effects = [new() { Kind = BoonEffectKind.Ward, Duration = 0 }] },
            sample with { Effects = [new() { Kind = BoonEffectKind.DamageNearest, ProcCoefficient = double.NaN }] },
            sample with { Effects = [new() { Kind = BoonEffectKind.ApplyStatus, Status = BoonStatus.Burning, Interval = 100, Ticks = 0 }] },
            sample with { Modifiers = [new("core:ability/arcanist_bolt", BoonModifierKind.LineSecondCell)] },
            sample with { EligibleClasses = ["core:actor/missing"] },
            sample with { PatternPreview = new(BoonPreviewTrigger.Wait) }
        ];
        foreach (var declaration in invalid) Assert.Throws<InvalidDataException>(() => BoonDefinitionValidation.Validate(declaration));
        Assert.Throws<InvalidDataException>(() => new BoonCatalog([sample, sample], [], []));
        Assert.Throws<InvalidDataException>(() => new BoonCatalog([sample], ["core:boon/missing"], []));
        Assert.Throws<InvalidDataException>(() => new BoonCatalog([sample with { Incompatible = ["core:boon/missing"] }], [], []));
    }

    [Fact]
    public void GrantsAreImmutablePermanentAndCheckBothSidesOfAConflict()
    {
        var empty = new BoonInventory();
        var first = empty.Grant("core:boon/storm_conductor", BoonRarity.Common, ClassKind.Ranger);
        var second = first.Grant("core:boon/storm_conductor", BoonRarity.Rare, ClassKind.Ranger, 2);
        Assert.Empty(empty.Stacks);
        Assert.Equal(new(1), first.Stacks["core:boon/storm_conductor"]);
        Assert.Equal(new(1, Rare: 2), second.Stacks["core:boon/storm_conductor"]);
        Assert.Throws<ArgumentOutOfRangeException>(() => second.Grant("core:boon/storm_conductor", BoonRarity.Common, ClassKind.Ranger, -1));
        Assert.Throws<InvalidOperationException>(() => second.Grant("core:boon/longblade_geometry", BoonRarity.Common, ClassKind.Ranger));
        var a = BoonCatalog.Default.Get("core:boon/storm_conductor") with { Incompatible = ["core:boon/emberbrand"] };
        var b = BoonCatalog.Default.Get("core:boon/emberbrand");
        var custom = new BoonCatalog([a, b], [], []);
        var ownsA = empty.Grant(a.Id, BoonRarity.Common, ClassKind.Adventurer, catalog: custom);
        var ownsB = empty.Grant(b.Id, BoonRarity.Common, ClassKind.Adventurer, catalog: custom);
        Assert.Throws<InvalidOperationException>(() => ownsA.Grant(b.Id, BoonRarity.Common, ClassKind.Adventurer, catalog: custom));
        Assert.Throws<InvalidOperationException>(() => ownsB.Grant(a.Id, BoonRarity.Common, ClassKind.Adventurer, catalog: custom));
        var corrupt = new BoonInventory { Stacks = ImmutableDictionary<string, BoonStacks>.Empty.Add(a.Id, new(1)).Add(b.Id, new(1)) };
        Assert.Throws<InvalidDataException>(() => corrupt.Validate(ClassKind.Adventurer, custom));
    }
}
