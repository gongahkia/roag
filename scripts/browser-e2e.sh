#!/usr/bin/env bash
set -euo pipefail

PORT="${PORT:-4173}"
URL="http://127.0.0.1:${PORT}"
LOG="${TMPDIR:-/tmp}/jomon-vite-${PORT}.log"
CLI=(npx --yes --package @playwright/cli playwright-cli)

"${CLI[@]}" install-browser chromium
npm run preview -- --host 127.0.0.1 --port "$PORT" >"$LOG" 2>&1 &
SERVER_PID=$!
cleanup() { "${CLI[@]}" close >/dev/null 2>&1 || true; kill "$SERVER_PID" >/dev/null 2>&1 || true; }
trap cleanup EXIT
for _ in {1..40}; do curl --fail --silent "$URL" >/dev/null && break; sleep .25; done
curl --fail --silent "$URL" >/dev/null
"${CLI[@]}" open "$URL"
"${CLI[@]}" snapshot
"${CLI[@]}" press n
"${CLI[@]}" snapshot
"${CLI[@]}" press Space
"${CLI[@]}" snapshot
"${CLI[@]}" press a
"${CLI[@]}" press e
"${CLI[@]}" snapshot
route="$("${CLI[@]}" eval "el => el.dataset.route" e3)"
grep --fixed-strings --quiet 'level' <<<"$route"
console="$("${CLI[@]}" console error)"
grep --fixed-strings --quiet 'Errors: 0' <<<"$console"
