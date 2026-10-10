"""Persistent five-stage roguelike run flow over named Regions."""

from __future__ import annotations

from dataclasses import dataclass

from .profile import INITIAL_REGIONS, PlayerProfile
from .state import GameState, Position, RunProgress, VerticalLink, new_run_progress, stage_rng


MAX_STAGE = 5
ENCOUNTER_FAMILIES = (
    "swarm", "crossfire", "pincer", "hazard", "elite_hunt",
    "reinforcement_pressure",
)
ENCOUNTER_LABELS = {
    "swarm": "Swarm",
    "crossfire": "Crossfire",
    "pincer": "Pincer",
    "hazard": "Hazard ground",
    "elite_hunt": "Elite hunt",
    "reinforcement_pressure": "Reinforcement pressure",
}
CHALLENGE_TIERS = {
    0: ("Ordinary", "The disclosed baseline run."),
    1: ("Hunted", "Danger responses arrive sooner."),
    2: ("Scarce", "Danger arrives sooner and loot costs more salvage."),
    3: ("Brittle", "As Scarce; incoming exposed damage is increased."),
    4: ("Relentless", "As Brittle; each stage begins under more pressure."),
    5: ("Last Light", "All challenge rules and the narrowest recovery margin."),
}


@dataclass(frozen=True)
class RunResult:
    changed: bool
    result_id: str
    message: str


def start_run(
    state: GameState,
    *,
    challenge_tier: int = 0,
    profile: PlayerProfile | None = None,
    class_id: str | None = None,
) -> None:
    """Reset only run authority and enter the fixed opening Region."""
    if challenge_tier not in CHALLENGE_TIERS:
        raise ValueError("unknown challenge tier")
    if profile is not None and challenge_tier > profile.unlocked_challenge_tier:
        raise ValueError("challenge tier is not unlocked")
    run = new_run_progress(state.seed, challenge_tier)
    from .run_items import unlocked_item_ids

    run.allowed_item_ids = sorted(unlocked_item_ids(profile))
    run.allowed_regions = sorted(
        profile.unlocked_regions if profile is not None else INITIAL_REGIONS
    )
    state.run = run
    state.world_ended = False
    from .run_classes import apply_class_kit

    definition = apply_class_kit(
        state, class_id or (profile.last_run_class if profile is not None else "breaker"),
    )
    if profile is not None:
        profile.last_run_class = definition.id
    from .regions import begin_region

    begin_region(state, "hearthford")
    prepare_stage(state)
    state.add_message(
        f"{definition.name} run: [A] attack; [B] {definition.movement_name}; "
        f"[X] {definition.signature_name}.",
        priority=3,
    )
    state.add_message(
        "Reach this region's sanctum and defeat its claimant; follow the threshold "
        "to the next region. The fifth claimant is the final boss.",
        priority=3,
    )


def run_intensity(state: GameState) -> int:
    run = state.run
    if run is None or run.status != "active":
        return 0
    cadence = 150 if run.challenge_tier >= 1 else 180
    return (run.stage_index - 1) * 2 + run.run_actions // cadence


def record_world_step(state: GameState) -> None:
    run = state.run
    if run is None or run.status != "active" or state.location != "region":
        return
    run.run_actions += 1
    run.stage_actions += 1
    if run.movement_cooldown:
        run.movement_cooldown -= 1
    if run.signature_cooldown:
        run.signature_cooldown -= 1
    if run.decoy_position is not None and state.world_time >= run.decoy_until:
        state.add_message("The decoy fades from the field.", priority=2)
        run.decoy_position, run.decoy_until = None, 0
    _resolve_due_charges(state)


