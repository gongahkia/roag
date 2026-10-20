using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Ithica.Core;
using Ithica.Core.Colony;
using Ithica.Core.Diagnostics;
using Ithica.Core.Expeditions;
using Ithica.Core.Generation;
using Xunit;
using Game = Ithica.Core.Campaign;
using Field = Ithica.Core.Expeditions.Campaign;

namespace Ithica.Tests.Generation;

public sealed class LunarProceduralTests
{
    private static string TerrainHash(Zone zone) => Convert.ToHexString(SHA256.HashData(zone.Tiles.Select(t => (byte)t).ToArray()));
    private static IEnumerable<int> Seeds() => Enumerable.Range(-32, 64).Concat([int.MinValue, int.MaxValue, 1337]);

    [Fact]
    public void GradientNoiseIsSmoothSeededBoundedAndStateless()
    {
        var noise = new GradientNoise(42); var values = new HashSet<double>();
        foreach (int seed in Seeds())
        {
            var a = new GradientNoise(seed); var b = new GradientNoise(seed);
            for (int n = -20; n < 20; n++)
            {
                double x = n * .37, y = n * -.21, value = a.Fractal(x, y);
                Assert.InRange(value, -1, 1); Assert.Equal(value, b.Fractal(x, y));
                Assert.InRange(Math.Abs(a.Sample(x + .001, y) - a.Sample(x, y)), 0, .01);
                values.Add(value);
            }
        }
        double original = noise.Fractal(-2.8, 19.6); noise.Sample(100, -100);
        Assert.Equal(original, noise.Fractal(-2.8, 19.6)); Assert.True(values.Count > 1000);
        Assert.Throws<ArgumentOutOfRangeException>(() => noise.Sample(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => noise.Fractal(0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => noise.Fractal(0, 0, 9));
    }

    [Fact]
    public void SurfaceShapesSizesProfilesAndApproachesVaryAcrossSeeds()
    {
        var hashes = new HashSet<string>(); var dimensions = new HashSet<(int, int)>(); var profiles = new HashSet<string>();
        var entrances = new HashSet<string>(); var goals = new HashSet<Cell>();
        for (int seed = 0; seed < 32; seed++)
        {
            var z = LunarGenerator.Create("surface", seed, false, false);
            hashes.Add(TerrainHash(z)); dimensions.Add((z.Width, z.Height)); profiles.Add(z.Generation.Profile);
            var entry = z.Objects.Single(o => o.Id == "return").Position;
            entrances.Add(entry.X == 3 ? "west" : entry.X == z.Width - 4 ? "east" : entry.Y == 3 ? "north" : "south");
            goals.Add(z.Objects.Single(o => o.Id == "component").Position);
            Assert.NotEmpty(z.Generation.Craters); Assert.Equal(4, z.Generation.NoiseOctaves); Assert.Equal(3, z.Generation.Version);
            Assert.Equal(Field.CreateField(seed, z).StateJson(), Field.CreateField(seed, LunarGenerator.Create("surface", seed, false, false)).StateJson());
        }
        Assert.Equal(32, hashes.Count); Assert.True(dimensions.Count > 20); Assert.Equal(4, profiles.Count);
        Assert.Equal(4, entrances.Count); Assert.True(goals.Count > 24);
    }

    [Fact]
    public void ChamberNetworksVaryAndVaultHasExactlyOneEntrance()
    {
        var dimensions = new HashSet<(int, int)>(); var chamberCounts = new HashSet<int>(); var hashes = new HashSet<string>();
        foreach (int seed in Seeds())
        {
            var z = LunarGenerator.Create("below", seed, true, false);
            var validation = LunarGenerator.Validate(z); Assert.True(validation.Valid, string.Join("; ", validation.Errors));
            dimensions.Add((z.Width, z.Height)); chamberCounts.Add(z.Generation.Chambers.Count); hashes.Add(TerrainHash(z));
            var vault = Assert.Single(z.Generation.Chambers, c => c.Kind == "sealed");
            var gate = z.Objects.Single(o => o.Id == "seal").Position;
            var boundary = new List<Cell>();
            for (int y = vault.Y; y < vault.Y + vault.Height; y++) for (int x = vault.X; x < vault.X + vault.Width; x++)
                if (x == vault.X || y == vault.Y || x == vault.X + vault.Width - 1 || y == vault.Y + vault.Height - 1) boundary.Add(new(x, y));
            Assert.Equal(gate, Assert.Single(boundary, z.Walkable));
            var entry = z.Objects.Single(o => o.Id == "return").Position;
            var pump = z.Objects.Single(o => o.Id == "terminal").Position;
            var salvage = z.Objects.Single(o => o.Id == "component").Position;
            Assert.NotEmpty(z.FindPath(entry, [pump], p => p != gate)); Assert.Empty(z.FindPath(entry, [salvage], p => p != gate));
        }
        Assert.True(dimensions.Count > 20); Assert.True(chamberCounts.Count >= 3); Assert.Equal(Seeds().Count(), hashes.Count);
    }

    [Fact]
    public void ValidatorRejectsACarvedSealBypassAndDuplicateObjectives()
    {
        var z = LunarGenerator.Create("below", 42, true, false);
        var gate = z.Objects.Single(o => o.Id == "seal").Position;
        z.Set(gate + new Cell(-1, 1), Ground.Stone); z.Set(gate + new Cell(0, 1), Ground.Stone);
        Assert.Contains(LunarGenerator.Validate(z).Errors, e => e.Contains("bypassed"));
        z.Objects.Add(z.Objects.Single(o => o.Id == "terminal"));
        Assert.Contains(LunarGenerator.Validate(z).Errors, e => e.Contains("Duplicate"));
        var surface = LunarGenerator.Create("surface", 42, false, true);
        Array.Fill(surface.Safe, false);
        foreach (var obj in surface.Objects) surface.Safe[surface.Index(obj.Position)] = true;
        Assert.Contains(LunarGenerator.Validate(surface).Errors, e => e.Contains("Unprotected objective route"));
    }

    [Fact]
    public void NewAreasHaveNoDisconnectedGroundAndProtectEveryObjective()
    {
        foreach (int seed in Seeds()) foreach (var (structure, rescue) in new[] { (false, false), (false, true), (true, false) })
        {
            var z = LunarGenerator.Create("area", seed, structure, rescue);
            Assert.True(LunarGenerator.Validate(z).Valid);
            var entry = z.Objects.Single(o => o.Id == "return").Position;
            var seen = new HashSet<Cell> { entry }; var queue = new Queue<Cell>(); queue.Enqueue(entry);
            while (queue.TryDequeue(out var p)) foreach (var next in z.Neighbors(p)) if (z.Walkable(next) && seen.Add(next)) queue.Enqueue(next);
            Assert.Equal(z.Tiles.Count(t => !Zone.Solid(t)), seen.Count);
            Assert.All(z.Objects, o => Assert.True(z.Safe[z.Index(o.Position)]));
            Assert.Equal(z.Objects.Count, z.Objects.Select(o => o.Position).Distinct().Count());
            Assert.DoesNotContain(z.Environment.Cells, h => h is HazardKind.Fire or HazardKind.Spores);
            Assert.False(z.Generation.UsedFallback);
        }
    }

    [Theory]
    [InlineData(48, 32)] [InlineData(96, 64)] [InlineData(160, 96)]
    public void ColonyNoisePreservesShelterAirlockSuppliesAndReachableContacts(int width, int height)
    {
        foreach (int seed in new[] { -7, 0, 42, int.MinValue, int.MaxValue })
        {
            var game = new Game(seed, width, height); var world = game.Home.World;
            int cx = width / 2, cy = height / 2;
            Assert.Equal(3, world.GenerationVersion); Assert.Contains(world.GeologyProfile, LunarRules.Default.Profiles.Select(p => p.Name));
            Assert.Equal(Terrain.Airlock, world.Tiles[world.Index(cx + 9, cy)]);
            Assert.True(world.Breathable(world.Center)); Assert.InRange(game.Home.Pressure, .9f, 1);
            foreach (var (x, y, type) in new[] { (cx - 7, cy - 8, Terrain.Ice), (cx + 5, cy + 8, Terrain.Ore), (cx + 12, cy - 5, Terrain.Crystal) })
                Assert.Equal(type, world.Tiles[world.Index(x, y)]);
            var contact = Assert.Single(game.Home.Survivors); var route = new List<int>();
            Assert.True(game.Home.Router.Find(game.Home.Crew[0].Cell, [contact.Cell], route));
            Assert.Equal(game.StateJson(), Game.FromJson(game.StateJson()).StateJson());
            Assert.Equal(game.Home.Random.State, new Ithica.Core.Colony.RandomStream(unchecked((ulong)seed)).State);
        }
    }

    [Fact]
    public void ColonyGeologyHasCoherentRegionsAndChangesWithSeed()
    {
        var a = new Game(42); var b = new Game(42); var c = new Game(7);
        Assert.Equal(a.Home.World.Tiles, b.Home.World.Tiles); Assert.NotEqual(a.Home.World.Tiles, c.Home.World.Tiles);
        var world = a.Home.World; int matches = 0, samples = 0;
        for (int y = 1; y < world.Height - 1; y++) for (int x = 1; x < world.Width - 2; x++)
        {
            if (Math.Abs(x - world.Width / 2) < 16 && Math.Abs(y - world.Height / 2) < 12) continue;
            samples++; if (world.Tiles[world.Index(x, y)] == world.Tiles[world.Index(x + 1, y)]) matches++;
        }
        Assert.True(matches > samples * .65, "Geological fields should form regions instead of independent scattered tiles.");
        Assert.Equal(1, new Simulation(42).World.GenerationVersion);
    }

    [Fact]
    public void PreviouslyDiscoveredUnvisitedSitesKeepTheirLegacyGenerator()
    {
        var game = new Game(42); var json = JsonNode.Parse(game.StateJson())!;
        foreach (var site in json["sites"]!.AsArray()) site!.AsObject().Remove("generatorVersion");
        var loaded = Game.FromJson(json.ToJsonString()); var siteRecord = Assert.Single(loaded.Sites);
        Assert.Equal(2, siteRecord.GeneratorVersion);
        CampaignScenarios.Require(loaded.Prepare(siteRecord.Id));
        Assert.Equal(2, siteRecord.Session!.Zone.Generation.Version);
        Assert.Equal(TerrainHash(LunarGenerator.Create(siteRecord.Id, siteRecord.Seed, false, true, 2)), TerrainHash(siteRecord.Session.Zone));
        Assert.Equal(3, Assert.Single(game.Sites).GeneratorVersion);
    }

    [Fact]
    public void SavedMapsAndGeologyAreAuthoritativeAcrossGenerationVersions()
    {
        var game = new Game(42); game.Sites[0].GeneratorVersion = 2;
        CampaignScenarios.Require(game.Prepare("rescue-0"));
        var zone = game.SelectedSite!.Session!.Zone; var entry = zone.Objects.Single(o => o.Id == "return").Position;
        zone.Set(entry, Ground.Path); game.Home.World.SetTerrain(1, Terrain.Crystal);
        var json = JsonNode.Parse(game.StateJson())!;
        foreach (var site in json["sites"]!.AsArray()) site!.AsObject().Remove("generatorVersion");
        json["home"]!.AsObject().Remove("generationVersion"); json["home"]!.AsObject().Remove("geologyProfile");
        var loaded = Game.FromJson(json.ToJsonString());
        Assert.Equal(Ground.Path, loaded.SelectedSite!.Session!.Zone.At(entry));
        Assert.Equal(Terrain.Crystal, loaded.Home.World.Tiles[1]); Assert.Equal(1, loaded.Home.World.GenerationVersion);
        Assert.Equal(loaded.StateJson(), Game.FromJson(loaded.StateJson()).StateJson());
    }

    [Theory]
    [InlineData(2)] [InlineData(3)]
    public void GeneratedMapsAndIndependentRandomStreamsContinueExactly(int version)
    {
        var game = new Game(-7); game.Sites[0].GeneratorVersion = version;
        CampaignScenarios.Require(game.Prepare("rescue-0")); CampaignScenarios.Require(game.Depart([0]));
        var loaded = Game.FromJson(game.StateJson());
        for (int n = 0; n < 48; n++) { CampaignScenarios.Require(game.Wait()); CampaignScenarios.Require(loaded.Wait()); }
        Assert.Equal(game.StateJson(), loaded.StateJson()); Assert.Equal(version, loaded.ActiveField!.Zone.Generation.Version);
    }

    [Theory]
    [InlineData("version")] [InlineData("crater")] [InlineData("chamber")]
    public void MalformedGenerationMetadataIsRejected(string corruption)
    {
        var game = new Game(); CampaignScenarios.Require(game.Prepare("rescue-0"));
        var json = JsonNode.Parse(game.StateJson())!;
        var generation = json["sites"]![0]!["field"]!["zones"]![0]!["generation"]!;
        switch (corruption)
        {
            case "version": json["sites"]![0]!["generatorVersion"] = 999; break;
            case "crater": generation["craters"]![0]!["radius"] = -1; break;
            case "chamber": generation["chambers"] = JsonNode.Parse("[{\"x\":999,\"y\":0,\"width\":5,\"height\":5,\"kind\":\"damaged\"}]"); break;
        }
        Assert.Throws<InvalidDataException>(() => Game.FromJson(json.ToJsonString()));
    }
}
