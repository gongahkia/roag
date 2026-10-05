#!/usr/bin/env sh
# Thin Docker service entrypoint. The Lua cockpit owns all simulation work.
set -eu
exec luajit tools/roag_debug.lua "$@"
