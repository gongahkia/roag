using System.Globalization;
using System.Text;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public sealed record LayoutMeasurement(int Seed, string Purpose, int Floor, string Layout, string RawHash, string FinalHash,
    int Walkable, int Components, int FallbackRegions, int RepairedCells, bool StairsReachable);
public sealed record VarietyReport(int FirstSeed, int Seeds, int Footprints, bool Connected, int DistinctRawLayouts,
    Dictionary<string, int> Families, List<LayoutMeasurement> Measurements, string Interpretation);
public sealed record VarietyAudit(VarietyReport Report, string Svg);

public static class WorldVarietyAudit
{
    public static VarietyAudit Run(int firstSeed, int samples, ContentCatalog? content = null)
    {
        if (samples is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(samples), "Use 1..64 seeds.");
        var measurements = new List<LayoutMeasurement>(); var svg = new StringBuilder();
        svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"760\" height=\"{samples * 320 + 40}\" viewBox=\"0 0 760 {samples * 320 + 40}\"><rect width=\"100%\" height=\"100%\" fill=\"#121825\"/><g font-family=\"monospace\" font-size=\"10\" fill=\"#e6dfca\"><text x=\"10\" y=\"18\">Whole-site layouts; rows alternate surface / first depth. Repairs included.</text>");
        bool valid = true;
        for (int n = 0; n < samples; n++)
        {
            int seed = unchecked(firstSeed + n); var plan = WorldPlan.Generate(seed); valid &= plan.Validate().Count == 0;
            var generator = new ChunkGenerator(plan, content);
            var cases = new List<(bool Town, int Id, int Floor)> { (true, 0, 0) };
            foreach (var site in plan.Sites.Take(4)) for (int floor = 0; floor < WorldPlan.Floors; floor++) cases.Add((false, site.Id, floor));
            foreach (var test in cases)
            {
                var f = generator.GenerateFootprint(test.Town, test.Id, test.Floor); var t = f.Trace;
                var seen = new HashSet<int>(); int components = 0;
                for (int i = 0; i < f.Cells.Length; i++)
                {
                    if (!Materials.Walkable(f.Cells[i]) || !seen.Add(i)) continue;
                    components++; var queue = new Queue<int>(); queue.Enqueue(i);
                    while (queue.TryDequeue(out int p)) foreach (int offset in new[] { -1, 1, -64, 64 })
                    {
                        int next = p + offset;
                        if (next >= 0 && next < 4096 && Math.Abs(next % 64 - p % 64) + Math.Abs(next / 64 - p / 64) == 1 && Materials.Walkable(f.Cells[next]) && seen.Add(next)) queue.Enqueue(next);
                    }
                }
                bool stairs = test.Town || (test.Floor == 0 || f.Cells[32 * 64 + 30] == Material.StairsUp) && (test.Floor == 3 || f.Cells[32 * 64 + 34] == Material.StairsDown);
                measurements.Add(new(seed, t.Purpose, test.Floor, t.Layout, t.RawHash, t.FinalHash, seen.Count, components, t.FallbackRegions, t.RepairedCells, stairs));
                valid &= components == 1 && stairs;
                if (test.Floor > 1) continue;
                int column = test.Town ? 0 : test.Id + 1, row = n * 2 + test.Floor;
                int ox = column * 150 + 10, oy = row * 160 + 44;
                svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{ox}\" y=\"{oy - 10}\">{seed} {t.Purpose} z{test.Floor}</text><text x=\"{ox}\" y=\"{oy}\">{t.Layout}</text>");
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    string color = f.Cells[y * 64 + x] switch { Material.Wall => "#454654", Material.Grass => "#4b6838", Material.Door => "#ba8254", Material.StairsUp => "#5ce0c2", Material.StairsDown => "#edc46b", _ => "#c6baa3" };
                    svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{ox + x * 2}\" y=\"{oy + 5 + y * 2}\" width=\"2\" height=\"2\" fill=\"{color}\"/>");
                }
            }
        }
        svg.Append("</g></svg>");
        return new(new(firstSeed, samples, measurements.Count, valid, measurements.Select(m => m.RawHash).Distinct().Count(), measurements.GroupBy(m => m.Layout).ToDictionary(g => g.Key, g => g.Count()), measurements,
            "Connectivity and hash diversity are mechanical checks. Compare purpose, topology, density and repair counts visually; this report does not establish that encounters or rewards are exciting."), svg.ToString());
    }
}
