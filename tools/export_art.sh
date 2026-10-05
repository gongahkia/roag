#!/usr/bin/env sh
# The source editor is a user-installed licensed Aseprite binary, so this one
# tooling command runs on the host rather than in the gameplay Docker image.
set -eu
exec luajit tools/export_art.lua "$@"