def _resolve_due_charges(state: GameState) -> None:
    """Resolve player-owned Sapper charges once at their declared due turn."""
    run = state.run
    if run is None or not run.placed_charges:
        return
    due, pending = (
        [row for row in run.placed_charges if row[1] <= state.world_time],
        [row for row in run.placed_charges if row[1] > state.world_time],
    )
    if not due:
        return
    run.placed_charges = pending
    from .enemy_equipment import harm_enemy
    from .run_items import after_terrain_destroyed
    from .terrain import replace_terrain, terrain_at
    from .world import distance, position_key

    for origin, _due_turn in due:
        affected = 0
        for actor in sorted(state.threats, key=lambda row: row.id):
            if (
                actor.health > 0 and actor.status in {"watching", "engaged"}
                and actor.position.z == origin.z and distance(origin, actor.position) <= 1
            ):
                harm_enemy(
                    state, actor, 2, "Sapper timed charge", damage_kind="blunt",
                    defeated_by_actor_id=state.active_courier_id or "courier",
                )
                affected += 1
        # A charge clears only soft, explicitly ordinary cover.  It cannot
        # erase links, objectives, walls or any protected authored structure.
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            point = Position(origin.x + dx, origin.y + dy, origin.z)
            terrain = terrain_at(state.region, point)
            if (
                terrain.destructible and "ordinary" in terrain.tags
                and terrain.hardness <= 2 and terrain.replacement_glyph
            ):
                replace_terrain(state.region, point, terrain.replacement_glyph)
                after_terrain_destroyed(state, terrain.material or "soil", point)
        state.smoke[position_key(origin)] = max(3, state.smoke.get(position_key(origin), 0))
        state.add_message(
            f"Timed charge detonates at {origin.x},{origin.y}: {affected} enemy"
            f"{' is' if affected == 1 else 'ies are'} caught in the blast.",
            priority=3,
        )


def finish_run(state: GameState, status: str, reason: str) -> RunResult:
    run = state.run
    if run is None or run.status != "active" or status not in {
        "victory", "defeat", "abandoned",
    }:
        return RunResult(False, "run.finish.rejected", "The run is already settled.")
    run.status = status
    run.failure_reason = reason
    state.world_ended = True
    if status == "defeat" and state.courier:
        state.courier.health = 0
        state.courier.alive = False
        state.courier.injury = "dead"
    message = {
        "victory": "The fifth threshold yields. This run is complete.",
        "defeat": f"The run ends: {reason}.",
        "abandoned": f"The run is abandoned: {reason}.",
    }[status]
    state.add_message(message, priority=3)
    state.remember(message)
    return RunResult(True, f"run.{status}", message)


def abandon_run(state: GameState) -> RunResult:
    return finish_run(state, "abandoned", "the courier withdrew from the field")


def _route_pool(state: GameState) -> list[str]:
    run = state.run
    if run is None:
        return []
    allowed = set(run.allowed_regions or INITIAL_REGIONS)
    visited = set(run.region_path)
    candidates = sorted((allowed & set(state.regions)) - visited)
    if len(candidates) < 2:
        candidates.extend(sorted(allowed - visited - set(candidates)))
    return candidates


def route_offers(state: GameState) -> tuple[str, ...]:
    """Return two stable branches without encoding one permanent route graph."""
    run = state.run
    if run is None or run.stage_index >= MAX_STAGE:
        return ()
    candidates = _route_pool(state)
    if not candidates:
        candidates = sorted(set(run.allowed_regions or INITIAL_REGIONS) - {state.active_region_id})
    rng = stage_rng(
        state.seed,
        f"run-route:{run.run_id}:{run.stage_index}:{state.active_region_id}",
    )
    candidates = list(dict.fromkeys(candidates))
    rng.shuffle(candidates)
    return tuple(candidates[:2])


def _blank_level(state: GameState) -> list[str]:
    return [" " * state.region.width for _ in range(state.region.height)]


def _place(rows: list[list[str]], point: Position, glyph: str) -> None:
    rows[point.y][point.x] = glyph


