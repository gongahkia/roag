using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum DungeonServiceKind { OfferingFont, CleanseFlask, EscapeScroll, ReturnToken, CharmWell, AttunementForge, TravellingTrader, Memorial, Camp }
public sealed record DungeonService(long Id, string Marker, DungeonServiceKind Kind, WorldPosition Position, int Seed, long UsedBy = 0);
public sealed record RetiredBoon(string Id, BoonRarity Rarity);
public sealed record DungeonServiceChoice(int Choice, string Label, string Detail, bool Enabled = true, string Reason = "",
    int Price = 0, int HealthCost = 0, int Healing = 0, bool Cleanse = false, bool Ward = false, bool Focus = false,
    UtilityKind? Utility = null, bool Replacement = false, BoonOffer? Offer = null, RetiredBoon? Retire = null);

public sealed class DungeonServiceState
{
    public long NextService { get; set; } = 1;
    public List<DungeonService> Nodes { get; set; } = [];

    internal void Validate(WorldPlan plan, WorldState world, ContentCatalog? content)
    {
        if (NextService < 1 || Nodes == null || Nodes.Count > 128 || Nodes.Any(n => n == null)
            || Nodes.Select(n => n.Id).Distinct().Count() != Nodes.Count
            || Nodes.Select(n => n.Marker).Distinct(StringComparer.Ordinal).Count() != Nodes.Count
            || Nodes.Select(n => n.Position).Distinct().Count() != Nodes.Count)
            throw new InvalidDataException("Invalid finite service registry.");
        using var chunks = Nodes.Count == 0 ? null : new ChunkCache(plan, world, content ?? ContentCatalog.Default);
        long previous = 0;
        foreach (var node in Nodes)
        {
            if (node.Id <= previous || node.Id >= NextService || string.IsNullOrWhiteSpace(node.Marker)
                || !Enum.IsDefined(node.Kind) || node.Seed <= 0 || node.Seed >= RoagRandom.Modulus
                || !plan.Contains(node.Position) || world.Dungeon != null && !world.Dungeon.Contains(node.Position)
                || !Materials.Walkable(chunks!.At(node.Position))
                || node.UsedBy != 0 && !world.Party.Contains(node.UsedBy))
                throw new InvalidDataException("Invalid service identity, ground or receipt.");
            previous = node.Id;
        }
    }
}

public sealed partial class TacticalCombat
{
    public DungeonService SpawnDungeonService(DungeonServiceKind kind, string marker, WorldPosition position)
    {
        if (!Enum.IsDefined(kind) || string.IsNullOrWhiteSpace(marker) || !_world.CanEnter(position)
            || _world.DungeonTerrainAt(position) != null || ProtectedForcedLanding(position)
            || State.Services.Nodes.Any(n => n.Marker == marker || n.Position == position)
            || State.OptionalTerrain.Nodes.Any(n => n.Position == position))
            throw new ArgumentException("A finite service needs its own clear physical ground and stable marker.");
        int seed = RoagRandom.Derive((int)(WorldRandom.Hash(_world.Seed, 0, salt: 9402) % (RoagRandom.Modulus - 1)) + 1, marker);
        var node = new DungeonService(State.Services.NextService++, marker, kind, position, seed);
        State.Services.Nodes.Add(node); return node;
    }

