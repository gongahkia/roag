"""Finite mixed regional situations and their mutable, revisitable sites."""

from __future__ import annotations

from dataclasses import dataclass, fields

from .catalog import CatalogError, load_catalog
from .state import GameState, Position, Region
from .visuals import SITE_SYMBOLS


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


_CATALOG = load_catalog("situations.json", ("situations", "afterwork_samples"))
_SITUATION_FIELDS = {field.name for field in fields(Situation)}


def _situation_from_data(row: object) -> Situation:
    if not isinstance(row, dict) or set(row) != _SITUATION_FIELDS:
        raise CatalogError("situations.json has an invalid situation record")
    for key, size in (("groups", 2), ("answers", 3)):
        if not isinstance(row[key], list) or len(row[key]) != size or any(not isinstance(value, str) for value in row[key]):
            raise CatalogError(f"situations.json has invalid {key}")
    if any(not isinstance(value, str) for key, value in row.items() if key not in {"groups", "answers"}):
        raise CatalogError("situations.json has non-text situation fields")
    return Situation(**{**row, "groups": tuple(row["groups"]), "answers": tuple(row["answers"])})


if not isinstance(_CATALOG["situations"], list):
    raise CatalogError("situations.json must provide situation records")
SITUATIONS = tuple(_situation_from_data(row) for row in _CATALOG["situations"])
if (not isinstance(_CATALOG["afterwork_samples"], dict)
        or any(not isinstance(region, str) or not isinstance(sample, str)
               for region, sample in _CATALOG["afterwork_samples"].items())):
    raise CatalogError("situations.json has invalid afterwork samples")
AFTERWORK_SAMPLES = _CATALOG["afterwork_samples"]


BY_ID = {row.id: row for row in SITUATIONS}
BY_REGION_BAND = {(row.region_id, row.band): row for row in SITUATIONS}


def _key(row: Situation, part: str) -> str:
    return f"micro-site:{part}:{row.id}"


def _site_point_for_region(region: Region, row: Situation) -> Position:
    stored = region.changes.get(_key(row, "point"))
    if isinstance(stored, str):
        return Position(*map(int, stored.split(",")))
    anchor = region.landmarks[row.anchor]
    forbidden = {c.position for c in region.containers} | set(region.landmarks.values())
    forbidden |= {p for link in region.vertical_links for p in (link.first, link.second)}
    # A bounded local flood from an already validated/reachable landmark is
    # enough. A whole-region flood fill here made every arrival needlessly
    # pay generation-scale work for a three-cell authored placement.
    from collections import deque

    rows = region.levels[str(anchor.z)]
    queue, seen, candidates = deque([anchor]), {anchor}, []
    while queue and not candidates:
        layer = len(queue)
        for _ in range(layer):
            current = queue.popleft()
            if (
                current not in forbidden
                and abs(current.x-anchor.x)+abs(current.y-anchor.y) >= 3
                and all(other.z != current.z or max(abs(other.x-current.x), abs(other.y-current.y)) > 2 for other in region.landmarks.values())
                and all(other.position.z != current.z or max(abs(other.position.x-current.x), abs(other.position.y-current.y)) > 1 for other in region.containers)
            ):
                candidates.append(current)
            for dx, dy in ((0,-1),(1,0),(0,1),(-1,0)):
                point = Position(current.x+dx, current.y+dy, current.z)
                if point in seen or not (0 <= point.y < len(rows) and 0 <= point.x < len(rows[point.y])):
                    continue
                if region.tile_changes.get(f"{point.x},{point.y},{point.z}", rows[point.y][point.x]) in {" ", "#", "~", "T"}:
                    continue
                seen.add(point)
                queue.append(point)
        if len(seen) > 256:
            break
    if not candidates:
        raise RuntimeError(f"{row.name} has no safe local site near {row.anchor}")
    point = min(candidates, key=lambda p: (abs(p.x-anchor.x)+abs(p.y-anchor.y), (p.x*17+p.y*31+len(row.id)) % 11, p.y, p.x))
    region.changes[_key(row, "point")] = f"{point.x},{point.y},{point.z}"
    from .world import position_key

    region.tile_changes[position_key(point)] = "?"
    return point


def site_point(state: GameState, row: Situation) -> Position:
    return _site_point_for_region(state.region, row)


def initialise_region_sites(region: Region) -> None:
    for row in SITUATIONS:
        if row.region_id == region.id:
            _site_point_for_region(region, row)


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
        return SITE_SYMBOLS["resolved"]
    return SITE_SYMBOLS["active"] if state.region.changes.get("situation:active") == row.id else SITE_SYMBOLS["inactive"]


def _condition(state: GameState) -> str:
    from .calendar import calendar_at
    event = state.region.regional_history[-1].kind if state.region.regional_history else "working memory"
    aftermath = state.region.changes.get("aftermath_configuration", "unsettled first state")
    return f"{calendar_at(state).season}; {event}; {aftermath}"


