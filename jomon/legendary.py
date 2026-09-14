"""Finite named working objects derived from each region's causal record."""

from __future__ import annotations

from .catalog import CatalogError, HISTORY_SECTIONS, load_catalog
from .state import GameState, LegendaryObject, stage_rng

_HISTORY = load_catalog("history.json", HISTORY_SECTIONS)
_bases = _HISTORY["legend_bases"]
_crisis_tags = _HISTORY["crisis_tags"]
if (not isinstance(_bases, dict) or len(_bases) != 8
        or any(not isinstance(region, str) or not isinstance(row, list) or len(row) != 5
               or not isinstance(row[0], str) or not row[0]
               or not isinstance(row[1], list) or not row[1] or any(not isinstance(tag, str) or not tag for tag in row[1])
               or type(row[2]) is not int
               or not isinstance(row[3], list) or not row[3] or any(not isinstance(verb, str) or not verb for verb in row[3])
               or not isinstance(row[4], str) or not row[4]
               for region, row in _bases.items())):
    raise CatalogError("history.json has invalid legendary bases")
LEGEND_BASES = {
    region: (row[0], tuple(row[1]), row[2], tuple(row[3]), row[4])
    for region, row in _bases.items()
}
if (not isinstance(_crisis_tags, dict) or set(_crisis_tags) != {"flood", "fire", "support loss"}
        or any(not isinstance(tag, str) or not tag for tag in _crisis_tags.values())):
    raise CatalogError("history.json has invalid crisis tags")
CRISIS_TAG = _crisis_tags


def initialise_region_legend(state: GameState, region_id: str) -> None:
    region = state.regions[region_id]
    if not region.regional_history:
        return
    legend_id = f"legend:{region_id}"
    cache = next(container for container in reversed(region.containers)
                 if not container.hidden and "-sanctum-" not in container.id)
    cache.legendary_id = legend_id
    if legend_id in state.legendary_objects:
        return
    base_kind, base_tags, range_bonus, verbs, epithet = LEGEND_BASES[region_id]
    crisis = region.regional_history[1]
    repair = region.regional_history[2]
    # The second local witness remains the authored maker even when travelling
    # institutions later add their own embodied regional contact.
    maker = state.contacts[region_id][1].name
    account = state.institutions[f"work:{region_id}"]
    noun = stage_rng(state.seed, f"legendary-object-v1:{region_id}").choice(("Measure", "Witness", "Working"))
    short_name = f"{maker.split()[0]}'s {noun}"
    tags = tuple(sorted(set(base_tags) | {CRISIS_TAG[crisis.kind]}))
    effect = (
        f"Its {epithet} construction grants {', '.join(tags)} handling"
        + (f" and {range_bonus:+d} prepared range" if range_bonus else "")
        + f"; it can {', '.join(verbs)} material where appropriate."
    )
    tradeoff = "The historic reinforcement adds one carried weight and one noise when committed."
    provenance = (
        f"Made or repaired by {maker} for {account.name} after {crisis.kind}; "
        f"{repair.account} Current claim: {account.dispute}."
    )
    state.legendary_objects[legend_id] = LegendaryObject(
        legend_id, short_name, region_id, base_kind, maker, account.id,
        crisis.id, provenance, effect, tradeoff, account.name,
        f"{crisis.account} The trail ends at {cache.name} ({cache.id}).",
        tags, range_bonus, verbs,
    )


def legend_for_item(state: GameState, item) -> LegendaryObject | None:
    return state.legendary_objects.get(item.legendary_id or "")


def active_legend(state: GameState) -> LegendaryObject | None:
    item = next((item for item in state.items if item.owner_id == state.active_courier_id and item.location == "readied" and item.condition > 0), None)
    return legend_for_item(state, item) if item else None


def active_tags(state: GameState) -> set[str]:
    legend = active_legend(state)
    return set(legend.tags) if legend else set()


def permits_material(state: GameState, verb: str) -> bool:
    legend = active_legend(state)
    return bool(legend and verb in legend.material_verbs)


def validate_legends(state: GameState) -> None:
    if len(state.legendary_objects) > 8:
        raise ValueError("too many finite legendary objects")
    for legend_id, legend in state.legendary_objects.items():
        if legend.id != legend_id or legend.region_id not in state.regions:
            raise ValueError("legend has an invalid identity or region")
        region = state.regions[legend.region_id]
        cache = next((cache for cache in region.containers if cache.legendary_id == legend_id), None)
        if cache is None or legend.institution_id not in state.institutions:
            raise ValueError("legend has no physical cache or interested institution")
        if legend.historical_event_id not in {event.id for event in region.regional_history}:
            raise ValueError("legend provenance refers to an unknown history event")
        if legend.base_kind != LEGEND_BASES[legend.region_id][0] or not legend.major_effect or not legend.tradeoff or not legend.clue:
            raise ValueError("legend has incomplete mechanics or provenance")
    for item in state.items:
        if item.legendary_id and item.legendary_id not in state.legendary_objects:
            raise ValueError("physical item refers to an unknown legend")
