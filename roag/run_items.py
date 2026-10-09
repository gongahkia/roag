"""Deterministic, stackable item definitions for one roguelike run.

Run items are intentionally not ordinary :class:`state.Item` objects.  They
represent the courier's current build and disappear when the run ends.  The
catalog is mechanical content; this module is the only place which interprets
its finite stacking vocabulary.
"""

from __future__ import annotations

from dataclasses import dataclass
from types import MappingProxyType
from typing import Mapping

from .catalog import CatalogError, load_catalog
from .profile import INITIAL_REGIONS, PlayerProfile
from .state import GameState, stage_rng


TIERS = ("common", "uncommon", "rare", "boss")
TIER_COUNTS = {"common": 24, "uncommon": 18, "rare": 10, "boss": 8}
STACKING = frozenset({"linear", "capped", "threshold", "diminishing"})


@dataclass(frozen=True)
class RunItemDefinition:
    id: str
    name: str
    tier: str
    family: str
    trigger: str
    effect: str
    base: int
    per_stack: int
    cap: int
    stacking: str
    region: str
    description: str


def _load_definitions() -> Mapping[str, RunItemDefinition]:
    raw = load_catalog("run_items.json", ("items",))["items"]
    if not isinstance(raw, list):
        raise CatalogError("run_items.json items must be a list")
    definitions: dict[str, RunItemDefinition] = {}
    tier_counts = {tier: 0 for tier in TIERS}
    for index, row in enumerate(raw):
        try:
            definition = RunItemDefinition(**row)
        except (TypeError, ValueError) as exc:
            raise CatalogError(f"invalid run item at index {index}") from exc
        if (
            not definition.id or definition.id in definitions
            or definition.tier not in TIERS
            or definition.stacking not in STACKING
            or not definition.family or not definition.trigger or not definition.effect
            or type(definition.base) is not int or definition.base < 0
            or type(definition.per_stack) is not int or definition.per_stack < 0
            or type(definition.cap) is not int or definition.cap < definition.base
            or (definition.tier == "boss") != bool(definition.region)
        ):
            raise CatalogError(f"invalid run item {definition.id!r}")
        definitions[definition.id] = definition
        tier_counts[definition.tier] += 1
    if tier_counts != TIER_COUNTS:
        raise CatalogError(f"run item tiers must be {TIER_COUNTS!r}")
    boss_regions = {
        definition.region for definition in definitions.values()
        if definition.tier == "boss"
    }
    if boss_regions != {
        "hearthford", "greywash", "greenwold", "whitecairn",
        "dunmire", "marlbank", "rillscar", "frostmere",
    }:
        raise CatalogError("run items require exactly one boss item per region")
    return MappingProxyType(definitions)


RUN_ITEMS = _load_definitions()

# Forty-four items are available on a fresh profile.  Later discoveries are
# horizontal breadth: they expand future pools but never add permanent stats.
_COMMON = tuple(key for key, row in RUN_ITEMS.items() if row.tier == "common")
_UNCOMMON = tuple(key for key, row in RUN_ITEMS.items() if row.tier == "uncommon")
_RARE = tuple(key for key, row in RUN_ITEMS.items() if row.tier == "rare")
_BOSS_INITIAL = tuple(
    key for key, row in RUN_ITEMS.items()
    if row.tier == "boss" and row.region in INITIAL_REGIONS
)
INITIAL_ITEM_IDS = frozenset((*_COMMON, *_UNCOMMON[:10], *_RARE[:4], *_BOSS_INITIAL))
if len(INITIAL_ITEM_IDS) != 44:
    raise RuntimeError("the initial run-item pool must contain 44 definitions")


def unlocked_item_ids(profile: PlayerProfile | None = None) -> frozenset[str]:
    return INITIAL_ITEM_IDS | frozenset(profile.unlocked_items if profile else ())


def stack_value(definition: RunItemDefinition, stacks: int) -> int:
    """Interpret one finite, integer-only stack rule."""
    if stacks <= 0:
        return 0
    if definition.stacking in {"linear", "capped"}:
        value = definition.base + definition.per_stack * (stacks - 1)
    elif definition.stacking == "threshold":
        # Threshold items gain a step every second copy.  Their base value is
        # useful immediately, while duplicates deliberately scale slowly.
        value = definition.base + definition.per_stack * ((stacks - 1) // 2)
    else:
        value = definition.base
        for copy_index in range(1, stacks):
            value += max(1, definition.per_stack // (copy_index + 1))
    return min(definition.cap, value)


def effect_value(state: GameState, effect: str, *, family: str | None = None) -> int:
    run = state.run
    if run is None:
        return 0
    total = 0
    for item_id, stacks in sorted(run.item_stacks.items()):
        definition = RUN_ITEMS.get(item_id)
        if definition and definition.effect == effect and (
            family is None or definition.family == family
        ):
            total += stack_value(definition, stacks)
    return total


def item_pool(
    tier: str,
    *,
    profile: PlayerProfile | None = None,
    region_id: str | None = None,
) -> tuple[RunItemDefinition, ...]:
    allowed = unlocked_item_ids(profile)
    rows = [
        row for row in RUN_ITEMS.values()
        if row.tier == tier and row.id in allowed
        and (tier != "boss" or row.region == region_id)
    ]
    return tuple(sorted(rows, key=lambda row: row.id))


def deterministic_item(
    state: GameState,
    tier: str,
    source_id: str,
    *,
    allowed_ids: frozenset[str] | None = None,
) -> RunItemDefinition:
    if state.run is None:
        raise ValueError("run item selection requires an active run")
    rows = [
        row for row in RUN_ITEMS.values()
        if row.tier == tier
        and (allowed_ids is None or row.id in allowed_ids)
        and (tier != "boss" or row.region == state.active_region_id)
    ]
    rows.sort(key=lambda row: row.id)
    if not rows:
        raise ValueError(f"no {tier!r} run item is available")
    rng = stage_rng(
        state.seed,
        f"run-item:{state.run.run_id}:{state.run.stage_index}:{source_id}:{tier}",
    )
    return rows[rng.randrange(len(rows))]


def collect_run_item(state: GameState, item_id: str) -> int:
    """Add one stack and apply only immediate collection consequences."""
    if state.run is None or state.run.status != "active" or item_id not in RUN_ITEMS:
        raise ValueError("run item is not available to this run")
    before_max = effect_value(state, "max_health")
    state.run.item_stacks[item_id] = state.run.item_stacks.get(item_id, 0) + 1
    after_max = effect_value(state, "max_health")
    if state.courier and after_max > before_max:
        increase = after_max - before_max
        state.courier.max_health += increase
        state.courier.health += increase
    if state.courier:
        healing = effect_value(state, "heal", family="economy")
        state.courier.health = min(
            state.courier.max_health, state.courier.health + healing,
        )
    return state.run.item_stacks[item_id]

