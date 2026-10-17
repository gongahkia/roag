#!/usr/bin/env bash
set -euo pipefail
repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
asset_tmp="$(mktemp "$repo_dir/.data.zip.XXXXXX")"
trap 'rm -f -- "$asset_tmp"' EXIT
curl --fail --location --retry 3 \
  https://thelettervsixtim.es/makeandplay/data.zip --output "$asset_tmp"
# Check the downloaded archive before replacing an existing asset pack.
python3 - "$asset_tmp" <<'PY'
import sys
from zipfile import ZipFile
with ZipFile(sys.argv[1]) as archive:
    if archive.testzip() is not None:
        raise SystemExit("The downloaded asset archive failed validation.")
PY
mv -- "$asset_tmp" "$repo_dir/data.zip"
echo "Downloaded the official Make and Play assets to data.zip."
