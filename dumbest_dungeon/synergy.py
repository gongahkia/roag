"""Derived mechanical relationships and conservative eligible-pool diagnostics."""

from __future__ import annotations

import json
from collections import defaultdict
from dataclasses import asdict, dataclass
from enum import StrEnum
from math import comb


class Relation(StrEnum):
    PRODUCES = "produces"
    EXPLOITS = "exploits"
    SPENDS = "spends"
    CONVERTS = "converts"
    REQUIRES = "requires"
    COVERS = "covers"


@dataclass(frozen=True, order=True)
class SynergyEdge:
    source_id: str
    relation: Relation
    mechanic: str
    owner: str
    ranks: tuple[int, ...]
    layer: str


def derive_edges(catalog, eligible: set[str] | None = None) -> tuple[SynergyEdge, ...]:
    result: set[SynergyEdge] = set()
    for identity, card in sorted(catalog.cards.items()):
        if eligible is not None and identity not in eligible:
            continue

        def edge(relation, mechanic, layer):
            result.add(SynergyEdge(identity, relation, mechanic, card["hero"], tuple(card["from_ranks"]), layer))

        if card["cost"]:
            edge(Relation.SPENDS, "shared:energy", "base")
        for layer, effects in (("base", card["effects"]), ("upgrade", card["upgrade_effects"])):
            for effect in effects:
                target = effect.get("target", card["target"])
                side = "enemy" if target in {"enemy", "all_enemies"} else "crew"
                op = effect["op"]
                if op == "status":
                    edge(Relation.PRODUCES, f"{side}:{effect['status']}", layer)
                elif op in {"block", "guard", "move", "draw", "discard", "energy"}:
                    mechanic = "movement" if op == "move" else op
                    scope = "shared" if op in {"draw", "discard", "energy"} else side
                    edge(Relation.PRODUCES, f"{scope}:{mechanic}", layer)
                    if op == "move" and side == "crew":
                        edge(Relation.COVERS, "crew:rank_access", layer)
                elif op == "heal":
                    edge(Relation.COVERS, "crew:injury", layer)
                    edge(Relation.COVERS, "crew:deaths_door", layer)
                elif op == "cleanse":
                    edge(Relation.COVERS, f"{side}:negative_status", layer)
                elif op == "stress":
                    edge(Relation.PRODUCES if effect["amount"] > 0 else Relation.COVERS, "crew:stress", layer)
                if effect.get("bonus_status"):
                    edge(Relation.EXPLOITS, f"{side}:{effect['bonus_status']}", layer)
                if effect.get("condition_status"):
                    condition_side = "enemy" if card["target"] in {"enemy", "all_enemies"} else "crew"
                    edge(Relation.REQUIRES, f"{condition_side}:{effect['condition_status']}", layer)
                for field, scope in (("condition_actor_state", "crew"), ("condition_target_state", side)):
                    if effect.get(field):
                        if field == "condition_target_state":
                            scope = "enemy" if card["target"] in {"enemy", "all_enemies"} else "crew"
                        state = "stress" if effect[field] == "stressed" else effect[field]
                        edge(Relation.REQUIRES, f"{scope}:{state}", layer)
    return tuple(sorted(result))


# these have an innate use or an ordinary world/combat source without another card.
INTRINSIC_SOURCES = frozenset({"shared:energy", "crew:stress", "crew:healthy", "crew:wounded",
                              "crew:deaths_door", "enemy:healthy", "enemy:wounded"})
INTRINSIC_USES = frozenset({"block", "guard", "movement", "draw", "discard", "energy", "dodge",
                           "focus", "riposte", "weak", "stun", "vulnerable", "wound", "stress"})
MIN_PRODUCERS = 2
MIN_PRODUCER_DENSITY_BP = 500


