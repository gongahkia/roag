#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
  printf 'usage: %s OUT.zip [POLICY]\n' "$0" >&2
  exit 2
fi

out=$1
policy=${2:-frequency}
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
mkdir -p "$(dirname "$out")"
out_abs=$(cd "$(dirname "$out")" && pwd)/$(basename "$out")

cp pyproject.toml README.md "$tmp/"
cp -R src "$tmp/src"
find "$tmp/src" \( -name '__pycache__' -o -name '*.egg-info' \) -type d -prune -exec rm -rf {} +
find "$tmp/src" -name '*.pyc' -delete
if [[ -f "$policy" ]]; then
  mkdir -p "$tmp/model"
  cp "$policy" "$tmp/model/$(basename "$policy")"
  policy_arg="model/$(basename "$policy")"
else
  policy_arg="$policy"
fi

python3 - "$tmp/bot.py" "$policy_arg" <<'PY'
from __future__ import annotations

from pathlib import Path
import sys

path = Path(sys.argv[1])
policy = sys.argv[2]
path.write_text(
    "from __future__ import annotations\n"
    "import sys\n"
    "from pathlib import Path\n"
    "sys.path.insert(0, str(Path(__file__).resolve().parent / 'src'))\n"
    "from kenjaku.bot import bot_main\n"
    "if __name__ == '__main__':\n"
    "    player_id = sys.argv[1] if len(sys.argv) > 1 else '0'\n"
    f"    raise SystemExit(bot_main(['--policy', {policy!r}, '--player-id', player_id]))\n",
    encoding="utf-8",
)
PY

(cd "$tmp" && zip -qr "$out_abs" bot.py pyproject.toml README.md src model 2>/dev/null || zip -qr "$out_abs" bot.py pyproject.toml README.md src)
