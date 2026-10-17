#!/usr/bin/env python3
"""Copy the unmodified VVVVVV sprite sheet for local prototype use only."""
from pathlib import Path
from zipfile import ZipFile

root = Path(__file__).resolve().parent.parent
target = root / "speedrunner/assets/sprites.png"
with ZipFile(root / "data.zip") as archive:
    source = archive.read("graphics/sprites.png")
if not target.exists() or target.read_bytes() != source:
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(source)