def install_threshold(state: GameState) -> None:
    """Attach a small physical branch room to the defeated boss's Region."""
    run = state.run
    if run is None or run.status != "active" or run.stage_index >= MAX_STAGE:
        return
    offers = route_offers(state)
    if not offers:
        finish_run(state, "victory", "the final regional claimant was defeated")
        return
    boss = state.region.landmarks.get("sanctum_boss")
    if boss is None:
        raise RuntimeError("stage Region has no boss threshold anchor")
    level = max(int(key) for key in state.region.levels) + 1
    entry = Position(boss.x, boss.y, level)
    rows = [list(row) for row in _blank_level(state)]
    for y in range(max(1, entry.y - 3), min(state.region.height - 1, entry.y + 4)):
        for x in range(max(1, entry.x - 7), min(state.region.width - 1, entry.x + 8)):
            rows[y][x] = "."
    left = Position(max(1, entry.x - 5), entry.y, level)
    right = Position(min(state.region.width - 2, entry.x + 5), entry.y, level)
    _place(rows, entry, "<")
    _place(rows, left, ">")
    if len(offers) > 1:
        _place(rows, right, ">")
    state.region.levels[str(level)] = ["".join(row) for row in rows]
    link_id = f"run:{run.run_id}:stage:{run.stage_index}:threshold"
    if not any(link.id == link_id for link in state.region.vertical_links):
        state.region.vertical_links.append(VerticalLink(
            boss, entry, "the run threshold", link_id,
        ))
    run.threshold_level = level
    run.threshold_entry = entry
    run.offered_regions = list(offers)
    run.branch_positions = {
        offers[0]: left,
        **({offers[1]: right} if len(offers) > 1 else {}),
    }
    state.add_message("A threshold rises beyond the defeated claimant.", priority=3)


def prepare_stage(state: GameState) -> None:
    run = state.run
    if run is None or run.status != "active":
        return
    run.stage_actions = 0
    run.stage_salvage = 0
    run.dropped_items.clear()
    run.offered_regions.clear()
    run.threshold_level = None
    run.threshold_entry = None
    run.branch_positions.clear()
    run.stage_reward_sources.clear()
    family = _select_encounter_family(state)
    run.encounter_family = family
    run.encounter_history.append(family)
    _prepare_opening_patrol(state)
    _prepare_field_worksite(state)
    # The first cache is deliberately close enough to make the opening build
    # decision precede the first sustained danger response.
    from .run_loot import seed_stage_loot

    seed_stage_loot(state)
    from .run_items import effect_value

    revealed = min(2, effect_value(state, "reveal_lead"))
    if revealed:
        offers = route_offers(state)[:revealed]
        if offers:
            names = ", ".join(
                state.regions[region_id].name
                if region_id in state.regions else region_id.title()
                for region_id in offers
            )
            state.add_message(f"The backwater map marks likely thresholds: {names}.")
    state.add_message(
        f"Stage {run.stage_index} encounter: {ENCOUNTER_LABELS[family]}.",
        priority=2,
    )


def _select_encounter_family(state: GameState) -> str:
    """Choose a deterministic tactical grammar without repetition streaks."""
    assert state.run is not None
    history = state.run.encounter_history
    candidates = [
        family for family in ENCOUNTER_FAMILIES
        if len(history) < 2 or not (history[-1] == history[-2] == family)
    ]
    # Every class must meet at least one terrain-aware arena early.  Later
    # stages remain seeded but use the whole grammar pool.
    if state.run.stage_index == 1:
        candidates = ["hazard"]
    rng = stage_rng(
        state.seed,
        f"run-encounter:{state.run.run_id}:{state.run.stage_index}:{state.active_region_id}",
    )
    return candidates[rng.randrange(len(candidates))]


def _opening_candidates(
    state: GameState, minimum: int, maximum: int,
) -> list[Position]:
    from .regions import region_reachable
    from .world import distance, is_walkable

    landing = state.region.landmarks["landing"]
    occupied = set(state.region.landmarks.values())
    occupied.update(box.position for box in state.region.containers)
    return sorted((
        point for point in region_reachable(state.region, landing)
        if point.z == landing.z
        and minimum <= distance(landing, point) <= maximum
        and point not in occupied
        and is_walkable(state, point, ignore_threat=True)
    ), key=lambda point: (point.y, point.x))


def _prepare_opening_patrol(state: GameState) -> None:
    candidates = _opening_candidates(state, 10, 18)
    ordinary = sorted((
        actor for actor in state.threats
        if not actor.elite and actor.profile not in {"animal", "machinery"}
        and actor.health > 0
    ), key=lambda actor: actor.id)
    if not candidates or not ordinary or state.run is None:
        return
    rng = stage_rng(
        state.seed,
        f"run-opening-patrol:{state.run.run_id}:{state.run.stage_index}:{state.active_region_id}",
    )
    family = state.run.encounter_family
    preferred = {
        "crossfire": {"ranged"},
        "pincer": {"pursuer", "reach"},
        "hazard": {"reach", "ranged"},
        "reinforcement_pressure": {"pursuer", "ranged"},
    }.get(family, set())
    pool = [actor for actor in ordinary if actor.profile in preferred] or ordinary
    if family == "elite_hunt":
        elite = sorted((
            actor for actor in state.threats
            if actor.elite and not actor.id.startswith("sanctum:") and actor.health > 0
        ), key=lambda actor: actor.id)
        pool = elite or pool
    count = {
        "swarm": 3, "crossfire": 2, "pincer": 2,
        "hazard": 2, "elite_hunt": 1, "reinforcement_pressure": 2,
    }[family]
    rng.shuffle(pool)
    rng.shuffle(candidates)
    for actor, point in zip(pool[:count], candidates):
        actor.position = point
        actor.home_position = point
        actor.status = "watching"
        actor.patrol = [point]
        actor.patrol_index = 0


