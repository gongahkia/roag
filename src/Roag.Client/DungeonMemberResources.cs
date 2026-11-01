using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed record DungeonOwnedObject(long Owner, WorldPosition Position, string Name, string Sprite, string Status, bool Spent = false);

internal static class DungeonMemberResources
{
    public static string Summary(Actor member, long clock)
    {
        if (member.Tactics is not { Build: { } build, Resources: { } r }) return "";
        return build.Class switch
        {
            ClassKind.Stormcaller => $"Charge {r.StormCharge}/3 / ward {(r.StormWard ? "ready" : "spent")}",
            ClassKind.Duelist => r.Riposte ? "Riposte ready" : r.Parry != null ? "Parry raised" : "Parry available",
            ClassKind.Tinkerer => r.Sentry is { } s ? $"Sentry {s.Charges}/3 / {s.Phase} / F{s.Position.Floor}" : "Sentry available",
            ClassKind.Shade => r.Decoy is { } d ? $"Decoy active / F{d.Floor}" : "Decoy available",
            ClassKind.Trapper => r.Snare is { } t ? $"Snare armed / F{t.Floor}" : "Snare available",
            ClassKind.Spiritbinder => r.Spirit is { } spirit ? $"Spirit placed / F{spirit.Floor}" : "Spirit unplaced",
            ClassKind.Geomancer => r.Bulwark is { } wall ? $"Bulwark {Math.Max(0, wall.Expires - clock) / 100.0:0.00}s / F{wall.Position.Floor}" : "Bulwark available",
            ClassKind.Wayfinder => r.Beacon is { } beacon ? $"Beacon planted / F{beacon.Position.Floor}" : "Beacon unplanted",
            _ => ""
        };
    }

    public static IEnumerable<DungeonOwnedObject> Objects(WorldSession session)
    {
        foreach (var actor in session.Party)
        {
            if (actor.Tactics is not { } tactics) continue;
            var r = tactics.Resources;
            if (tactics.Supplies.Utility?.Marker is { } marker) yield return new(actor.Id, marker.Position, "Return token", "banner", "Planted utility marker");
            if (r.Decoy is { } decoy) yield return new(actor.Id, decoy, "Decoy", "resident", "Redirects eligible enemy aim");
            if (r.Snare is { } snare) yield return new(actor.Id, snare, "Snare", "trap", "Armed trap");
            if (r.Spirit is { } spirit) yield return new(actor.Id, spirit, "Spirit", "wisp", "Commanded by its owner");
            if (r.Beacon is { } beacon) yield return new(actor.Id, beacon.Position, "Beacon", "banner", "Return requires its owner's legal known route");
            if (r.Bulwark is { } wall) yield return new(actor.Id, wall.Position, "Bulwark", "boulder", $"Expires in {Math.Max(0, wall.Expires - session.DungeonCombat.Clock) / 100.0:0.00}s");
            if (r.Sentry is { } sentry) yield return new(actor.Id, sentry.Position, "Sentry", "bow", $"{sentry.Charges} shots / {sentry.Phase}", sentry.Phase == SentryPhase.Spent);
        }
    }
}
