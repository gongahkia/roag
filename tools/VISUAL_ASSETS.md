# ROAG Visual Assets

ROAG currently uses **shape-first presentation**. Characters, enemies,
projectiles, terrain, hazards, effects, and world objects must remain readable
with no texture files installed. The renderer maps class IDs, mechanical enemy
roles, terrain, and object interaction roles to small procedural glyphs, then
applies the existing dynamic outlines, facing marker, threat footprints,
recoil, hit flash, and damage feedback.

Optional future PNG assets are a presentation enhancement, never a gameplay
dependency. If `assets/presentation/manifest.json` is absent—as it is in the
current checkout—the renderer uses canonical shapes.

## Optional runtime manifest

An imported pack may declare a compact ROAG-owned schema:

```json
{
  "schema_version": 1,
  "assets": [
    {
      "id": "character.example",
      "image": "assets/presentation/example.png",
      "frame_width": 24,
      "frame_height": 24,
      "pivot": [12, 21],
      "animations": {
        "idle": { "frames": [0, 1], "durations_ms": [300, 300] }
      }
    }
  ],
  "character_bindings": { "expedition.gunner": "character.example" },
  "role_bindings": {
    "terrain.floor": "terrain.example_floor",
    "object.door": "object.example_door",
    "effect.fire": "effect.example_fire"
  }
}
```

This metadata is deliberately source-tool agnostic. Image files use nearest
filtering; the manifest is loaded once, and an invalid or unavailable optional
asset falls back to a shape without affecting simulation, saves, or packaging.
Role IDs are presentation-only; current production content declares none.

See [ASSET_PACKS.md](ASSET_PACKS.md) before importing any third-party work.
