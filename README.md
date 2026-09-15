# Codex Beacon

Codex Beacon is a small Wi-Fi notifier for the standard 1.9-inch rectangular
LILYGO T-Display-S3. A Codex completion sends one HTTP event and the display
shows a large `DONE` until either physical button is pressed.

## Hardware

- LILYGO T-Display-S3 (no extra hardware)
- USB-C cable for flashing
- A 2.4 GHz Wi-Fi network

The firmware uses LILYGO's official board definition (`lilygo-t-display-s3`)
and the official parallel ST7789 setup: LCD CS/DC/RST/WR/RD are GPIO 6/7/5/8/9,
data D0-D7 are GPIO 39/40/41/42/45/46/47/48, backlight is GPIO 38, and LCD
power is GPIO 15. The two acknowledgement buttons are BOOT/GPIO 0 and GPIO 14.
Source: [official T-Display-S3 repository](https://github.com/Xinyuan-LilyGO/T-Display-S3).

## Architecture

```text
┌──────────────┐
│  Codex CLI   │
└──────┬───────┘
       │ completion
       ▼
┌──────────────────┐
│ codex-beacon     │
│ Go utility       │
└────────┬─────────┘
         │ HTTP / Wi-Fi
         ▼
┌──────────────────┐
│ LILYGO           │
│ T-Display-S3     │
│                  │
│      ✓ DONE      │
└──────────────────┘
```

The ESP32 serves `GET /health` and `POST /event` on port 80. Supported event
types are `started`, `done`, and `idle`.

## Wi-Fi setup

Copy the example and edit it locally:

```bash
cp firmware/include/secrets.example.h firmware/include/secrets.h
$EDITOR firmware/include/secrets.h
```

Set `CODEX_BEACON_WIFI_SSID` to the 2.4 GHz network name and
`CODEX_BEACON_WIFI_PASSWORD` to its password. `secrets.h` is ignored by Git;
never commit it. The example fallback lets the project compile before this
file exists, but the real values are required for a working device.

## Firmware build and flash

Install PlatformIO (the `pio` command), then:

```bash
cd firmware
pio run
pio run -t upload --upload-port /dev/ttyACM0
pio device monitor -b 115200
```

The serial log prints the assigned IP. If the port is not available, hold
BOOT while pressing RESET, release BOOT, and retry the upload. The board shows
`OFFLINE` while it retries Wi-Fi; after connecting it shows `READY`.

## Host utility

Build and test from `host/`:

```bash
cd host
go test ./...
go build -o ~/.local/bin/codex-beacon .
export CODEX_BEACON_URL=http://DEVICE_IP
codex-beacon test
```

The commands are:

```text
codex-beacon started [project]
codex-beacon done [project]
codex-beacon idle
codex-beacon test
codex-beacon run -- codex [args...]
```

Without a project argument, `started` and `done` use the current directory's
name. `test` sends `started` followed by `done`. Requests have a sub-second
timeout; an offline device prints a warning/error quickly and never blocks the
Codex process.

Manual device test:

```bash
curl http://DEVICE_IP/health
curl -X POST http://DEVICE_IP/event -H 'Content-Type: application/json' \
  -d '{"type":"started","project":"yuho"}'
curl -X POST http://DEVICE_IP/event -H 'Content-Type: application/json' \
  -d '{"type":"done","project":"yuho"}'
```

Press GPIO 0 (BOOT) or GPIO 14 to return from `DONE` to `READY`. Malformed
JSON and unknown paths receive a 4xx response without changing the state.

## Codex completion integration

Codex CLI 0.154.0 reports the `hooks` feature as stable. The official
completion lifecycle event is the `Stop` hook, documented in the [Codex Hooks
documentation](https://learn.chatgpt.com/docs/hooks). A user-level hook can
read the working directory from its JSON stdin and invoke the host utility:

```json
{
  "description": "Codex Beacon completion notification",
  "hooks": {
    "Stop": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "/home/gongahkia/.local/bin/codex-beacon hook",
            "async": true,
            "timeout": 3
          }
        ]
      }
    ]
  }
}
```

Save that as `~/.codex/hooks.json` after adjusting the binary path if needed.
Then run `/hooks` inside Codex to review/trust the command. The hook derives
the project from `cwd`, sends `done`, runs asynchronously, and treats an
unreachable LILYGO as a warning. This repository does not modify
`~/.codex/config.toml`.

To disable the integration, remove this `Stop` entry (or rename the hooks
file) and review the change with `/hooks`. The wrapper remains available when
an explicit completion hook is not desired (disable the `Stop` hook first to
avoid duplicate notifications):

```bash
codex-beacon run -- codex
```

It sends `started`, runs Codex, then sends `done` while preserving Codex's exit
status. The current hook is turn-level (`Stop`), not a multi-session task
tracker; one notification is sent for each completed turn.

## Troubleshooting

- No IP on serial: confirm the 2.4 GHz SSID/password and wait for the retry;
  check the router's DHCP leases.
- Upload permission denied on Linux: add your user to `dialout`, log in again,
  and retry (`sudo usermod -aG dialout "$USER"`).
- `CODEX_BEACON_URL is not set`: export the device's current URL in the shell
  where the CLI or hook runs.
- Hook not firing: run `/hooks`, trust the command, confirm the absolute binary
  path, and ensure `features.hooks` has not been disabled.
