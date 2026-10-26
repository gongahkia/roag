using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public sealed record ConsequenceFixture(long Actor, WorldPosition Source, int Town);
public static class ConsequencePreview
{
    public static ConsequenceFixture Prepare(WorldSession world)
    {
        var work = OccupationPreview.Prepare(world); var actor = world.ActorById(work.Actor);
        var observer = work.Source.Offset(1, 0); world.Chunks.Edit(observer, Material.Floor);
        world.State.Position = world.Party.Single().Position = observer;
        var town = world.State.Towns[actor.HomeTown]; town.Stocks["food"] = 0; town.Shortage = 1;
        world.State.Inventory["food"] = 40;
        world.State.Tasks.Add(new() { Id = "preview-food-" + actor.HomeTown, TownId = actor.HomeTown, Description = "Bring 20 food to the town." });
        world.Reveal(); world.Paused = true; return new(actor.Id, work.Source, actor.HomeTown);
    }
    public static void DamageSource(WorldSession world, ConsequenceFixture fixture)
    {
        var queued = world.Submit(new(WorldCommandKind.Build, fixture.Source, Material: Material.Floor));
        if (!queued.Success) throw new InvalidDataException(queued.Message);
        long id = world.State.Work.Single(w => w.Target == fixture.Source).Id;
        world.Paused = false;
        for (int i = 0; i < 120 && world.State.Work.Any(w => w.Id == id); i++)
            if (world.AdvanceTicks(1) != 1) throw new InvalidDataException("The consequence work fixture froze.");
        if (world.State.Work.Any(w => w.Id == id) || !world.ActorById(fixture.Actor).Consequences.ContainsKey(ConsequenceKind.DamagedLivelihood))
            throw new InvalidDataException("Completed source damage did not create a consequence.");
        world.Paused = true;
    }
    public static long PrepareDanger(WorldSession world, ConsequenceFixture fixture)
    {
        var actor = world.ActorById(fixture.Actor);
        var enemy = world.State.Actors.First(a => a.Kind == ActorKind.Raider && a.EncounterLeader == 0 && a.Life == LifeState.Alive);
        enemy.Position = actor.Position.Offset(0, -2); world.Chunks.Edit(enemy.Position, Material.Floor);
        enemy.Body.RestUntil = world.State.Tick + 1000;
        var hero = world.Party.Single(); hero.Role = 0; hero.BaseSpeed = 30; hero.BasePower = 100;
        world.Paused = false; world.AdvanceTicks(5); world.Paused = true;
        if (actor.Routine is not { Kind: RoutineKind.Danger } routine || routine.Other != enemy.Id)
            throw new InvalidDataException("The aid fixture did not put the resident in real danger.");
        return enemy.Id;
    }
    public static object Run(ContentCatalog content, int seed)
    {
        using var world = WorldSession.Create(seed, content); var fixture = Prepare(world); var actor = world.ActorById(fixture.Actor);
        long leader = world.Party.Single().Id; DamageSource(world, fixture);
        int harmed = actor.Trust.GetValueOrDefault(leader);
        if (world.ResidentGrievance(actor).Length == 0 || !world.ConsequenceDialogue(actor, world.Party.Single()).Contains("livelihood"))
            throw new InvalidDataException("Source damage did not affect decisions and dialogue.");
        if (!world.Submit(new(WorldCommandKind.CompleteDelivery)).Success || actor.Trust.GetValueOrDefault(leader) <= harmed
            || world.ResidentGrievance(actor).Length != 0 || !world.ConsequenceDialogue(actor, world.Party.Single()).Contains("food"))
            throw new InvalidDataException("Real food relief did not rebuild trust.");
        using var interrupted = WorldSession.Create(seed, content); var interruption = Prepare(interrupted); var worker = interrupted.ActorById(interruption.Actor);
        var target = interrupted.State.Position.Offset(0, 1);
        if (!interrupted.Submit(new(WorldCommandKind.Build, target, Material: Material.Wall)).Success
            || !interrupted.Submit(new(WorldCommandKind.AssignWork, ActorId: worker.Id, WorkId: interrupted.State.Work.Single().Id)).Success
            || !worker.Consequences.ContainsKey(ConsequenceKind.InterruptedWork)) throw new InvalidDataException("An actual occupation interruption was not remembered.");
        using var danger = WorldSession.Create(seed, content); var aid = Prepare(danger); long enemy = PrepareDanger(danger, aid);
        danger.StartBattle(enemy);
        for (int i = 0; i < 30 && danger.IsInCombat; i++)
            if (danger.State.Battle!.Active.Friendly) danger.Act(0, danger.State.Battle.Units.First(u => !u.Friendly && u.Alive).Id); else danger.AdvanceEnemy();
        var helped = danger.ActorById(aid.Actor);
        if (danger.IsInCombat || !helped.Consequences.ContainsKey(ConsequenceKind.AidInDanger)) throw new InvalidDataException("Victory over the real threat did not create an aid memory.");
        return new { passed = true, seed, fixture, harmedTrust = harmed, repairedTrust = actor.Trust.GetValueOrDefault(leader),
            consequences = new[] { actor.Consequences, worker.Consequences, helped.Consequences },
            scope = "Actual completed crop paving, paid food delivery, interrupted resident occupation and victory over a witnessed threat. Isolated approaches; real identities, costs, work and combat. No player saves, graphics or model calls." };
    }
}
