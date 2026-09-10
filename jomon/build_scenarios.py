"""Inspectable build demonstrations used by documentation and audits."""

from dataclasses import dataclass


@dataclass(frozen=True)
class BuildScenario:
    id: str
    identity: str
    weapon: str
    gear: str
    support: str
    technique: str
    passives: tuple[str, ...]
    region: str
    elevation: int
    weather: str
    guarded: bool
    burdened: bool
    expected_combo: str
    decision: str
    production_reducer: str


BUILD_SCENARIOS = (
    BuildScenario("quiet-route-scout", "Quiet route scout", "staff", "quiet shoes", "route survey", "quiet passage", (), "hearthford", 0, "clear", False, False, "surveyed soft-step", "trade support for lower travel noise through ordinary lanes", "move"),
    BuildScenario("mobile-hook-fighter", "Mobile hook fighter", "billhook", "rope", "porter watch", "sure footing", (), "hearthford", 0, "clear", False, False, "mobile hook", "pull a guard out of line and occupy the vacated cell", "attack"),
    BuildScenario("brace-guard", "Armoured brace guard", "spear", "buckler", "porter watch", "set stance", (), "hearthford", 0, "clear", False, False, "shielded set stance", "spend a turn pressing hostile morale while denying a telegraph", "guard"),
    BuildScenario("wet-crossbow", "Weatherproof crossbow courier", "crossbow", "rope", "route survey", "quiet passage", ("waxed bowstring",), "greywash", 0, "hard rain", False, False, "weatherproof aim", "retain a prepared shot when rain would spoil the string", "attack"),
    BuildScenario("controlled-breaker", "Controlled floor breaker", "cudgel", "repair tools", "carpenter rig", "lever craft", ("mill-tooth wedge",), "hearthford", 0, "clear", False, False, "controlled breach", "open a marked weak floor without changing weapons", "interact"),
    BuildScenario("smoke-walker", "Smoke walker", "long knife", "smoke pot", "route survey", "wind listener", ("smoke lens",), "greenwold", 0, "crosswind", False, False, "smoke walker", "trade finite smoke for cover while retaining adjacent sight", "sight_radius"),
    BuildScenario("flood-controller", "Flood controller", "boat hook", "rope", "carpenter rig", "sure footing", ("river hooks",), "hearthford", 0, "hard rain", False, False, "flood rig", "cross and redirect released water while retaining physical recovery", "move"),
    BuildScenario("field-healer", "Healer-survivor", "staff", "rope", "field care", "field binding", ("salted dressing",), "hearthford", 0, "clear", False, False, "deep field binding", "absorb a severe first hit and preserve scarce dressings", "apply_damage"),
    BuildScenario("cargo-negotiator", "Cargo-backed negotiator", "arming sword", "trade seals", "factor surety", "measured terms", ("witness token", "echo bead"), "marlbank", 0, "clear", False, False, "valuable leverage", "carry visible value to open material terms rather than attack", "negotiate"),
    BuildScenario("elevated-marksman", "Elevated marksman", "longbow", "quiet shoes", "route survey", "high arc", ("high tread",), "whitecairn", 1, "clear", False, False, "high-ground drive", "use height to turn a hit into forced movement", "attack"),
    BuildScenario("ebb-accountant", "Ebb accountant", "weighted net", "trade seals", "factor surety", "ebb reader", ("tide ledger",), "greywash", 0, "salt wind", False, False, "accounted ebb", "time a route and settle coast work from the same evidence", "move"),
    BuildScenario("buoyant-porter", "Buoyant cargo porter", "staff", "cargo harness", "porter watch", "sure footing", ("cork float",), "greywash", 0, "coast squall", False, False, "buoyant cargo rig", "preserve one physical lot if current defeats the courier", "_lose_goods"),
    BuildScenario("wind-bow", "Wind-read bow hunter", "longbow", "quiet shoes", "route survey", "wind listener", ("storm vane",), "greenwold", 0, "crosswind", False, False, "wind-read aim", "gain a forecastable firing lane by accepting wind exposure", "effective_weapon_range"),
    BuildScenario("masked-smoke", "Masked smoke hunter", "long knife", "smoke pot", "route survey", "wind listener", ("charcoal mask",), "greenwold", 0, "crosswind", False, False, "masked smoke passage", "break pursuit and remain functional inside a short smoke field", "sight_radius"),
    BuildScenario("crosswind-caller", "Crosswind decoy scout", "longbow", "hooded lantern", "route survey", "wind listener", ("bird whistle",), "greenwold", 0, "crosswind", False, False, "crosswind decoy", "place sound away from the courier to move perceived pursuit", "use_gear"),
    BuildScenario("thorn-bracer", "Thorn brace fighter", "arming sword", "buckler", "porter watch", "set stance", ("thorn weave",), "greenwold", 0, "clear", True, False, "thorn-held momentum", "bank a guarded step for a harder close counter", "attack"),
    BuildScenario("quiet-scree", "Quiet scree courier", "staff", "quiet shoes", "route survey", "scree step", ("limestone cleat",), "whitecairn", 0, "ridge gust", False, False, "quiet scree step", "take a low-noise ridge approach without heavy boots", "terrain_status_for"),
    BuildScenario("high-slinger", "Elevated sling controller", "sling", "quiet shoes", "route survey", "high arc", ("sling cup",), "whitecairn", 1, "clear", False, False, "high sling arc", "arc over partial cover and daze from height", "attack"),
    BuildScenario("floor-salvager", "Weighted floor salvager", "staff", "rope", "porter watch", "lever craft", ("quarry brace",), "whitecairn", 0, "clear", False, True, "weighted floor brace", "use an accountable load as the counterweight for a breach", "_destroy_floor"),
    BuildScenario("directed-descender", "Directed rope descender", "staff", "rope", "route survey", "sure footing", ("fall sail",), "greywash", 1, "salt wind", False, False, "directed fall", "turn an intentional drop into lateral access", "_fall"),
    BuildScenario("javelin-retriever", "Thrown-weapon retriever", "hooked javelin", "rope", "porter watch", "sure footing", ("gullbone reel",), "greywash", 0, "clear", False, False, "retrieval cast", "pull a target while recovering the physical shaft", "attack"),
    BuildScenario("animal-ward", "Grounded animal ward", "boar spear", "rope", "porter watch", "sure footing", ("limestone cleat",), "dunmire", 0, "clear", False, False, "grounded charge brace", "pin a charge before an animal reaches adjacent range", "attack"),
    BuildScenario("roofline-marksman", "Fixed roof marksman", "crossbow", "quiet shoes", "route survey", "quiet passage", ("roof nail",), "rillscar", 1, "ridge gust", False, False, "fixed roof aim", "move once on an upper work without abandoning prepared aim", "move"),
    BuildScenario("market-factor", "Verified market factor", "long knife", "trade seals", "factor surety", "measured terms", ("market weights",), "marlbank", 0, "clear", False, False, "verified market lot", "turn one physical dependency lot into verified stock and confidence", "deliver_dependency"),
)


def validate_build_scenarios() -> None:
    from .content import GEAR, PASSIVES, SUPPORTS, WEAPONS

    ids = [scenario.id for scenario in BUILD_SCENARIOS]
    combos = [scenario.expected_combo for scenario in BUILD_SCENARIOS]
    if len(BUILD_SCENARIOS) != 24 or len(ids) != len(set(ids)) or len(combos) != len(set(combos)):
        raise ValueError("build demonstrations must have 24 unique identities and combinations")
    for scenario in BUILD_SCENARIOS:
        if scenario.weapon not in WEAPONS or scenario.gear not in GEAR or scenario.support not in SUPPORTS:
            raise ValueError(f"unknown build equipment: {scenario.id}")
        if not set(scenario.passives) <= set(PASSIVES):
            raise ValueError(f"unknown build discovery: {scenario.id}")
        if not scenario.decision or not scenario.production_reducer:
            raise ValueError(f"build lacks a decision or production reducer: {scenario.id}")
