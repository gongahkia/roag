using Vagabonds.Core.World;

namespace Vagabonds.Client;

internal sealed partial class WorldGame
{
    private void PrepareProgressionScene()
    {
        CampPreview.Prepare(_session);
        int index = 0;
        foreach (var actor in _session.Party)
        {
            var role = _session.Content.Classes.Single(c => c.Role == index);
            // Leave an observed approach east; the camp is immediately north.
            actor.Position = _session.State.Position.Offset(-index, 0);
            actor.Role = role.Role; actor.SpriteId = role.Sprite!; actor.Level = 4;
            actor.BaseHealth = role.Health; actor.BaseMana = role.Mana; actor.BaseArmor = role.Armor;
            actor.BaseSpeed = 28 - index++ * 4; actor.BasePower = 6;
            actor.Body.Wounds = 0; actor.Body.Fatigue = actor.Body.Hunger = actor.Stress = 0;
            actor.Talents.Clear(); actor.Preparations.Clear(); actor.Equipment.Clear();
            // Isolate chosen developments; this innate only changes initiative equally.
            actor.CombatTrait = "talent.quick-footed";
            actor.Health = actor.MaxHealth; actor.Mana = actor.MaxMana;
        }
        var enemy = _session.State.Actors.First(a => a.Kind == ActorKind.Beast && a.EncounterLeader == 0);
        enemy.Position = _session.State.Position.Offset(6, 0); _session.Chunks.Edit(enemy.Position, Material.Floor);
        enemy.BaseHealth = enemy.Health = 500; enemy.BaseSpeed = 1; enemy.BasePower = 4;
        _session.Paused = true; _session.Reveal(); _panel = "inventory";
    }
}
