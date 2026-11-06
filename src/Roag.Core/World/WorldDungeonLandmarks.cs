using Roag.Core.Dungeons;

namespace Roag.Core.World;

public sealed record DungeonEntranceReport(int Id, string Name, WorldPosition Position);

public sealed partial class WorldSession
{
    /// <summary>Shared remembered reports, never live occupants or unobserved underground terrain.</summary>
    public IReadOnlyList<DungeonEntranceReport> KnownDungeonEntrances()
        => State.NativeCampaign?.Complexes?.Where(c => c.KnownEntrance).OrderBy(c => c.Parcel.Id)
            .Select(DungeonEntrance).ToArray() ?? [];

    private DungeonEntranceReport DungeonEntrance(DungeonComplexState complex)
    {
        var position = complex.Run.Floors.Single(f => f.Stage == 1 && f.Origin.Floor == 1).Entry.Offset(4, 0) with { Floor = 0 };
        var town = Plan.Towns.OrderBy(t => t.Center.Distance(position)).ThenBy(t => t.Id).First();
        return new(complex.Parcel.Id, town.Name + " labyrinth", position);
    }

    private void ObserveDungeonEntrances()
    {
        if (State.NativeCampaign?.Complexes is not { } complexes) return;
        foreach (var complex in complexes)
            if (!complex.KnownEntrance && Visible.Contains(DungeonEntrance(complex).Position)) complex.KnownEntrance = true;
    }

    private string ReportDungeonEntrance(WorldPosition listener)
    {
        var origin = listener with { Floor = 0 };
        var next = State.NativeCampaign?.Complexes?.Where(c => !c.KnownEntrance)
            .Select(c => (Complex: c, Entrance: DungeonEntrance(c)))
            .OrderBy(c => c.Entrance.Position.Distance(origin)).ThenBy(c => c.Entrance.Id).FirstOrDefault();
        if (next is not { Complex: { } complex, Entrance: { } entrance }) return "";
        complex.KnownEntrance = true;
        return $" Entrance report: {entrance.Name}, {entrance.Position.Distance(origin)} surface tiles {Direction(origin, entrance.Position)} ({entrance.Position.X}, {entrance.Position.Y}). Walk to the stairs; the expedition journal keeps this location.";
    }
}
