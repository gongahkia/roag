namespace Roag.Core.Tactics;

/// <summary>Native authoring validation. Invalid conditions/effects cannot silently become no-ops.</summary>
public static class BoonDefinitionValidation
{
    public static void Validate(BoonDefinition definition)
    {
        void Require(bool valid, string field)
        { if (!valid) throw new InvalidDataException($"Invalid boon {field}: {definition.Id}."); }
        Require(!string.IsNullOrWhiteSpace(definition.Id) && definition.Id.StartsWith("core:boon/", StringComparison.Ordinal), "identity");
        Require(!string.IsNullOrWhiteSpace(definition.Name) && !string.IsNullOrWhiteSpace(definition.Description), "text");
        Require(!definition.Tags.IsDefault && definition.Tags.All(t => !string.IsNullOrWhiteSpace(t)), "tags");
        Require(!definition.Triggers.IsDefault && definition.Triggers.All(Enum.IsDefined)
            && definition.Triggers.Distinct().Count() == definition.Triggers.Length, "triggers");
        Require(definition.Scope != null && Enum.IsDefined(definition.Scope.Kind)
            && (definition.Scope.Kind is not (BoonScopeKind.Radius or BoonScopeKind.Nearby) || definition.Scope.Radius is >= 0), "scope");
        Require(Enum.IsDefined(definition.StackPolicy), "stack policy");
        Require(definition.Parameters != null && definition.Parameters.Count == BoonCatalog.Rarities.Length, "rarities");
        long lastChance = -1; int lastPower = -1;
        foreach (var rarity in BoonCatalog.Rarities)
        {
            Require(definition.Parameters!.TryGetValue(rarity, out var strength) && strength != null
                && strength.Chance >= lastChance && strength.Power >= lastPower && strength.Chance >= 0 && strength.Power >= 0, "rarity strength");
            lastChance = strength!.Chance; lastPower = strength.Power;
        }
        if (definition.Predicate != null) ValidatePredicate(definition.Predicate);
        Require(!definition.Effects.IsDefault && definition.Effects.All(e => e != null), "effects");
        foreach (var effect in definition.Effects) ValidateEffect(effect);
        Require(!definition.Modifiers.IsDefault && definition.Modifiers.All(m => m != null), "modifiers");
        Require(!definition.EligibleClasses.IsDefault && definition.EligibleClasses.Distinct().Count() == definition.EligibleClasses.Length
            && definition.EligibleClasses.All(ClassCatalog.Ids.Contains), "class eligibility");
        Require(!definition.Incompatible.IsDefault && definition.Incompatible.All(id => !string.IsNullOrWhiteSpace(id)), "incompatibilities");
        if (definition.AttackPayment != null) Require(definition.AttackPayment.Hp is > 0 and <= 100, "attack payment");
        if (definition.PatternPreview is { } preview)
            Require(Enum.IsDefined(preview.Trigger) && (preview.Axis == null || Enum.IsDefined(preview.Axis.Value))
                && preview.Special != false && definition.Effects.Length == 1 && definition.Effects[0].Pattern != null, "pattern preview");
        foreach (var modifier in definition.Modifiers)
        {
            Require(ValidModifier(modifier), "ability modifier");
            if (modifier.Kind == BoonModifierKind.WeaponForm)
                Require(definition.EligibleClasses.Length == 1
                    && definition.EligibleClasses[0] == ClassCatalog.Get(BoonCatalog.WeaponForm(modifier.Form!.Value).Class).Id, "weapon form class");
        }
    }

