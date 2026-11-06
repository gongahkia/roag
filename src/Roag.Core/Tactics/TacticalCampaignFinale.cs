using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    internal void StrengthenFinalRegent(Actor actor)
    {
        if (_world.CampaignFinale is not { Role: CampaignFinalRole.HollowRegent } finale || finale.FinalActor != actor.Id
            || State.NextAction != 1 || actor is not { Life: LifeState.Alive, Tactics: { DefinitionId: "core:enemy/hollow_regent", Enemy: { Attacks: 0, PhaseTwo: false, Awareness: null } } }
            || actor.Health != actor.MaxHealth || State.Attacks.Any(a => a.Source == actor.Id)
            || State.Rewards.Enemies.ContainsKey(actor.Id) || actor.Tactics.RewardsClaimed)
            throw new InvalidOperationException("Only the selected untouched generated Regent can receive its fixed final role.");
        var definition = GeneratedCampaignFinale.FinalRegentDefinition;
        actor.Tactics.InnateStats = definition.CreateTactics().InnateStats;
        actor.Tactics.ExperienceReward = definition.XpReward; actor.Tactics.CurrencyDrop = definition.CurrencyDrop;
        actor.Health = actor.MaxHealth; actor.Name = definition.Name;
    }

    private void SettleCampaignFinale()
    {
        if (_world.CampaignFinale is not { VictoryAt: null } finale || _world.State.Endgame.Victory
            || _world.State.AwaitingContinuation || Actor(finale.FinalActor) is not { Life: LifeState.Dead, Tactics.RewardsClaimed: true } final)
            return;
        var victor = _world.Party.FirstOrDefault(a => a.Life == LifeState.Alive && a.Health > 0);
        if (victor == null) return;
        finale.VictoryAt = Clock; finale.Victor = victor.Id; _world.State.Endgame.Victory = true;
        Emit("CampaignVictory", victor.Id, final.Id, 0, final.Position);
        _world.RecordGeneratedCampaignVictory(final.Id);
    }
}
