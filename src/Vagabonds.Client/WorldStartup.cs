using Vagabonds.Core.World;

namespace Vagabonds.Client;

internal static class WorldStartup
{
    public static (IGameSession Session, bool Resumed) Open(Options options, Action<WorldDiagnosticEvent>? diagnostics = null)
    {
        bool resume = options.Load || !options.NewGame && !options.Preview && File.Exists(options.Save);
        return (resume ? SessionStore.Load(options.Save, diagnostics) : WorldSession.Create(options.SeedSpecified || options.Preview ? options.Seed : FreshSeed(), diagnostics: diagnostics), resume);
    }
    internal static int FreshSeed() => BitConverter.ToInt32(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4));
}