    public IReadOnlyList<DungeonServiceChoice> DungeonServiceChoices(long serviceId, long recipientId)
    {
        var actor = Actor(recipientId); var node = State.Services.Nodes.SingleOrDefault(n => n.Id == serviceId);
        if (node == null || node.UsedBy != 0 || !Friendly(actor) || actor is not { Life: LifeState.Alive, Health: > 0, Tactics.Build: not null }
            || actor.Position.Distance(node.Position) > 1 || !_world.VisionFrom(actor.Position).Contains(node.Position)) return [];
        var choices = new List<DungeonServiceChoice>(); var inventory = actor.Tactics.Boons.Inventory;
        void Add(DungeonServiceChoice option)
        {
            if (option.Price > _world.Count("gold")) option = option with { Enabled = false, Reason = "The shared purse cannot afford this choice." };
            if (option.HealthCost >= actor.Health) option = option with { Enabled = false, Reason = "The offering must leave at least one HP." };
            if (option.Healing > 0 && actor.Health == actor.MaxHealth) option = option with { Enabled = false, Reason = "Already at full HP." };
            if (option.Offer is { } offer)
            {
                try
                {
                    var probe = option.Retire is { } removed ? inventory.Retire(removed.Id, removed.Rarity) : inventory;
                    _ = probe.Grant(offer.Boon, offer.Rarity, actor.Tactics.Build.Class);
                }
                catch (Exception e) when (e is InvalidOperationException or OverflowException)
                { option = option with { Enabled = false, Reason = e.Message }; }
            }
            choices.Add(option with { Choice = choices.Count + 1 });
        }
        DungeonServiceChoice Utility(UtilityKind kind, int price = 0) => new(0, ServiceUtilityName(kind),
            "One carried charge. " + (actor.Tactics.Supplies.Utility != null ? "Replaces the carried utility; confirm the exchange." : "Uses the one carried utility slot."),
            Price: price, Utility: kind, Replacement: actor.Tactics.Supplies.Utility != null);
        switch (node.Kind)
        {
            case DungeonServiceKind.OfferingFont:
                int payment = (int)(((long)actor.MaxHealth + 4) / 5);
                foreach (var offer in State.Chests.Offers(node.Seed, "core:chest/standard", actor.Tactics.Build.Class))
                    Add(new(0, BoonCatalog.Default.Get(offer.Boon).Name + " / " + offer.Rarity,
                        $"Offer {payment} HP. {BoonCatalog.Default.Get(offer.Boon).Description}", HealthCost: payment, Offer: offer));
                break;
            case DungeonServiceKind.CleanseFlask: Add(Utility(UtilityKind.CleanseFlask)); break;
            case DungeonServiceKind.EscapeScroll: Add(Utility(UtilityKind.EscapeScroll)); break;
            case DungeonServiceKind.ReturnToken: Add(Utility(UtilityKind.ReturnToken)); break;
            case DungeonServiceKind.TravellingTrader:
                var stock = node.Seed % 2 == 0 ? UtilityKind.CleanseFlask : UtilityKind.EscapeScroll;
                Add(Utility(stock, stock == UtilityKind.CleanseFlask ? 2 : 3));
                break; // Original food offer is deferred with survival by explicit user choice.
            case DungeonServiceKind.CharmWell:
                Add(new(0, "Focus / +2 to next attack", "One immediate attack batch within three paid actions. Replaces the prepared supply condition.", Focus: true));
                Add(new(0, "Brace / one-hit ward", "One positive hit within three paid actions. Replaces the prepared supply condition.", Ward: true));
                break;
            case DungeonServiceKind.Camp:
                Add(new(0, "Recover HP", "One use. Recover a quarter of maximum HP.", Healing: (int)(((long)actor.MaxHealth + 3) / 4)));
                Add(new(0, "Cleanse Burning and brace", "One positive hit within three paid actions; replaces the prepared supply condition.", Cleanse: true, Ward: true));
                break;
            case DungeonServiceKind.AttunementForge:
            case DungeonServiceKind.Memorial:
                var pool = State.Chests.AvailableBoons.Where(id => BoonCatalog.Default.Get(id).Allows(actor.Tactics.Build.Class)).Order(StringComparer.Ordinal).ToArray();
                foreach (var (id, owned) in inventory.Stacks.OrderBy(p => p.Key, StringComparer.Ordinal)) foreach (var rarity in BoonCatalog.Rarities)
                {
                    if (owned[rarity] == 0) continue;
                    var retired = new RetiredBoon(id, rarity); string name = BoonCatalog.Default.Get(id).Name + " / " + rarity;
                    if (node.Kind == DungeonServiceKind.Memorial)
                        Add(new(0, "Retire " + name, "Retire exactly one copy and recover 35% maximum HP. Pending effects remain committed.",
                            Healing: checked((int)(((long)actor.MaxHealth * 35 + 99) / 100)), Retire: retired));
                    else
                    {
                        var alternatives = pool.Where(other => other != id).ToList();
                        var random = new RoagRandom(RoagRandom.Derive(node.Seed, id + ":" + rarity.ToString().ToLowerInvariant()));
                        for (int i = 0; i < 3 && alternatives.Count > 0; i++)
                        {
                            int selected = random.NextOneBased(alternatives.Count) - 1; string other = alternatives[selected]; alternatives.RemoveAt(selected);
                            Add(new(0, "Trade " + name + " for " + BoonCatalog.Default.Get(other).Name,
                                "Retire one copy and pay three gold. " + BoonCatalog.Default.Get(other).Description,
                                Price: 3, Offer: new(other, rarity), Retire: retired));
                        }
                    }
                }
                break;
        }
        return choices;
    }

