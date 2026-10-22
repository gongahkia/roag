using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public sealed record EncounterFrame(int Round, string Active, string Action, long Tick, Dictionary<string, int> Health, string[] Intentions);
public sealed record EncounterReport(int Seed, string Boss, string Variant, string Tactics, string Weakness, List<EncounterFrame> Frames, bool PartySurvived);
public static class EncounterPreview
{
    public static EncounterReport Run(ContentCatalog content, string id, int seed)
    {
        var definition = content.Get<BossDefinition>(id); var world = WorldSession.Create(seed, content);
        foreach (var a in world.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(3).ToArray())
        { a.Position = world.State.Position; world.Submit(new(WorldCommandKind.Recruit, ActorId: a.Id)); }
        long bossId = world.State.NextActorId++;
        var boss = ActorFactory.CreateBoss(content, id, definition.Variants[0], bossId, world.State.Position.Offset(2, 0), 0);
        world.State.Actors.Add(boss);
        foreach (string minionId in definition.Minions)
        {
            var minion = ActorFactory.Create(content, minionId, seed, world.State.NextActorId++, boss.Position, 0);
            minion.EncounterLeader = bossId; world.State.Actors.Add(minion);
        }
        world.StartBattle(boss.Id); var frames = new List<EncounterFrame>();
        for (int i = 0; i < 80 && world.State.Battle is { Round: <= 4 } b; i++)
        {
            string name = b.Active.Name; int round = b.Round;
            var intentions = b.Units.Where(u => !u.Friendly && u.Alive).Select(u => world.EnemyIntent(u.Id)).ToArray();
            var result = b.Active.Friendly ? world.Act(0, b.Units.First(u => !u.Friendly && u.Alive).Id) : world.AdvanceEnemy();
            frames.Add(new(round, name, result.Message, world.State.Tick, b.Units.ToDictionary(u => u.Id + ":" + u.Name, u => u.Health), intentions));
        }
        return new(seed, id, boss.VariantId, definition.Tactics, definition.Weakness, frames, world.Party.Any(a => a.Life == LifeState.Alive));
    }
}
