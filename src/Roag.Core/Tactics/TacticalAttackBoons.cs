using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record BoonPreviewCell(WorldPosition Position, string BoonId, BoonPreviewTrigger Condition, int? Delay);

public sealed partial class TacticalCombat
{
    private sealed record EffectiveAttack(AttackFootprint Footprint, int Damage, BoonWeaponForm? Form, MirrorReflection? Reflection);

    private EffectiveAttack ClassAttack(Actor actor, TacticalIntent intent, bool explicitAim, bool knownOnly)
    {
        var footprint = ClassFootprint(actor, intent, explicitAim);
        int damage = intent.Action == TacticalAction.BasicAttack ? actor.Power : SpecialPower(actor);
        if (intent.Action == TacticalAction.BasicAttack && actor.Tactics!.Resources.Riposte) damage = checked(damage + 2);
        var inventory = actor.Tactics!.Boons.Inventory;
        var modifiers = BoonCatalog.Default.Definitions.Where(d => inventory.Stacks.ContainsKey(d.Id))
            .SelectMany(d => d.Modifiers).Where(m => m.AbilityId == footprint.AbilityId).ToArray();
        AttackDelivery? routeFirst = modifiers.Any(m => m.Kind == BoonModifierKind.WeaponForm && m.Form == BoonWeaponForm.NeedleFlight && explicitAim)
            ? AttackDelivery.PiercingLine : modifiers.Any(m => m.Kind == BoonModifierKind.WeaponForm && m.Form == BoonWeaponForm.VolatileVial)
                ? AttackDelivery.Projectile : null;
        MirrorReflection? reflection = null;
        if (routeFirst is { } first)
        {
            var route = RouteMirrors(actor, actor.Position, footprint.Cells, first, knownOnly);
            footprint = footprint with { Cells = route.Cells }; reflection = route.Reflection;
        }
        BoonWeaponForm? form = null;
        if (modifiers.Length > 0)
        {
            var vision = knownOnly ? _world.VisionFrom(actor.Position) : null;
            var bodies = _world.State.Actors.Where(a => a.Life == LifeState.Alive && a.Health > 0
                    && a.Position.Floor == actor.Position.Floor && (vision == null || vision.Contains(a.Position)))
                .Select(a => new BoonBody(a.Id, a.Position, BoonFaction(a) != BoonFaction(actor), a.Tactics?.Burning != null)).ToArray();
            var modified = BoonAbilityGeometry.Apply(inventory, actor.Id, actor.Position,
                footprint, damage, intent.Facing, bodies, ProjectileOpen, p => _world.Plan.Contains(p) && _world.GroundOpen(p), explicitAim, knownOnly);
            footprint = footprint with { Cells = modified.Cells, Delivery = modified.Delivery };
            damage = modified.Damage; form = modified.Form;
        }
        if (routeFirst == null && footprint.AbilityId is not ("core:ability/hookrunner_hook" or "core:ability/duelist_thrust"))
        {
            var route = RouteMirrors(actor, actor.Position, footprint.Cells, footprint.Delivery, knownOnly);
            footprint = footprint with { Cells = route.Cells }; reflection = route.Reflection;
        }
        return new(footprint, damage, form, reflection);
    }

    private bool ImmediateAttack(Actor actor, TacticalAction action, TacticalPreview preview) => action == TacticalAction.BasicAttack
        || action == TacticalAction.Special && (actor.Tactics?.Build?.Class switch
        {
            ClassKind.Spiritbinder => preview.Cells.Count == 1 && BodyAt(preview.Cells[0]) is { Life: LifeState.Alive },
            ClassKind.Shade or ClassKind.Trapper or ClassKind.Duelist or ClassKind.Geomancer or ClassKind.Tinkerer
                or ClassKind.Stormcaller or ClassKind.Wayfinder or null => false,
            _ => true
        });

