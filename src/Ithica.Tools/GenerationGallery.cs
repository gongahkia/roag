using System.Security;
using System.Text;
using Ithica.Core.Colony;
using Ithica.Core.Expeditions;
using Ithica.Core.Generation;

namespace Ithica.Tools;

internal static class GenerationGallery
{
    public static void Write(string path, int firstSeed, int samples)
    {
        if (samples is < 1 or > 12 || !string.Equals(Path.GetExtension(path), ".svg", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Generation export needs a .svg path and 1-12 samples.");
        var svg = new StringBuilder(); int height = samples * 340 + 100;
        svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1260\" height=\"{height}\" viewBox=\"0 0 1260 {height}\"><rect width=\"100%\" height=\"100%\" fill=\"#10171e\"/>");
        Text(22, 28, "Ithica / generation diagnostics", 19);
        Text(22, 51, "Independent inputs per row: colony geology, surface, compartments. Mint = protected approaches; gold = objectives.", 12);
        for (int row = 0; row < samples; row++)
        {
            int seed = unchecked(firstSeed + row), top = 84 + row * 340;
            var world = new World(96, 64); world.GenerateLunar(seed);
            Panel(20, top, $"Seed {seed} / colony / {world.GeologyProfile}", world.Width, world.Height,
                i => world.Tiles[i] switch { Terrain.Vacuum => "#243340", Terrain.Rock => "#67737b", Terrain.Ice => "#9bdce5", Terrain.Ore => "#ca925b",
                    Terrain.Crystal => "#bb92dc", Terrain.Floor => "#52766c", Terrain.Wall => "#121b24", _ => "#e3d1a0" });
            foreach (bool structure in new[] { false, true })
            {
                var z = LunarGenerator.Create(structure ? "below" : "surface", seed, structure, false);
                var origin = Panel(structure ? 850 : 435, top, $"{(structure ? "Compartments" : "Surface")} / {z.Generation.Profile}", z.Width, z.Height,
                    i => z.Safe[i] && z.Tiles[i] != Ground.Wall ? "#568b7b" : z.Tiles[i] == Ground.Wall ? "#26323e" : z.GroundSprites[i] == "grit" ? "#899294" : "#adb3ac");
                foreach (var obj in z.Objects)
                    svg.Append(FormattableString.Invariant($"<circle cx=\"{origin.X + (obj.Position.X + .5) * origin.Scale:F2}\" cy=\"{origin.Y + (obj.Position.Y + .5) * origin.Scale:F2}\" r=\"3\" fill=\"{(obj.Kind == ObjectKind.Return ? "#e7f2ee" : "#ffc77f")}\"/>"));
            }
        }
        svg.Append("</svg>");
        string target = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, svg.ToString()); File.Move(temporary, target, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return;

        void Text(double x, double y, string text, int size) => svg.Append(FormattableString.Invariant(
            $"<text x=\"{x}\" y=\"{y}\" fill=\"#dce5de\" font-family=\"monospace\" font-size=\"{size}\">{SecurityElement.Escape(text)}</text>"));
        (double X, double Y, double Scale) Panel(int left, int top, string label, int width, int mapHeight, Func<int, string> color)
        {
            Text(left, top, label, 12); Text(left, top + 20, $"{width} x {mapHeight}", 11);
            double scale = Math.Min(390.0 / width, 270.0 / mapHeight), ox = left, oy = top + 34;
            for (int y = 0; y < mapHeight; y++) for (int x = 0; x < width;)
            {
                string fill = color(y * width + x); int end = x + 1;
                while (end < width && color(y * width + end) == fill) end++;
                svg.Append(FormattableString.Invariant($"<rect x=\"{ox + x * scale:F2}\" y=\"{oy + y * scale:F2}\" width=\"{(end - x) * scale + .03:F2}\" height=\"{scale + .03:F2}\" fill=\"{fill}\"/>"));
                x = end;
            }
            return (ox, oy, scale);
        }
    }
}
