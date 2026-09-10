"""Versioned offline challenge contracts and custom-expedition codes."""

from __future__ import annotations

import base64
from dataclasses import dataclass
from datetime import date
import hashlib
import json
import zlib
from typing import TYPE_CHECKING, Any

from .manifest import canonical_bytes

if TYPE_CHECKING:
    from .content import Catalog


CHALLENGE_SCHEMA = 1
DAILY_RULESET = 1
DAILY_SALT = "dullest-dungeon/offline-daily/2026-09"
CODE_PREFIX = "DD1"
MODIFIER_IDS = frozenset(
    {
        "accelerated_pressure", "bright_but_loud", "cash_marks", "casualty_cache",
        "control_recoil", "curse_bargain", "discard_current", "elite_weather",
        "fragile_guard", "guarded_clock", "hazardous_routes", "hungry_light",
        "narrow_fire", "objective_sprint", "pressured_rewards", "scarce_supply",
        "stress_furnace", "third_card_reaction", "wound_dividend", "zero_margin",
    }
)


class ChallengeError(ValueError):
    """Raised when a custom expedition or challenge code is incompatible."""


@dataclass(frozen=True)
class Contract:
    id: str
    name: str
    rule: str
    lesson: str
    modifier: str


CONTRACTS = (
    Contract("base:fast_signal", "Fast Signal", "Pressure gains 20% faster.", "Route with visible global time.", "accelerated_pressure"),
    Contract("base:flare_path", "Flare Path", "Begin brighter, but travel is louder.", "Spend local light against global pressure.", "bright_but_loud"),
    Contract("base:marked_receipts", "Marked Receipts", "Mark cash-outs return a small bounty.", "Sequence setup before payoff.", "cash_marks"),
    Contract("base:last_manifest", "Last Manifest", "The first casualty leaves a salvage cache.", "Continue meaningfully after owner loss.", "casualty_cache"),
    Contract("base:feedback_lock", "Feedback Lock", "Repeated control causes stress recoil.", "Rotate control instead of hard-locking.", "control_recoil"),
    Contract("base:debt_engine", "Debt Engine", "Begin cursed and gain a rare engine offer.", "Accept burden for exceptional power.", "curse_bargain"),
    Contract("base:open_current", "Open Current", "The first discard each round draws one.", "Treat discard as velocity.", "discard_current"),
    Contract("base:elite_weather", "Elite Weather", "Every elite carries a visible mutation.", "Read reactions before committing.", "elite_weather"),
    Contract("base:glass_bulwark", "Glass Bulwark", "Guard is stronger, but unused block inflicts stress.", "Size defense instead of hoarding it.", "fragile_guard"),
    Contract("base:held_line", "Held Line", "Guarding slows pressure; unguarded rounds hasten it.", "Trade actions for expedition time.", "guarded_clock"),
    Contract("base:long_way_home", "Long Way Home", "Biome hazards reach one tile farther.", "Value route geometry and facilities.", "hazardous_routes"),
    Contract("base:lamp_hunger", "Lamp Hunger", "Begin with 25 less light.", "Separate visibility from pressure.", "hungry_light"),
    Contract("base:firing_solution", "Firing Solution", "Narrow-rank attacks hit harder after movement.", "Solve position before artillery.", "narrow_fire"),
    Contract("base:two_keys", "Two Keys", "Only one objective is needed, but pressure begins high.", "Practice an objective rush.", "objective_sprint"),
    Contract("base:red_market", "Red Market", "High pressure improves rare reward weight.", "Detour into a dangerous reward lane.", "pressured_rewards"),
    Contract("base:empty_locker", "Empty Locker", "Begin with two fewer supplies.", "Plan attrition and facility use.", "scarce_supply"),
    Contract("base:heat_sink", "Heat Sink", "Stress conversions are stronger; recovery is weaker.", "Build around stress as a resource.", "stress_furnace"),
    Contract("base:third_knock", "Third Knock", "Enemies visibly react to each third card played.", "Sequence around a local reaction.", "third_card_reaction"),
    Contract("base:open_wounds", "Open Wounds", "Wound ticks improve the next technique offer.", "Carry attrition for later targeting.", "wound_dividend"),
    Contract("base:zero_margin", "Zero Margin", "Death's Door checks are harsher; recovery is stronger.", "Choose when to gamble at the edge.", "zero_margin"),
)


@dataclass(frozen=True)
class ExpeditionConfig:
    seed: int
    party: tuple[str, ...] = ()
    loadouts: tuple[tuple[str, str], ...] = ()
    doctrine: str | None = None
    biomes: tuple[str, ...] = ()
    layout: str | None = None
    starting_pressure: int = 0
    ladder_rank: int = 0
    content_packs: tuple[str, ...] = ("base:core",)
    modifiers: tuple[str, ...] = ()

    def payload(self) -> dict[str, Any]:
        return {
            "v": CHALLENGE_SCHEMA, "s": self.seed, "p": list(self.party),
            "o": [list(pair) for pair in self.loadouts], "d": self.doctrine,
            "b": list(self.biomes), "l": self.layout, "r": self.starting_pressure,
            "a": self.ladder_rank, "k": list(self.content_packs), "m": list(self.modifiers),
        }


