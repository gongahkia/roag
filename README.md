[![](https://img.shields.io/badge/jomon_1.0.0-passing-green)](https://github.com/gongahkia/jomon/releases/tag/1.0.0)
![](https://github.com/gongahkia/jomon/actions/workflows/ci.yml/badge.svg)
![](https://github.com/gongahkia/jomon/actions/workflows/pages.yml/badge.svg)

# `Jomon`

Browser-based, turn-based courier roguelike. Every seed draws and orders four distinct biomes from an eight-biome pool for a sixteen-floor campaign; cleared floors increase shared threat regardless of biome identity.

## Stack

* Language: [TypeScript](https://www.typescriptlang.org/)
* Runtime: browser [Canvas 2D API](https://developer.mozilla.org/docs/Web/API/CanvasRenderingContext2D) and Web Audio
* Tooling: [Vite](https://vite.dev/)
* Tests: [Vitest](https://vitest.dev/)
* Assets/Fonts: [BigBlue Terminal](https://int10h.org/blog/2015/12/bigblue-terminal-oldschool-fixed-width-font/)

## Screenshots

<p align="center">
  <img src="asset/reference/1.gif" alt="Jomon reference capture 1" width="32%" />
  <img src="asset/reference/2.gif" alt="Jomon reference capture 2" width="32%" />
  <img src="asset/reference/3.gif" alt="Jomon reference capture 3" width="32%" />
</p>

## Usage

The below commands run `Jomon` locally.

```sh
git clone https://github.com/gongahkia/jomon && cd jomon
npm ci
npm run dev
```

Audit deterministic level generation for all ten biomes:

```sh
npm run debug:levels
```

Build a production bundle:

```sh
npm run build
npm run preview
```

Generate deterministic spatial metrics; JSON reports default to ignored `generation-reports/`:

```sh
npm run generation:report
```

## Controls

| Key | Action |
| --- | --- |
| `I` `O` `P` / `K` `;` / `,` `.` `/` or numpad | Move in eight directions |
| `L` | Wait / rest |
| `Shift` + move | Run |
| `Alt` + direction | Quick cast |
| `G` `U` `D` `E` `T` | Get, use, drop, equip, throw |
| `A` `S` `B` `R` | Skills, charm, bomb, rope |
| `C` `Q` `X` | Operate, descend, swap |
| `Y` / `W` | Choose ritual tool / spend Time Knot rewind |
| `H` `J` `F1` | Help, journal, settings |
| `V` | Cycle ASCII and runes |
| `F` / `Shift` + `F` | Toggle autoplay / change autoplay policy |
| `+` `-` `0` or mouse wheel | Change / reset board zoom |
| `Esc` or backtick | Pause or cancel |

Menu controls: `N` creates a courier, `L`/`Enter` resumes one, arrows select, and `D` retires one. Key bindings can be changed in settings.

## Buildcraft

Every floor places one Waycache and three Boon sites. Waycaches bind two ritual tools: Stone Wedge, Reedwing, Cord Anchor, or Ashway Rites. Tools recharge by turn cooldown and can be explicitly overdriven for a stronger final effect before retiring. Boon sites offer deterministic three-way, run-only drafts; Boons stack without a cap. Powerful Boons can suppress conflicting trigger families instead of adding generic penalties. Cliffs add reusable vertical rope links; Burial adds oath, body, and cursed-object risk/reward engines. Local discovery is default; Scout Eye reveals all remaining milestones. `Shift` + `F` includes an `EXPLORE` autoplay policy that pursues discovered milestones before exit.

## Generated sprite atlas

Generated PNG sheets are intentionally absent. The manifest, atlas renderer, inspector, normalizer, and validator remain so sprite rendering can be restored without rebuilding gameplay mappings. Until then, `V` offers ASCII and runes only; the atlas inspector shows metadata instead of pixels.

To restore sprites, generate every runtime sheet named by `src/assets/generated-sprites/sprite-manifest.json` as a 16-pixel cell grid with the manifest's columns and rows. Use `scripts/normalize-sprite-sheet.py INPUT OUTPUT --columns N --rows N` to normalize source art. Restore the matching static Vite URLs in `src/sprites.ts`, restore `sprites` in `src/visual-mode.ts`, then run:

```sh
node scripts/validate-generated-sprites.mjs
npm test
npm run build
npm run test:e2e
```

## Reference

`Jomon` is thoroughly inspired by [Bob Nystrom](https://x.com/munificentbob?lang=en) *(a.k.a [@munifcent](https://github.com/munificent))*'s [Hauberk](https://github.com/munificent/hauberk).

<div align="center">
    <img src="./asset/logo/hauberk.png" width="75%">
</div>
