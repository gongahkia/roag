#!/usr/bin/env python3
"""Compare native terrain with the pinned, read-only Vagabonds structural generator.

Run after building Roag.Core. Only namespace imports change in the captured source;
the original generator executes in an isolated temporary .NET project. Evidence is
retained in artifacts/migration/verification/structural-generation/result.json.
"""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
REFERENCE = ROOT.parent / "vagabonds"
REVISION = "38654e90e8546466b2ce9762032155f3890eaf3f"
SOURCE = "src/Vagabonds.Core/World/StructuralLayouts.cs"
ASSEMBLY = ROOT / "src/Roag.Core/bin/Debug/net10.0/Roag.Core.dll"
EVIDENCE = ROOT / "artifacts/migration/verification/structural-generation"


def main():
    source = subprocess.check_output(["git", "-C", str(REFERENCE), "show", f"{REVISION}:{SOURCE}"])
    if not ASSEMBLY.is_file():
        raise SystemExit("Build Roag.Core in Debug first.")
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="roag-structural-oracle-") as directory:
        folder = Path(directory)
        text = source.decode().replace("Vagabonds.Core", "Roag.Core")
        text = text.replace("namespace Roag.Core.World;", "using Roag.Core.World;\nnamespace Original;")
        (folder / "OriginalStructuralLayouts.cs").write_text(text)
        (folder / "Oracle.csproj").write_text(f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup><Reference Include="Roag.Core"><HintPath>{escape(str(ASSEMBLY))}</HintPath></Reference></ItemGroup>
</Project>
""")
        (folder / "Program.cs").write_text("""using System.Text.Json;
using Roag.Core.World;
var method = typeof(ChunkGenerator).Assembly.GetType("Roag.Core.World.StructuralLayouts")!
    .GetMethod("Generate", [typeof(string), typeof(int)])!;
var checks = new List<object>();
foreach (string family in new[] { "bsp-fort", "bsp-workings", "ring-sanctuary", "caverns" })
foreach (int seed in Enumerable.Range(-8, 16).Concat(new[] { 42, 173, int.MinValue, int.MaxValue }))
{
    var original = Original.StructuralLayouts.Generate(family, seed);
    var native = (Material[])method.Invoke(null, [family, seed])!;
    if (!original.SequenceEqual(native)) throw new Exception($"Terrain changed: {family}, seed {seed}");
    checks.Add(new { family, seed, originalHash = ChunkGenerator.Hash(original), nativeHash = ChunkGenerator.Hash(native) });
}
File.WriteAllText(args[0], JsonSerializer.Serialize(checks, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Verified {checks.Count} structural layouts against original Vagabonds source.");
""")
        result = EVIDENCE / "comparisons.json"
        run = subprocess.run([str(ROOT / "tools/dotnet.sh"), "run", "--project", str(folder / "Oracle.csproj"),
                              "--", str(result)], cwd=ROOT, capture_output=True, text=True)
        (EVIDENCE / "build-and-run.log").write_text(run.stdout + run.stderr)
        if run.returncode:
            raise SystemExit(run.stdout + run.stderr)
        evidence = {"reference_revision": REVISION, "reference_path": SOURCE,
                    "reference_sha256": hashlib.sha256(source).hexdigest(),
                    "native_assembly_sha256": hashlib.sha256(ASSEMBLY.read_bytes()).hexdigest(),
                    "checks": json.loads(result.read_text())}
        (EVIDENCE / "result.json").write_text(json.dumps(evidence, indent=2) + "\n")
        print(run.stdout.strip())


if __name__ == "__main__":
    main()
