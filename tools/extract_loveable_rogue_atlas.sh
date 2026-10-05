#!/usr/bin/env sh
# Extract the committed 256x256 native-pixel source panel from the local
# Loveable Rogue collage. This is intentionally mechanical: no pixels are
# painted, resampled, or downloaded.
set -eu
source_image=${1:-assets/art_packs/loveable_rogue.png}
output_image=${2:-assets/visual/loveable_rogue_atlas.png}
[ -f "$source_image" ] || { printf 'Missing Loveable Rogue source: %s\n' "$source_image" >&2; exit 2; }
command -v ffmpeg >/dev/null 2>&1 || { printf 'ffmpeg is required to extract the local Loveable Rogue atlas.\n' >&2; exit 2; }
mkdir -p "$(dirname "$output_image")"
ffmpeg -y -loglevel error -i "$source_image" -vf 'crop=256:256:0:0' "$output_image"
printf 'Extracted %s from %s (crop 0,0 256x256)\n' "$output_image" "$source_image"
