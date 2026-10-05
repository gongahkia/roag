#!/usr/bin/env sh
# Build a validated LÖVE archive from the checkout. The temporary archive is
# only moved into place after zip and unzip validation both succeed.
set -eu

output=${1:-roag.love}
workspace=$(pwd)
temporary=$(mktemp -d "${TMPDIR:-/tmp}/roag-package.XXXXXX")
archive="$temporary/roag.love"

cleanup() {
  [ ! -d "$temporary" ] || rm -rf "$temporary"
}
trap cleanup EXIT HUP INT TERM

cd "$workspace"
luajit tools/validate_loveable_rogue.lua
zip -q -r "$archive" . \
  -x '.git/*' \
  -x '.docker/*' \
  -x '.roag-debug/*' \
  -x 'assets/art_packs/loveable_rogue.png' \
  -x 'assets/fonts/*' \
  -x 'art/*' \
  -x 'tools/*' \
  -x 'studio/*' \
  -x 'level_editor/*' \
  -x '__pycache__/*' \
  -x 'roag.love'
unzip -tqq "$archive"
unzip -Z1 "$archive" | grep -qx 'assets/visual/loveable_rogue_atlas.png'
unzip -Z1 "$archive" | grep -qx 'content/presentation/loveable_rogue_atlas.json'
if unzip -Z1 "$archive" | grep -Eq '(^assets/art_packs/|\.aseprite$|^assets/fonts/)'; then
  printf 'Package contains retired visual authoring/source assets.\n' >&2
  exit 1
fi
mv "$archive" "$output"
printf 'Built and validated %s\n' "$output"
