#!/bin/sh
# Compatibility launcher: the colony now starts the unified Ithica campaign.
set -eu
exec "$(dirname "$0")/ithica.sh" "$@"
