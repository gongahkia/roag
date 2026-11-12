#!/bin/sh
set -eu
cd "$(dirname "$0")"
exec "${LOVE_BIN:-love}" . "$@"
