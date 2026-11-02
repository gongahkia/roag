using Roag.Core.Content;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private void ObserveCampaignAdventurers()
    {
        if (State.NativeCampaign is not { } campaign) return;
        foreach (var town in Plan.Towns.Where(t => Party.Any(a => a.Life != LifeState.Dead && a.Position.Distance(t.Center) <= 28)))
        {
            if (campaign.AdventurerTowns.Contains(town.Id)) continue;
            var threats = State.Actors.Where(a => IsHostile(a) && a.Position.Distance(town.Center) <= 50).Select(a => a.Position).ToArray();
            var sites = Enumerable.Range(-28, 57).SelectMany(y => Enumerable.Range(-28, 57).Select(x => town.Center.Offset(x, y)))
                .Where(p => Plan.Contains(p) && Chunks.At(p) == Material.Floor && CanEnter(p) && ObjectAt(p) == null
                    && RoomAt(p) is { Area: RoomArea.Town } && threats.All(t => t.Distance(p) > 11))
                .OrderBy(p => RoagRandom.Derive(DungeonSeed, $"town-adventurer:{town.Id}:{p.X},{p.Y}")).ToArray();
            var occupied = new List<WorldPosition>();
            foreach (var position in sites)
            {
                if (occupied.Any(p => p.Distance(position) < 3)) continue;
                // Reserved source range is below dynamic identities and outside
                // retained encounters/discoveries; exploration order cannot change builds.
                long id = 80000 + town.Id * 4 + occupied.Count;
                if (State.Actors.Any(a => a.Id == id)) throw new InvalidDataException("Native adventurer identity already belongs to another actor.");
                var actor = ActorFactory.Create(Content, Content.Game.Traveller, Seed, id, position, town.Id, town.Culture, town.FactionId);
                var kind = (ClassKind)(RoagRandom.Derive(DungeonSeed, "recruit-class:" + id) % Enum.GetValues<ClassKind>().Length);
                actor.Job = "adventurer"; actor.CombatTrait = ""; actor.Talents.Clear(); actor.Equipment.Clear();
                actor.SpriteId = NativeAppearance.ForClass(kind);
                if (RoagRandom.Derive(DungeonSeed, "recruit-medicine:" + id) % 100 < State.Tactics!.Rescue.MedicineCarrierPercent
                    && State.Towns[town.Id].Stocks["medicine"] > 0)
                { actor.Cargo["medicine"] = 1; State.Towns[town.Id].Stocks["medicine"]--; }
                State.Actors.Add(actor); campaign.Recruits.Add(new(id, kind)); occupied.Add(position);
                if (occupied.Count == 3) break;
            }
            campaign.AdventurerTowns.Add(town.Id);
        }
    }
}
