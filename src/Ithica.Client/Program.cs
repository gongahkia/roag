using Ithica.Client;
using Ithica.Core.Colony;

if (args.Contains("--preview")) return Ithica.Client.Preview.PreviewEntry.Run(args.Where(a => a != "--preview").ToArray());

try
{
    var options = ClientOptions.Parse(args);
    if (options.Help)
    {
        Console.WriteLine("ITHICA / MonoGame moon colony\n  --seed N --width N --height N --demo --load --save PATH --skip-intro\n  --window-size WIDTHxHEIGHT --screenshot PATH --frames N --smoke-test\n--scene colony|preparation|surface|battle|structure|arrival|atlas|incidents|resident|work|mining|industry\n  --import-colony PATH (writes only to the new Ithica save)\nNew games choose a fresh seed unless --seed is supplied.\nHeadless: ./ithica.sh --headless or ./ithica.sh --benchmark.");
        return 0;
    }
    using var game = new IthicaGame(options);
    game.Run();
    return 0;
}
catch (Exception error) when (error is ArgumentException or FormatException or IOException or System.Text.Json.JsonException or OverflowException)
{
    Console.Error.WriteLine("Ithica: " + error.Message);
    return 1;
}

namespace Ithica.Client
{
    public sealed class ClientOptions
    {
        public int Seed { get; private set; } = 42;
        public bool SeedSpecified { get; private set; }
        private static int FreshSeed() => System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue);
        public int NextCampaignSeed() => SeedSpecified ? Seed : FreshSeed();
        public int Width { get; private set; } = 96;
        public int Height { get; private set; } = 64;
        public string SavePath { get; private set; } = Ithica.Core.Campaign.DefaultSave;
        public string? ImportPath { get; private set; }
        public string Scene { get; private set; } = "colony";
        public bool Load
        {
            get; private set;
        }
        public bool Demo
        {
            get; private set;
        }
        public bool SkipIntro
        {
            get; private set;
        }
        public bool SmokeTest
        {
            get; private set;
        }
        public string? Screenshot
        {
            get; private set;
        }
        public int Frames
        {
            get; private set;
        }
        public int WindowWidth { get; private set; } = 1440;
        public int WindowHeight { get; private set; } = 900;
        public bool Help
        {
            get; private set;
        }
        public static ClientOptions Parse(string[] args)
        {
            var result = new ClientOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string Value()
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for " + args[i]);
                    }

                    return args[++i];
                }
                int Number() => int.Parse(Value(), System.Globalization.CultureInfo.InvariantCulture);
                switch (args[i])
                {
                    case "--seed":
                        result.Seed = Number(); result.SeedSpecified = true;
                        break;
                    case "--width":
                        result.Width = Number();
                        break;
                    case "--height":
                        result.Height = Number();
                        break;
                    case "--save":
                        result.SavePath = Path.GetFullPath(Value());
                        break;
                    case "--import-colony":
                        result.ImportPath = Path.GetFullPath(Value());
                        break;
                    case "--scene":
                        result.Scene = Value();
                        break;
                    case "--load":
                        result.Load = true;
                        break;
                    case "--demo":
                        result.Demo = true;
                        break;
                    case "--skip-intro":
                        result.SkipIntro = true;
                        break;
                    case "--smoke-test":
                        result.SmokeTest = true;
                        break;
                    case "--screenshot":
                        result.Screenshot = Path.GetFullPath(Value());
                        break;
                    case "--frames":
                        result.Frames = Number();
                        break;
                    case "--help":
                        result.Help = true;
                        break;
                    case "--window-size":
                        var size = Value().Split('x');
                        if (size.Length != 2)
                        {
                            throw new ArgumentException("Window size must be WIDTHxHEIGHT.");
                        }

                        result.WindowWidth = int.Parse(size[0]);
                        result.WindowHeight = int.Parse(size[1]);
                        break;
                    default:
                        throw new ArgumentException("Unknown option: " + args[i]);
                }
            }
            if (result.Width < 48 || result.Height < 32 || result.Width > 2048 || result.Height > 2048 || result.Frames < 0)
            {
                throw new ArgumentException("Invalid dimensions or frame count.");
            }

            if (result.WindowWidth < 1024 || result.WindowHeight < 720 || result.WindowWidth > 4096 || result.WindowHeight > 2160)
            {
                throw new ArgumentException("Window size must be between 1024x720 and 4096x2160.");
            }

            if (!result.SeedSpecified && !result.SmokeTest && result.Frames == 0 && result.Scene == "colony"
                && !result.Load && result.ImportPath is null && !result.Help) result.Seed = FreshSeed();
            return result;
        }
    }
}
