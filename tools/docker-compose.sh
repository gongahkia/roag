#!/usr/bin/env sh
# Run Compose with a ROAG-local client configuration. This avoids a WSL client
# inheriting a Windows-only credential helper while leaving the user's global
# Docker credentials untouched. Public project images need no registry login.
set -eu

if [ -z "${DOCKER_CONFIG:-}" ]; then
  roag_cache_root=${XDG_CACHE_HOME:-"$HOME/.cache"}
  export DOCKER_CONFIG="$roag_cache_root/roag/docker"
  umask 077
  mkdir -p "$DOCKER_CONFIG"
  if [ ! -f "$DOCKER_CONFIG/config.json" ]; then
    printf '%s\n' '{"auths":{}}' > "$DOCKER_CONFIG/config.json"
  fi
fi

exec docker compose "$@"
