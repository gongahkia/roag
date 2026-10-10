"""Sparse physical placement and acquisition of stackable run items."""

from __future__ import annotations

from dataclasses import dataclass

from .run_items import (
    INITIAL_ITEM_IDS, RUN_ITEMS, can_collect_run_item, collect_run_item, deterministic_item,
    effect_value,
)
from .state import GameState, Position, stage_rng


@dataclass(frozen=True)
class RunLootResult:
    changed: bool
    result_id: str
    item_id: str | None = None


def _candidate_cells(state: GameState) -> list[Position]:
    from .regions import region_reachable
    from .world import distance, is_walkable

    landing = state.region.landmarks["landing"]
    occupied = set(state.region.landmarks.values())
    occupied.update(box.position for box in state.region.containers)
    cells = [
        point for point in region_reachable(state.region, landing)
        if point.z == landing.z
        and 6 <= distance(landing, point) <= 12
        and point not in occupied
        and is_walkable(state, point, ignore_threat=True)
    ]
    return sorted(cells, key=lambda point: (point.y, point.x, point.z))


def seed_stage_loot(state: GameState) -> None:
    run = state.run
    if run is None or run.status != "active":
        return
    candidates = _candidate_cells(state)
    if not candidates:
        return
    rng = stage_rng(
        state.seed, f"run-loot-position:{run.run_id}:{run.stage_index}:{state.active_region_id}",
    )
    point = candidates[rng.randrange(len(candidates))]
    tier = "uncommon" if run.stage_index >= 3 else "common"
    from .run_rewards import eligible_boons

    item = deterministic_item(
        state, tier, f"opening:{state.active_region_id}",
        allowed_ids=frozenset(eligible_boons(state, tier)),
    )
    drop_id = f"stage-{run.stage_index}-opening"
    run.dropped_items.setdefault(drop_id, (item.id, point))
    # One physical chest is seeded in every stage.  Its location varies with
    # each region and it complements, rather than replaces, XP offers.
    if "chest" not in run.stage_reward_sources:
        run.stage_reward_sources.append("chest")


def loot_at(state: GameState, position: Position | None = None) -> tuple[tuple[str, str], ...]:
    run = state.run
    point = position or state.position
    if run is None:
        return ()
    return tuple(sorted(
        (drop_id, item_id)
        for drop_id, (item_id, drop_position) in run.dropped_items.items()
        if drop_position == point
    ))


def collect_at(state: GameState, drop_id: str) -> RunLootResult:
    run = state.run
    if run is None or run.status != "active" or drop_id not in run.dropped_items:
        return RunLootResult(False, "run.loot.missing")
    item_id, position = run.dropped_items[drop_id]
    if position != state.position:
        return RunLootResult(False, "run.loot.out_of_reach")
    salvage_cost = (
        max(0, run.challenge_tier - 1)
        if not drop_id.endswith("-opening") else 0
    )
    salvage_cost = max(0, salvage_cost - effect_value(state, "chest_discount"))
    if run.stage_salvage < salvage_cost:
        return RunLootResult(False, "run.loot.salvage_required", item_id)
    if not can_collect_run_item(state, item_id):
        # A physical cache is allowed to persist while the player takes other
        # rewards.  When its original stack reaches its shown cap, turn the
        # cache into another eligible boon rather than asking the player to
        # spend a choice on a no-op.
        from .run_rewards import eligible_boons

        replacements = frozenset(eligible_boons(state, RUN_ITEMS[item_id].tier))
        if not replacements:
            return RunLootResult(False, "run.loot.no_effective_choice", item_id)
        replacement = deterministic_item(
            state, RUN_ITEMS[item_id].tier, f"replacement:{drop_id}",
            allowed_ids=replacements,
        )
        item_id = replacement.id
        run.dropped_items[drop_id] = (item_id, position)
        state.add_message(
            f"The exhausted cache retunes to {RUN_ITEMS[item_id].name}.",
            priority=2,
        )
    run.stage_salvage -= salvage_cost
    collect_run_item(state, item_id)
    duplicate = effect_value(state, "duplicate_drop")
    if duplicate and can_collect_run_item(state, item_id) and stage_rng(
        state.seed, f"run-item-duplicate:{run.run_id}:{drop_id}",
    ).randrange(100) < duplicate:
        collect_run_item(state, item_id)
    del run.dropped_items[drop_id]
    run.opened_loot_ids.append(drop_id)
    source = "Chest discovery" if drop_id.startswith("stage-") else "Combat discovery"
    state.add_message(
        f"{source}: {RUN_ITEMS[item_id].name}: {RUN_ITEMS[item_id].description}",
        priority=3,
    )
    return RunLootResult(True, "run.loot.collected", item_id)


