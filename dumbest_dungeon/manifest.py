"""Canonical content identity, independent of JSON key and catalog ordering."""

from __future__ import annotations

import hashlib
import json
from dataclasses import asdict, dataclass
from typing import Any

from .versions import CONTENT_SCHEMA, ENGINE_VERSION, MANIFEST_SCHEMA, RNG_ARCHITECTURE


def canonical_bytes(value: Any) -> bytes:
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=True, allow_nan=False).encode("ascii")


@dataclass(frozen=True)
class ContentManifest:
    schema: int
    engine: str
    content_schema: int
    rng_architecture: int
    fingerprint: str
    enabled_packs: tuple[str, ...]

    def snapshot(self) -> dict[str, Any]:
        return json.loads(canonical_bytes(asdict(self)))


def content_manifest(catalog) -> ContentManifest:
    # declaration order is irrelevant; effect, action and formation order is not.
    rules = {
        name: {identity: definition for identity, definition in getattr(catalog, name).items()}
        for name in catalog.__dataclass_fields__
        if name not in {"raw", "balance", "art"} and name in catalog.raw
    }
    schema = catalog.raw["schema_version"]
    rules.update(balance=catalog.balance, art=catalog.art, content_schema=schema)
    digest = hashlib.sha256(canonical_bytes(rules)).hexdigest()
    return ContentManifest(MANIFEST_SCHEMA, ENGINE_VERSION, schema, RNG_ARCHITECTURE,
                           digest, ("base:core",))
