using Roag.Core.Expeditions;
using Roag.Core.World;

namespace Roag.Core.Content;

public static class ActorFactory
{
    public static Actor CreateBoss(ContentCatalog content, string archetype, string variantId, long id, WorldPosition position, int town, int culture = 0, int faction = 0)
    {
        var d = content.Get<BossDefinition>(archetype); var v = content.Get<VariantDefinition>(variantId);
        var boss = new Actor { Id = id, Name = v.Name + " " + d.Name, Kind = ActorKind.Raider, Archetype = d.Id,
            VariantId = v.Id, EncounterLeader = id, SpriteId = d.Sprite!, BaseHealth = d.Health * v.HealthPercent / 100,
            BaseMana = d.Mana, BaseArmor = Math.Max(0, d.Armor + v.Armor), BasePower = Math.Max(1, d.Power + v.Power),
            BaseSpeed = Math.Max(1, d.Speed + v.Speed), Position = position, HomeTown = town, Culture = culture, FactionId = faction, Job = "dungeon guardian" };
        boss.Health = boss.MaxHealth; boss.Mana = boss.MaxMana; return boss;
    }
    public static Actor Create(ContentCatalog content, string archetype, int seed, long id, WorldPosition position, int town, int culture = 0, int faction = 0)
    {
        var d = content.Get<CharacterDefinition>(archetype);
        var r = WorldRandom.Stream(seed, unchecked((int)id), salt: 500);
        var a = new Actor { Id = id, Archetype = d.Id, Name = d.Name, Kind = d.ActorKind, Position = position, HomeTown = town,
            Culture = culture, FactionId = faction, BaseHealth = d.Health, BaseMana = d.Mana, BaseArmor = d.Armor, BasePower = d.Power, BaseSpeed = d.Speed, SpriteId = d.Sprite ?? "warden" };
        if (d.Names is { } pool)
        {
            var n = content.Get<NamesDefinition>(pool);
            a.Name = n.First[r.Next(n.First.Length)] + " " + n.Last[r.Next(n.Last.Length)];
            a.Background = n.Backgrounds[r.Next(n.Backgrounds.Length)];
            a.TraitId = n.Traits[r.Next(n.Traits.Length)]; a.Trait = content.Get<TraitDefinition>(a.TraitId).Name;
            a.Motivation = n.Motivations[r.Next(n.Motivations.Length)];
        }
        if (d.Classes.Length > 0)
        {
            var c = content.Get<ClassDefinition>(d.Classes[r.Next(d.Classes.Length)]);
            a.Role = c.Role; a.BaseHealth = c.Health; a.BaseMana = c.Mana; a.BaseArmor = c.Armor; a.BaseSpeed = c.Speed; a.SpriteId = c.Sprite!;
        }
        WorldSession.AssignCombatTrait(content, a, seed);
        a.Health = a.MaxHealth; a.Mana = a.MaxMana; return a;
    }
}
