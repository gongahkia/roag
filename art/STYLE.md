# ROAG Pixel-Art Style Contract

This is the production contract for editable ROAG art. It applies to
`.aseprite` sources, deterministic exports, and runtime sprites; it does not
replace the dynamic readability effects rendered by PLAY-01.

## View and scale

- Camera: top-down tactical view. A slight front/top bias is acceptable only
  when it improves a tiny silhouette; never draw side-scroller or isometric
  characters.
- Native character canvas: **24 × 24 px**. ROAG renders an actor into a
  16–24 logical-pixel cell, so 24 px preserves a readable head, torso, and
  equipment silhouette without changing grid geometry.
- Scaling: integer nearest-neighbour only. No bilinear filtering, fractional
  source transforms, or antialiased brush edges.
- Background: transparent. Body pixels use alpha 0 or 255; translucent pixels
  are reserved for deliberate effect assets, never the Gunner body.

## Palette and light

- Use `art/palettes/roag-base.json`; do not introduce near-duplicate colours
  for noise.
- Light direction is upper-left. Keep one shadow and one highlight step where
  possible; readability beats material detail.
- Player accents are cool steel-blue plus restrained amber energy. Hostile and
  player allegiance outlines remain runtime presentation, not baked sprite
  colours.

## Silhouette and outline

- Character readability order: silhouette, pose, equipment cue, then detail.
- A local charcoal contour is allowed when it helps separate body parts. Do
  not bake the bright PLAY-01 white player outline or hostile red outline.
- Gunner: compact head/torso, narrow legs, a close-held ranged-device cue, and
  one amber energy accent. Leave the forward cell visually open for the
  renderer's weapon-direction line and facing marker.

## Pivot and animation

- Logical pivot is lower centre. For a 24 × 24 character it is normally
  `(12, 21)` in native pixels; it aligns to the actor's tile without altering
  simulation position.
- Keep feet/body centre stable across frames. Movement, recoil, squash, hit
  flash, and screen shake are PLAY-01 presentation transforms, not baked
  translation in source frames.
- Required tags: `idle`, `move`, `attack`, `hurt`. Tags use source-authored
  frame durations. Animation never delays turn resolution.
- Prefer 2–4 idle frames, 4 move frames, 3–5 attack frames, and 2 hurt frames.

## Avoid

- AI-generated sprite sheets as runtime art.
- Text, logos, watermarks, accidental mattes, semitransparent anti-aliasing,
  excessive single-pixel noise, or copied character silhouettes.
- Four directional animation sets until a future art pass proves they are
  needed. Existing facing marker, weapon orientation, and attack footprints
  remain authoritative readability cues.
