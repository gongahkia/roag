using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class BoonEffectTargetTests
{
    [Fact]
    public void NativeEffectTargetsMatchOriginalGameAcrossBodiesFloorsPatternsWallsAndMissingOwners()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Tactics", "Fixtures", "original-boon-effect-targets.json")));
        int index = 0;
        foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input");
            var e = input.GetProperty("event").Deserialize<BoonEvent>(BoonCatalog.Json)!;
            var activation = new BoonActivation(1, "core:boon/temporal_echo", 1, "player", e.SourcePosition!.Value,
                1, 1, 1, [], 3, e);
            var actors = new List<BoonActorView>();
            foreach (var value in input.GetProperty("actors").EnumerateArray())
            {
                var actor = new Actor { Id = value.GetProperty("id").GetInt64(), Health = value.GetProperty("hp").GetInt32(),
                    Position = value.GetProperty("position").Deserialize<WorldPosition>(BoonCatalog.Json),
                    Tactics = new() { InnateStats = new(100, 1, 1, 0, 100, 0, 150) } };
                if (value.TryGetProperty("burning", out var burning) && burning.GetBoolean())
                    actor.Tactics.Burning = new(1, 1, 100, 100, 1, activation);
                actors.Add(new(actor, value.GetProperty("faction").GetString()!));
            }
            var walls = input.GetProperty("walls").EnumerateArray().Select(p => p.Deserialize<WorldPosition>(BoonCatalog.Json)).ToHashSet();
            var effect = input.GetProperty("effect").Deserialize<BoonEffect>(BoonCatalog.Json)!;
            BoonDefinitionValidation.ValidateEffect(effect);
            var actual = BoonEffectTargets.Select(effect, activation, actors, p => !walls.Contains(p));
            var expected = sample.GetProperty("expected").EnumerateArray().Select(p => p.Deserialize<WorldPosition>(BoonCatalog.Json)).ToArray();
            Assert.True(expected.SequenceEqual(actual), $"Effect target case {index} ({input.GetProperty("effect")}): expected {JsonSerializer.Serialize(expected)}, actual {JsonSerializer.Serialize(actual)}.");
            index++;
        }
        Assert.Equal(1896, index);
    }
}
