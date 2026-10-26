#!/usr/bin/env python3
"""Reproducible reference inventory; records evidence, never claims feature parity."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
REVISION = "d498fe0889050d2a23f2ff86d80db6084ed6467a"


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)


paths = git("ls-tree", "-r", "--name-only", REVISION).decode().splitlines()
sources = []
identifiers = {}
for path in paths:
    if not path.endswith(".lua") or not (path.startswith("src/") or path in ["main.lua", "conf.lua"]):
        continue
    raw = git("show", f"{REVISION}:{path}")
    code = raw.decode()
    ids = sorted(set(re.findall(r"['\"](core:[a-z_]+/[a-zA-Z0-9_/.-]+)['\"]", code)))
    for identifier in ids:
        identifiers.setdefault(identifier, []).append(path)
    sources.append({"path": path, "sha256": hashlib.sha256(raw).hexdigest(),
        "namedFunctions": re.findall(r"\bfunction\s+([a-zA-Z_][a-zA-Z0-9_.:]*)\s*\(", code),
        "literalContentReferences": ids})

result = {"revision": REVISION,
    "scope": "All original src/*.lua modules plus main.lua and conf.lua. Literal IDs include references, not just definitions; computed IDs require runtime catalog inventory and source review.",
    "sources": sources,
    "contentReferences": [{"id": key, "sources": value} for key, value in sorted(identifiers.items())],
    "originalTests": [path for path in paths if path.startswith("tests/") and path.endswith(".lua")]}
destination = ROOT / "docs/migration/original-source-inventory.json"
destination.parent.mkdir(parents=True, exist_ok=True)
destination.write_text(json.dumps(result, indent=2) + "\n")
print(f"{len(sources)} source files, {len(identifiers)} literal IDs, {len(result['originalTests'])} test files -> {destination}")
