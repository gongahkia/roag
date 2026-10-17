#!/usr/bin/env bash
set -euo pipefail
repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ -n "${GODOT_BIN:-}" ]]; then
  exec "$GODOT_BIN" "$@"
fi
for candidate in godot4 godot; do
  if command -v "$candidate" >/dev/null 2>&1; then
    installed_version="$("$candidate" --version)"
    if [[ "$installed_version" =~ ^4\.([0-9]+)\. && "${BASH_REMATCH[1]}" -ge 7 ]]; then
      exec "$candidate" "$@"
    fi
  fi
done
version="4.7.2-stable"
binary="$repo_dir/.tools/Godot_v${version}_linux.x86_64"
if [[ ! -x "$binary" ]]; then
  if [[ "$(uname -s)" != Linux || "$(uname -m)" != x86_64 ]]; then
    echo "Install Godot 4.7+, or set GODOT_BIN to its executable path." >&2
    exit 1
  fi
  mkdir -p "$repo_dir/.tools"
  archive="$(mktemp "$repo_dir/.tools/godot.XXXXXX.zip")"
  trap 'rm -f -- "$archive"' EXIT
  curl --fail --location --retry 3 \
    "https://github.com/godotengine/godot/releases/download/${version}/Godot_v${version}_linux.x86_64.zip" \
    --output "$archive"
  python3 - "$archive" "$repo_dir/.tools" <<'PY'
import sys
from zipfile import ZipFile
with ZipFile(sys.argv[1]) as archive:
    archive.extractall(sys.argv[2])
PY
  chmod +x "$binary"
  rm -f -- "$archive"
  trap - EXIT
fi
exec "$binary" "$@"
