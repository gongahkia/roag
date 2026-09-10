"""Sixteen bounded learned practices earned through reciprocal world work."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState


@dataclass(frozen=True)
class Practice:
    name: str
    region_id: str
    source: str
    effect: str
    description: str


PRACTICES = {
    practice.name: practice
    for practice in (
        Practice("bank-water cadence", "hearthford", "network", "shallow-water-step", "Fresh shallows no longer add a crossing action; deep current still does."),
        Practice("field-rill measure", "marlbank", "network", "mud-quiet", "Mud no longer adds routine step noise, though deep bogging still slows."),
        Practice("wreck-title hold", "greywash", "network", "secured-salvage", "The first difficult cache recovery in a region earns one witnessed confidence."),
        Practice("span-watch stance", "rillscar", "network", "support-guard", "Guarding on a damaged support seats one point and cancels its warning."),
        Practice("ash-refuge breathing", "greenwold", "network", "smoke-sight", "Smoke leaves five paces of sight and inhalation clears sooner."),
        Practice("island porter relay", "dunmire", "network", "wet-load-step", "A laden pack does not add a second action on mud or shallow water."),
        Practice("ridge-sounding line", "whitecairn", "network", "elevated-range", "A ranged weapon gains one pace from a higher level."),
        Practice("winter-braid reading", "frostmere", "network", "ice-step", "Known ice no longer causes poor footing; salt water remains open."),
        Practice("siltgate hand", "hearthford", "aftermath", "material-dig", "Digging a worked watercourse no longer needs a heavy tool."),
        Practice("ebb beacon watch", "greywash", "aftermath", "storm-sight", "Coastal rain, fog and squall leave one additional pace of sight."),
        Practice("living firebreak", "greenwold", "aftermath", "material-cut", "Dry burning reeds and timber can be cut or extinguished without a heavy tool."),
        Practice("honest stair breath", "whitecairn", "aftermath", "stair-economy", "A lower-limb injury or heavy armour cannot both double a known upward climb."),
        Practice("peat brace seating", "dunmire", "aftermath", "material-brace", "Wet timber support can be braced without a levering tool."),
        Practice("two-span withdrawal", "rillscar", "aftermath", "reach-retreat", "A guarded reach weapon preserves one controlled reposition and presses morale."),
        Practice("seed-clay tread", "marlbank", "aftermath", "clay-step", "Clay mud neither bogs the courier nor adds routine movement noise."),
        Practice("thaw-net recovery", "frostmere", "aftermath", "net-recover", "A committed weighted net remains as one physical recoverable bundle."),
    )
}

NETWORK_CONTACT_PRACTICE = {
    "network-contact-hearthford": "bank-water cadence",
    "network-contact-marlbank": "field-rill measure",
    "network-contact-greywash": "wreck-title hold",
    "network-contact-rillscar": "span-watch stance",
    "network-contact-greenwold": "ash-refuge breathing",
    "network-contact-dunmire": "island porter relay",
    "network-contact-whitecairn": "ridge-sounding line",
    "network-contact-frostmere": "winter-braid reading",
}

AFTERMATH_REGION_PRACTICE = {
    "hearthford": "siltgate hand",
    "greywash": "ebb beacon watch",
    "greenwold": "living firebreak",
    "whitecairn": "honest stair breath",
    "dunmire": "peat brace seating",
    "rillscar": "two-span withdrawal",
    "marlbank": "seed-clay tread",
    "frostmere": "thaw-net recovery",
}


def learned_effects(state: GameState) -> set[str]:
    known = set(state.courier.learned_techniques) if state.courier else set()
    return {practice.effect for name, practice in PRACTICES.items() if name in known}


def has_effect(state: GameState, effect: str) -> bool:
    return effect in learned_effects(state)


def teach_network_practice(state: GameState, contact_id: str) -> tuple[bool, str]:
    name = NETWORK_CONTACT_PRACTICE.get(contact_id)
    if name is None or state.courier is None:
        return False, "No embodied network instruction is available here."
    if name in state.courier.learned_techniques:
        return False, f"{state.courier.name} already knows {name}."
    state.courier.learned_techniques.append(name)
    practice = PRACTICES[name]
    state.remember(f"{state.courier.name} learned {name} through {contact_id}: {practice.description}")
    return True, f"{state.courier.name} learns {name}: {practice.description}"


def teach_aftermath_practice(state: GameState, region_id: str) -> str:
    if state.courier is None:
        return ""
    name = AFTERMATH_REGION_PRACTICE[region_id]
    if name in state.courier.learned_techniques:
        return ""
    state.courier.learned_techniques.append(name)
    practice = PRACTICES[name]
    text = f" {state.courier.name} learns {name} from the completed aftermath: {practice.description}"
    state.remember(text.strip())
    return text


def validate_practices() -> None:
    if len(PRACTICES) != 16 or len({practice.effect for practice in PRACTICES.values()}) != 16:
        raise ValueError("practices must provide sixteen distinct mechanical effects")
    if set(NETWORK_CONTACT_PRACTICE.values()) | set(AFTERMATH_REGION_PRACTICE.values()) != set(PRACTICES):
        raise ValueError("every practice needs one bounded production source")
    if set(NETWORK_CONTACT_PRACTICE) != {
        f"network-contact-{region_id}" for region_id in AFTERMATH_REGION_PRACTICE
    }:
        raise ValueError("network practice witnesses do not cover every region")


validate_practices()
