using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    internal bool WillingToRescue(Actor resident, Actor casualty, int dangerRange)
    {
        int trust = resident.Trust.GetValueOrDefault(casualty.Id);
        if (trust <= -20 || resident.Health * 3L <= resident.MaxHealth || resident.Body.Hunger >= Simulation.StarveAt) return false;
        var trait = Disposition(resident);
        int willingness = 20 + trust + (resident.FriendId == casualty.Id ? 40 : 0)
            + (trait?.Value == "fellowship" ? 20 : 0) + (resident.TraitId == "trait.bold" ? 20 : 0);
        var sight = VisionFrom(resident.Position);
        int threats = State.Actors.Count(a => a.Life == LifeState.Alive && IsHostile(a)
            && a.Position.Distance(resident.Position) <= dangerRange && sight.Contains(a.Position));
        if (threats > 0) willingness -= trait?.Aversion == "danger" ? 30 : trait?.Aversion == "conflict" ? 15 : 0;
        return willingness > threats * 30;
    }

    internal void RememberNativeRescue(Actor rescuer, Actor casualty)
    {
        ChangeTrust(casualty, rescuer, 12);
        Remember(casualty, "revived", rescuer.Name + " helped me back to my feet.", 8, rescuer.Id);
        if (!rescuer.Recruited) Remember(rescuer, "rescued", "I helped " + casualty.Name + " survive.", 4, casualty.Id);
    }

    internal void ShareRecruitSupplies(Actor recruit)
    {
        foreach (var (stack, count) in recruit.Cargo)
            if (count > 0) Add(stack, count);
        recruit.Cargo.Clear();
    }
}
