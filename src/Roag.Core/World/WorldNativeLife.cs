namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private void RememberCombatFall(Actor fallen)
    {
        foreach (var witness in State.Actors.Where(a => a.Id != fallen.Id && a.Life == LifeState.Alive
            && (a.FriendId == fallen.Id || a.Trust.GetValueOrDefault(fallen.Id) > 0 || a.Recruited && fallen.Recruited)))
            Remember(witness, "fallen-friend", fallen.Name + " fell in the fighting.", -8, fallen.Id);
    }

    internal void RecordNativeFall(Actor fallen)
    {
        if (State.NativeCampaign == null) return;
        // The retained combat applies an immediate party shock, then the
        // relationship memory. Permanent death after downing repeats neither.
        foreach (var member in Party.Where(a => a.Id != fallen.Id && a.Life == LifeState.Alive))
            member.Stress = Math.Min(100, member.Stress + 10);
        RememberCombatFall(fallen);
    }

    internal void RecordNativeDeath(Actor fallen, bool wasAlive)
    {
        if (State.NativeCampaign == null) return;
        if (wasAlive)
        {
            fallen.Body.Wounds = Math.Min(10, fallen.Body.Wounds + 1);
            RememberCombatFall(fallen);
        }
        if (!fallen.Recruited) LeaveWildlifeRemains(fallen);
    }
}
