# ActionV1

`kenjaku-action-v1` is the strict JSON action boundary. Its version is encoded in `kind`; a future incompatible shape must use a new kind.

Each payload contains `ruleset`, `action`, logical `tile`, `tsumogiri`, and physical `consumed` tiles. `tile` uses canonical non-red tile-type notation; `consumed` preserves red fives as `0m`, `0p`, or `0s`.

The 4-player vocabulary is discard, riichi, chi, pon, minkan, ankan, kakan, pass, tsumo, ron, and kyushu. Sanma replaces chi with kita. `from_dict` rejects missing and unknown fields, and `from_core`/`to_core` provide lossless conversion to the existing `Action` type.
