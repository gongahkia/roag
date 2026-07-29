# LegalActionMaskV1

`kenjaku-legal-action-mask-v1` serializes legal actions as a strict 276-boolean vector. It uses the existing PPO coordinates: eight 34-tile action families in fixed order (discard, ron, chi, pon, minkan, ankan, kakan, Kita), then pass, tsumo, riichi, and kyushu.

The payload contains `ruleset` and `mask`. It rejects empty masks and coordinates invalid for that ruleset, including 4-player Kita, Sanma chi, and excluded Sanma manzu tiles. `from_actions` accepts `ActionV1` values, `actions` restores canonical actions, and the existing PPO helpers now use the same coordinates.