    private TacticalPreview PreviewService(Actor actor, TacticalIntent intent)
    {
        var options = DungeonServiceChoices(intent.Target, actor.Id);
        if (intent.Choice < 1 || intent.Choice > options.Count) return Invalid("Stand beside a visible unused service and choose an option.");
        var option = options[intent.Choice - 1];
        if (!option.Enabled) return Invalid(option.Reason);
        if (option.Replacement && !intent.ReplaceUtility) return Invalid("Confirm replacing the carried utility.");
        return new(true, "", ActionTiming.Recovery(100, EffectiveSpeed(actor)), [State.Services.Nodes.Single(n => n.Id == intent.Target).Position]);
    }

    private void CommitService(Actor actor, TacticalIntent intent, long action)
    {
        int index = State.Services.Nodes.FindIndex(n => n.Id == intent.Target); var node = State.Services.Nodes[index];
        var option = DungeonServiceChoices(node.Id, actor.Id)[intent.Choice - 1];
        var boons = actor.Tactics!.Boons;
        _world.State.Inventory["gold"] = _world.Count("gold") - option.Price;
        if (option.Price > 0) Emit("CurrencySpent", actor.Id, 0, action, node.Position, option.Price);
        if (option.HealthCost > 0)
        { actor.Health -= option.HealthCost; Emit("FontOfferingPaid", actor.Id, actor.Id, action, node.Position, option.HealthCost); }
        if (option.Retire is { } retired)
        {
            boons.Inventory = boons.Inventory.Retire(retired.Id, retired.Rarity);
            if (!boons.Inventory.Stacks.ContainsKey(retired.Id)) boons.Memory.Remove(retired.Id);
            Emit("BoonRetired", actor.Id, actor.Id, action, node.Position, ability: retired.Id);
        }
        if (option.Offer is { } offer)
        {
            boons.Inventory = boons.Inventory.Grant(offer.Boon, offer.Rarity, actor.Tactics.Build!.Class);
            Emit("BoonGranted", actor.Id, actor.Id, action, node.Position, ability: offer.Boon);
        }
        if (option.Healing > 0)
        {
            int gain = Math.Min(option.Healing, actor.MaxHealth - actor.Health); actor.Health += gain;
            RecordCombatHealing(actor.Id, actor.Id, gain);
            Emit("Healed", actor.Id, actor.Id, action, actor.Position, gain);
        }
        if (option.Cleanse && actor.Tactics.Burning != null)
        { RemoveBurning(actor); Emit("StatusCleansed", actor.Id, actor.Id, action, actor.Position); }
        if (option.Ward || option.Focus)
        {
            actor.Tactics.Supplies.WardActions = option.Ward ? 4 : 0;
            actor.Tactics.Supplies.FocusActions = option.Focus ? 4 : 0;
            Emit("SupplyPrepared", actor.Id, actor.Id, action, actor.Position, 3, ability: option.Ward ? "ward" : "focus");
        }
        if (option.Utility is { } utility)
        { actor.Tactics.Supplies.Utility = new(utility); Emit("UtilityCollected", actor.Id, actor.Id, action, node.Position, ability: UtilityId(utility)); }
        State.Services.Nodes[index] = node with { UsedBy = actor.Id };
        Emit("InteractionUsed", actor.Id, actor.Id, action, node.Position, ability: node.Kind.ToString());
    }

    public static string ServiceName(DungeonServiceKind kind) => kind switch
    {
        DungeonServiceKind.OfferingFont => "Offering font", DungeonServiceKind.CharmWell => "Charm well",
        DungeonServiceKind.AttunementForge => "Attunement forge", DungeonServiceKind.TravellingTrader => "Travelling trader",
        DungeonServiceKind.Memorial => "Memorial", DungeonServiceKind.Camp => "Camp",
        DungeonServiceKind.CleanseFlask => "Cleanse flask", DungeonServiceKind.EscapeScroll => "Escape scroll",
        DungeonServiceKind.ReturnToken => "Return token", _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static string ServiceUtilityName(UtilityKind kind) => kind switch
    { UtilityKind.CleanseFlask => "Take cleanse flask", UtilityKind.EscapeScroll => "Take escape scroll", _ => "Take return token" };
}
