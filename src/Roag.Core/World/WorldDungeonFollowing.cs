using Roag.Core.Expeditions;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public const int DungeonRegroupRadius = 6;

    /// <summary>Free group policy changes never relocate actors, reorder party IDs or touch readiness.</summary>
    public ActionResult RegroupDungeonParty(long leaderId)
    {
        if (!HasNativeParty || State.AwaitingContinuation) return new(false, "No active dungeon party.");
        var leader = Party.FirstOrDefault(a => a.Id == leaderId && a.Life == LifeState.Alive);
        if (leader == null || IsActorInCombat(leader.Id)) return new(false, "Regroup around a conscious member outside local combat.");
        var nearby = Party.Where(a => a.Life == LifeState.Alive && a.Position.Distance(leader.Position) <= DungeonRegroupRadius
            && !IsActorInCombat(a.Id)).ToArray();
        if (nearby.Length < 2) return new(false, "Bring companions within six tiles on this floor to regroup.");
        foreach (var member in nearby) DetachDungeonMember(member.Id);
        foreach (var member in nearby.Where(a => a.Id != leader.Id)) PartyFollowing![member.Id] = leader.Id;
        return new(true, "Nearby companions follow " + leader.Name + " on their own paid turns.");
    }

    public ActionResult ChooseDungeonLeader(long memberId)
    {
        if (!HasNativeParty || State.AwaitingContinuation) return new(false, "No active dungeon party.");
        var leader = Party.FirstOrDefault(a => a.Id == memberId && a.Life == LifeState.Alive);
        if (leader == null) return new(false, "Choose a conscious party member.");
        long old = PartyFollowing!.GetValueOrDefault(memberId, memberId);
        var group = PartyFollowing!.Where(p => p.Value == old).Select(p => p.Key).Append(old).ToArray();
        foreach (long id in group) PartyFollowing!.Remove(id);
        foreach (long id in group.Where(id => id != memberId)) PartyFollowing![id] = memberId;
        return new(true, leader.Name + " leads this group; combat turn order is unchanged.");
    }

    public ActionResult SplitDungeonMember(long memberId)
    {
        if (!HasNativeParty || State.AwaitingContinuation || !Party.Any(a => a.Id == memberId && a.Life != LifeState.Dead))
            return new(false, "Choose a surviving party member.");
        DetachDungeonMember(memberId);
        return new(true, "Member split. Separated members receive individual scheduled turns.");
    }

    internal void DetachDungeonMember(long memberId)
    {
        if (!HasNativeParty) return;
        PartyFollowing!.Remove(memberId);
        foreach (long id in PartyFollowing!.Where(p => p.Value == memberId).Select(p => p.Key).ToArray())
            PartyFollowing!.Remove(id);
    }

    internal void FollowDungeonRecruit(Actor member, Actor recruiter)
    {
        if (!HasNativeParty || IsActorInCombat(recruiter.Id) || IsActorInCombat(member.Id)) return;
        long leaderId = PartyFollowing!.GetValueOrDefault(recruiter.Id, recruiter.Id);
        var leader = ActorById(leaderId);
        if (leader.Life == LifeState.Alive && member.Position.Distance(leader.Position) <= DungeonRegroupRadius)
            PartyFollowing![member.Id] = leaderId;
    }
}
