using System.Text.Json;
using Vagabonds.Client;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.World;

namespace Vagabonds.Client
{
public static class GameEntry
{
public static int Run(string[] args)
{
try
{
    var options = Options.Parse(args);
    if (options.Help)
    {
        Console.WriteLine("""
            VAGABONDS / a finite procedural fantasy world
            ./vagabonds.sh [--seed N] [--load | --new] [--save PATH]
              Normal launch resumes the selected world or legacy campaign; absent slots start a world.
              --new (start over; normal saving/quitting replaces the selected slot)
              --load (require and resume the selected slot)
              --import-legacy PATH (validate/copy an old RPG save and exit)
              --scene world|world-site|world-battle|world-inspector (preview without autosaving)
              --scene town|forest|crypt|battle|victory|atlas (legacy previews)
              --world-headless (recruit, construct, trade and verify simulation)
              --world-report (macro plan and all town/site WFC diagnostics)
              --validate-worlds N (macro checks for consecutive seeds from --seed)
              --world-smoke (native render/cache/material checks and measured timings)
              --headless | --generation-report | --validate-seeds N | --smoke-test (legacy checks)
              --screenshot PATH --frames N --window-size WIDTHxHEIGHT
            Space pauses/resumes. E speaks/takes stairs. B builds. X digs. F1 opens the guide.
            """);
        return 0;
    }
    if (options.ImportLegacy is { } legacy)
    {
        Campaign.ImportLegacy(legacy, options.Save);
        Console.WriteLine(JsonSerializer.Serialize(new { imported = true, save = options.Save }));
        return 0;
    }
    if (options.WorldReport || options.ValidateWorlds > 0 || options.WorldHeadless)
    {
        object report = options.WorldReport ? WorldDiagnostics.GenerationReport(options.Seed) : options.WorldHeadless ? WorldDiagnostics.Headless(options.Seed) : WorldDiagnostics.ValidateSeeds(options.Seed, options.ValidateWorlds);
        Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    if (options.ValidateSeeds > 0)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        int fallbackZones = 0;
        for (int i = 0; i < options.ValidateSeeds; i++)
        {
            int seed = unchecked(options.Seed + i);
            var zones = WorldBuilder.Create(seed);
            var validation = WorldBuilder.Validate(zones);
            if (!validation.Valid) throw new InvalidDataException($"Seed {seed}: {string.Join("; ", validation.Errors)}");
            fallbackZones += zones.Count(z => z.Generation.UsedFallback);
        }
        Console.WriteLine(JsonSerializer.Serialize(new { validatedSeeds = options.ValidateSeeds, firstSeed = options.Seed, fallbackZones, elapsedMs = timer.Elapsed.TotalMilliseconds }));
        return 0;
    }
    if (options.GenerationReport)
    {
        Console.WriteLine(JsonSerializer.Serialize(new Campaign(options.Seed).GenerationSummary(), new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    if (options.Headless)
    {
        var campaign = new Campaign(options.Seed);
        campaign.RunDemoExpedition();
        Console.WriteLine(JsonSerializer.Serialize(campaign.Summary(), new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    if (options.LegacyScene || options.SmokeTest && !options.WorldSmoke)
    {
        using var legacyGame = new VagabondsGame(options); legacyGame.Run();
    }
    else
    {
        var (session, _) = WorldStartup.Open(options);
        if (session is WorldSession world) { using var game = new WorldGame(options, world); game.Run(); }
        else { using var game = new VagabondsGame(options); game.Run(); }
    }
    return 0;
}
catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or InvalidDataException or JsonException or FormatException or OverflowException)
{
    Console.Error.WriteLine("Vagabonds: " + e.Message);
    return 1;
}

}
}

    public sealed class Options
    {
        public int Seed { get; private set; } = 42;
        public bool Load
        {
            get; private set;
        }
        public bool NewGame { get; private set; }
        public bool Preview => _sceneSpecified || Frames > 0 || SmokeTest;
        public bool LegacyScene => _sceneSpecified && !Scene.StartsWith("world", StringComparison.Ordinal);
        public bool WorldSmoke { get; private set; }
        private bool _sceneSpecified;
        public bool SkipIntro
        {
            get; private set;
        }
        public bool Headless
        {
            get; private set;
        }
        public bool SmokeTest
        {
            get; private set;
        }
        public bool GenerationReport { get; private set; }
        public bool WorldReport { get; private set; }
        public bool WorldHeadless { get; private set; }
        public int ValidateWorlds { get; private set; }
        public int ValidateSeeds { get; private set; }
        public bool Help
        {
            get; private set;
        }
        public string Save { get; private set; } = Campaign.DefaultSave;
        public string? ImportLegacy { get; private set; }
        public string? Screenshot
        {
            get; private set;
        }
        public string Scene { get; private set; } = "town";
        public int Frames
        {
            get; private set;
        }
        public int Width { get; private set; } = 1440;
        public int Height { get; private set; } = 900;
        public static Options Parse(string[] args)
        {
            var o = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException("Missing option value.");
                switch (args[i])
                {
                    case "--world-report": o.WorldReport = true; break;
                    case "--world-smoke": o.WorldSmoke = true; o.SmokeTest = true; break;
                    case "--world-headless": o.WorldHeadless = true; break;
                    case "--validate-worlds":
                        o.ValidateWorlds = int.Parse(Value());
                        if (o.ValidateWorlds is < 1 or > 10000) throw new ArgumentException("Validate 1..10000 worlds.");
                        break;
                    case "--import-legacy":
                        o.ImportLegacy = Path.GetFullPath(Value());
                        break;
                    case "--seed":
                        o.Seed = int.Parse(Value());
                        break;
                    case "--load":
                        o.Load = true;
                        break;
                    case "--new":
                        o.NewGame = true;
                        break;
                    case "--save":
                        o.Save = Path.GetFullPath(Value());
                        break;
                    case "--skip-intro":
                        o.SkipIntro = true;
                        break;
                    case "--headless":
                        o.Headless = true;
                        break;
                    case "--generation-report":
                        o.GenerationReport = true;
                        break;
                    case "--validate-seeds":
                        o.ValidateSeeds = int.Parse(Value());
                        if (o.ValidateSeeds is < 1 or > 10000) throw new ArgumentException("Validate 1..10000 seeds.");
                        break;
                    case "--smoke-test":
                        o.SmokeTest = true;
                        break;
                    case "--help":
                        o.Help = true;
                        break;
                    case "--screenshot":
                        o.Screenshot = Path.GetFullPath(Value());
                        break;
                    case "--frames":
                        o.Frames = int.Parse(Value());
                        break;
                    case "--scene":
                        o.Scene = Value();
                        o.SkipIntro = true;
                        o._sceneSpecified = true;
                        break;
                    case "--demo":
                        o.Scene = "battle";
                        o.SkipIntro = true;
                        o._sceneSpecified = true;
                        break;
                    case "--window-size":
                        var parts = Value().Split('x');
                        if (parts.Length != 2)
                        {
                            throw new ArgumentException("Use WIDTHxHEIGHT.");
                        }

                        o.Width = int.Parse(parts[0]);
                        o.Height = int.Parse(parts[1]);
                        break;
                    default:
                        throw new ArgumentException("Unknown option: " + args[i]);
                }
            }
            if (o.Load && o.NewGame)
            {
                throw new ArgumentException("Choose either --load or --new.");
            }
            if (o.Width < 1024 || o.Height < 720 || o.Width > 4096 || o.Height > 2160 || o.Frames < 0)
            {
                throw new ArgumentException("Minimum window is 1024x720; maximum 4096x2160.");
            }

            if (o.Scene is not ("world" or "world-site" or "world-battle" or "world-inspector" or "town" or "forest" or "crypt" or "battle" or "victory" or "atlas"))
            {
                throw new ArgumentException("Unknown scene.");
            }

            return o;
        }
    }
}
