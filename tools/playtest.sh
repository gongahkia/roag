#!/usr/bin/env bash
# Launch an actual LÖVE session with save/config/cache paths below one private root.
set -euo pipefail

repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)
mode=${1:-}
root=
if [[ $mode == --resume ]]; then
  [[ $# -eq 2 ]] || { printf 'Usage: %s --resume /absolute/playtest/root\n' "$0" >&2; exit 2; }
  root=$2
elif [[ $mode == --check ]]; then
  if [[ $# -eq 3 && $2 == --root ]]; then root=$3
  elif [[ $# -ne 1 ]]; then printf 'Usage: %s --check [--root /absolute/playtest/root]\n' "$0" >&2; exit 2; fi
elif [[ $mode != --new ]]; then
  printf 'Usage: %s --new | --resume /absolute/playtest/root | --check [--root /absolute/playtest/root]\n' "$0" >&2
  exit 2
fi

love_bin=${COSMONAUTS_LOVE_BIN:-love}
if ! command -v -- "$love_bin" >/dev/null 2>&1; then
  printf 'LÖVE runtime not found: %s\n' "$love_bin" >&2
  exit 1
fi

inside_repo() { [[ $1 == "$repo" || $1 == "$repo/"* ]]; }
new_root() {
  local base=${TMPDIR:-/tmp}
  root=$(mktemp -d -- "$base/cosmonauts-v01.XXXXXXXX")
  root=$(realpath -e -- "$root")
  inside_repo "$root" && { printf 'Refusing playtest root inside repository: %s\n' "$root" >&2; exit 1; }
  chmod 700 -- "$root"
  mkdir -p -- "$root/data" "$root/config" "$root/cache" "$root/logs"
  chmod 700 -- "$root/data" "$root/config" "$root/cache" "$root/logs"
  printf 'format=cosmonauts-playtest-root-v1\nrepository=%s\n' "$repo" > "$root/.cosmonauts-playtest-root"
  chmod 600 -- "$root/.cosmonauts-playtest-root"
}

verify_root() {
  [[ -n $root && $root == /* && -d $root && ! -L $root ]] || { printf 'Invalid playtest root: %s\n' "${root:-<none>}" >&2; exit 1; }
  local canonical
  canonical=$(realpath -e -- "$root")
  [[ $root == "$canonical" ]] || { printf 'Playtest root must be canonical and contain no symlink: %s\n' "$root" >&2; exit 1; }
  root=$canonical
  inside_repo "$root" && { printf 'Refusing playtest root inside repository: %s\n' "$root" >&2; exit 1; }
  [[ $(stat -c '%u' -- "$root") == $(id -u) && $(stat -c '%a' -- "$root") == 700 ]] || { printf 'Playtest root must be owned by this user and mode 700: %s\n' "$root" >&2; exit 1; }
  [[ -f $root/.cosmonauts-playtest-root && ! -L $root/.cosmonauts-playtest-root ]] || { printf 'Missing playtest marker: %s\n' "$root" >&2; exit 1; }
  grep -Fxq 'format=cosmonauts-playtest-root-v1' "$root/.cosmonauts-playtest-root" || { printf 'Invalid playtest marker: %s\n' "$root" >&2; exit 1; }
  grep -Fxq "repository=$repo" "$root/.cosmonauts-playtest-root" || { printf 'Playtest root belongs to a different checkout: %s\n' "$root" >&2; exit 1; }
  local child
  for child in data config cache logs; do
   [[ -d $root/$child && ! -L $root/$child && $(realpath -e -- "$root/$child") == "$root/$child" ]] || { printf 'Invalid playtest child: %s/%s\n' "$root" "$child" >&2; exit 1; }
  done
  if [[ -n ${COSMONAUTS_PLAYTEST_ROOT:-} && ${COSMONAUTS_PLAYTEST_ROOT} != "$root" ]]; then
   printf 'Conflicting COSMONAUTS_PLAYTEST_ROOT: %s\n' "$COSMONAUTS_PLAYTEST_ROOT" >&2
   exit 1
  fi
}

run_probe() {
  local probe_log=$root/logs/probe.log
  XDG_DATA_HOME=$root/data XDG_CONFIG_HOME=$root/config XDG_CACHE_HOME=$root/cache \
  COSMONAUTS_PLAYTEST_ROOT=$root COSMONAUTS_PLAYTEST_PROBE=1 \
  "$love_bin" "$repo" >"$probe_log" 2>&1
  local probe
  probe=$(find "$root/data" -type f -name playtest-probe.txt -print -quit)
  [[ -n $probe && -f $probe ]] || { printf 'Probe did not create an isolated report; see %s\n' "$probe_log" >&2; exit 1; }
  grep -Fxq "root=$root" "$probe" || { printf 'Probe root mismatch; see %s\n' "$probe" >&2; exit 1; }
  local save_dir
  save_dir=$(sed -n 's/^save_directory=//p' "$probe")
  [[ $save_dir == "$root" || $save_dir == "$root/"* ]] || { printf 'Probe save directory escaped playtest root: %s\n' "$save_dir" >&2; exit 1; }
  printf 'PLAYTEST_ROOT=%s\nEFFECTIVE_SAVE_DIRECTORY=%s\nPROBE_REPORT=%s\n' "$root" "$save_dir" "$probe"
}

if [[ $mode == --new || ( $mode == --check && -z $root ) ]]; then new_root; fi
verify_root
if [[ $mode == --check ]]; then run_probe; exit 0; fi

printf 'PLAYTEST_ROOT=%s\nPLAYTEST_RESUME=bash %q --resume %q\n' "$root" "$repo/tools/playtest.sh" "$root"
printf 'LÖVE output will be retained in %s/logs/love.log\n' "$root"
XDG_DATA_HOME=$root/data XDG_CONFIG_HOME=$root/config XDG_CACHE_HOME=$root/cache \
COSMONAUTS_PLAYTEST_ROOT=$root \
"$love_bin" "$repo" >"$root/logs/love.log" 2>&1
