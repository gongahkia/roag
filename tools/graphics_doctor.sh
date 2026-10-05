#!/usr/bin/env sh
# Read-only graphics diagnostics for the Docker/LOVE path.
set -eu

printf '%s\n' 'ROAG graphics doctor'
printf 'LOVE: '; love --version
printf 'DISPLAY: %s\n' "${DISPLAY:-<unset>}"
printf 'XDG_RUNTIME_DIR: %s\n' "${XDG_RUNTIME_DIR:-<unset>}"
printf 'WAYLAND_DISPLAY: %s\n' "${WAYLAND_DISPLAY:-<unset>}"
printf 'LD_LIBRARY_PATH: %s\n' "${LD_LIBRARY_PATH:-<unset>}"
printf 'LIBGL_ALWAYS_SOFTWARE: %s\n' "${LIBGL_ALWAYS_SOFTWARE:-<unset>}"
printf 'GALLIUM_DRIVER: %s\n' "${GALLIUM_DRIVER:-<unset>}"
printf 'ALSOFT_DRIVERS: %s\n' "${ALSOFT_DRIVERS:-<unset>}"

if [ -n "${DISPLAY:-}" ]; then
  display_number=${DISPLAY#*:}
  display_number=${display_number%%.*}
  socket="/tmp/.X11-unix/X${display_number}"
  if [ -S "$socket" ]; then
    printf 'X11 socket: available (%s)\n' "$socket"
  else
    printf 'X11 socket: MISSING (%s)\n' "$socket" >&2
  fi
else
  printf '%s\n' 'X11 socket: skipped (DISPLAY is unset)' >&2
fi

if [ -c /dev/dxg ]; then
  printf '%s\n' 'WSLg GPU bridge: available (/dev/dxg)'
else
  printf '%s\n' 'WSLg GPU bridge: not mounted (software Mesa is still possible)'
fi

if command -v glxinfo >/dev/null 2>&1; then
  printf '%s\n' 'OpenGL renderer:'
  glxinfo -B
else
  printf '%s\n' 'glxinfo is unavailable in this image.' >&2
  exit 2
fi