    public TacticalPreview Preview(TacticalIntent intent)
    {
        var preview = PreviewAction(intent);
        if (!preview.Valid) return preview;
        var actor = Active!;
        var executed = preview.Bump ? intent with { Action = TacticalAction.BasicAttack } : intent;
        bool immediate = ImmediateAttack(actor, executed.Action, preview);
        AttackDelivery delivery = AttackDelivery.Melee;
        if (immediate)
        {
            var attack = ClassAttack(actor, executed, !preview.Bump, knownOnly: true);
            delivery = attack.Footprint.Delivery;
            preview = preview with { Cells = attack.Footprint.Cells, Damage = attack.Damage, WeaponForm = attack.Form, Reflection = attack.Reflection,
                FatePower = actor.Tactics!.Boons.Inventory.Stacks.ContainsKey("core:boon/fateful_dice")
                    ? actor.Tactics.Boons.Inventory.Magnitude("core:boon/fateful_dice").Power : null };
        }
        if (intent.Vow)
        {
            if (actor.Tactics?.Boons.Vow() is not { } vow) return Invalid("Requires Pilgrim's Vow.");
            if (!immediate || intent.Action is not (TacticalAction.BasicAttack or TacticalAction.Special))
                return Invalid("Vow requires an immediate attack.");
            if (actor.Health <= vow.HealthCost) return Invalid($"Vow costs {vow.HealthCost} HP and must leave 1 HP.");
            preview = preview with { Vow = vow, Damage = checked((int)(preview.Damage!.Value + vow.DamageBonus)) };
        }
        return preview with { BoonCells = PreviewBoonPatterns(actor, intent, preview, immediate, delivery),
            Recovery = checked(preview.Recovery + (Friendly(actor) ? (long)actor.Tactics!.EnemySnareDelay + actor.Tactics.WebRecoveryDelay : 0)) };
    }

    private IReadOnlyList<BoonPreviewCell> PreviewBoonPatterns(Actor actor, TacticalIntent intent, TacticalPreview preview,
        bool immediate, AttackDelivery delivery)
    {
        if (!immediate && intent.Action is not (TacticalAction.Dash or TacticalAction.Wait)) return [];
        var inventory = actor.Tactics!.Boons.Inventory;
        if (!inventory.Stacks.Keys.Any(id => BoonCatalog.Default.Get(id).PatternPreview != null)) return [];
        var sight = _world.VisionFrom(actor.Position);
        var events = new List<BoonPatternOrigin>();
        if (intent.Action is TacticalAction.Dash or TacticalAction.Wait)
        {
            var at = preview.ReturnDistance != null ? preview.Cells[0] : actor.Position;
            events.Add(new(at, actor.Position, at));
        }
        else
        {
            var origin = intent.Action == TacticalAction.Special && actor.Tactics.Build?.Class == ClassKind.Spiritbinder
                ? actor.Tactics.Resources.Spirit ?? actor.Position : actor.Position;
            foreach (var cell in preview.Cells)
            {
                bool pass = delivery == AttackDelivery.Melee || ProjectileOpen(cell);
                if (!pass && delivery != AttackDelivery.Area) break;
                var target = pass && sight.Contains(cell) ? BodyAt(cell, actor.Id) : null;
                if (target is { Health: > 0 } && BoonFaction(target) != BoonFaction(actor) && target.Tactics?.Invulnerable != true)
                    events.Add(new(cell, origin));
                if (target is { Health: > 0 } && delivery == AttackDelivery.Projectile) break;
            }
        }
        var result = new List<BoonPreviewCell>();
        var seen = new HashSet<(string, WorldPosition)>();
        foreach (var definition in BoonCatalog.Default.Definitions)
        {
            if (!inventory.Stacks.ContainsKey(definition.Id) || definition.PatternPreview is not { } info) continue;
            bool trigger = intent.Action switch
            {
                TacticalAction.Dash => info.Trigger == (preview.ReturnDistance != null ? BoonPreviewTrigger.Return : BoonPreviewTrigger.Dash),
                TacticalAction.Wait => info.Trigger == BoonPreviewTrigger.Wait,
                _ => info.Trigger is BoonPreviewTrigger.Hit or BoonPreviewTrigger.Kill
            };
            if (!trigger || info.Special == true && intent.Action != TacticalAction.Special) continue;
            var effect = definition.Effects[0];
            foreach (var e in events)
            {
                if (info.Axis is { } axis && !BoonGeometry.Axis(e.Position, e.Source, axis)) continue;
                foreach (var cell in BoonGeometry.Pattern(effect.Pattern!.Value, e, ProjectileOpen))
                    if (sight.Contains(cell) && seen.Add((definition.Id, cell)))
                        result.Add(new(cell, definition.Id, info.Trigger, effect.Delay));
            }
        }
        return result;
    }
}