    public static void ValidatePredicate(BoonPredicate predicate, int depth = 0)
    {
        if (depth > 48) throw new InvalidDataException("Boon predicate tree is too deep.");
        switch (predicate)
        {
            case AllBoonPredicates all:
                if (all.Children.IsDefaultOrEmpty) throw new InvalidDataException("Empty all predicate.");
                foreach (var child in all.Children) ValidatePredicate(child, depth + 1);
                return;
            case AnyBoonPredicates any:
                if (any.Children.IsDefaultOrEmpty) throw new InvalidDataException("Empty any predicate.");
                foreach (var child in any.Children) ValidatePredicate(child, depth + 1);
                return;
            case NotBoonPredicate not: ValidatePredicate(not.Child, depth + 1); return;
            case BoonCondition c:
                if (!Enum.IsDefined(c.Op) || c.Text != null && c.Number != null
                    || c.Field != null && !Enum.IsDefined(c.Field.Value) || c.Neighborhood != null && !Enum.IsDefined(c.Neighborhood.Value))
                    throw new InvalidDataException("Invalid boon condition.");
                bool valid = c.Op switch
                {
                    BoonPredicateKind.OnSpecialTerrain => c.Text == "heat_vent",
                    BoonPredicateKind.EventStatus => c.Text == "burning",
                    BoonPredicateKind.PatternAxis or BoonPredicateKind.OwnerAxis => c.Text is "cardinal" or "diagonal",
                    BoonPredicateKind.RollFace => c.Number is >= 1 and <= 8,
                    BoonPredicateKind.SupplyKind => c.Text is "food" or "health",
                    BoonPredicateKind.EventAmount => c.Field is BoonEventField.Amount or BoonEventField.ActualGain or BoonEventField.Price && c.Min is >= 0,
                    BoonPredicateKind.PairAdjacentDamage => c.Neighborhood != null,
                    BoonPredicateKind.AdjacentHostiles => c.Min is >= 1,
                    BoonPredicateKind.DistinctChainGroup => c.Size is >= 2 && c.Field is null or BoonEventField.TargetId or BoonEventField.BoonId,
                    BoonPredicateKind.SourceInRadius => c.Radius is >= 0,
                    BoonPredicateKind.OwnerHpPercent or BoonPredicateKind.TargetHpPercentBefore => c.Max is >= 0 and <= 100,
                    BoonPredicateKind.AlignedHostiles => c.Min is >= 2,
                    BoonPredicateKind.EventDistance => c.Min is >= 0 && c.Max >= c.Min,
                    BoonPredicateKind.SourceId or BoonPredicateKind.TargetId => c.Number is > 0,
                    BoonPredicateKind.AbilityId or BoonPredicateKind.SourceTag or BoonPredicateKind.TargetTag
                        or BoonPredicateKind.SourceFaction or BoonPredicateKind.TargetFaction or BoonPredicateKind.EventKind
                        or BoonPredicateKind.FromTerrain or BoonPredicateKind.PathTerrain or BoonPredicateKind.SourceOnTerrain
                        or BoonPredicateKind.TargetNearTerrain or BoonPredicateKind.TerrainId or BoonPredicateKind.DamageType => !string.IsNullOrEmpty(c.Text),
                    _ => true
                };
                if (!valid) throw new InvalidDataException("Invalid boon predicate operands: " + c.Op);
                return;
            default: throw new InvalidDataException("Unknown boon predicate node.");
        }
    }

