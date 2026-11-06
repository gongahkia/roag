using System.Text.Json;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class StatePositionIndexTests
{
    public sealed record Marker(int Id, WorldPosition Position, WorldPosition? Other = null);

    [Fact]
    public void ImmutableRegistryQueriesFollowReplacementsReorderingAndBothPositionsWithoutChangingJson()
    {
        var a = new WorldPosition(100, 100); var b = a.Offset(1, 0); var c = a with { Floor = 1 };
        var index = new StatePositionIndex<Marker>(static n => n.Position, static n => n.Other);
        StateList<Marker> nodes = [new(1, a, b), new(2, b, b)];
        Compare();
        nodes.Insert(0, new(3, b, c)); Compare();
        nodes[1] = nodes[1] with { Position = c, Other = null }; Compare();
        nodes[0] = nodes[0] with { Other = a }; Compare();
        nodes.Sort(static (left, right) => right.Id.CompareTo(left.Id)); Compare();
        nodes.RemoveAt(1); Compare();
        nodes = [new(4, c, a), new(5, a)]; Compare();
        nodes = JsonSerializer.Deserialize<StateList<Marker>>(JsonSerializer.Serialize(nodes))!; Compare();
        nodes.Clear(); Compare();

        void Compare()
        {
            string before = JsonSerializer.Serialize(nodes);
            Assert.StartsWith("[", before);
            foreach (var cell in new[] { a, b, c, a.Offset(-1, 0) })
                Assert.Equal(nodes.Where(n => n.Position == cell || n.Other == cell), index.At(nodes, cell) ?? []);
            Assert.Equal(before, JsonSerializer.Serialize(nodes));
        }
    }
}
