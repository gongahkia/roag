using Microsoft.Xna.Framework;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private bool TryOperateCampaignBossValve(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var p = new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        return OperateCampaignBossValve(p);
    }
    private bool OperateCampaignBossValve(WorldPosition p)
    {
        if (!_session.Visible.Contains(p) || _session.CampaignBossFixtureAt(p) is not { Kind: CampaignBossFixtureKind.Valve } valve) return false;
        var arena = _session.State.Tactics!.CampaignBosses.Arenas.Single(a => a.Contains(p));
        var result = _session.SubmitDungeon(new(TacticalAction.CampaignBossInteract, Target: arena.Id, Choice: checked((int)valve.Id)));
        _notice = result.Success ? "Valve opened. The marked channel is permanently drained." : result.Message;
        CollectDungeonFeedback(); return true;
    }
    private bool OperateAdjacentCampaignBossValve()
    {
        var p = _session.State.Tactics!.CampaignBosses.Arenas.SelectMany(a => a.Fixtures)
            .FirstOrDefault(f => f.Kind == CampaignBossFixtureKind.Valve && f.Position.Distance(_session.DungeonMember.Position) <= 1
                && _session.Visible.Contains(f.Position) && !_session.State.Tactics.CampaignBosses.Arenas.Single(a => a.Contains(f.Position)).Channels[f.Channel].Drained)?.Position;
        return p is { } position && OperateCampaignBossValve(position);
    }
}