def validate_config(config: ExpeditionConfig, catalog: Catalog) -> ExpeditionConfig:
    if type(config.seed) is not int or not 0 <= config.seed < 2**64:
        raise ChallengeError("seed must be an unsigned 64-bit integer")
    if config.party and (len(config.party) != 4 or len(set(config.party)) != 4 or any(hero_id not in catalog.heroes for hero_id in config.party)):
        raise ChallengeError("party must contain four distinct known crew IDs")
    loadouts = dict(config.loadouts)
    if len(loadouts) != len(config.loadouts) or any(hero_id not in config.party or loadout_id not in catalog.loadouts or catalog.loadouts[loadout_id]["hero"] != hero_id for hero_id, loadout_id in config.loadouts):
        raise ChallengeError("loadouts must belong to selected crew")
    if config.doctrine is not None and config.doctrine not in catalog.doctrines:
        raise ChallengeError("unknown doctrine")
    if config.biomes and (len(config.biomes) != 4 or len(set(config.biomes)) != 4 or any(biome_id not in catalog.biomes for biome_id in config.biomes)):
        raise ChallengeError("biomes must contain four distinct known IDs")
    layouts = {world["layout"] for world in catalog.worlds.values()}
    if config.layout is not None and config.layout not in layouts:
        raise ChallengeError("unknown layout")
    if type(config.starting_pressure) is not int or not 0 <= config.starting_pressure <= 10_000:
        raise ChallengeError("starting pressure must be 0..10000")
    if type(config.ladder_rank) is not int or not 0 <= config.ladder_rank <= 20:
        raise ChallengeError("ladder rank must be 0..20")
    if not config.content_packs or tuple(sorted(set(config.content_packs))) != config.content_packs or any(pack != "base:core" for pack in config.content_packs):
        raise ChallengeError("this ruleset supports only the base:core content pack")
    if tuple(sorted(set(config.modifiers))) != config.modifiers or any(modifier not in MODIFIER_IDS for modifier in config.modifiers):
        raise ChallengeError("modifiers must be sorted, unique, and supported")
    return config


def daily_seed(day: date) -> int:
    if not isinstance(day, date):
        raise ChallengeError("daily seed requires a calendar date")
    material = f"{DAILY_SALT}:{DAILY_RULESET}:{day.isoformat()}".encode("ascii")
    return int.from_bytes(hashlib.sha256(material).digest()[:8], "big")


def daily_config(day: date) -> ExpeditionConfig:
    seed = daily_seed(day)
    return ExpeditionConfig(seed=seed, modifiers=(CONTRACTS[seed % len(CONTRACTS)].modifier,))


def encode_code(config: ExpeditionConfig, catalog: Catalog) -> str:
    validate_config(config, catalog)
    packed = zlib.compress(canonical_bytes(config.payload()), level=9)
    token = base64.urlsafe_b64encode(packed).rstrip(b"=").decode("ascii")
    checksum = hashlib.sha256(packed).hexdigest()[:8]
    return f"{CODE_PREFIX}.{token}.{checksum}"


def decode_code(code: str, catalog: Catalog) -> ExpeditionConfig:
    if not isinstance(code, str) or len(code) > 1024:
        raise ChallengeError("challenge code is missing or too long")
    parts = code.split(".")
    if len(parts) != 3 or parts[0] != CODE_PREFIX or len(parts[2]) != 8:
        raise ChallengeError("challenge code prefix or framing is invalid")
    try:
        packed = base64.b64decode(parts[1] + "=" * (-len(parts[1]) % 4), altchars=b"-_", validate=True)
    except (ValueError, UnicodeEncodeError) as exc:
        raise ChallengeError("challenge code payload is not base64url") from exc
    if hashlib.sha256(packed).hexdigest()[:8] != parts[2]:
        raise ChallengeError("challenge code checksum does not match")
    try:
        inflater = zlib.decompressobj()
        decoded = inflater.decompress(packed, 8193)
        if len(decoded) > 8192 or inflater.unconsumed_tail or not inflater.eof:
            raise ChallengeError("challenge code expands beyond its limit")

        def unique_object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
            result: dict[str, Any] = {}
            for key, value in pairs:
                if key in result:
                    raise ChallengeError("challenge code contains duplicate fields")
                result[key] = value
            return result

        raw = json.loads(
            decoded.decode("ascii"), object_pairs_hook=unique_object,
            parse_constant=lambda value: (_ for _ in ()).throw(
                ChallengeError(f"challenge code contains non-finite {value}")
            ),
        )
    except (UnicodeError, json.JSONDecodeError, zlib.error) as exc:
        raise ChallengeError("challenge code payload is corrupt") from exc
    expected = {"v", "s", "p", "o", "d", "b", "l", "r", "a", "k", "m"}
    if not isinstance(raw, dict) or set(raw) != expected or raw.get("v") != CHALLENGE_SCHEMA:
        raise ChallengeError("challenge code schema is unsupported")
    try:
        config = ExpeditionConfig(seed=raw["s"], party=tuple(raw["p"]), loadouts=tuple(tuple(pair) for pair in raw["o"]), doctrine=raw["d"], biomes=tuple(raw["b"]), layout=raw["l"], starting_pressure=raw["r"], ladder_rank=raw["a"], content_packs=tuple(raw["k"]), modifiers=tuple(raw["m"]))
    except (KeyError, TypeError) as exc:
        raise ChallengeError("challenge code has malformed fields") from exc
    return validate_config(config, catalog)
