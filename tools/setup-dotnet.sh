#!/bin/sh
# Optional per-user SDK installation; no administrator access or PATH edits.
set -eu
setup_dir=$(mktemp -d "${TMPDIR:-/tmp}/vagabonds-dotnet.XXXXXX")
trap 'rm -rf "$setup_dir"' EXIT HUP INT TERM
curl --fail --location --silent --show-error https://dot.net/v1/dotnet-install.sh -o "$setup_dir/install.sh"
bash "$setup_dir/install.sh" --version 10.0.402 --install-dir "$HOME/.local/share/vagabonds/dotnet" --no-path
echo 'SDK ready. Run ./roag.sh from the repository.'
