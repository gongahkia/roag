#!/usr/bin/env bash
# run from apps/settlement; every web asset is local, including CanvasKit and fonts.
set -euo pipefail
settlement_base="${1:-/settlement/}"
case "$settlement_base" in
  /*/) ;;
  *) echo 'The base path must start and end with /.' >&2; exit 2;;
esac
settlement_flutter="${FLUTTER_BIN:-flutter}"
"$settlement_flutter" --version --machine | python3 -c '
import json,sys
v=json.load(sys.stdin)
if v["frameworkVersion"] != "3.50.0" or not v["dartSdkVersion"].startswith("3.14."):
 raise SystemExit("This release requires Flutter 3.50.0 / Dart 3.14.x.")
'
"$settlement_flutter" build web --release --base-href "$settlement_base" --no-web-resources-cdn --output build/release-web
rm -rf build/site
mkdir -p build/site/settlement
cp -a build/release-web/. build/site/settlement/
touch build/site/.nojekyll
cat > build/site/index.html <<'HTML'
<!DOCTYPE html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta http-equiv="refresh" content="0;url=settlement/"><title>ROAG Settlement</title><body><a href="settlement/">Play ROAG Settlement</a></body></html>
HTML
printf 'Built release with base path %s in build/site/\n' "$settlement_base"
