using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed record SpecialDungeonReport(int Complex, string Name, WorldPosition Activation,
    SpecialDungeonAftermath Aftermath, string Warning, long? ActivatedAt, long ExpiresAt, long ExpiredAt, long PursuerWarningAt);

public sealed partial class WorldSession
{
    /// <summary>Fresh generation only; existing absent-state saves never acquire a deadline on load.</summary>
    internal void InitializeSpecialDungeons(long finalActor = 0, SpecialDungeonTuning? tuning = null)
    {
        if (State.NativeCampaign?.Complexes is not { } complexes || State.Tactics is not { NextAction: 1 } tactics
            || tactics.Queue.Clock != State.NativeCampaign.StartedAt * tactics.Tuning.UnitsPerWorldTick
            || complexes.Any(c => c.Special != null)) throw new InvalidOperationException("Special dungeons must be selected once during generation.");
        tuning ??= new(); tuning.Validate();
        var choices = complexes.Where(c => c.Populated && !c.Run.Floors.Any(f => f.Boss == finalActor))
            .OrderBy(c => WorldRandom.Hash(Seed, c.Parcel.Id, salt: 9450)).ThenBy(c => c.Parcel.Id).Take(2).ToArray();
        for (int i = 0; i < choices.Length; i++) choices[i].Special = new()
        {
            Aftermath = (SpecialDungeonAftermath)i, Tuning = tuning,
            RandomState = RoagRandom.Derive(choices[i].Seed, "special-pressure")
        };
    }

    public IReadOnlyList<SpecialDungeonReport> KnownSpecialDungeons()
        => State.NativeCampaign?.Complexes?.Where(c => c.Special != null && (c.KnownEntrance || Visible.Contains(SpecialDungeonActivation(c))))
            .OrderBy(c => c.Parcel.Id).Select(SpecialDungeonReportFor).ToArray() ?? [];

    internal static WorldPosition SpecialDungeonActivation(DungeonComplexState complex)
        => complex.Run.Floors.Single(f => f.Stage == 1 && f.Origin.Floor == 1).Entry;

    private SpecialDungeonReport SpecialDungeonReportFor(DungeonComplexState complex)
    {
        var special = complex.Special!;
        string consequence = special.Aftermath == SpecialDungeonAftermath.LethalCollapse
            ? "Lethal collapse: everyone underground inside this labyrinth dies at expiry. Its entrance caves in; outside survivors may excavate the remains."
            : "Partial collapse: several clear outer passages may become diggable rubble. Surviving inhabitants, treasure and reward receipts persist; the remains can be revisited.";
        string warning = $"Optional unstable labyrinth. Activation spends one action and starts a {special.Tuning.DurationSeconds}-second world-time deadline. Reinforcements and a confined invulnerable pursuer can arrive. Retreat is allowed; leaving never resets the deadline. {consequence}";
        return new(complex.Parcel.Id, DungeonEntrance(complex).Name, SpecialDungeonActivation(complex), special.Aftermath,
            warning, special.ActivatedAt, special.ExpiresAt, special.ExpiredAt, special.PursuerAt);
    }

    internal IReadOnlySet<WorldPosition> SpecialAftermathReservations()
    {
        var protectedCells = CampaignBossPlacementReservations();
        foreach (var floor in DungeonFloors) { protectedCells.Add(floor.Entry); protectedCells.Add(floor.Exit); }
        return protectedCells;
    }

    internal bool CanAlterForSpecialAftermath(WorldPosition position, IReadOnlySet<WorldPosition> protectedCells)
        => !protectedCells.Contains(position) && Chunks.At(position) == Material.Floor && ObjectAt(position) == null;

    internal void RecordSpecialDungeonMessage(string message) => Log(message);
}
