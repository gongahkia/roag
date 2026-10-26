using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class BoonEventTests
{
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Tactics", "Fixtures", "original-boon-events.json")));

    [Fact]
    public void AllSixtyPredicatesAndAuthoredTreesMatchOriginalIncludingStatefulShortCircuitOrder()
    {
        using var reference = Reference();
        string boon = reference.RootElement.GetProperty("boon").GetString()!;
        var scenarios = reference.RootElement.GetProperty("scenarios").EnumerateArray().ToArray();
        var operations = new HashSet<BoonPredicateKind>();
        foreach (var sample in reference.RootElement.GetProperty("predicates").EnumerateArray())
        {
            var node = sample.GetProperty("node").Deserialize<BoonPredicate>(BoonCatalog.Json)!;
            BoonDefinitionValidation.ValidatePredicate(node);
            if (node is BoonCondition leaf) operations.Add(leaf.Op);
            if (node is BoonCondition)
            {
                var outcomes = sample.GetProperty("expected").EnumerateArray().SelectMany(a => a.EnumerateArray()).Select(v => v.GetBoolean()).ToHashSet();
                Assert.True(outcomes.SetEquals([true, false]), $"Missing positive or negative reference coverage for {node}.");
            }
            int scenarioIndex = 0;
            foreach (var scenario in scenarios)
            {
                var world = Context(scenario);
                var history = new BoonChainHistory();
                foreach (var prior in scenario.GetProperty("prior").EnumerateArray())
                    history.Damages.Add(new(prior.GetProperty("target_id").GetInt64(), prior.GetProperty("target_faction").GetString(), Position(prior.GetProperty("position"))));
                var expected = sample.GetProperty("expected")[scenarioIndex];
                int eventIndex = 0;
                foreach (var input in scenario.GetProperty("events").EnumerateArray())
                {
                    var e = input.Deserialize<BoonEvent>(BoonCatalog.Json)!;
                    bool actual = BoonConditions.Evaluate(node, world, world.Actors[0], boon, e, history);
                    Assert.True(expected[eventIndex].GetBoolean() == actual,
                        $"Predicate {sample.GetProperty("node")} scenario {scenarioIndex} event {eventIndex}: expected {expected[eventIndex]}, actual {actual}");
                    eventIndex++;
                }
                scenarioIndex++;
            }
        }
        Assert.Equal(Enum.GetValues<BoonPredicateKind>().Order(), operations.Order());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealCatalogDispatchMatchesOriginalOwnerOrderChanceChainsAndEffects(bool resume)
    {
        using var reference = Reference();
        int caseIndex = 0;
        foreach (var sample in reference.RootElement.GetProperty("dispatch").EnumerateArray())
        {
            var input = sample.GetProperty("input");
            var world = Context(input);
            var state = new BoonDispatchState();
            var random = new RoagRandom(input.GetProperty("seed").GetInt32());
            var output = new JsonArray();
            int eventIndex = 0;
            foreach (var value in input.GetProperty("events").EnumerateArray())
            {
                var e = value.Deserialize<BoonEvent>(BoonCatalog.Json)!;
                BoonDispatch.OnEvent(state, world, e, random, a =>
                {
                    a.Validate(state.NextActivationId);
                    var record = new JsonObject
                    {
                        ["type"] = "activation", ["id"] = a.Id, ["boon"] = a.BoonId, ["owner"] = a.OwnerId,
                        ["power"] = a.Power, ["chain"] = a.ChainId, ["parent"] = a.ParentEventId,
                        ["ancestry"] = new JsonArray(a.Ancestry.Select(k => (JsonNode?)JsonValue.Create($"{k.Owner}:{k.Boon}")).ToArray())
                    };
                    if (a.RootActionId != null) record["root"] = a.RootActionId;
                    if (a.PairTargetId != null) record["pair"] = a.PairTargetId;
                    output.Add(record);
                }, (a, effect) => output.Add(new JsonObject
                {
                    ["type"] = "effect", ["activation"] = a.Id, ["kind"] = JsonNamingPolicy.SnakeCaseLower.ConvertName(effect.Kind.ToString())
                }));
                if (resume && ++eventIndex % 7 == 0)
                {
                    state = JsonSerializer.Deserialize<BoonDispatchState>(JsonSerializer.Serialize(state))!;
                    state.Validate();
                    random = new(random.State);
                }
            }
            var expected = sample.GetProperty("expected");
            var expectedOutput = JsonNode.Parse(expected.GetProperty("output").GetRawText())!.AsArray();
            Assert.True(expectedOutput.Count == output.Count, $"Dispatch case {caseIndex}: expected {expectedOutput.Count} outputs, got {output.Count}.");
            for (int i = 0; i < output.Count; i++)
                Assert.True(JsonNode.DeepEquals(expectedOutput[i], output[i]), $"Dispatch case {caseIndex}, output {i}: expected {expectedOutput[i]}, got {output[i]}.");
            Assert.Equal(expected.GetProperty("random").GetInt32(), random.State);
            Assert.Equal(expected.GetProperty("next_id").GetInt64(), state.NextActivationId);
            caseIndex++;
        }
    }

    [Fact]
    public void VisibilityBelongsToBoonOwnerAndLiveActorFactsDoNotDuplicateWorldState()
    {
        using var reference = Reference();
        var world = Context(reference.RootElement.GetProperty("scenarios")[0]);
        var first = world.Actors[0];
        var second = world.Actors[1];
        var e = new BoonEvent { Id = 1, ChainId = 1, Kind = BoonTrigger.HitConfirmed, Position = new(11, 10), SourcePosition = new(12, 10), TargetId = 2 };
        world = world with { Visible = (owner, _) => owner == first.Actor.Id };
        var history = new BoonChainHistory();
        foreach (var kind in new[] { BoonPredicateKind.VisibleEvent, BoonPredicateKind.VisibleSource })
        {
            Assert.True(BoonConditions.Evaluate(new BoonCondition(kind), world, first, "boon", e, history));
            Assert.False(BoonConditions.Evaluate(new BoonCondition(kind), world, second, "boon", e, history));
        }
        var target = new BoonCondition(BoonPredicateKind.TargetEntityHostile);
        Assert.True(BoonConditions.Evaluate(target, world, first, "boon", e, history));
        second.Actor.Health = 0;
        Assert.False(BoonConditions.Evaluate(target, world, first, "boon", e, history));
        Assert.True(BoonConditions.Observes(first, new(BoonScopeKind.Radius, 1), e));
        first.Actor.Position = new(40, 40);
        Assert.False(BoonConditions.Observes(first, new(BoonScopeKind.Radius, 1), e));
    }

    [Fact]
    public void ScopeUsesPositionThenFromThenSourceAndOwnerScopeDoesNotRequireSameFloor()
    {
        var owner = new BoonActorView(new Actor { Id = 1, Position = new(10, 10) }, "player");
        var e = new BoonEvent { Id = 1, ChainId = 1, Kind = BoonTrigger.Moved, SourceId = 1,
            Position = new(10, 10, 1), From = new(10, 10), SourcePosition = new(11, 10) };
        Assert.True(BoonConditions.Observes(owner, new(BoonScopeKind.Owner), e));
        Assert.False(BoonConditions.Observes(owner, new(BoonScopeKind.Floor), e));
        Assert.True(BoonConditions.Observes(owner, new(BoonScopeKind.Radius, 0), e with { Position = null }));
        Assert.False(BoonConditions.Observes(owner, new(BoonScopeKind.Radius, 0), e with { Position = null, From = null }));
        Assert.True(BoonConditions.Observes(owner, new(BoonScopeKind.Nearby, 1), e with { Position = null, From = null }));
        Assert.False(BoonConditions.Observes(owner, new(BoonScopeKind.Floor), e with { Position = null, From = null, SourcePosition = null }));
    }

    [Fact]
    public void DelayedChainsRetainPairAndGroupHistoryWhileCompletedChainsCanBePruned()
    {
        var state = new BoonDispatchState { NextActivationId = 91 };
        state.Chains.Add(1, new()); state.Chains.Add(2, new());
        var e = new BoonEvent { Id = 1, ChainId = 2, Kind = BoonTrigger.DamageTaken, TargetId = 3, TargetFaction = "enemy", Position = new(10, 10), ActualLoss = 1 };
        var history = state.Chains[2];
        history.RecordDamage(e);
        Assert.False(history.Distinct(1, "boon", null, e, 2));
        state.Prune([2, 2, 99]);
        Assert.Single(state.Chains);
        var copy = JsonSerializer.Deserialize<BoonDispatchState>(JsonSerializer.Serialize(state))!;
        Assert.Single(copy.Chains[2].Damages);
        Assert.False(copy.Chains[2].Distinct(1, "boon", null, e, 2));
        Assert.True(copy.Chains[2].Distinct(1, "boon", null, e with { TargetId = 4 }, 2));
        Assert.False(copy.Chains[2].Distinct(1, "boon", null, e, 2));
        copy.Prune([]);
        Assert.Empty(copy.Chains);
        Assert.Equal(91, copy.NextActivationId);
    }

    [Fact]
    public void MalformedSnapshotsRejectBeforeRandomConsumptionOrHistoryMutation()
    {
        using var reference = Reference();
        var world = Context(reference.RootElement.GetProperty("scenarios")[0]);
        var state = new BoonDispatchState();
        var random = new RoagRandom(42);
        var e = new BoonEvent { Id = 1, ChainId = 1, Kind = BoonTrigger.HitConfirmed };
        var key = new BoonActivationKey(1, "core:boon/temporal_echo");
        BoonEvent[] invalid = [e with { Id = 0 }, e with { ChainId = 0 }, e with { RootActionId = -1 },
            e with { SourceId = -1 }, e with { TargetId = 0 }, e with { ProcCoefficient = double.NaN },
            e with { ProcCoefficient = -1 }, e with { ActualLoss = -1 }, e with { Path = default },
            e with { FateFace = 9 }, e with { Kind = (BoonTrigger)999 }, e with { DamageType = (DamageElement)999 },
            e with { Ancestry = [key, key] }, e with { Ancestry = [new(1, "unknown")] }];
        foreach (var malformed in invalid)
        {
            Assert.Throws<InvalidDataException>(() => BoonDispatch.OnEvent(state, world, malformed, random,
                _ => throw new Exception("No activation should emit."), (_, _) => throw new Exception("No effect should enqueue.")));
            Assert.Equal(42, random.State); Assert.Equal(1, state.NextActivationId); Assert.Empty(state.Chains);
        }
    }

    [Fact]
    public void PersistedActivationCannotForgeCausalParentOrRepeatAncestry()
    {
        const string boon = "core:boon/temporal_echo";
        var e = new BoonEvent { Id = 9, ChainId = 3, RootActionId = 5, Kind = BoonTrigger.AttackPerformed,
            TargetCells = [new(10, 10)], SourceHpAtAttack = 20, SourceMaxHpAtAttack = 24 };
        var a = new BoonActivation(1, boon, 2, "player", new(10, 9), 3, 5, 9, [new(2, boon)], 4, e);
        a.Validate(2);
        var copy = JsonSerializer.Deserialize<BoonActivation>(JsonSerializer.Serialize(a))!;
        copy.Validate(2);
        Assert.Equal(e.TargetCells.ToArray(), copy.Event.TargetCells.ToArray());
        Assert.Equal(e.SourceHpAtAttack, copy.Event.SourceHpAtAttack);
        foreach (var bad in new[] { a with { Id = 2 }, a with { ParentEventId = 8 }, a with { RootActionId = 6 },
                     a with { ChainId = 4 }, a with { OwnerId = 3 }, a with { Ancestry = [] },
                     a with { Ancestry = [new(2, boon), new(2, boon)] }, a with { Power = -1 } })
            Assert.Throws<InvalidDataException>(() => bad.Validate(2));
    }

    [Fact]
    public void CorruptChainGroupsAndUnorderedPairKeysRejectOnRestore()
    {
        const string boon = "core:boon/temporal_echo";
        var state = new BoonDispatchState();
        var history = new BoonChainHistory(); state.Chains.Add(1, history);
        history.Pairs.Add(new(1, boon, 3, 2));
        Assert.Throws<InvalidDataException>(() => state.Validate());
        history.Pairs.Clear(); history.Pairs.Add(new(1, boon, 2, 3));
        state.Validate();
        history.Groups.Add(new() { Owner = 1, Boon = boon, Field = BoonEventField.BoonId, Count = 1, Seen = [new(Target: 2)] });
        Assert.Throws<InvalidDataException>(() => state.Validate());
        history.Groups.Clear();
        var group = new BoonDistinctGroup { Owner = 1, Boon = boon, Count = 1, Seen = [new(Target: 2)] };
        history.Groups.Add(group); state.Validate();
        history.Groups.Add(group);
        Assert.Throws<InvalidDataException>(() => state.Validate());
        history.Groups.RemoveAt(1); group.Count = 2;
        Assert.Throws<InvalidDataException>(() => state.Validate());
    }

    private static WorldPosition Position(JsonElement p) => p.Deserialize<WorldPosition>(BoonCatalog.Json);
    private static BoonEvaluationContext Context(JsonElement input)
    {
        var actors = new List<BoonActorView>();
        foreach (var a in input.GetProperty("actors").EnumerateArray())
        {
            var actor = new Actor { Id = a.GetProperty("id").GetInt64(), Position = Position(a.GetProperty("position")),
                Health = a.GetProperty("hp").GetInt32(), Tactics = new() };
            int maximum = a.GetProperty("max_hp").GetInt32();
            if (a.TryGetProperty("class", out var name))
            {
                var kind = Enum.GetValues<ClassKind>().Single(c => ClassCatalog.Get(c).Id == name.GetString());
                var build = CharacterBuild.Create(kind, 42);
                actor.Tactics.Build = build with { Stats = build.Stats with { MaxHealth = maximum } };
            }
            else actor.Tactics.InnateStats = new(maximum, 4, 4, 0, 100, 0, 150);
            if (a.TryGetProperty("memory", out var memory)) actor.Tactics.Boons.Memory = memory.Deserialize<Dictionary<string, BoonMemory>>(BoonCatalog.Json)!;
            if (a.TryGetProperty("boons", out var boons)) foreach (var id in boons.EnumerateArray())
                actor.Tactics.Boons.Inventory = actor.Tactics.Boons.Inventory
                    .Grant(id.GetString()!, BoonRarity.Common, actor.Tactics.Build?.Class)
                    .Grant(id.GetString()!, BoonRarity.Rare, actor.Tactics.Build?.Class);
            actors.Add(new(actor, a.GetProperty("faction").GetString()!, a.TryGetProperty("regular", out var regular) && regular.GetBoolean(),
                a.TryGetProperty("locked", out var locked) ? locked.EnumerateArray().Select(Position).ToArray() : null));
        }
        string? Tile(JsonElement values, WorldPosition p, string field, string? fallback)
        {
            foreach (var value in values.EnumerateArray())
                if (value.GetProperty("x").GetInt32() == p.X && value.GetProperty("y").GetInt32() == p.Y && value.GetProperty("floor").GetInt32() == p.Floor)
                    return value.GetProperty(field).GetString();
            return fallback;
        }
        var hidden = input.GetProperty("hidden").EnumerateArray().Select(Position).ToHashSet();
        return new(input.GetProperty("clock").GetInt64(), actors,
            p => Tile(input.GetProperty("terrain"), p, "symbol", "."),
            p => Tile(input.GetProperty("special"), p, "kind", null),
            (_, p) => p.Floor == 0 && !hidden.Contains(p),
            id => id switch { "fixture:regular" or "fixture:boss" => 3, "fixture:empty" => 0, _ => null },
            input.GetProperty("dash").GetBoolean());
    }
}