def record_enemy_defeat(
    state: GameState, actor_id: str, *, elite: bool,
    allow_secondary: bool = True,
) -> str | None:
    """Award player credit and at most one bounded secondary defeat effect.

    Direct, delayed, and environmental player-owned harm all carry the courier
    as owner, so they receive the same XP and loot credit.  Secondary splash
    harm preserves that credit but disables another defeat splash: useful
    chains remain legible without recursive clears or duplicate rewards.
    """
    run = state.run
    if run is None or run.status != "active" or actor_id.startswith("sanctum:"):
        return None
    from .run_items import effect_value

    if elite:
        run.elite_kills += 1
        source = "elite"
    else:
        run.ordinary_kills += 1
        source = "combat"
    if source not in run.stage_reward_sources:
        run.stage_reward_sources.append(source)
    from .run_rewards import award_experience, eligible_boons

    award_experience(state, 3 if elite else 1, source)
    run.stage_salvage += 1 + effect_value(state, "salvage")
    from .danger import pressure

    if pressure(state).band in {"strained", "critical"}:
        run.stage_salvage += effect_value(state, "critical_salvage")
    heal_and_pressure = effect_value(state, "heal_and_pressure")
    if heal_and_pressure:
        state.noise += heal_and_pressure
        if state.courier:
            state.courier.health = min(
                state.courier.max_health,
                state.courier.health + heal_and_pressure,
            )
    charge = effect_value(state, "gain_charge")
    if charge:
        from .circuits import cell_key, gain_charge, space_id

        space = space_id(state)
        racks = sorted(
            (key for key, cell in state.circuits.items()
             if cell.space == space and cell.kind == "rack"),
        )
        if racks:
            gain_charge(state, racks[0], charge)
    blast = (
        effect_value(state, "blast_damage", trigger="actor.defeated")
        if allow_secondary else 0
    )
    if blast:
        from .enemy_equipment import harm_enemy
        from .world import distance

        defeated = next(
            (candidate for candidate in state.threats if candidate.id == actor_id),
            None,
        )
        if defeated is not None:
            for actor in sorted(state.threats, key=lambda candidate: candidate.id):
                if (
                    actor.id != actor_id
                    and actor.health > 0
                    and actor.status in {"watching", "engaged"}
                    and actor.position.z == defeated.position.z
                    and distance(defeated.position, actor.position) <= 2
                ):
                    harm_enemy(
                        state, actor, blast, "run-item defeat blast",
                        defeated_by_actor_id=state.active_courier_id or "courier",
                        allow_run_secondary=False,
                        apply_run_attack_bonus=False,
                    )
    kill_index = run.ordinary_kills + run.elite_kills
    # A finite pity cadence prevents long item droughts.  Greed effects make
    # drops more frequent but contribute to pressure in danger.pressure().
    interval = max(2, 5 - min(2, effect_value(state, "drop_chance") // 4))
    if not elite and kill_index % interval:
        return None
    roll = stage_rng(
        state.seed,
        f"run-loot-kill:{run.run_id}:{run.stage_index}:{actor_id}:{kill_index}",
    ).randrange(100)
    roll = max(0, roll - effect_value(state, "pressure_loot"))
    tier = "rare" if elite and roll < 30 else "uncommon" if elite or roll < 30 else "common"
    choice_count = 1 + min(2, effect_value(state, "extra_choice"))
    choices = [
        deterministic_item(
            state, tier, f"defeat:{actor_id}:choice:{index}",
            allowed_ids=frozenset(eligible_boons(state, tier)),
        )
        for index in range(choice_count)
    ]
    item = min(
        choices,
        key=lambda row: (run.item_stacks.get(row.id, 0), row.id),
    )
    drop_id = f"defeat-{run.stage_index}-{kill_index}-{actor_id}"
    actor = next((candidate for candidate in state.threats if candidate.id == actor_id), None)
    if actor is not None:
        run.dropped_items[drop_id] = (item.id, actor.position)
        return item.id
    return None


def record_terrain_destroyed(
    state: GameState, material_id: str, position: Position,
) -> str | None:
    """Place one per-stage boon at the first completed terrain harvest.

    Ordinary material yields remain physical inventory.  This separate reward
    makes terrain a deliberate build route without letting repeated reeds farm
    an unbounded run-item stream.
    """
    run = state.run
    if (
        run is None or run.status != "active"
        or "terrain" in run.stage_reward_sources
    ):
        return None
    from .run_rewards import eligible_boons

    allowed = frozenset(eligible_boons(state, "common"))
    item = deterministic_item(
        state, "common", f"terrain:{material_id}:{position.x},{position.y},{position.z}",
        allowed_ids=allowed,
    )
    drop_id = f"terrain-{run.stage_index}-{position.x}-{position.y}-{position.z}"
    run.dropped_items[drop_id] = (item.id, position)
    run.stage_reward_sources.append("terrain")
    state.add_message(
        f"Terrain reward: {RUN_ITEMS[item.id].name} remains in the cleared ground.",
        priority=3,
    )
    return item.id