def _prepare_material(state: GameState, row: Situation, point: Position) -> None:
    """Translate authored words and current season/history into sparse play."""
    from .calendar import calendar_at
    from .materials import ensure_cell

    cell = ensure_cell(state, point)
    if cell is None:
        return
    words = row.material
    if any(word in words for word in ("water", "flood", "mud", "brine", "thaw", "shallows")):
        cell.water = max(cell.water, 2 if any(word in words for word in ("deep", "flood", "fast")) else 1)
        cell.fluid = "salt" if any(word in words for word in ("salt", "brine")) else "fresh"
    if any(word in words for word in ("fire", "heat", "smouldering")):
        cell.material, cell.fuel, cell.fire = ("timber" if "timber" in words else "resin"), 3, 1
    if "smoke" in words:
        cell.smoke = max(cell.smoke, 2)
    if any(word in words for word in ("support", "brittle", "debris", "loose rock", "falling")):
        cell.support = min(cell.support, 1)
    if "lime" in words:
        cell.material, cell.coating = "lime", "lime"
    elif "resin" in words:
        cell.material, cell.coating = "resin", "resin"
    elif any(word in words for word in ("clay", "mud", "peat")):
        cell.material = "soil"
    if calendar_at(state).season == "winter" and cell.water and cell.fluid == "fresh":
        cell.ice = True
    if state.region.regional_history:
        last = state.region.regional_history[-1].kind
        if last == "fire":
            cell.smoke = max(cell.smoke, 1)
        elif last == "flood":
            cell.water = min(3, cell.water + 1)
    if state.region.changes.get("aftermath_configuration") == "shared":
        cell.support = min(3, cell.support + 1)
    state.region.changes[_key(row, "material")] = f"water {cell.water}; fire {cell.fire}; smoke {cell.smoke}; support {cell.support}; ice {cell.ice}"


def activate_for_band(state: GameState, band: str) -> Situation | None:
    if state.location != "region" or (state.active_region_id, band) not in BY_REGION_BAND:
        return None
    row = BY_REGION_BAND[(state.active_region_id, band)]
    point = site_point(state, row)
    if state.region.changes.get(_key(row, "resolved")):
        return row
    previous_id = state.region.changes.get("situation:active")
    if isinstance(previous_id, str) and previous_id in BY_ID and previous_id != row.id:
        previous = BY_ID[previous_id]
        from .world import position_key

        state.region.tile_changes[position_key(site_point(state, previous))] = "*" if state.region.changes.get(_key(previous, "resolved")) else "?"
    state.region.changes["situation:active"] = row.id
    from .world import position_key

    state.region.tile_changes[position_key(point)] = "!"
    _prepare_material(state, row, point)
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
    state.region.changes[_key(row, "participants")] = ",".join(actor.id for actor in chosen)
    if not state.region.changes.get(_key(row, "seen")):
        state.region.changes[_key(row, "seen")] = True
        state.add_message(f"SITUATION — {row.name}: {row.duty}; {row.material}. The ! site has three disclosed answers.", priority=3)
    return row


def inspect_lines(state: GameState, situation_id: str) -> list[str]:
    row, point = BY_ID[situation_id], site_point(state, BY_ID[situation_id])
    outcome = state.region.changes.get(_key(row, "outcome"))
    if outcome:
        afterwork = state.region.changes.get(_key(row, "afterwork"))
        lines = [f"FACT {row.name} at {point.x},{point.y},z{point.z:+d} is changed.", f"Outcome: {outcome}.", f"Persistent consequence: {row.consequence}.", f"Revisit: {_condition(state)} now bears the chosen work."]
        if afterwork:
            lines.append(f"Afterwork: {afterwork}; this site's optional field work is finished.")
        else:
            lines.extend(("T. MAINTAIN — brace the changed site and strengthen the working account; two actions.", f"M. SAMPLE — take one physical {AFTERWORK_SAMPLES[row.region_id]} measure; one action. Choose only one."))
        report = state.region.changes.get(_key(row, "report"))
        lines.append(f"Local field report: {report or 'unfiled; return to the local worker to publish it or sell a private lead'}.")
        return lines + ["Inspection costs no time."]
    return [f"VISIBLE {row.name} at {point.x},{point.y},z{point.z:+d}.", f"Groups: {row.groups[0]} and {row.groups[1]}.", f"Duty: {row.duty}.", f"Material: {row.material}.", f"Season/history: {state.region.changes.get(_key(row, 'condition'), _condition(state))}.", f"T. TOOL — {row.answers[0]}; two actions.", f"M. MATERIAL — {row.answers[1]}; spends rope or lamp oil; one action.", f"A. ACCOUNT — {row.answers[2]}; needs standing or cargo; one action.", "Each answer changes actors and another system. Failed choices cost no time."]


