using Roag.Core.Profiles;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private void AwardProfile(string key, long member = 0)
    {
        if (State.Profile is not { } progress || !progress.Earned.Add(AchievementCatalog.Prefix + key)) return;
        progress.Pending.Add(AchievementCatalog.Prefix + key);
        if (key == "chain_reaction") progress.Chains.Clear();
        var actor = _world.State.Actors.FirstOrDefault(a => a.Id == member) ?? _world.Party.First();
        Emit("AchievementEarned", actor.Id, actor.Id, 0, actor.Position, ability: AchievementCatalog.Prefix + key);
    }

    private void ObserveProfileEvent(TacticalEvent e, BoonCause? cause)
    {
        if (State.Profile is not { } progress) return;
        bool member = _world.State.Party.Contains(e.Source);
        switch (e.Kind)
        {
            case "EliteDefeated" when _world.State.Actors.Any(a => a.Id == e.Target && a.Life == LifeState.Dead && a.Tactics?.Enemy != null): AwardProfile("first_guardian", e.Source); break;
            case "SecretDiscovered" when member: AwardProfile("uncovered_truth", e.Source); break;
            case "BoonActivated" when member && cause is { ChainId: > 0 } && BoonCatalog.Default.Contains(e.Ability)
                && !progress.Earned.Contains(AchievementCatalog.Prefix + "chain_reaction"):
                var chain = progress.Chains.FirstOrDefault(c => c.Chain == cause.ChainId && c.Owner == e.Source);
                if (chain == null) { chain = new() { Chain = cause.ChainId, Owner = e.Source }; progress.Chains.Add(chain); }
                chain.Boons.Add(e.Ability);
                if (chain.Boons.Count >= 3) AwardProfile("chain_reaction", e.Source);
                break;
            case "EntityKilled" when member && !_world.State.Party.Contains(e.Target)
                && _world.State.Actors.Any(a => a.Id == e.Target && a.Life == LifeState.Dead && a.Tactics?.Enemy != null):
                AwardProfile("first_blood", e.Source); break;
            case "FloorChanged" when member:
                AwardProfile("pathfinder", e.Source);
                if (_world.State.Dungeon?.Floors.FirstOrDefault(f => f.Contains(e.Position)) is { } floor)
                    RecordDungeonEntry(e.Source, floor.Stage, floor.Stage == 4);
                break;
            case "ChestClaimed" when member: AwardProfile("treasure_hunter", e.Source); break;
            case "CurrencyCollected" when member && _world.Count("gold") >= 50: AwardProfile("coin_purse", e.Source); break;
            case "LevelUp" when _world.State.Party.Contains(e.Target):
                if (e.Amount >= 2) AwardProfile("first_level", e.Target);
                if (e.Amount >= 5) AwardProfile("veteran", e.Target);
                break;
            case "BoonGranted" when member: AwardProfile("boon_bearer", e.Source); break;
            case "TerrainBroken" when member: AwardProfile("stonebreaker", e.Source); break;
            case "OreBroken" when member: AwardProfile("ore_seeker", e.Source); break;
            case "TimedGateOpened" when member: AwardProfile("gate_opener", e.Source); break;
            case "VaultKeyCollected" when member: AwardProfile("key_bearer", e.Source); break;
            case "WardFlaskCollected" when member: AwardProfile("warded", e.Source); break;
            case "BrazierLit" when member: AwardProfile("torchbearer", e.Source); break;
            case "SupplyCollected" when member: AwardProfile("long_heal", e.Source); break;
            case "RegentPhaseChanged" when _world.State.Actors.Any(a => a.Id == e.Target && a.Tactics is { DefinitionId: "core:enemy/hollow_regent", Enemy.PhaseTwo: true }):
                AwardProfile("regent_awakened"); break;
        }
    }

    internal void RecordDungeonEntry(long member, int stage, bool sanctum)
    {
        var actor = _world.Party.FirstOrDefault(a => a.Id == member && a.Life == LifeState.Alive && a.Position.Floor > 0);
        if (actor == null || stage is < 1 or > 4 || sanctum && stage != 4) return;
        var floor = _world.State.Dungeon?.Floors.FirstOrDefault(f => f.Contains(actor.Position))
            ?? _world.State.NativeCampaign?.Complexes?.SelectMany(c => c.Run.Floors).FirstOrDefault(f => f.Contains(actor.Position));
        if (floor == null || floor.Stage != stage) return;
        if (stage >= 2) AwardProfile("deeper_road", member);
        if (sanctum) AwardProfile("sanctum_reached", member);
    }

    internal void RecordPursuerArrival(long pursuer)
    {
        if (_world.State.Actors.Any(a => a.Id == pursuer && a.Life == LifeState.Alive && a.Tactics?.DefinitionId == "core:enemy/extraction_pursuer"))
            AwardProfile("pressure_survivor");
    }

    internal void RecordCampaignVictory(long boss)
    {
        if (_world.State.AwaitingContinuation || !_world.Party.Any(a => a.Life == LifeState.Alive && a.Health > 0)) return;
        var victim = _world.State.Actors.FirstOrDefault(a => a.Id == boss);
        if (victim is { Life: LifeState.Dead, Tactics.DefinitionId: "core:enemy/hollow_regent" }) AwardProfile("regents_fall");
    }

    private void SettleProfileProgress()
    {
        if (State.Profile is not { } progress) return;
        if (_world.State.Dungeon is { Victory: true } run && run.Floors.FirstOrDefault(f => f.Stage == 4) is { } finale)
            RecordCampaignVictory(finale.Boss);
        if (!progress.Earned.Contains(AchievementCatalog.Prefix + "regents_fall") && _world.State.NativeCampaign?.Complexes is { } complexes)
            foreach (var complex in complexes.Where(c => c.Run.Victory))
                foreach (var floor in complex.Run.Floors.Where(f => f.Stage == 4 && f.Boss > 0)) _world.RecordComplexVictory(floor.Boss);
        if (progress.Chains.Count > 0)
            progress.Chains.RemoveAll(c => !State.BoonEffects.Dispatch.Chains.ContainsKey(c.Chain));
    }
}
