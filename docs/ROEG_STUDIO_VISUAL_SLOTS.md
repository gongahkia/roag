# ROEG Studio visual slots

All slots below use **static** PNGs. They change presentation during existing
playback beats; they do not add animation frames, combat actions, map rules, or
save fields. Studio's pixel editor draws each image: create a sprite with **C**
or import a PNG, select it, press **E**, paint, and press **S**. Back in asset
metadata, press **4** and type the exact binding. **B** remains a shortcut for
the ordinary player binding only. Each binding can be used by one asset in a
project. Press **S** again after changing metadata.

The asset's stable ID can be your own `studio:...` ID; the **Binding** field is
what selects its game role. The Studio **F10** help screen lists the patterns.
All artwork remains editable in the LÖVE save directory shown by Studio.

## Actors and rewards

| Binding | Display |
| --- | --- |
| `core:actor/adventurer` | Ordinary player sprite |
| `core:actor/adventurer/attack` | Player during an `AttackPerformed` beat |
| `core:actor/adventurer/hurt` | Player during positive `DamageTaken` |
| `core:enemy/mossbound_guard` | Ordinary enemy sprite; the enemy ID itself also works |
| `core:enemy/mossbound_guard/attack` | That enemy during windup/attack beats |
| `core:enemy/mossbound_guard/hurt` | That enemy during positive damage |
| `core:drop/currency` | Currency on the floor |
| `core:drop/currency/detail` | Large currency pickup card |
| `core:chest/standard` or `core:chest/free_elite` | Unclaimed chest |
| `core:boon/storm_conductor/icon` | Small icon in chest/owned-boon UI |
| `core:boon/storm_conductor/detail` | Large chest offer, pickup card and owned-boon detail |

Replace `mossbound_guard` with any built-in enemy slug, or `storm_conductor`
with any built-in boon slug. The existing fallback art remains when a slot is
empty. On a shared beat, hurt takes priority over attack. These are still
images, not animation timelines. The game currently grants boons from chests;
it has no individual boon drops on the floor. Currency is its floor pickup.
Chest offers display the detailed boon image when supplied. After a claim, a
large card appears briefly. Click an owned boon in the right HUD to reopen its
detail; **Esc** or click closes it without advancing world time.

## Biome backdrop and automatic terrain

Use `core:background/overgrown_ruins` for a gameplay backdrop, replacing the
biome slug with `fungal_hollows`, `sunken_aqueduct`, or `ashen_bastion` as
needed. The backdrop fills the 704×608 world viewport beneath terrain. Built-in
walkable tile colors become slightly translucent when a backdrop exists, so it
can show through; walls retain full opacity. Title/pause backgrounds remain
separate E04 theme slots.

For terrain, bind a painted asset to:

`core:terrain/<biome>/<floor|wall|water>/<shape>`

The renderer classifies each tile from its neighboring tiles of the **same**
terrain type. Available shapes include `center`, `edge_n/e/s/w`,
`corner_nw/ne/se/sw`, `inner_corner_nw/ne/se/sw`, `horizontal`, `vertical`,
`end_n/e/s/w`, and `isolated`. `corner_nw` means the north and west neighbors
are different terrain; `inner_corner_nw` means all four cardinal neighbors
match but the northwest diagonal does not. Off-map space counts as different
terrain. A missing oriented shape falls back to its generic `corner`,
`inner_corner`, or `edge` slot, then `center`, then `base`, then built-in art.
Paint symmetric generic sprites if orientation matters.

Example bindings for Overgrown Ruins walls:

```text
core:terrain/overgrown_ruins/wall/base
core:terrain/overgrown_ruins/wall/center
core:terrain/overgrown_ruins/wall/edge_n
core:terrain/overgrown_ruins/wall/corner_nw
core:terrain/overgrown_ruins/wall/inner_corner_nw
```

These bindings work on generated and authored maps without changing collision,
line of sight, or floor snapshots. **W World** still lets you paint floor,
wall, water, and explicit tile/decoration images for individual authored rooms.
An explicit authored image at a cell appears above the automatic terrain art.
Publish image assets first; if you also edited room geometry or explicit
visual cells, publish the world pack separately as described in
[E03](ROEG_STUDIO_E03.md).

## Publish and inspect

From the checkout root, substitute the save directory shown by Studio and
your project ID. The first command is read-only:

```sh
luajit tools/studio_publish.lua --save-dir "$HOME/Library/Application Support/LOVE/roeg-foundation" --project player_sprite
luajit tools/studio_publish.lua --save-dir "$HOME/Library/Application Support/LOVE/roeg-foundation" --project player_sprite --apply
```

If the dry run says `overwrite`, review it and add `--overwrite` to the apply
command. A running game uses **F5** to refresh published visuals. The Studio
**P** screen reports whether the loose image pack is current. The art pack is
derived; back up the editable `studio/projects/<project-id>/` directory
separately.