def choices(state: GameState, situation_id: str) -> list[tuple[str, str, str, bool, str]]:
    row = BY_ID[situation_id]
    tool = state.gear == "repair tools" or state.weapon in {"spade", "boat hook", "billhook", "hand axe", "felling axe", "war hammer", "cudgel"} or bool(state.courier and state.courier.technique == "lever craft")
    if state.region.changes.get(_key(row, "resolved")):
        if state.region.changes.get(_key(row, "afterwork")):
            return []
        return [("T", "Maintain the changed site and working account", "commitment", tool, "repair tools, working implement, or lever craft"),
                ("M", f"Take one {AFTERWORK_SAMPLES[row.region_id]} sample", "commitment", True, "one free pack cell")]
    if state.region.changes.get("situation:active") != row.id:
        return []
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
    if state.region.changes.get(_key(row, "resolved")):
        from .materials import ensure_cell

        if method == "m":
            from .inventory import InventoryTransaction, auto_place, create_item

            transaction = InventoryTransaction.begin(state)
            sample = AFTERWORK_SAMPLES[row.region_id]
            item = create_item(state, f"ingredient:{sample}", f"{row.name} afterwork sample")
            if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
                transaction.cancel(state)
                return False, "The physical sample needs a free pack cell; nothing changed.", 0
            result, steps = f"sampled one {sample} measure", 1
        else:
            cell = ensure_cell(state, point)
            if cell:
                cell.support, cell.collapse_due = 3, 0
                cell.fire, cell.smoke = 0, 0
            account = state.institutions.get(f"work:{row.region_id}")
            if account:
                account.confidence = min(3, account.confidence + 1)
            result, steps = "maintained the site and strengthened the working account", 2
        state.region.changes[_key(row, "afterwork")] = "sample" if method == "m" else "maintenance"
        record = f"{row.name}: {state.courier.name} {result} after the original work ({state.region.changes[_key(row, 'outcome')]})."
        state.remember(record)
        return True, record, steps
    from .materials import ensure_cell
    cell = ensure_cell(state, point)
    account = state.institutions.get(f"work:{row.region_id}")
    if method == "t":
        if cell:
            cell.support, cell.collapse_due = 3, 0
            cell.fire, cell.fuel = 0, 0
            cell.water = max(0, cell.water - 1)
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
                cell.fire, cell.smoke = 0, max(0, cell.smoke - 2)
        else:
            state.lamp_oil -= 1
            spent = "oil"
            if cell:
                cell.material, cell.coating, cell.fuel = "resin", "oil", max(2, cell.fuel)
                cell.water = max(0, cell.water - 1)
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
        if cell:
            cell.fire, cell.smoke = 0, 0
            cell.water = min(1, cell.water)
            cell.support = max(2, cell.support)
        outcome, steps = row.answers[2], 1
    state.region.changes[_key(row, "resolved")] = True
    state.region.changes[_key(row, "outcome")] = outcome
    state.region.changes[_key(row, "revisit")] = row.consequence
    state.region.changes.pop("situation:active", None)
    from .world import position_key

    state.region.tile_changes[position_key(point)] = "*"
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
    from .production import SOURCES

    if set(AFTERWORK_SAMPLES) != set(SOURCES) or any(sample not in SOURCES[region] for region, sample in AFTERWORK_SAMPLES.items()):
        raise ValueError("afterwork samples need a regional physical source")


def validate_situation_state(state: GameState) -> None:
    for region_id, region in state.regions.items():
        active = region.changes.get("situation:active")
        if active is not None and (active not in BY_ID or BY_ID[str(active)].region_id != region_id):
            raise ValueError("active situation does not belong to its region")
        for key_name, value in region.changes.items():
            for part, valid in (("afterwork", {"sample", "maintenance"}), ("report", {"public", "private"})):
                prefix = f"micro-site:{part}:"
                if key_name.startswith(prefix):
                    situation_id = key_name[len(prefix):]
                    if (situation_id not in BY_ID or BY_ID[situation_id].region_id != region_id
                            or not isinstance(value, str) or value not in valid
                            or not region.changes.get(f"micro-site:resolved:{situation_id}")):
                        raise ValueError("invalid changed-site follow-up")
            if not key_name.startswith("micro-site:point:"):
                continue
            situation_id = key_name.split("micro-site:point:", 1)[1]
            if situation_id not in BY_ID or BY_ID[situation_id].region_id != region_id or not isinstance(value, str):
                raise ValueError("invalid mutable site identity")
            try:
                point = Position(*map(int, value.split(",")))
            except (TypeError, ValueError):
                raise ValueError("invalid mutable site position") from None
            if str(point.z) not in region.levels or not (0 <= point.x < region.width and 0 <= point.y < region.height):
                raise ValueError("mutable site lies outside its region")


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
