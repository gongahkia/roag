using Roag.Core.World;

namespace Roag.Client;

internal static class WorldStartup
{
    public static (IGameSession Session, bool Resumed) Open(Options options, Action<WorldDiagnosticEvent>? diagnostics = null)
    {
        bool resume = options.Load || !options.NewGame && !options.Preview && File.Exists(options.Save);
        bool native = options.NativeDungeonLaunch && !options.WorldSmoke;
        int seed = options.SeedSpecified || options.Preview ? options.Seed : FreshSeed();
        var session = resume ? Resume(options, native, diagnostics) : native
            ? options.NativeCampaignLaunch ? WorldSession.CreateOpenCampaign(seed) : WorldSession.CreateDungeonRun(seed, survival: true) : WorldSession.Create(seed, diagnostics: diagnostics);
        if (session is WorldSession nativeWorld && native)
        {
            nativeWorld.DiagnosticSink = diagnostics;
            if (!resume && (!options.Preview || options.PlaytestDirectory != null || options.ProfileSpecified))
            {
                try { nativeWorld.EnablePermanentProfile(options.Save, options.ProfileDirectory); }
                catch { nativeWorld.Dispose(); throw; }
            }
        }
        if (session is WorldSession world) world.Paused = options.Preview;
        return (session, resume);
    }

    private static IGameSession Resume(Options options, bool native, Action<WorldDiagnosticEvent>? diagnostics)
    {
        try
        {
            var session = native && SessionStore.Format(options.Save) is "roag-world" or "vagabonds-world"
                ? WorldSession.Load(options.Save, diagnostics: diagnostics, profileDirectory: options.ProfileDirectory)
                : SessionStore.Load(options.Save, diagnostics);
            if (native && session is not WorldSession { HasNativeParty: true })
            {
                (session as IDisposable)?.Dispose();
                throw new InvalidDataException("This save belongs to an earlier prototype and cannot resume a native run.");
            }
            return session;
        }
        catch (InvalidDataException error) when (native)
        {
            string folder = Path.GetDirectoryName(Path.GetFullPath(options.Save))!;
            string fresh = Path.Combine(folder, "native-playtest.json");
            for (int suffix = 2; File.Exists(fresh) || Directory.Exists(fresh) || Directory.Exists(fresh + ".chunks"); suffix++)
                fresh = Path.Combine(folder, $"native-playtest-{suffix}.json");
            static string Quote(string value) => "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
            string launcher = Path.Combine(AppContext.BaseDirectory, "roag");
            string command = File.Exists(launcher) ? Quote(launcher) : "./roag.sh";
            throw new InvalidDataException($"{error.Message}\nExisting save preserved: {options.Save}\nStart a fresh run in a separate slot:\n  {command} --new --save {Quote(fresh)}\nResume that run later:\n  {command} --load --save {Quote(fresh)}", error);
        }
    }
    internal static int FreshSeed() => BitConverter.ToInt32(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4));
}
