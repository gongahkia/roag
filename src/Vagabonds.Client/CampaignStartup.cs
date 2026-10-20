using Vagabonds.Core.Expeditions;

namespace Vagabonds.Client;

internal static class CampaignStartup
{
    public static (Campaign Campaign, bool Resumed) Open(Options options)
    {
        bool resume = options.Load || !options.NewGame && !options.Preview && File.Exists(options.Save);
        return (resume ? Campaign.LoadRpg(options.Save) : new Campaign(options.Seed), resume);
    }
}
