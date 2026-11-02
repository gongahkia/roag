using Roag.Core.Expeditions;
using Roag.Core.Content;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    /// <summary>The inspected member; readiness remains owned by the shared tactical timeline.</summary>
    public Actor DungeonMember => HasNativeParty && State.Tactics is { } tactics
        ? Party.FirstOrDefault(a => a.Id == tactics.ControlledActor)
            ?? throw new InvalidOperationException("No dungeon party member is selected.")
        : throw new InvalidOperationException("No native dungeon run is loaded.");

    public ActionResult SelectDungeonMember(long memberId)
    {
        if (!HasNativeParty || State.Tactics == null || State.AwaitingContinuation)
            return new(false, "This run has ended or no dungeon is loaded.");
        var member = Party.FirstOrDefault(a => a.Id == memberId);
        if (member is not { Life: not LifeState.Dead, Tactics.Build: not null })
            return new(false, "Choose a surviving party member.");
        State.Tactics.ControlledActor = member.Id;
        State.Position = member.Position;
        Chunks.Prefetch(member.Position);
        Reveal();
        return new(true, member.Name + " selected.");
    }

    private bool SelectedDungeonMemberReady => State.Tactics is { ActiveActor: > 0 } tactics
        && tactics.ControlledActor == tactics.ActiveActor;

    public ActionResult ConfigureDungeonEating(long memberId, bool automatic, bool duringCombat)
    {
        var member = Party.FirstOrDefault(a => a.Id == memberId && a.Life != LifeState.Dead && a.Tactics?.Build != null);
        if (!HasNativeParty || State.AwaitingContinuation || member == null)
            return new(false, "Choose a surviving member's eating policy.");
        member.Tactics!.AutoEat = automatic; member.Tactics.AutoEatInCombat = duringCombat;
        return new(true, "Eating policy saved. Automatic meals spend this member's scheduled turns.");
    }

    private void PopulateDungeonRecruits(DungeonFloorState floor)
    {
        var room = floor.Rooms.Single(r => r.Role == "start");
        int count = floor.Origin.Floor == 1 ? 3 : 1;
        var occupied = new List<WorldPosition>();
        var sites = Enumerable.Range(1, room.Width - 2)
            .SelectMany(x => Enumerable.Range(1, room.Height - 2).Select(y => room.Origin.Offset(x, y)))
            .Where(p => p != floor.Entry && p != floor.Exit && CanEnter(p) && ObjectAt(p) == null
                && Chunks.At(p) == Material.Floor && DungeonTerrainAt(p) == null
                && EncounterDirections.Count(d => GroundOpen(p.Offset(d.X, d.Y))) >= 3)
            .OrderBy(p => RoagRandom.Derive(DungeonSeed, $"recruit-cell:{floor.Origin.Floor}:{p.X},{p.Y}"));
        foreach (var p in sites)
        {
            if (occupied.Any(other => p.Distance(other) < 2)) continue;
            var actor = ActorFactory.Create(Content, Content.Game.Traveller, Seed, State.NextActorId++, p, 0);
            var kind = (ClassKind)(RoagRandom.Derive(DungeonSeed, "recruit-class:" + actor.Id) % Enum.GetValues<ClassKind>().Length);
            actor.Job = "adventurer"; actor.CombatTrait = ""; actor.Equipment.Clear(); actor.Talents.Clear();
            actor.Body.Hunger = actor.Body.Fatigue = 0; actor.Body.Wounds = 0;
            actor.SpriteId = NativeAppearance.ForClass(kind);
            // Existing adventurers may carry a real, finite rescue provision.
            // This is seeded at creation, never spawned when someone falls.
            if (RoagRandom.Derive(DungeonSeed, "recruit-medicine:" + actor.Id) % 100 < State.Tactics!.Rescue.MedicineCarrierPercent)
                actor.Cargo["medicine"] = 1;
            State.Actors.Add(actor); State.Dungeon!.Recruits.Add(new(actor.Id, kind));
            occupied.Add(p);
            if (occupied.Count == count) return;
        }
        throw new InvalidDataException("The start room lacks clear ground for its adventurers.");
    }
}
