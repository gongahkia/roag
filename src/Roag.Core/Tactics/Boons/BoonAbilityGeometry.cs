using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>Transient query facts about living bodies, never a second health/position authority.</summary>
public sealed record BoonBody(long Id, WorldPosition Position, bool Hostile, bool Burning = false, bool Visible = true);
public sealed record BoonAbilityShape(IReadOnlyList<WorldPosition> Cells, int Damage, AttackDelivery Delivery, BoonWeaponForm? Form);

public static class BoonAbilityGeometry
{
    private static readonly (int X, int Y)[] Cardinal = [(0, -1), (1, 0), (0, 1), (-1, 0)];

    /// <summary>
    /// Apply every original passive ability modifier in catalog order. Status/actor queries
    /// come from the world caller; damage, targets and random rolls are not resolved here.
    /// </summary>
    public static BoonAbilityShape Apply(BoonInventory inventory, long ownerId, WorldPosition owner,
        AttackFootprint footprint, int baseDamage, Facing facing, IReadOnlyList<BoonBody> bodies,
        Func<WorldPosition, bool> open, Func<WorldPosition, bool> walkable, bool explicitAim = true,
        bool knownOnly = false, BoonCatalog? catalog = null)
    {
        catalog ??= BoonCatalog.Default;
        var cells = footprint.Cells.ToList(); int damage = baseDamage;
        var delivery = footprint.Delivery;
        BoonWeaponForm? form = null;
        var (dx, dy) = AttackGeometry.Offset(facing);
        BoonBody? LivingAt(WorldPosition p) => bodies.FirstOrDefault(a => a.Id != ownerId && a.Position == p);
        void Append(WorldPosition p)
        { if (p != owner && open(p) && !cells.Contains(p)) cells.Add(p); }
        foreach (var definition in catalog.Definitions)
        {
            if (!inventory.Stacks.TryGetValue(definition.Id, out var stacks)) continue;
            long power = BoonInventory.Aggregate(definition, stacks).Power;
            foreach (var modifier in definition.Modifiers)
            {
                if (modifier.AbilityId != footprint.AbilityId) continue;
                switch (modifier.Kind)
                {
                    case BoonModifierKind.WeaponForm:
                        var changed = BoonGeometry.Weapon(modifier.Form!.Value, owner, facing, cells, footprint.Cells,
                            delivery, open, p => LivingAt(p) != null, explicitAim, knownOnly, p => LivingAt(p)?.Visible != false);
                        cells = changed.Cells.ToList(); delivery = changed.Delivery; form = changed.Form;
                        break;
                    case BoonModifierKind.ForwardCell:
                        var far = owner.Offset(dx * 2, dy * 2);
                        if (walkable(owner.Offset(dx, dy)) && walkable(far) && !cells.Contains(far)) cells.Add(far);
                        break;
                    case BoonModifierKind.LineSecondCell:
                        var mid = owner.Offset(dx, dy); var second = mid.Offset(dx, dy);
                        if (explicitAim && open(mid) && open(second)) cells.Add(second);
                        break;
                    case BoonModifierKind.BranchNear:
                    case BoonModifierKind.StatusChain:
                        int index = cells.FindIndex(p => LivingAt(p) != null);
                        var primary = index < 0 ? null : LivingAt(cells[index]);
                        if (primary is null || modifier.Kind == BoonModifierKind.StatusChain && !primary.Burning) break;
                        cells.RemoveRange(index + 1, cells.Count - index - 1);
                        var candidates = bodies.Where(a => a.Id != primary.Id && a.Id != ownerId && a.Hostile
                            && a.Position.Floor == owner.Floor
                            && Math.Max(Math.Abs(a.Position.X - primary.Position.X), Math.Abs(a.Position.Y - primary.Position.Y)) == 1
                            && (modifier.Kind == BoonModifierKind.BranchNear || a.Burning)).OrderBy(a => a.Id);
                        int added = 0;
                        foreach (var candidate in candidates)
                        {
                            var p = candidate.Position;
                            int sx = p.X - primary.Position.X, sy = p.Y - primary.Position.Y;
                            if (sx != 0 && sy != 0 && (!open(primary.Position.Offset(sx, 0)) || !open(primary.Position.Offset(0, sy)))) continue;
                            Append(p); added++;
                            if (modifier.Kind == BoonModifierKind.BranchNear && added >= 2) break;
                        }
                        delivery = AttackDelivery.PiercingLine;
                        break;
                    case BoonModifierKind.LineExtend:
                        var end = owner.Offset(dx * 5, dy * 5);
                        if (cells.Count > 0 && cells[^1] == end && open(end)
                            && (dx == 0 || dy == 0 || open(end.Offset(dx, 0)) && open(end.Offset(0, dy)))) Append(end.Offset(dx, dy));
                        break;
                    case BoonModifierKind.AreaExpand:
                        var center = owner.Offset(dx * 2, dy * 2);
                        if (cells.Contains(center) && open(center))
                            for (int oy = -2; oy <= 2; oy++) for (int ox = -2; ox <= 2; ox++)
                                if (Math.Max(Math.Abs(ox), Math.Abs(oy)) == 2) Append(center.Offset(ox, oy));
                        break;
                    case BoonModifierKind.Shockwave:
                        if (cells.Count > 0)
                        {
                            var hit = cells[0];
                            foreach (var step in Cardinal) Append(hit.Offset(step.X, step.Y));
                        }
                        break;
                    case BoonModifierKind.AddDamage: damage = checked((int)(damage + power)); break;
                    // These modifiers are interpreted by action/effect handlers, not footprint geometry.
                    case BoonModifierKind.PathStrike:
                    case BoonModifierKind.DashPrime: break;
                    default: throw new InvalidDataException("Unknown ability modifier.");
                }
            }
        }
        return new(cells, damage, delivery, form);
    }
}
