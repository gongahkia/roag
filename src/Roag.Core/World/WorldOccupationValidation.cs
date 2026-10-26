using Roag.Core.Content;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private static void ValidateOccupations(WorldPlan plan, WorldState state, ContentCatalog catalog)
    {
        if (new[] { state.ResourcesProduced, state.ResourcesConsumed, state.ResourcesLost, state.ResourcesImported, state.ResourcesExported, state.NamedResourcesProduced, state.RegionalResourcesProduced }.Any(v => v is < 0 or > 1000000000000000000)
            || state.NamedResourcesProduced + state.RegionalResourcesProduced > state.ResourcesProduced)
            throw new InvalidDataException("Invalid occupation accounting.");
        var used = state.Work.Select(w => w.Id).ToHashSet();
        var stacks = catalog.All<ItemDefinition>().Select(i => i.Stack).ToHashSet(StringComparer.Ordinal);
        bool Counts(Dictionary<string, int>? values) => values != null && values.All(p => stacks.Contains(p.Key) && p.Value is >= 1 and <= 9999);
        bool Equal(Dictionary<string, int> a, Dictionary<string, int> b) => a.Count == b.Count && a.All(p => b.GetValueOrDefault(p.Key) == p.Value);
        foreach (var actor in state.Actors)
        {
            if (actor.OccupationVersion == 0 && actor.Occupation == null && actor.OccupationAfter == 0 && actor.OccupationWorkplace == null && actor.OccupationAvoid == null && actor.OccupationAvoidUntil == 0) continue;
            var home = plan.Towns[actor.HomeTown].Center;
            if (actor.OccupationVersion != 1 || actor.Kind != ActorKind.Person || !ProductiveOccupation(actor)
                || actor.OccupationAfter < 0 || actor.OccupationAfter > state.Tick + TicksPerHour
                || actor.OccupationAvoidUntil < 0 || actor.OccupationAvoidUntil > state.Tick + TicksPerHour
                || actor.OccupationWorkplace is { } workplace && (!plan.Contains(workplace) || workplace.Distance(home) > 32)
                || actor.OccupationAvoid is { } avoid && (!plan.Contains(avoid) || avoid.Distance(home) > OccupationRadius))
                throw new InvalidDataException("Invalid occupation activation or destination.");
            if (actor.Occupation is not { } job) continue;
            if (actor.Recruited || job.Id < 1 || job.Id >= state.NextWorkId || !used.Add(job.Id) || !Enum.IsDefined(job.Kind) || !Enum.IsDefined(job.Phase)
                || !plan.Contains(job.Target) || job.Target.Distance(home) > OccupationRadius || !Enum.IsDefined(job.Before) || job.Definition == null
                || !double.IsFinite(job.Required) || job.Required is < 1 or > 1000 || !double.IsFinite(job.Progress) || job.Progress < 0 || job.Progress > job.Required
                || job.LastProgressTick < 0 || job.LastProgressTick > state.Tick || !plan.Contains(job.LastPosition)
                || !Counts(job.Inputs) || !Counts(job.Yield) || !Counts(job.Carried) || !Counts(job.Delivered) || !Counts(job.Consumed))
                throw new InvalidDataException("Invalid occupation work or cargo.");
            Dictionary<string, int> expectedCost = [], expectedYield = []; double required;
            switch (job.Kind)
            {
                case OccupationKind.Gather:
                    if (actor.Job != "farmer" || !catalog.Game.Resources.Contains(job.Definition) || catalog.Entries.GetValueOrDefault(job.Definition) is not ResourceDefinition node
                        || node.Underground || node.Yield.Count != 1 || !node.Yield.Any(p => catalog.Get<ItemDefinition>(p.Key).Stack == "food") || job.Before != node.Terrain)
                        throw new InvalidDataException("Invalid occupation crop source.");
                    required = node.Work; expectedYield = node.Yield.ToDictionary(p => catalog.Get<ItemDefinition>(p.Key).Stack, p => p.Value); break;
                case OccupationKind.Fell: case OccupationKind.Quarry:
                    var material = job.Kind == OccupationKind.Fell ? Material.Tree : Material.Rock;
                    if (actor.Job != (job.Kind == OccupationKind.Fell ? "woodcutter" : "quarrier") || job.Before != material || job.Definition.Length != 0)
                        throw new InvalidDataException("Invalid occupation extraction.");
                    required = Materials.Hardness(material) * 5; expectedYield[Materials.Yield(material)] = Materials.Hardness(material) + 1; break;
                case OccupationKind.RepairWorkplace:
                    if (actor.Job != "healer" || job.Before != Material.Camp || actor.OccupationWorkplace != job.Target || job.Definition.Length != 0)
                        throw new InvalidDataException("Invalid workplace repair.");
                    required = 12; expectedCost["wood"] = 1; break;
                default:
                    if (actor.Job != "healer" || actor.OccupationWorkplace != job.Target || !catalog.Game.Recipes.Contains(job.Definition)
                        || catalog.Entries.GetValueOrDefault(job.Definition) is not RecipeDefinition recipe
                        || (job.Kind == OccupationKind.Craft ? recipe.Output == null || catalog.Get<ItemDefinition>(recipe.Output).Stack != "medicine" || job.Before != Material.Camp
                            : recipe.Output != null || recipe.Material != Material.Camp || !Materials.Walkable(job.Before) || job.Before is Material.Camp or Material.StairsDown or Material.StairsUp))
                        throw new InvalidDataException("Invalid occupation recipe or workplace.");
                    required = recipe.Work; expectedCost = recipe.Cost.ToDictionary(p => catalog.Get<ItemDefinition>(p.Key).Stack, p => p.Value);
                    if (recipe.Output != null) expectedYield[catalog.Get<ItemDefinition>(recipe.Output).Stack] = recipe.Amount; break;
            }
            if (job.Required != required || !Equal(job.Inputs, expectedCost) || !Equal(job.Yield, expectedYield)) throw new InvalidDataException("Occupation differs from its actual recipe or resource.");
            if (job.Phase == OccupationPhase.Delivering)
            {
                if (job.Progress != job.Required || job.Kind is OccupationKind.BuildWorkplace or OccupationKind.RepairWorkplace
                    || job.Carried.Keys.Concat(job.Delivered.Keys).Concat(job.Consumed.Keys).Any(k => !job.Yield.ContainsKey(k))
                    || job.Consumed.Any(p => p.Key != "food") || job.Yield.Any(p => (long)job.Carried.GetValueOrDefault(p.Key) + job.Delivered.GetValueOrDefault(p.Key) + job.Consumed.GetValueOrDefault(p.Key) != p.Value))
                    throw new InvalidDataException("Invalid produced occupation ownership.");
            }
            else if (job.Phase == OccupationPhase.Returning)
            {
                if (job.Progress != 0 || job.Consumed.Count != 0 || job.Carried.Keys.Concat(job.Delivered.Keys).Any(k => !job.Inputs.ContainsKey(k))
                    || job.Inputs.Any(p => (long)job.Carried.GetValueOrDefault(p.Key) + job.Delivered.GetValueOrDefault(p.Key) != p.Value))
                    throw new InvalidDataException("Invalid returned occupation ingredients.");
            }
            else if (job.Progress >= job.Required || job.Delivered.Count != 0 || job.Consumed.Count != 0
                || (job.Phase == OccupationPhase.Collecting ? job.Progress != 0 || job.Carried.Count != 0 || job.Inputs.Count == 0 : !Equal(job.Carried, job.Inputs)))
                throw new InvalidDataException("Invalid incomplete occupation ownership.");
        }
    }
}
