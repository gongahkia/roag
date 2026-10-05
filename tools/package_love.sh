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
zip -q -r "$archive" . \
  -x '.git/*' \
  -x '.docker/*' \
  -x '.roag-debug/*' \
  -x '__pycache__/*' \
  -x 'roag.love'
unzip -tqq "$archive"
mv "$archive" "$output"
printf 'Built and validated %s\n' "$output"
