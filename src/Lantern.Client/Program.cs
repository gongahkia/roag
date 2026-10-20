using System.Text.Json;
using Lantern.Core;
using Lantern.Client;

try
{
    var options = Options.Parse(args);
    if (options.Help)
    {
        Console.WriteLine("LANTERN BELOW / top-down party RPG\n./rpg.sh [--seed N] [--load] [--save PATH] [--skip-intro]\n  --scene town|forest|crypt|battle|victory|atlas\n  --headless (plays a deterministic complete expedition without graphics)\n  --smoke-test --screenshot PATH --frames N --window-size WIDTHxHEIGHT");
        return 0;
    }
    if (options.Headless)
    {
        var campaign = new Campaign(options.Seed);
        campaign.RunDemoExpedition();
        Console.WriteLine(JsonSerializer.Serialize(campaign.Summary(), new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    using var game = new LanternGame(options);
    game.Run();
    return 0;
}
catch (Exception e) when (e is ArgumentException or IOException or JsonException or FormatException or OverflowException)
{
    Console.Error.WriteLine("Lantern: " + e.Message);
    return 1;
}

namespace Lantern.Client
{
    public sealed class Options
    {
        public int Seed { get; private set; } = 42;
        public bool Load
        {
            get; private set;
        }
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
        public bool Help
        {
            get; private set;
        }
        public string Save { get; private set; } = Campaign.DefaultSave;
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
                    case "--seed":
                        o.Seed = int.Parse(Value());
                        break;
                    case "--load":
                        o.Load = true;
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
                        break;
                    case "--demo":
                        o.Scene = "battle";
                        o.SkipIntro = true;
                        break;
                    case "--window-size":
                        var parts = Value().Split('x');
                        if (parts.Length != 2)
                            throw new ArgumentException("Use WIDTHxHEIGHT.");
                        o.Width = int.Parse(parts[0]);
                        o.Height = int.Parse(parts[1]);
                        break;
                    default:
                        throw new ArgumentException("Unknown option: " + args[i]);
                }
            }
            if (o.Width < 1024 || o.Height < 720 || o.Width > 4096 || o.Height > 2160 || o.Frames < 0)
                throw new ArgumentException("Minimum window is 1024x720; maximum 4096x2160.");
            if (o.Scene is not ("town" or "forest" or "crypt" or "battle" or "victory" or "atlas"))
                throw new ArgumentException("Unknown scene.");
            return o;
        }
    }
}
