using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private void DrawCampaignBossDetails()
    {
        if (_session.State.Tactics!.CampaignBosses is not { Arenas.Count: > 0 } store) return;
        var combat = _session.DungeonCombat; var mouse = Mouse.GetState();
        WorldPosition? hover = null;
        if (_map.Contains(mouse.Position))
        {
            var point = _camera.ScreenToWorld(new(mouse.X, mouse.Y));
            hover = new((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        }
        foreach (var arena in store.Arenas)
        {
            if (arena.Center.Floor != _session.State.Position.Floor) continue;
            foreach (var valve in arena.Fixtures.Where(f => f.Kind == CampaignBossFixtureKind.Valve && _session.Visible.Contains(f.Position)))
            {
                var r = TileRect(valve.Position); bool dry = arena.Channels[valve.Channel].Drained;
                Text("#" + (valve.Channel + 1), r.X + 2, r.Y + 2, dry ? Muted : Gold);
                if (hover != valve.Position) continue;
                foreach (var cell in arena.Channels[valve.Channel].Cells.Where(_session.Visible.Contains))
                    FillClipped(TileRect(cell), new Color(113, 187, 195) * .3f);
                var pool = arena.Channels[valve.Channel].Pool;
                if (_session.Visible.Contains(pool)) { var tile = TileRect(pool); Text("POOL #" + (valve.Channel + 1), tile.X, tile.Y - 14, Gold); }
            }
            var boss = _session.ActorById(arena.Boss);
            if (boss.Life != LifeState.Alive || !_session.Visible.Contains(boss.Position)) continue;
            var memory = boss.Tactics!.Enemy!.CampaignBoss!;
            var attack = combat.State.Attacks.FirstOrDefault(a => a.Source == boss.Id && a.Boss != null);
            if (attack?.Boss is { } cast)
            {
                var tile = TileRect(boss.Position);
                Text(CampaignBossCatalog.MoveName(cast.Move) + (cast.Feature >= 0 ? " #" + (cast.Feature + 1) : "")
                    + $" {Math.Max(0, attack.Due - combat.Clock) / 100d:0.00}s", tile.X, tile.Y - 16, new Color(240, 112, 78));
                if (cast.Move == CampaignBossMove.ChannelSurge)
                {
                    var (x,y) = AttackGeometry.Offset(cast.Facing); var direction = new Vector2(x,y); var side = new Vector2(-y,x);
                    foreach (var p in attack.Cells.Where(_session.Visible.Contains))
                    {
                        var center = TileRect(p).Center.ToVector2(); var tip = center + direction * 7;
                        DrawDungeonLine(center - direction * 6, tip, Gold);
                        DrawDungeonLine(tip, tip - direction * 5 + side * 4, Gold);
                        DrawDungeonLine(tip, tip - direction * 5 - side * 4, Gold);
                    }
                }
            }
            else if (arena.Kind == CampaignBossKind.OssuaryCaptain && memory.ExposedUntil <= combat.Clock)
                foreach (var p in AttackGeometry.Arc(boss.Position, memory.ShieldFacing).Where(_session.Visible.Contains))
                    FillClipped(TileRect(p), Gold * .12f);
        }
    }

    private bool TryOperateCampaignBossValve(Point pointer)
    {
        var point = _camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var p = new WorldPosition((int)Math.Floor(point.X), (int)Math.Floor(point.Y), _session.State.Position.Floor);
        return OperateCampaignBossValve(p);
    }
    private bool OperateCampaignBossValve(WorldPosition p)
    {
        if (!_session.Visible.Contains(p) || _session.CampaignBossFixtureAt(p) is not { Kind: CampaignBossFixtureKind.Valve } valve) return false;
        var arena = _session.State.Tactics!.CampaignBosses!.Arenas.Single(a => a.Contains(p));
        var result = _session.SubmitDungeon(new(TacticalAction.CampaignBossInteract, Target: arena.Id, Choice: checked((int)valve.Id)));
        _notice = result.Success ? "Valve opened. The marked channel is permanently drained." : result.Message;
        CollectDungeonFeedback(); return true;
    }
    private bool OperateAdjacentCampaignBossValve()
    {
        if (_session.State.Tactics!.CampaignBosses is not { } store) return false;
        var p = store.Arenas.SelectMany(a => a.Fixtures)
            .FirstOrDefault(f => f.Kind == CampaignBossFixtureKind.Valve && f.Position.Distance(_session.DungeonMember.Position) <= 1
                && _session.Visible.Contains(f.Position) && !store.Arenas.Single(a => a.Contains(f.Position)).Channels[f.Channel].Drained)?.Position;
        return p is { } position && OperateCampaignBossValve(position);
    }
}
