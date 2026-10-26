using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record ParryWindow(WorldPosition Origin, Facing Facing, long Expires);
public sealed record BulwarkState(WorldPosition Position, long Action, long Schedule, long Expires);
public sealed record ReturnBeacon(WorldPosition Position, WorldRoomId Room);
public enum SentryPhase { Waiting, Impact, Spent }
public sealed class SentryState
{
    public WorldPosition Position { get; set; }
    public int Charges { get; set; } = 3;
    public SentryPhase Phase { get; set; }
    public long Action { get; set; }
    public long Schedule { get; set; }
    public long Due { get; set; }
    public WorldPosition[] Cells { get; set; } = [];
}

/// <summary>Only the owning actor holds these resources. No selected-member globals.</summary>
public sealed class ClassResources
{
    public WorldPosition? Decoy { get; set; }
    public WorldPosition? Snare { get; set; }
    public WorldPosition? Spirit { get; set; }
    public SentryState? Sentry { get; set; }
    public BulwarkState? Bulwark { get; set; }
    public ReturnBeacon? Beacon { get; set; }
    public ParryWindow? Parry { get; set; }
    public bool Riposte { get; set; }
    public int StormCharge { get; set; }
    public bool StormWard { get; set; }
    public long StormLastAction { get; set; }

    public void Validate(ClassKind? kind)
    {
        if (Decoy != null && kind != ClassKind.Shade || Snare != null && kind != ClassKind.Trapper
            || Spirit != null && kind != ClassKind.Spiritbinder || Sentry != null && kind != ClassKind.Tinkerer
            || Bulwark != null && kind != ClassKind.Geomancer
            || Beacon != null && kind != ClassKind.Wayfinder
            || (Parry != null || Riposte) && kind != ClassKind.Duelist
            || (StormCharge != 0 || StormWard || StormLastAction != 0) && kind != ClassKind.Stormcaller
            || StormCharge is < 0 or > 3 || StormLastAction < 0
            || Parry is { } parry && (!Enum.IsDefined(parry.Facing) || parry.Expires < 0))
            throw new InvalidDataException("Class resources do not belong to this build.");
        if (Bulwark is { } block && (block.Action < 1 || block.Schedule < 1 || block.Expires < 0))
            throw new InvalidDataException("Invalid bulwark state.");
        if (Sentry is { } sentry && (!Enum.IsDefined(sentry.Phase) || sentry.Charges is < 0 or > 3
            || sentry.Cells is null || sentry.Action < 1
            || sentry.Phase == SentryPhase.Spent && (sentry.Charges != 0 || sentry.Schedule != 0 || sentry.Due != 0 || sentry.Cells.Length != 0)
            || sentry.Phase != SentryPhase.Spent && (sentry.Schedule < 1 || sentry.Due < 0)
            || sentry.Phase == SentryPhase.Waiting && (sentry.Charges == 0 || sentry.Cells.Length != 0)
            || sentry.Phase == SentryPhase.Impact && sentry.Cells.Length is < 1 or > 4))
            throw new InvalidDataException("Invalid sentry state.");
    }
}
