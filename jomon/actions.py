"""Direct deterministic actions for Jomon and seamless Hearthford."""

from __future__ import annotations

from dataclasses import dataclass

from .content import COMMODITIES, GEAR, MERCHANT_ITEMS, PASSIVES, RELICS, SUPPORTS, WEAPONS
from .state import CommodityStack, GameState, Person, Position, Threat, stage_rng
from .world import (
    JOMON_GANGPLANK,
    area_name,
    base_tile,
    build_combinations,
    capacity,
    carried_bulk,
    distance,
    field_of_view,
    is_walkable,
    line_of_sight,
    passive_bulk,
    passive_capacity,
    position_key,
    pressure,
    vertical_destination,
)


@dataclass(frozen=True)
class ActionResult:
    changed: bool
    time_advanced: bool
    message: str
    overlay: str | None = None


def _remember_contact(state: GameState, text: str) -> None:
    state.contact.memories.append(text)
    del state.contact.memories[:-8]


def _plain(
    state: GameState,
    message: str,
    *,
    changed: bool = False,
    overlay: str | None = None,
) -> ActionResult:
    if message and overlay is None:
        state.add_message(message)
    return ActionResult(changed, False, message, overlay)


def inspect(state: GameState, subject: str = "area") -> ActionResult:
    if subject == "household":
        living = sum(person.alive for person in state.household)
        active = state.courier.name if state.courier else "not chosen"
        text = f"Household: {living}/6 living; active courier {active}."
    elif subject == "cargo":
        goods = ", ".join(
            f"{name} {stack.quantity}" for name, stack in state.vessel_cargo.items()
        )
        text = f"Jomon hold: {goods}. {state.region.pressure}"
    else:
        text = f"{state.region.condition} {state.region.objective_text}"
    return _plain(state, text, overlay=text)


def choose_courier(state: GameState, person_id: str) -> ActionResult:
    person = next((candidate for candidate in state.household if candidate.id == person_id), None)
    if state.location != "jomon" or person is None or not person.alive:
        return _plain(state, "That household member cannot serve as courier.")
    state.active_courier_id = person.id
    return _plain(state, f"{person.name}, {person.role}, will carry this expedition.", changed=True)


def choose_weapon(state: GameState, weapon: str) -> ActionResult:
    if state.location != "jomon" or weapon not in WEAPONS or weapon not in state.owned_weapons:
        return _plain(state, "That weapon is not available aboard Jomon.")
    state.weapon, state.crossbow_loaded, state.aimed_target = weapon, True, None
    return _plain(state, f"Readied {WEAPONS[weapon][0]}.", changed=True)


def choose_gear(state: GameState, gear: str) -> ActionResult:
    if state.location != "jomon" or gear not in GEAR or gear not in state.owned_gear:
        return _plain(state, "That secondary item is not available aboard Jomon.")
    state.gear = gear
    return _plain(state, f"Packed {GEAR[gear][0]}.", changed=True)


def choose_support(state: GameState, support: str) -> ActionResult:
    if state.location != "jomon" or support not in SUPPORTS:
        return _plain(state, "That crew preparation is not available here.")
    state.support, state.support_spent = support, False
    return _plain(state, f"Prepared {SUPPORTS[support][0]}.", changed=True)


def choose_relic(state: GameState, relic: str | None) -> ActionResult:
    if state.location != "jomon" or (relic is not None and state.relics.get(relic, 0) <= 0):
        return _plain(state, "That finite relic is not available.")
    state.carried_relic = relic
    return _plain(state, f"Carried relic: {relic or 'none'}.", changed=True)


def choose_passive(state: GameState, passive: str) -> ActionResult:
    if state.location != "jomon" or passive not in state.owned_passives:
        return _plain(state, "That discovery is not available aboard Jomon.")
    if state.carried_passives.get(passive, 0):
        del state.carried_passives[passive]
        return _plain(state, f"Stowed {passive} aboard.", changed=True)
    if passive_bulk(state) + PASSIVES[passive][0] > passive_capacity(state):
        return _plain(state, f"Discovery load exceeds {passive_capacity(state)} bulk.")
    state.carried_passives[passive] = 1
    return _plain(state, f"Packed {passive}.", changed=True)


