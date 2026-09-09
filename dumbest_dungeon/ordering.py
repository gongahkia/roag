"""Frozen RNG-1 enumeration compatibility; new IDs follow in lexical order."""

from __future__ import annotations

import hashlib
from functools import lru_cache
from importlib.resources import files

from .json_data import loads
from .manifest import canonical_bytes
from .contracts import freeze

ORDER_SHA256 = "3c2f52af04e162871d8b6720432ce247b96757d47e17c1805e5b7293dd8680b4"


@lru_cache(maxsize=1)
def legacy_order() -> dict[str, dict[str, int]]:
    path = files("dumbest_dungeon.data").joinpath("legacy_order.json")
    value = loads(path.read_text(encoding="utf-8"))
    if hashlib.sha256(canonical_bytes(value)).hexdigest() != ORDER_SHA256:
        raise ValueError("legacy RNG-1 order was changed without a compatibility decision")
    if not isinstance(value, dict) or any(
        not isinstance(name, str) or not isinstance(identities, list)
        or any(not isinstance(identity, str) for identity in identities)
        or len(set(identities)) != len(identities)
        for name, identities in value.items()
    ):
        raise ValueError("invalid legacy content order")
    return freeze({name: {identity: index for index, identity in enumerate(identities)} for name, identities in value.items()})


def ordered_definitions(section: str, definitions: dict) -> dict:
    historical = legacy_order().get(section, {})
    return {identity: definitions[identity] for identity in sorted(definitions,
            key=lambda identity: (historical.get(identity, len(historical)), identity))}
