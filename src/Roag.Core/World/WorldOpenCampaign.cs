using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    /// <summary>Constructs the living world and all fixed original complexes before exploration or paid actions.</summary>
    public static WorldSession CreateOpenCampaign(int seed, ClassKind kind = ClassKind.Adventurer)
    {
        var world = CreateNativeCampaign(seed, kind);
        try
        {
            foreach (var parcel in DungeonParcelPlanner.Reserve(world.Plan))
                world.InstallDungeonComplex(DungeonComplexPlanner.Generate(world.Plan, parcel));
            world.PopulateGeneratedCampaign();
            // Construction must not reveal distant entrances/rooms. Recompute
            // only the selected founder's normal private observation.
            world.Reveal();
            return world;
        }
        catch { world.Dispose(); throw; }
    }
}
