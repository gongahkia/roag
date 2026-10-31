using Roag.Core.World;

namespace Roag.Client;

internal static class WorldStartup
{
    public static (IGameSession Session, bool Resumed) Open(Options options, Action<WorldDiagnosticEvent>? diagnostics = null)
    {
        bool resume = options.Load || !options.NewGame && !options.Preview && File.Exists(options.Save);
        bool native = options.NativeDungeonLaunch && !options.WorldSmoke;
        int seed = options.SeedSpecified || options.Preview ? options.Seed : FreshSeed();
        var session = resume ? SessionStore.Load(options.Save, diagnostics) : native
            ? WorldSession.CreateDungeonRun(seed) : WorldSession.Create(seed, diagnostics: diagnostics);
        if (native && resume && session is not WorldSession { State.Dungeon: not null })
        {
            (session as IDisposable)?.Dispose();
            throw new InvalidDataException("This save belongs to an earlier prototype. Start an explicit new run; the selected save is preserved.");
        }
        if (session is WorldSession nativeWorld && native) nativeWorld.DiagnosticSink = diagnostics;
        if (session is WorldSession world) world.Paused = options.Preview;
        return (session, resume);
    }
    internal static int FreshSeed() => BitConverter.ToInt32(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4));
}