def _step_toward(state: GameState, threat: Threat, target: Position) -> Position:
    dx = 0 if target.x == threat.position.x else (1 if target.x > threat.position.x else -1)
    dy = 0 if target.y == threat.position.y else (1 if target.y > threat.position.y else -1)
    occupied = {
        other.position for other in state.threats
        if other.id != threat.id and other.status in {"watching", "engaged"}
    }
    candidates = (
        Position(threat.position.x + dx, threat.position.y + dy, threat.position.z),
        Position(threat.position.x + dx, threat.position.y, threat.position.z),
        Position(threat.position.x, threat.position.y + dy, threat.position.z),
    )
    for candidate in candidates:
        if candidate != state.position and candidate not in occupied and is_walkable(
            state, candidate, ignore_threat=True
        ):
            return candidate
    return threat.position


def _step_away(state: GameState, threat: Threat) -> Position:
    dx = 1 if threat.position.x >= state.position.x else -1
    dy = 1 if threat.position.y >= state.position.y else -1
    candidates = (
        Position(threat.position.x + dx, threat.position.y + dy, threat.position.z),
        Position(threat.position.x + dx, threat.position.y, threat.position.z),
        Position(threat.position.x, threat.position.y + dy, threat.position.z),
    )
    return next(
        (point for point in candidates if is_walkable(state, point, ignore_threat=True)),
        threat.position,
    )


def _activate(threat: Threat) -> str:
    threat.status = "engaged"
    threat.intent = {
        "pursuer": "rushes directly toward you",
        "reach": "levels a spear and holds two paces",
        "ranged": "takes aim; a bolt follows one clear turn",
        "animal": "scrapes the mud before a territorial charge",
        "machinery": "sweeps marked mill aisles on alternating turns",
    }[threat.profile]
    return f"The {threat.name} notices you: {threat.intent}."
def emit_sound(
    state: GameState, amount: int, origin: Position | None = None
) -> list[str]:
    """Raise noise and alert nearby actors, including actors one level away."""
    if state.location != "region" or amount <= 0:
        return []
    origin = origin or state.position
    state.noise += amount
    messages: list[str] = []
    for threat in state.threats:
        horizontal = max(
            abs(threat.position.x - origin.x), abs(threat.position.y - origin.y)
        )
        if (
            threat.status == "watching"
            and abs(threat.position.z - origin.z) <= 1
            and horizontal <= 4 + amount * 2
        ):
            messages.append(_activate(threat))
            if threat.position.z != origin.z and "echo bead" in state.carried_passives:
                messages.append(
                    f"The echo bead answers: danger stirs on level {threat.position.z:+d}."
                )
    return messages


def _lose_goods(state: GameState) -> str:
    if not state.carried_goods:
        return ""
    if state.support == "porter watch":
        return " The porter's watch preserves the accountable load."
    names = sorted(state.carried_goods)
    if state.gear == "cargo harness" or "river hooks" in state.carried_passives:
        kept = names[0]
        state.carried_goods = {kept: state.carried_goods[kept]}
        return f" The harnessed {kept} survives; other cargo is lost."
    state.carried_goods.clear()
    return f" The {', '.join(names)} is lost."


def _successor(state: GameState, dead: Person) -> Person | None:
    living = [person for person in state.household if person.alive]
    if not living:
        return None
    return sorted(
        living,
        key=lambda person: (-person.relationships.get(dead.id, 0), person.id),
    )[0]


def _return_after_defeat(state: GameState, text: str, permanent: bool) -> str:
    courier = state.courier
    if courier is None:
        return text
    loss = _lose_goods(state)
    state.carried_passives.clear()
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        _remember_contact(state, f"{courier.name} failed to return with Hearthford's need.")
    state.location, state.current_room, state.position = "jomon", None, JOMON_GANGPLANK
    if permanent:
        courier.alive, courier.health, courier.injury = False, 0, "dead"
        successor = _successor(state, courier)
        state.remember(
            f"{courier.name} died in {state.region.hazard}; carried discoveries were lost."
        )
        if successor is None:
            state.active_courier_id, state.world_ended = None, True
            return f"{text}{loss} No eligible adult survives; this world ends."
        state.active_courier_id = successor.id
        successor.relationships[courier.id] = min(
            3, successor.relationships.get(courier.id, 0) + 1
        )
        state.weapon = state.gear = state.support = None
        return f"{text}{loss} {successor.name} succeeds the dead courier."
    courier.health, courier.injury = max(2, courier.max_health // 3), "deep cut"
    state.remember(
        f"{courier.name} escaped to Jomon injured; carried discoveries were lost."
    )
    return f"{text}{loss} The courier reaches Jomon with a deep cut."