def _prepare_field_worksite(state: GameState) -> None:
    candidates = _opening_candidates(state, 18, 30)
    if not candidates or state.run is None:
        state.run.stage_worksite = None
        return
    rng = stage_rng(
        state.seed,
        f"run-field-worksite:{state.run.run_id}:{state.run.stage_index}:{state.active_region_id}",
    )
    point = candidates[rng.randrange(len(candidates))]
    state.run.stage_worksite = point
    state.region.landmarks["run_field_worksite"] = point
    from .terrain import replace_terrain

    replace_terrain(state.region, point, "f")
    if state.run.encounter_family == "hazard":
        # This is authoritative shallow water, not a display effect.  The
        # movement and material systems retain their established water
        # consequences, while every class can exploit or route around it.
        state.water[f"{point.x},{point.y},{point.z}"] = 2


def record_boss_defeat(state: GameState, actor_id: str) -> RunResult:
    run = state.run
    if run is None or run.status != "active":
        return RunResult(False, "run.boss.ignored", "")
    expected = f"sanctum:{state.active_region_id}:boss"
    if actor_id != expected or state.active_region_id in run.boss_kills:
        return RunResult(False, "run.boss.ignored", "")
    run.boss_kills.append(state.active_region_id)
    from .run_items import RUN_ITEMS, collect_run_item

    boss_item = next(
        item for item in RUN_ITEMS.values()
        if item.tier == "boss" and item.region == state.active_region_id
    )
    collect_run_item(state, boss_item.id)
    if "boss" not in run.stage_reward_sources:
        run.stage_reward_sources.append("boss")
    if run.stage_index < MAX_STAGE:
        from .run_rewards import award_experience

        award_experience(state, 5, "boss")
    if run.stage_index >= MAX_STAGE:
        return finish_run(state, "victory", "five regional claimants were defeated")
    install_threshold(state)
    return RunResult(
        True, "run.boss.defeated",
        f"{boss_item.name} joins the run. Choose the next threshold.",
    )


def branch_at(state: GameState, position: Position) -> str | None:
    run = state.run
    if run is None or run.status != "active":
        return None
    return next(
        (region_id for region_id, point in run.branch_positions.items() if point == position),
        None,
    )


def choose_branch(state: GameState, region_id: str) -> RunResult:
    run = state.run
    if (
        run is None or run.status != "active"
        or region_id not in run.offered_regions
        or run.branch_positions.get(region_id) != state.position
    ):
        return RunResult(False, "run.branch.rejected", "No run threshold answers here.")
    from .regions import begin_region, store_active_region

    store_active_region(state)
    run.stage_index += 1
    run.region_path.append(region_id)
    begin_region(state, region_id)
    prepare_stage(state)
    return RunResult(
        True, "run.branch.chosen",
        f"Stage {run.stage_index}: {state.region.name}.",
    )


def critical_structure_destroyed(
    state: GameState, position: Position, terrain_id: str,
) -> RunResult | None:
    """Resolve the disclosed causal loss for destroying a required run link."""
    if state.run is None or state.run.status != "active":
        return None
    critical = any(
        position in {link.first, link.second}
        for link in state.region.vertical_links
    ) or position == state.run.threshold_entry or position in state.run.branch_positions.values() or any(
        state.region.landmarks.get(name) == position
        for name in ("landing", "sanctum_entry", "sanctum_boss")
    )
    if not critical:
        return None
    return finish_run(
        state,
        "defeat",
        f"the courier destroyed the required route at {position.x},{position.y},{position.z}",
    )
