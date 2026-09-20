#!/usr/bin/env bash
set -euo pipefail

repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)
tmp=$(mktemp -d -- /tmp/cosmonauts-playtest-test.XXXXXXXX)
trap 'rm -rf -- "$tmp"' EXIT
normal="$tmp/normal-save-sentinel"
printf 'do not touch\n' > "$normal"

parent="$tmp/space parent"
mkdir -p -- "$parent"
output=$(TMPDIR="$parent" bash "$repo/tools/playtest.sh" --check)
root=$(sed -n 's/^PLAYTEST_ROOT=//p' <<<"$output")
save=$(sed -n 's/^EFFECTIVE_SAVE_DIRECTORY=//p' <<<"$output")
[[ -n $root && -n $save && $save == "$root"/* && -f $normal && $(<"$normal") == 'do not touch' ]]
bash "$repo/tools/playtest.sh" --check --root "$root" >/dev/null

invalid="$tmp/invalid"
mkdir -m 700 -- "$invalid"
if bash "$repo/tools/playtest.sh" --check --root "$invalid" >/dev/null 2>&1; then exit 1; fi
ln -s -- "$root" "$tmp/root-link"
if bash "$repo/tools/playtest.sh" --check --root "$tmp/root-link" >/dev/null 2>&1; then exit 1; fi
if COSMONAUTS_PLAYTEST_ROOT="$tmp/conflict" bash "$repo/tools/playtest.sh" --check --root "$root" >/dev/null 2>&1; then exit 1; fi
if COSMONAUTS_LOVE_BIN=/definitely/missing/love bash "$repo/tools/playtest.sh" --check >/dev/null 2>&1; then exit 1; fi
printf 'PASS playtest launcher: isolated probe, resumable marked root, spaces, sentinels, invalid roots, conflicts, and missing runtime.\n'