def analyze_pool(catalog, eligible: set[str], formation: tuple[str, ...] = ()) -> dict:
    edges = derive_edges(catalog, eligible)
    producers = defaultdict(set)
    payoffs = defaultdict(set)
    for edge in edges:
        if edge.relation == Relation.PRODUCES:
            producers[edge.mechanic].add(edge.source_id)
        elif edge.relation in {Relation.EXPLOITS, Relation.REQUIRES}:
            payoffs[edge.mechanic].add(edge.source_id)
    diagnostics = []
    densities = {}
    external = defaultdict(set)
    for identity, enemy in sorted(catalog.enemies.items()):
        for action in enemy["actions"]:
            for effect in action["effects"]:
                if effect["op"] == "status" and effect.get("target", action["target"]) not in {"self", "weakest_ally", "weakest_enemy"}:
                    external[f"crew:{effect['status']}"].add(identity)
    for mechanic, consumers in sorted(payoffs.items()):
        sources = producers[mechanic]
        density = len(sources) * 10000 // max(1, len(eligible))
        densities[mechanic] = {"producers": sorted(sources), "payoffs": sorted(consumers), "density_bp": density,
                              "external_enemies": sorted(external[mechanic])}
        if mechanic in INTRINSIC_SOURCES:
            continue
        if not sources and external[mechanic]:
            diagnostics.append({"code": "encounter_dependent_setup", "mechanic": mechanic,
                                "cards": sorted(consumers), "meaning": "setup exists in enemy actions; eligible biome coverage still matters"})
            continue
        code = "unsupported_payoff" if not sources else "thin_producer_pool" if (
            len(sources) < MIN_PRODUCERS or density < MIN_PRODUCER_DENSITY_BP
        ) else None
        if code:
            diagnostics.append({"code": code, "mechanic": mechanic, "cards": sorted(consumers)})
        owners = {catalog.cards[identity]["hero"] for identity in sources}
        if len(owners) == 1:
            provider = next(iter(owners))
            exposed = sorted(identity for identity in consumers if catalog.cards[identity]["hero"] != provider)
            if exposed:
                diagnostics.append({"code": "single_owner_dependency", "mechanic": mechanic,
                                    "provider": provider, "cards": exposed,
                                    "meaning": "payoff loses its card-based setup if this owner dies; fallback effects may remain"})
    for mechanic, sources in sorted(producers.items()):
        if not payoffs[mechanic] and mechanic.split(":", 1)[1] not in INTRINSIC_USES:
            diagnostics.append({"code": "orphan_producer", "mechanic": mechanic, "cards": sorted(sources)})
    rank_report = []
    if formation:
        starters = [(owner, identity) for owner in formation for identity in catalog.heroes[owner]["starter_deck"]]
        invalid = sum(formation.index(owner) + 1 not in catalog.cards[identity]["from_ranks"] for owner, identity in starters)
        denominator = comb(len(starters), 5) if len(starters) >= 5 else 1
        rank_report.append({"formation": list(formation), "starter_cards": len(starters), "rank_invalid": invalid,
                            "expected_invalid_in_five_bp": invalid * 50000 // max(1, len(starters)),
                            "all_five_invalid_chance_bp": (comb(invalid, 5) if invalid >= 5 else 0) * 10000 // denominator})
        for rank, owner in enumerate(formation, 1):
            usable = sorted(identity for identity in eligible if catalog.cards[identity]["hero"] == owner
                            and rank in catalog.cards[identity]["from_ranks"])
            if len(usable) < 2:
                diagnostics.append({"code": "rank_bottleneck", "owner": owner, "rank": rank, "usable": usable})
    return {"eligible_count": len(eligible), "densities": densities, "diagnostics": diagnostics, "rank_access": rank_report}


def analyze_catalog(catalog) -> dict:
    from .content_audit import audit_content

    pools = {"global": analyze_pool(catalog, set(catalog.cards))}
    for identity, squad in sorted(catalog.squads.items()):
        formation = tuple(squad["formation"])
        eligible = {identity for identity, card in catalog.cards.items() if card["hero"] in formation}
        pools[identity] = analyze_pool(catalog, eligible, formation)
    return {"version": 1, "thresholds": {"minimum_producers": MIN_PRODUCERS,
            "minimum_producer_density_bp": MIN_PRODUCER_DENSITY_BP},
            "edges": [asdict(edge) for edge in derive_edges(catalog)], "pools": pools,
            "normalized_groups": audit_content(catalog)["normalized_card_groups"]}


if __name__ == "__main__":
    from .content import load_catalog

    print(json.dumps(analyze_catalog(load_catalog()), indent=2, sort_keys=True))
