# Workforce contract

## Roles and tasks

| Duty | Work covered |
|---|---|
| Mining | Dig jobs |
| Construction | Build/dismantle; gathering and delivering its required supplies |
| Hauling | Moving resource piles to completed storage |
| Food / crops | Irrigation of farms/wards and crop harvest |
| Pumps | Operating completed hand pumps |
| Fieldwork | Survey, salvage, cull and arming charges |

Eating, resting, physical falling, immediate escape and rally directives are not
ordinary work duties. OFF mining does not prevent a person eating. A builder can
carry supplies for their own construction even when general hauling is OFF.

## Policy state

`world.labor` stores `quotas`, seven percentage weights and one person record per
settler. A person record contains stable `id`, `role` and six priorities 0..3.
0 forbids that duty. Values 1..3 add a preference bias to ordinary task selection.
`world.laborAssignments` is deterministic per-tick allocation state. Do not update
any of this from draw/input code: queue a `labor` command with a complete draft.

Pinned roles are assigned first and are outside the percentage pool. AUTO means
participation in that pool; General means all enabled duties. With quotas disabled,
AUTO workers are General. With quotas enabled, counts use largest remainder:

1. Multiply each share by the number of living AUTO workers and floor it.
2. Distribute the remaining workers by descending fractional remainder.
3. Break ties with fixed role order: general, dig, build, haul, farm, pump, field.
4. Select eligible specialists by priority and relevant skill, then stable worker ID.
5. Assign the apportioned General positions last. An impossible specialist vacancy
   is shown as `unfilled`, not silently reassigned to a forbidden duty.

Example: six AUTO workers and 50% mining, 25% construction, 25% hauling yield 3, 2,
1 workers. With three workers the same split is 1, 1, 1. These are headcounts, not
strict time-use or throughput guarantees. Disabling a needed duty can stall a colony.
Dying workers leave the pool on subsequent refresh; no replacement population arrives.

## Named jobs and rally

`order.worker` optionally names a worker. A named order overrides a role or quota,
not OFF. Other workers do not take it over if the owner becomes unavailable. The
player can clear/reassign it with `target_order` (Shift+Y) or cancel it normally.
`field` and `arm` commands accept the same ownership restriction.

`rally` has a worker ID, or 0 for everyone, and a fine-cell target. Workers find a
reachable standing position within three Manhattan cells and hold there. Needs and
immediate hazards override it. `releaserally` removes the directive. Crowds do not
have body-to-body collision avoidance; workers can overlap, as before.

Policy changes release active ordinary tasks, claims and carried goods safely, then
replan. The item/job/work-position reservation machinery remains authoritative.
Queued drafts do not apply until the next tick. Challenges cannot issue policies,
field orders, arming, rally or assignment changes from their historical archive.

## Controls

H opens the draft. Tab switches page. Arrows select. Space/click cycles, 0–3 sets a
duty, Shift-click pins it, R resets a person. Q toggles quotas on the workforce page;
+/- adjusts five points while retaining 100 total. Presets 1–4 change the draft.
Enter queues, Escape discards. The read-only archive still lets you inspect policy.

Click a settler then Y to target future orders. Repress Y to clear. Shift+Y assigns
the open order at the selected or hovered block. M then click rallies the selected
settler (or everyone if none selected); Shift+M explicitly targets everyone.
J releases the selected settler, Shift+J or no selection releases everyone.

Known limitation: a job ownership column is not yet exposed for every field target
in a unified jobs table. Cancel/reissue a field order with a new target worker when
needed. There is no automatic quota borrowing, shift schedule, skill training or
optimal global scheduler in this release.
