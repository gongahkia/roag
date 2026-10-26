using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record EncounterGroupFixture(string Encounter, long Leader, long[] Members);
public sealed record EncounterGroupReport(int Seed, string Encounter, string Tactics, string Reward,
    List<EncounterFrame> Frames, CombatEvent[] Events, bool PartySurvived);

/// <summary>A saved-state-valid encounter fixture shared by the native client and headless tools.</summary>
public static class EncounterGroupPreview
{
    public static EncounterGroupFixture Prepare(WorldSession world, string encounterId = "encounter.cinder-ambush")
    {
        var definition = world.Content.Get<EncounterDefinition>(encounterId);
        CampPreview.Prepare(world);
        string trait = world.Content.All<TalentDefinition>().FirstOrDefault(t => t.Acquisition == "innate" && t.Bonuses.Keys.All(k => k is "healing" or "calm"))?.Id
            ?? world.Content.All<TalentDefinition>().First(t => t.Acquisition == "innate").Id;
        int index = 0;
        foreach (var actor in world.Party)
        {
            // Ranger acts, the channeler prepares, then the Warden can interrupt it.
            var role = world.Content.Classes.Single(c => c.Role == new[] { 1, 0, 2, 3 }[index]);
            actor.Role = role.Role; actor.SpriteId = role.Sprite!; actor.BaseHealth = role.Health;
            actor.BaseMana = role.Mana; actor.BaseArmor = role.Armor; actor.BasePower = 6;
            actor.BaseSpeed = new[] { 30, 20, 15, 10 }[index++];
            actor.Body.Wounds = actor.Stress = 0; actor.Body.Fatigue = 0;
            actor.Talents.Clear(); actor.Preparations.Clear(); actor.Equipment.Clear();
            actor.CombatTrait = trait;
            actor.Health = actor.MaxHealth; actor.Mana = actor.MaxMana;
        }
        long leader = world.State.NextActorId;
        var members = new List<long>();
        for (int i = 0; i < definition.Members.Length; i++)
        {
            long id = world.State.NextActorId++;
            var position = world.State.Position.Offset(5 + i, -2);
            world.Chunks.Edit(position, Material.Floor);
            var actor = ActorFactory.Create(world.Content, definition.Members[i], world.Seed, id, position, 0);
            actor.EncounterLeader = leader; actor.EncounterId = encounterId;
            actor.BaseHealth = actor.Health = i == 0 ? 160 : 100; actor.BaseSpeed = i == 0 ? 25 : 5;
            world.State.Actors.Add(actor); members.Add(id);
        }
        world.StartBattle(leader); world.Paused = true;
        return new(encounterId, leader, members.ToArray());
    }

    public static EncounterGroupReport Run(ContentCatalog content, string id, int seed)
    {
        var definition = content.Get<EncounterDefinition>(id);
        var world = WorldSession.Create(seed, content); Prepare(world, id);
        var frames = new List<EncounterFrame>();
        for (int i = 0; i < 32 && world.State.Battle is { Round: <= 4 } battle; i++)
        {
            int round = battle.Round; string active = battle.Active.Name;
            var intentions = battle.Units.Where(u => !u.Friendly && u.Alive).Select(u => world.EnemyIntent(u.Id)).ToArray();
            // Guard lets each composition demonstrate its behaviour without spending scarce consumables.
            var result = battle.Active.Friendly ? world.Act(3) : world.AdvanceEnemy();
            frames.Add(new(round, active, result.Message, world.State.Tick,
                battle.Units.ToDictionary(u => u.Id + ":" + u.Name, u => u.Health), intentions));
        }
        return new(seed, id, definition.Tactics, definition.Reward, frames, world.State.CombatEvents.ToArray(),
            world.Party.Any(a => a.Life == LifeState.Alive));
    }
}
