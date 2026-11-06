using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public GeneratedCampaignFinale? CampaignFinale => State.NativeCampaign?.Finale;
    public CampaignFinalRole? FinalCampaignRole => CampaignFinale?.Role;
    public WorldPosition? FinalCampaignPosition => CampaignFinale == null ? null : FinalBoss.Position;
    public IReadOnlyList<GeneratedBossSite> GeneratedBossSites => CampaignFinale?.BossSites ?? [];
    public RetainedGuardianHome? RetainedGuardianLocation(int sourceSite)
        => CampaignFinale?.Guardians.FirstOrDefault(g => g.SourceSite == sourceSite);
    public IReadOnlyList<RetainedGuardianHome> RetainedGuardiansAt(int siteId)
        => CampaignFinale?.Guardians.Where(g => g.SiteId == siteId).ToArray() ?? [];

    /// <summary>Native victory is a recorded milestone; the paid world remains playable.</summary>
    public bool ExpeditionEndedByVictory => State.Endgame.Victory && !HasNativeParty;

    internal void RecordGeneratedCampaignVictory(long boss)
    { Log(FinalObjective); RecordCampaignVictory(boss); }

    internal EnemyDefinition? FinalEnemyDefinition(Actor actor)
        => CampaignFinale is { Role: CampaignFinalRole.HollowRegent } finale && finale.FinalActor == actor.Id
            ? GeneratedCampaignFinale.FinalRegentDefinition : null;

    private string GeneratedFinalObjective()
    {
        var finale = CampaignFinale!;
        if (State.Endgame.Victory) return $"{FinalBoss.Name} has fallen. Victory is recorded; exploration of this world continues.";
        string location = finale.Role == CampaignFinalRole.HollowRegent ? $"the final sanctum of Deep Complex {finale.RegentComplex}"
            : Plan.Sites[finale.FinalSite].Name;
        var position = FinalBoss.Position;
        return $"{FinalBoss.Name} guards {location} ({position.X}, {position.Y}), depth {position.Floor}. "
            + "Challenge it in any order; other major bosses are optional preparation. Physical routes can be excavated.";
    }
}