    public static void ValidateEffect(BoonEffect e)
    {
        bool valid = Enum.IsDefined(e.Kind) && (e.ProcCoefficient is null || double.IsFinite(e.ProcCoefficient.Value) && e.ProcCoefficient is >= 0 and <= 10)
            && (e.Radius is null or >= 0) && (e.DamageType == null || Enum.IsDefined(e.DamageType.Value))
            && (e.Status == null || Enum.IsDefined(e.Status.Value));
        valid &= e.Kind switch
        {
            BoonEffectKind.Area or BoonEffectKind.DamageNearest => e.Radius is >= 0,
            BoonEffectKind.DisplaceTarget => e.Mode != null && Enum.IsDefined(e.Mode.Value) && e.CollisionDamage != false,
            BoonEffectKind.StunTarget => e.Cap is >= 1 and <= 100 && e.Cooldown is >= 400 and <= 10000,
            BoonEffectKind.ExtendBurning => e.Cap is >= 3 and <= 8,
            BoonEffectKind.Echo => e.Delay is > 0,
            BoonEffectKind.ApplyStatus => e.Status == BoonStatus.Burning && e.Interval is > 0 && e.Ticks is > 0,
            BoonEffectKind.Prime or BoonEffectKind.SpeedBoost or BoonEffectKind.Ward => e.Duration is > 0 and <= 10000,
            BoonEffectKind.MarkDamage => e.Duration is > 0 and <= 10000 && e.Threshold is >= 2 && e.Threshold <= e.Cap && e.Cap <= 20,
            BoonEffectKind.StatusNeighbors or BoonEffectKind.CleanseOwner => e.Status == BoonStatus.Burning,
            _ => true
        };
        if (e.Target != null) valid &= e.Kind == BoonEffectKind.ApplyStatus && e.Target == BoonTarget.Nearest && e.Radius is >= 1 and <= 8;
        if (e.ClearPath != null) valid &= e.ClearPath == true && (e.Kind == BoonEffectKind.DamageNearest || e.Kind == BoonEffectKind.ApplyStatus && e.Target == BoonTarget.Nearest);
        if (e.Cooldown != null && e.Kind != BoonEffectKind.StunTarget) valid &= e.Kind == BoonEffectKind.HealOwner && e.Cooldown is >= 400 and <= 10000;
        if (e.Pattern != null || e.Kind == BoonEffectKind.Pattern) valid &= e.Pattern != null && Enum.IsDefined(e.Pattern.Value) && e.Kind is BoonEffectKind.Pattern or BoonEffectKind.Echo;
        if (e.ConsumePrime != null) valid &= e.ConsumePrime == true && e.Kind is BoonEffectKind.DamageTarget or BoonEffectKind.HealOwner or BoonEffectKind.Area;
        if (e.Scale != null) valid &= e.Scale == BoonScale.AdjacentHostiles || e.Scale == BoonScale.SupplyExcess && e.Kind == BoonEffectKind.Ward;
        if (!valid) throw new InvalidDataException("Invalid boon effect: " + e.Kind);
        if (e.When != null) ValidatePredicate(e.When);
    }

    private static bool ValidModifier(BoonModifier modifier)
    {
        if (!Enum.IsDefined(modifier.Kind)) return false;
        if (modifier.Kind == BoonModifierKind.WeaponForm)
            return modifier.Form != null && Enum.IsDefined(modifier.Form.Value) && BoonCatalog.WeaponForm(modifier.Form.Value).AbilityId == modifier.AbilityId;
        if (modifier.Form != null) return false;
        if (modifier.Kind == BoonModifierKind.AddDamage && Enum.GetValues<BoonWeaponForm>().Any(f => BoonCatalog.WeaponForm(f).AbilityId == modifier.AbilityId)) return true;
        return modifier.AbilityId switch
        {
            "core:ability/sweeping_slash" => modifier.Kind is BoonModifierKind.ForwardCell or BoonModifierKind.AddDamage,
            "core:ability/basic_attack" => modifier.Kind is BoonModifierKind.LineSecondCell or BoonModifierKind.AddDamage,
            "core:ability/dash" => modifier.Kind is BoonModifierKind.PathStrike or BoonModifierKind.DashPrime,
            "core:ability/ranger_shot" => modifier.Kind is BoonModifierKind.BranchNear or BoonModifierKind.AddDamage,
            "core:ability/ranger_volley" => modifier.Kind is BoonModifierKind.LineExtend or BoonModifierKind.AddDamage,
            "core:ability/arcanist_sigil" => modifier.Kind is BoonModifierKind.AreaExpand or BoonModifierKind.AddDamage,
            "core:ability/arcanist_bolt" => modifier.Kind is BoonModifierKind.StatusChain or BoonModifierKind.AddDamage,
            "core:ability/berserker_crush" => modifier.Kind is BoonModifierKind.Shockwave or BoonModifierKind.AddDamage,
            _ => false
        };
    }
}
