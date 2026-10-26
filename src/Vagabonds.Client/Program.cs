using System.Text.Json;
using Vagabonds.Client;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.World;
using Vagabonds.Core.Diagnostics;

namespace Vagabonds.Client
{
public static class GameEntry
{
public static int Run(string[] args)
{
LocalSessionRecorder? recording = null;
UnhandledExceptionEventHandler? crashHandler = null;
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
              --scene world|world-streaming|world-site|world-output|world-supplies|world-occupations|world-habitat|world-migration|world-recovery|world-memories|world-weather|world-routines|world-resources|world-wildlife|world-situation|world-camp|world-discovery|world-battle|world-cooperation|world-boss|world-progression|world-encounters|world-inspector (preview without autosaving)
              --scene town|forest|crypt|battle|victory|atlas (legacy previews)
              --world-headless (recruit, construct, trade and verify simulation)
              --world-report (macro plan and all town/site WFC diagnostics)
              --validate-worlds N (macro checks for consecutive seeds from --seed)
              --world-smoke (native render/cache/material checks and measured timings)
              --headless | --generation-report | --validate-seeds N | --smoke-test (legacy checks)
              --screenshot PATH --frames N --window-size WIDTHxHEIGHT
              --ui-report PATH (mouse UI diagnostic snapshot; preview without autosaving)
              --session-root PATH (local session recordings; default ~/.vagabonds/sessions)
              --no-recording (disable ordinary session recording)
              --playtest-control PATH (isolated local mouse automation; explicit waits advance time)
            Click ground to travel; click people, caches or stairs to approach and interact.
            Use on-screen Play/Pause, action buttons and Menu. Right-click stops/closes.
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
    if (!options.NoRecording && (!options.Preview || options.PlaytestDirectory != null) && !options.SmokeTest)
    {
        recording = LocalSessionRecorder.TryStart(options.SessionRoot, new { build = LocalSessionRecorder.BuildIdentity(typeof(GameEntry).Assembly),
            selectedSave = options.Save, requestedSeed = options.SeedSpecified ? (int?)options.Seed : null,
            newGame = options.NewGame, requireLoad = options.Load, mode = options.PlaytestDirectory == null ? "ordinary" : "automated-playtest" });
        if (recording != null)
        {
            Console.WriteLine("Session diagnostics: " + recording.Folder);
            crashHandler = (_, e) => { if (e.ExceptionObject is Exception exception) { recording.Crash(exception); recording.Dispose(); } };
            AppDomain.CurrentDomain.UnhandledException += crashHandler;
        }
    }
    if (options.LegacyScene || options.SmokeTest && !options.WorldSmoke)
    {
        using var legacyGame = new VagabondsGame(options); legacyGame.Run();
    }
    else
    {
        Action<WorldDiagnosticEvent>? sink = recording == null ? null : recording.Observe;
        long opened = System.Diagnostics.Stopwatch.GetTimestamp();
        var (session, resumed) = WorldStartup.Open(options, sink);
        recording?.Record("world-open", new { session.Seed, resumed, format = session is WorldSession ? "world" : "legacy",
            elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(opened).TotalMilliseconds });
        if (session is WorldSession world) { using var game = new WorldGame(options, world, recording); game.Run(); }
        else { using var game = new VagabondsGame(options); game.Run(); }
    }
    return 0;
}
catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or InvalidDataException or JsonException or FormatException or OverflowException)
{
    recording?.Crash(e);
    Console.Error.WriteLine("Vagabonds: " + e.Message);
    return 1;
}
catch (Exception e) { recording?.Crash(e); throw; }
finally
{
    if (crashHandler != null) AppDomain.CurrentDomain.UnhandledException -= crashHandler;
    recording?.Dispose();
}

}
}

    public sealed class Options
    {
        public int Seed { get; private set; } = 42;
        public bool SeedSpecified { get; private set; }
        public bool Load
        {
            get; private set;
        }
        public bool NewGame { get; private set; }
        public bool Preview => _sceneSpecified || Frames > 0 || SmokeTest || UiReport != null || PlaytestDirectory != null;
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
        public string? UiReport { get; private set; }
        public string? SessionRoot { get; private set; }
        public bool NoRecording { get; private set; }
        public string? PlaytestDirectory { get; private set; }
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
                    case "--ui-report": o.UiReport = Path.GetFullPath(Value()); break;
                    case "--session-root": o.SessionRoot = Path.GetFullPath(Value()); break;
                    case "--no-recording": o.NoRecording = true; break;
                    case "--playtest-control": o.PlaytestDirectory = Path.GetFullPath(Value()); break;
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
                        o.SeedSpecified = true;
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
            if (o.PlaytestDirectory is { } playtest)
            {
                if (o.LegacyScene || o.Headless || o.WorldHeadless || o.GenerationReport || o.WorldReport || o.SmokeTest || o.ValidateWorlds > 0 || o.ValidateSeeds > 0 || o.Frames > 0 || o.ImportLegacy != null)
                    throw new ArgumentException("Playtest control requires an interactive world scene.");
                if (!args.Contains("--save")) o.Save = Path.Combine(playtest, "campaign.json");
                string relative = Path.GetRelativePath(playtest, o.Save);
                if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    throw new ArgumentException("Automated playtest saves must stay inside the isolated control directory.");
                foreach (string target in new[] { o.Save, o.Save + ".chunks" })
                {
                    string? current = target;
                    while (current != null && current.Length >= playtest.Length)
                    {
                        if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                            throw new ArgumentException("Automated playtest save paths must not contain symbolic links.");
                        current = Path.GetDirectoryName(current);
                    }
                }
                o.SessionRoot ??= Path.Combine(playtest, "recordings");
            }
            if (o.Load && o.NewGame)
            {
                throw new ArgumentException("Choose either --load or --new.");
            }
            if (o.Width < 1024 || o.Height < 720 || o.Width > 4096 || o.Height > 2160 || o.Frames < 0)
            {
                throw new ArgumentException("Minimum window is 1024x720; maximum 4096x2160.");
            }

            if (o.Scene is not ("world" or "world-streaming" or "world-site" or "world-output" or "world-supplies" or "world-occupations" or "world-habitat" or "world-migration" or "world-recovery" or "world-memories" or "world-weather" or "world-routines" or "world-resources" or "world-wildlife" or "world-situation" or "world-camp" or "world-discovery" or "world-battle" or "world-cooperation" or "world-boss" or "world-progression" or "world-encounters" or "world-inspector" or "town" or "forest" or "crypt" or "battle" or "victory" or "atlas"))
            {
                throw new ArgumentException("Unknown scene.");
            }

            return o;
        }
    }
}
