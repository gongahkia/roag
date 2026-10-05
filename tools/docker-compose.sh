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

# WSLg presents GPU acceleration through /dev/dxg plus host Mesa's D3D12
# bridge.  The base Compose file remains portable for ordinary Linux/X11; use
# this narrow overlay only when every required WSLg path exists.  Set
# ROAG_DISABLE_WSLG=1 to diagnose the generic X11 path explicitly.
if [ -z "${ROAG_DISABLE_WSLG:-}" ] \
  && [ -d /mnt/wslg ] \
  && [ -d /usr/lib/wsl/lib ] \
  && [ -c /dev/dxg ]; then
  exec docker compose -f compose.yaml -f compose.wslg.yaml "$@"
fi

exec docker compose "$@"
