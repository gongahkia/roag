#!/usr/bin/env bash
set -euo pipefail

PORT="${PORT:-4173}"
URL="http://127.0.0.1:${PORT}"
LOG="${TMPDIR:-/tmp}/jomon-vite-${PORT}.log"
CLI=(npx --yes --package @playwright/cli playwright-cli)
canvas_hash() { "${CLI[@]}" eval "(() => { const el = document.querySelector('#game'); const data = el.getContext('2d').getImageData(0, 0, el.width, el.height).data; let hash = 2166136261; for (let i = 0; i < data.length; i += 4) { hash ^= data[i]; hash = Math.imul(hash, 16777619) } return hash >>> 0 })()"; }

"${CLI[@]}" close >/dev/null 2>&1 || true
"${CLI[@]}" install-browser chromium
./node_modules/.bin/vite preview --host 127.0.0.1 --port "$PORT" --strictPort >"$LOG" 2>&1 &
SERVER_PID=$!
cleanup() { "${CLI[@]}" close >/dev/null 2>&1 || true; kill "$SERVER_PID" >/dev/null 2>&1 || true; }
trap cleanup EXIT
for _ in {1..40}; do curl --fail --silent "$URL" >/dev/null && break; sleep .25; done
curl --fail --silent "$URL" >/dev/null
"${CLI[@]}" open "$URL"
"${CLI[@]}" snapshot
"${CLI[@]}" eval "el => { localStorage.clear(); sessionStorage.clear() }" e3
"${CLI[@]}" reload
"${CLI[@]}" snapshot
"${CLI[@]}" click '#game'
focus="$("${CLI[@]}" eval "document.activeElement?.id")"
grep --fixed-strings --quiet 'game' <<<"$focus"
"${CLI[@]}" press Tab
focus="$("${CLI[@]}" eval "document.activeElement?.id")"
grep --fixed-strings --quiet 'game' <<<"$focus"
"${CLI[@]}" press n
"${CLI[@]}" snapshot
"${CLI[@]}" screenshot
blank_cursor_frame="$(canvas_hash)"
sleep .6
blank_cursor_next_frame="$(canvas_hash)"
test "$blank_cursor_frame" != "$blank_cursor_next_frame"
"${CLI[@]}" press Enter
route="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.route")"
grep --fixed-strings --quiet 'createCourier' <<<"$route"
for key in u n n a m e d Space c o u r i e r; do "${CLI[@]}" press "$key"; done
"${CLI[@]}" press Enter
route="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.route")"
grep --fixed-strings --quiet 'createCourier' <<<"$route"
for _ in {1..15}; do "${CLI[@]}" press Backspace; done
"${CLI[@]}" press a
"${CLI[@]}" press r
"${CLI[@]}" press i
"${CLI[@]}" press Enter
"${CLI[@]}" press v
visual="$("${CLI[@]}" eval "localStorage.getItem('jomon-visual-mode')")"
grep --fixed-strings --quiet 'runes' <<<"$visual"
route="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.route")"
grep --fixed-strings --quiet 'approach' <<<"$route"
"${CLI[@]}" press v
visual="$("${CLI[@]}" eval "localStorage.getItem('jomon-visual-mode')")"
grep --fixed-strings --quiet 'ascii' <<<"$visual"
"${CLI[@]}" press Space
"${CLI[@]}" snapshot
route="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.route")"
grep --fixed-strings --quiet 'hub' <<<"$route"
ascii_frame="$(canvas_hash)"
"${CLI[@]}" press v
visual="$("${CLI[@]}" eval "localStorage.getItem('jomon-visual-mode')")"
grep --fixed-strings --quiet 'runes' <<<"$visual"
rune_frame="$(canvas_hash)"
test "$ascii_frame" != "$rune_frame"
"${CLI[@]}" press v
visual="$("${CLI[@]}" eval "localStorage.getItem('jomon-visual-mode')")"
grep --fixed-strings --quiet 'ascii' <<<"$visual"
"${CLI[@]}" press =
zoom="$("${CLI[@]}" eval "localStorage.getItem('jomon-board-zoom')")"
grep --fixed-strings --quiet '1.25' <<<"$zoom"
zoom_frame="$(canvas_hash)"
test "$ascii_frame" != "$zoom_frame"
"${CLI[@]}" press -
zoom="$("${CLI[@]}" eval "localStorage.getItem('jomon-board-zoom')")"
grep --fixed-strings --quiet '1' <<<"$zoom"
"${CLI[@]}" press Shift+ArrowUp >/dev/null
for _ in {1..7}; do "${CLI[@]}" press ArrowUp >/dev/null; done
for _ in {1..15}; do "${CLI[@]}" press ArrowLeft >/dev/null; done
"${CLI[@]}" press c
"${CLI[@]}" press 1
notice="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.notice")"
grep --fixed-strings --quiet 'Need' <<<"$notice"
"${CLI[@]}" press 9
notice="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.notice")"
grep --fixed-strings --quiet 'Choose a listed option' <<<"$notice"
"${CLI[@]}" press c
for _ in {1..15}; do "${CLI[@]}" press ArrowRight >/dev/null; done
for _ in {1..7}; do "${CLI[@]}" press ArrowUp >/dev/null; done
"${CLI[@]}" press c
"${CLI[@]}" press e
"${CLI[@]}" snapshot
"${CLI[@]}" screenshot
route="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.route")"
grep --fixed-strings --quiet 'level' <<<"$route"
"${CLI[@]}" press f
autoplay="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.autoplay")"
grep --fixed-strings --quiet 'visible' <<<"$autoplay"
"${CLI[@]}" press v
visual="$("${CLI[@]}" eval "localStorage.getItem('jomon-visual-mode')")"
grep --fixed-strings --quiet 'runes' <<<"$visual"
autoplay="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.autoplay")"
grep --fixed-strings --quiet 'visible' <<<"$autoplay"
"${CLI[@]}" press f
autoplay="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.autoplay")"
grep --fixed-strings --quiet 'omniscient' <<<"$autoplay"
"${CLI[@]}" press f
autoplay="$("${CLI[@]}" eval "document.querySelector('#game')?.dataset.autoplay")"
grep --fixed-strings --quiet 'off' <<<"$autoplay"
console="$("${CLI[@]}" console error)"
if grep --invert-match --fixed-strings '/favicon.ico' <<<"$console" | grep --fixed-strings --quiet '[ERROR]'; then exit 1; fi
