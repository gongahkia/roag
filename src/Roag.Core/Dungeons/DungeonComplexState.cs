using Roag.Core.World;
using System.Text.Json.Serialization;

namespace Roag.Core.Dungeons;

/// <summary>Physical complex ownership. Live tiles and actors remain in the shared world.</summary>
public sealed record DungeonComplexState(DungeonParcel Parcel, int Seed, DungeonRunState Run)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Populated { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool KnownEntrance { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SpecialDungeonState? Special { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DungeonComplexRoutes? Routes { get; set; }
    public bool Contains(WorldPosition position) => position.Floor is >= 1 and <= 7 && Parcel.Contains(position);

    public WorldRoomId? RoomAt(WorldPosition position) => Contains(position) ? Run.RoomAt(position, Parcel.Id) : null;

    internal void Validate(WorldPlan plan, IReadOnlyDictionary<long, Actor> actors)
    {
        if (plan.FloorCount < 8 || Parcel == null || Parcel.Id is < 1 or > 8 || Run == null)
            throw new InvalidDataException("Invalid physical dungeon complex ownership.");
        var parcel = DungeonParcelPlanner.Reserve(plan, Parcel.Id)[^1];
        if (Parcel != parcel) throw new InvalidDataException("A physical complex moved outside its reserved parcel.");
        var blueprint = DungeonComplexPlanner.GenerateGeometry(plan, parcel);
        var floors = blueprint.Floors.Select(f => f.Describe()).Append(blueprint.Finale).ToArray();
        if (Seed != blueprint.Seed || Run.Floors == null || Run.Floors.Count != floors.Length
            || Run.Floors.Any(f => f == null) || Run.Terrain == null || Run.Terrain.Count > 100000
            || Run.Recruits == null || Run.Recruits.Count != 0 || Run.Following == null || Run.Following.Count != 0
            || Run.Stage is < 1 or > 4 || Run.RouteHistory == null || Run.RouteHistory.Count != Run.Stage - 1
            || Run.BossExitReceipts == null || Run.PendingEliteRewards == null
            || Run.Floors.Where(f => f.Boss > 0).Select(f => f.Boss).Distinct().Count() != Run.Floors.Count(f => f.Boss > 0))
            throw new InvalidDataException("Invalid physical complex layout or duplicated party policy.");
        for (int i = 0; i < Run.RouteHistory.Count; i++)
            if (!DungeonRoutes.ForStage(i + 1, Run.RouteHistory.Take(i).ToArray()).Any(r => r.Id == Run.RouteHistory[i]))
                throw new InvalidDataException("Invalid physical complex progression route.");
        for (int i = 0; i < floors.Length; i++)
        {
            var actual = Run.Floors[i]; var expected = floors[i];
            if (actual.Stage != expected.Stage || actual.Biome != expected.Biome || actual.Origin != expected.Origin
                || actual.Width != expected.Width || actual.Height != expected.Height || actual.Entry != expected.Entry
                || actual.Exit != expected.Exit || actual.Branch != expected.Branch || actual.Rooms == null
                || !actual.Rooms.SequenceEqual(expected.Rooms) || actual.Boss < 0
                || (actual.Boss > 0) != (Populated && (actual.Origin.Floor % 2 == 0 || actual.Stage == 4))
                || actual.Boss > 0 && (actual.Origin.Floor % 2 != 0 && actual.Stage != 4
                    || !actors.TryGetValue(actual.Boss, out var boss) || boss.Tactics?.Enemy == null
                    || boss.Tactics.DefinitionId != (actual.Stage == 4 ? "core:enemy/hollow_regent"
                        : DungeonCatalog.Default.Biomes.Single(b => b.Id == actual.Biome).EliteId)))
                throw new InvalidDataException("Invalid fixed physical dungeon floor, rooms or boss.");
        }
        foreach (var (key, glyph) in Run.Terrain)
        {
            var position = new WorldPosition((int)(key % WorldPlan.Size), (int)(key / WorldPlan.Size % WorldPlan.Size),
                (int)(key / WorldPlan.Size / WorldPlan.Size));
            if (key < 0 || key >= (long)WorldPlan.Size * WorldPlan.Size * plan.FloorCount || !plan.Contains(position)
                || glyph is not ('b' or 'g' or 'i' or 'm' or 'c') || !Run.Contains(position))
                throw new InvalidDataException("Invalid physical dungeon terrain overlay.");
        }
        if (Run.BossExitReceipts.Any(id => id <= 0 || !Run.Floors.Any(f => f.Boss == id) || actors[id].Life != LifeState.Dead)
            || Run.PendingEliteRewards.Any(id => !Run.BossExitReceipts.Contains(id) || !Run.Floors.Any(f => f.Boss == id && f.Stage < 4))
            || Run.Victory && !Run.Floors.Any(f => f.Stage == 4 && f.Boss > 0 && actors[f.Boss].Life == LifeState.Dead))
            throw new InvalidDataException("Invalid physical complex boss reward or completion receipt.");
    }
}
