using System.Text.Json;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class LoadVisibilityTests
{
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void LoadingARestrictedSurfaceViewDoesNotDiscoverOrActivateAnything(int seed)
    {
        using var world = WorldSession.Create(seed); var p = world.State.Position;
        foreach (var delta in world.State.Chunks) delta.Discovered.Clear();
        world.RevealSurface(p.X - 3, p.Y - 3, p.X + 4, p.Y + 4);
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-load-visibility-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(directory, "world.json"); world.Save(path);
            string before = JsonSerializer.Serialize(world.State), file = File.ReadAllText(path);
            using var loaded = WorldSession.Load(path);
            Assert.Equal(before, JsonSerializer.Serialize(loaded.State)); Assert.Equal(file, File.ReadAllText(path));
            Assert.All(loaded.Visible, cell => Assert.True(loaded.Chunks.Seen(cell)));
            Assert.Equal(world.State.EcologyChunks, loaded.State.EcologyChunks);
            loaded.RevealSurface(p.X - 3, p.Y - 3, p.X + 4, p.Y + 4);
            Assert.Equal(before, JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
