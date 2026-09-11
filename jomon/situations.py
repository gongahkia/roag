"""Finite mixed regional situations and their mutable, revisitable sites."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Position


@dataclass(frozen=True)
class Situation:
    id: str
    region_id: str
    band: str
    name: str
    anchor: str
    groups: tuple[str, str]
    duty: str
    material: str
    answers: tuple[str, str, str]
    consequence: str


def _s(region, band, suffix, name, anchor, groups, duty, material, answers, consequence):
    return Situation(f"{region}:{band}:{suffix}", region, band, name, anchor, groups, duty, material, answers, consequence)


# Three authored collisions per region. The records select content; reducers
# below retain explicit, shared tool/material/account behavior.
SITUATIONS = (
    _s("hearthford", "steady", "reed-tally", "The reed-bank tally", "settlement", ("mill workers", "reed animals"), "recover marked grain before the bank softens", "mud and shallow water", ("lay a tool-marked dry path", "redirect the shallow run", "witness a shared carrying line"), "the bank becomes a witnessed public landing"),
    _s("hearthford", "strained", "wheel-lane", "The wheel lane", "mill", ("mill levy", "cargo reavers"), "hold a moving load clear of the crown wheel", "timber supports and noise", ("brace the load lane", "wet and slow the wheel apron", "name whose cargo has precedence"), "the mill approach remains open under pressure"),
    _s("hearthford", "critical", "flood-watch", "The flood watch crossing", "watchtower", ("road watch", "flood wildlife"), "carry a warning across the divided meadow", "deepening fresh water", ("set a raised warning rail", "cut a relief rill", "join the two watch accounts"), "both banks remember the same flood warning"),
    _s("greywash", "steady", "pan-birds", "The pan-bird line", "saltworks", ("salt workers", "shore scavengers"), "keep brine out of a nesting strip", "saltwater and loose gravel", ("raise a carrying trestle", "pour a decoy brine run", "agree a quiet work interval"), "the salt edge keeps a quiet shared lane"),
    _s("greywash", "strained", "wreck-title", "The wreck-title stand", "wreck", ("wreck salvagers", "shore claimants"), "move one named fitting above the tide", "rising saltwater and wet timber", ("rig the fitting above water", "float it along the draining cut", "witness temporary shared title"), "the wreck fitting reaches a marked dry store"),
    _s("greywash", "critical", "chain-ebb", "The last chain ebb", "chain_house", ("chain guards", "tide runners"), "hold the road while a link is seated", "wind-driven floodwater", ("brace the chain cradle", "open a sacrificial drain", "record a common-road claim"), "one final ebb route survives the closing tide"),
    _s("greenwold", "steady", "coppice-deer", "The coppice feeding edge", "clearing", ("medicine cutters", "woodland grazers"), "separate fresh shoots from a working trail", "wet loam and living brush", ("cut a woven barrier", "lay an ash-scented turn", "set a shared grazing boundary"), "the coppice and trail remain visibly divided"),
    _s("greenwold", "strained", "resin-smoke", "The resin-yard smoke turn", "resin_yard", ("resin workers", "charcoal runners"), "move sealed resin away from the burn wind", "resin, heat and smoke", ("lever the sealed rack", "dampen a smoke break", "witness priority for medicine stores"), "the resin rack stands behind a readable firebreak"),
    _s("greenwold", "critical", "burn-refuge", "The burn refuge", "burn_walk", ("burn crew", "displaced predators"), "open a retreat without feeding fire", "dry timber and dense smoke", ("cut a two-sided refuge", "turn water through the ash bed", "call a bounded common withdrawal"), "the burn leaves a maintained refuge"),
    _s("whitecairn", "steady", "wool-step", "The wool stair", "settlement", ("upland carriers", "ridge scavengers"), "move wet wool without losing the switchback", "slick limestone", ("pin a handline", "spread grit over the step", "witness staggered right of way"), "the stair keeps a marked passing bay"),
    _s("whitecairn", "strained", "kiln-scree", "The kiln scree bargain", "lime_kiln", ("kiln workers", "private toll crew"), "keep hot lime below a failing shelf", "lime dust and loose rock", ("brace the kiln shelf", "wet the lime-dust lane", "record the public load warning"), "the kiln road gains an honest warning bay"),
    _s("whitecairn", "critical", "bell-span", "The two-bell span", "bell_tower", ("load witnesses", "false-bell guards"), "cross before the face sheds stone", "damaged supports and debris", ("seat a counterbrace", "break the false echo screen", "make both crews answer one interval"), "the surviving span carries one warning"),
    _s("dunmire", "steady", "peat-sledge", "The peat-sledge island", "settlement", ("peat cutters", "fen grazers"), "move dry fuel beside a nesting edge", "wet peat and reeds", ("lay a reusable sledge way", "flood a decoy reed cut", "mark alternating work hours"), "the island keeps work and nesting margins"),
    _s("dunmire", "strained", "bank-fire", "The smoking bank", "ruin", ("bank company", "fuel runners"), "stop peat fire without flooding homes", "smouldering peat and shallow flood", ("cut and brace a trench", "smother the face with wet peat", "call in the raised-bank account"), "the repaired bank holds a named firebreak"),
    _s("dunmire", "critical", "causeway-claim", "The divided causeway", "far_bank", ("causeway keepers", "opportunist raiders"), "recover a plug while water divides the parties", "deep mud and floodwater", ("lever a dry side passage", "set a temporary peat plug", "trade safe withdrawal for the plug"), "the far bank keeps a second retreat"),
    _s("rillscar", "steady", "ore-goats", "The ore-shelf herd", "settlement", ("ore carriers", "shelf animals"), "clear a narrow load shelf without panic", "loose ironstone", ("set a low guide rail", "cast gravel toward open shelf", "hold work until the herd crosses"), "the shelf receives an animal bypass"),
    _s("rillscar", "strained", "charcoal-span", "The charcoal span", "ruin", ("bridge crews", "fuel guards"), "carry fuel over a disputed brace", "brittle timber over water", ("sister the failing brace", "lower the fuel by rope", "witness one provisional crossing"), "the old span holds a light-load route"),
    _s("rillscar", "critical", "tailrace-convoy", "The tailrace convoy", "works", ("cutworks convoy", "gorge raiders"), "protect iron through the fracture", "fast water and damaged stone", ("brace a covered lane", "release water against the shelf", "offer passage without the iron"), "shipment and fracture enter both accounts"),
    _s("marlbank", "steady", "seed-birds", "The seed-clay margin", "settlement", ("field workers", "terrace birds"), "keep seed above watered clay", "mud and loose seed", ("lay fired stepping tiles", "draw water into a lower furrow", "mark a tolerated gleaning edge"), "the terrace keeps a gleaning margin"),
    _s("marlbank", "strained", "kiln-water", "The kiln-water turn", "works", ("potters", "field irrigators"), "divide water between firing and shoots", "heat, clay and flowing water", ("fit a measured gate stop", "seal a side rill with hot spoil", "record a timed half-share"), "the release follows a shared measure"),
    _s("marlbank", "critical", "terrace-collapse", "The falling terrace", "far_bank", ("seed court", "private carriers"), "save grain while the face fails", "saturated clay and debris", ("brace the upper face", "cut a safe spill channel", "exchange the load for repair"), "the failed face becomes a public stair"),
    _s("frostmere", "steady", "net-seals", "The net-house shoal", "settlement", ("net workers", "estuary grazers"), "free a net without closing the animal channel", "cold shallows and gravel", ("raise the net on ice pegs", "open a warmer side braid", "suspend work for one sounding"), "the shoal keeps an animal opening"),
    _s("frostmere", "strained", "wool-thaw", "The thaw-wool crossing", "ruin", ("winter pilots", "wool carriers"), "move wool before the ice braid separates", "thinning ice and wet cargo", ("set a pegged handline", "break a controlled float channel", "join pilot and shelter tallies"), "the crossing retains a thaw detour"),
    _s("frostmere", "critical", "false-sounding", "The false sounding", "works", ("channel pilots", "wreck claimants"), "recover the true marker under a squall", "wind, ice and saltwater", ("brace a high signal line", "thaw the marker free", "force one public sounding"), "the true channel remains marked"),
)

BY_ID = {row.id: row for row in SITUATIONS}
BY_REGION_BAND = {(row.region_id, row.band): row for row in SITUATIONS}


def _key(row: Situation, part: str) -> str:
    return f"micro-site:{part}:{row.id}"


def site_point(state: GameState, row: Situation) -> Position:
    stored = state.region.changes.get(_key(row, "point"))
    if isinstance(stored, str):
        return Position(*map(int, stored.split(",")))
    from .regions import region_reachable

    anchor = state.region.landmarks[row.anchor]
    forbidden = {c.position for c in state.region.containers} | set(state.region.landmarks.values())
    forbidden |= {p for link in state.region.vertical_links for p in (link.first, link.second)}
    candidates = [
        p for p in region_reachable(state.region)
        if p.z == anchor.z and p not in forbidden
        and all(
            other.z != p.z or max(abs(other.x-p.x), abs(other.y-p.y)) > 2
            for other in state.region.landmarks.values()
        )
        and all(
            other.position.z != p.z
            or max(abs(other.position.x-p.x), abs(other.position.y-p.y)) > 1
            for other in state.region.containers
        )
    ]
    point = min(candidates, key=lambda p: (abs(p.x-anchor.x)+abs(p.y-anchor.y), (p.x*17+p.y*31+len(row.id)) % 11, p.y, p.x))
    state.region.changes[_key(row, "point")] = f"{point.x},{point.y},{point.z}"
    return point


def site_at(state: GameState, point: Position, *, adjacent: bool = False) -> Situation | None:
    if state.location != "region":
        return None
    for row in SITUATIONS:
        if row.region_id == state.active_region_id:
            site = site_point(state, row)
            if site.z == point.z and max(abs(site.x-point.x), abs(site.y-point.y)) <= int(adjacent):
                return row
    return None


def site_glyph(state: GameState, point: Position) -> str | None:
    row = site_at(state, point)
    if not row:
        return None
    if state.region.changes.get(_key(row, "resolved")):
        return "*"
    return "!" if state.region.changes.get("situation:active") == row.id else "?"


def _condition(state: GameState) -> str:
    from .calendar import calendar_at
    event = state.region.regional_history[-1].kind if state.region.regional_history else "working memory"
    aftermath = state.region.changes.get("aftermath_configuration", "unsettled first state")
    return f"{calendar_at(state).season}; {event}; {aftermath}"


def activate_for_band(state: GameState, band: str) -> Situation | None:
    if state.location != "region" or (state.active_region_id, band) not in BY_REGION_BAND:
        return None
    row = BY_REGION_BAND[(state.active_region_id, band)]
    point = site_point(state, row)
    if state.region.changes.get(_key(row, "resolved")):
        return row
    state.region.changes["situation:active"] = row.id
    state.region.changes.setdefault(_key(row, "condition"), _condition(state))
    candidates = sorted((a for a in state.combatants if a.status in {"watching", "dormant"}), key=lambda a: (a.group, a.id))
    chosen = []
    for actor in candidates:
        if not chosen or actor.group != chosen[0].group:
            chosen.append(actor)
        if len(chosen) == 2:
            break
    for index, actor in enumerate(chosen):
        actor.status, actor.objective_position = "watching", point
        actor.intent = f"works around {row.name.lower()}; reacts to {row.material}"
        actor.goal_reason = f"{row.groups[index]} have a material stake here"
    if not state.region.changes.get(_key(row, "seen")):
        state.region.changes[_key(row, "seen")] = True
        state.add_message(f"SITUATION — {row.name}: {row.duty}; {row.material}. The ! site has three disclosed answers.", priority=3)
    return row


def inspect_lines(state: GameState, situation_id: str) -> list[str]:
    row, point = BY_ID[situation_id], site_point(state, BY_ID[situation_id])
    outcome = state.region.changes.get(_key(row, "outcome"))
    if outcome:
        return [f"FACT {row.name} at {point.x},{point.y},z{point.z:+d} is changed.", f"Outcome: {outcome}.", f"Persistent consequence: {row.consequence}.", f"Revisit: {_condition(state)} now bears the chosen work.", "Inspection costs no time."]
    return [f"VISIBLE {row.name} at {point.x},{point.y},z{point.z:+d}.", f"Groups: {row.groups[0]} and {row.groups[1]}.", f"Duty: {row.duty}.", f"Material: {row.material}.", f"Season/history: {state.region.changes.get(_key(row, 'condition'), _condition(state))}.", f"T. TOOL — {row.answers[0]}; two actions.", f"M. MATERIAL — {row.answers[1]}; spends rope or lamp oil; one action.", f"A. ACCOUNT — {row.answers[2]}; needs standing or cargo; one action.", "Each answer changes actors and another system. Failed choices cost no time."]


def choices(state: GameState, situation_id: str) -> list[tuple[str, str, str, bool, str]]:
    row = BY_ID[situation_id]
    if state.region.changes.get(_key(row, "resolved")) or state.region.changes.get("situation:active") != row.id:
        return []
    tool = state.gear == "repair tools" or state.weapon in {"spade", "boat hook", "billhook", "hand axe", "felling axe", "war hammer", "cudgel"} or bool(state.courier and state.courier.technique == "lever craft")
    material = state.rope_uses > 0 or state.lamp_oil > 0
    account = state.contact.disposition >= 0 or bool(state.carried_goods.get(state.region.objective_commodity))
    return [("T", row.answers[0].title(), "commitment", tool, "repair tools, working implement, or lever craft"), ("M", row.answers[1].title(), "commitment", material, "one rope use or lamp-oil measure"), ("A", row.answers[2].title(), "commitment", account, "non-hostile standing or one relevant cargo lot")]


def resolve(state: GameState, situation_id: str, method: str) -> tuple[bool, str, int]:
    row = BY_ID.get(situation_id)
    if not row or row.region_id != state.active_region_id:
        return False, "That situation is not present here.", 0
    available = {key.lower(): (ok, reason) for key, _, _, ok, reason in choices(state, situation_id)}
    if method not in available:
        return False, "That site's work is already settled.", 0
    okay, requirement = available[method]
    if not okay:
        return False, f"That answer needs {requirement}.", 0
    point = site_point(state, row)
    from .materials import ensure_cell
    cell = ensure_cell(state, point)
    account = state.institutions.get(f"work:{row.region_id}")
    if method == "t":
        if cell:
            cell.support, cell.collapse_due = 3, 0
        for actor in state.combatants:
            if actor.objective_position == point:
                actor.morale -= 1
                actor.intent = "yields to completed physical work"
        outcome, steps = row.answers[0], 2
    elif method == "m":
        if state.rope_uses:
            state.rope_uses -= 1
            spent = "rope"
            if cell:
                cell.support, cell.coating = min(3, cell.support + 1), "wet"
        else:
            state.lamp_oil -= 1
            spent = "oil"
            if cell:
                cell.material, cell.coating, cell.fuel = "resin", "oil", max(2, cell.fuel)
        state.region.changes[f"population-shift:{row.id}"] = f"{row.groups[1]} use the altered margin"
        outcome, steps = f"{row.answers[1]} with {spent}", 1
    else:
        if account:
            account.trust = min(3, account.trust + 1)
            account.witnessed_acts.append(f"{row.name}: {row.answers[2]}")
            del account.witnessed_acts[:-12]
        state.contact.disposition = min(3, state.contact.disposition + 1)
        state.market[state.region.objective_commodity].demand = max(0, state.market[state.region.objective_commodity].demand - 1)
        for actor in state.combatants:
            if actor.objective_position == point and actor.profile != "animal":
                actor.status, actor.intent = "negotiated", "accepts the bounded account"
        outcome, steps = row.answers[2], 1
    state.region.changes[_key(row, "resolved")] = True
    state.region.changes[_key(row, "outcome")] = outcome
    state.region.changes[_key(row, "revisit")] = row.consequence
    state.region.changes.pop("situation:active", None)
    record = f"{row.name} settled by {outcome}; {row.consequence}."
    state.remember(record)
    state.contact.memories.append(record)
    del state.contact.memories[:-8]
    return True, record, steps


def interaction(state: GameState) -> str | None:
    row = site_at(state, state.position, adjacent=True)
    return f"situation:{row.id}" if row else None


def validate_situations() -> None:
    if len(SITUATIONS) != 24 or len(BY_ID) != 24:
        raise ValueError("exactly 24 distinct situations are required")
    for region in {row.region_id for row in SITUATIONS}:
        if {row.band for row in SITUATIONS if row.region_id == region} != {"steady", "strained", "critical"}:
            raise ValueError(f"{region} needs all pressure bands")
    if len({row.region_id for row in SITUATIONS}) != 8 or any(len(set(row.groups)) != 2 or not all((*row.answers, row.duty, row.material, row.consequence)) for row in SITUATIONS):
        raise ValueError("mixed situation content is incomplete")


def audit_situations(samples: int = 200) -> dict[str, object]:
    """Audit deterministic occurrence and variety without generating new actors."""
    from collections import Counter
    from .state import stage_rng

    occurrences = Counter()
    signatures = set()
    for index in range(samples):
        # Seeded visit order demonstrates that definitions do not depend on
        # mapping order while every regional pressure opportunity still occurs.
        seed = f"situation-audit-{index}"
        rows = list(SITUATIONS)
        stage_rng(seed, "situation-order").shuffle(rows)
        for row in rows:
            occurrences[row.id] += 1
            signatures.add((row.groups, row.duty, row.material, row.answers, row.consequence))
    total = sum(occurrences.values())
    share = max(occurrences.values(), default=0) / total if total else 0.0
    failures = []
    if len(occurrences) != 24 or len(signatures) != 24:
        failures.append("not every authored situation occurred distinctly")
    if share > 0.10:
        failures.append("one situation exceeded ten percent of all opportunities")
    return {
        "samples": samples,
        "opportunities": total,
        "situations": len(occurrences),
        "distinct_signatures": len(signatures),
        "max_share": round(share, 4),
        "failures": failures,
    }
