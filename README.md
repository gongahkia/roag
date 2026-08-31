[![](https://img.shields.io/badge/jomon_1.0.0-passing-green)](https://github.com/gongahkia/jomon/releases/tag/1.0.0)
![](https://github.com/gongahkia/jomon/actions/workflows/pages.yml/badge.svg)

# `Jomon`

[ASCII](https://en.wikipedia.org/wiki/ASCII)-art based [procedurally-generated](https://en.wikipedia.org/wiki/Procedural_generation) space-exploration [roguelike](https://en.wikipedia.org/wiki/Roguelike) that lives in the browser. 

See [LORE.md](LORE.md) for the canonical world and content reference.

## Stack

* Scripting: [TypeScript](https://www.typescriptlang.org/), [Canvas 2D API](https://developer.mozilla.org/docs/Web/API/CanvasRenderingContext2D), [Web Audio API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Audio_API)
* Test: [Vite](https://vite.dev/), [Vitest](https://vitest.dev/)
* Font: [BigBlue Terminal](https://int10h.org/blog/2015/12/bigblue-terminal-oldschool-fixed-width-font/)

## Screenshots

<p align="center">
  <img src="asset/reference/1.gif" alt="Jomon reference capture 1" width="32%" />
  <img src="asset/reference/2.gif" alt="Jomon reference capture 2" width="32%" />
  <img src="asset/reference/3.gif" alt="Jomon reference capture 3" width="32%" />
</p>

## Usage

> [!IMPORTANT]  
> The easiest way to play `Jomon` is via its live deployment ***[here](https://gabrielongzm.com/jomon/)***.

Alternatively, the below commands are for running `Jomon` locally.

```console
$ git clone https://github.com/gongahkia/jomon && cd jomo
$ npm ci && npm run dev
$ npm run build
$ npm run preview
```

## Verification

```console
$ npm run test:autoplay:tasks
$ npm run test:route-reckoning:soak
$ npm run test:e2e:install
$ npm run test:e2e
```

`test:autoplay:tasks` runs the deterministic task queue against the headless engine. It verifies tactical play plus Jomon route discovery, connector residency, cargo delivery and recovery, landing conditions, and sector simulation. `test:route-reckoning:soak` verifies deterministic long-session Route Reckoning state. `test:e2e` exercises the browser entry and input path with Playwright.

## Controls

| Key | Action |
| --- | --- |
| `I` `O` `P` / `K` `;` / `,` `.` `/` or numpad | Move in eight directions |
| `L` | Wait / rest |
| `Shift` + move | Run |
| `Alt` + direction | Quick cast |
| `G` `U` `D` `E` `T` | Get, use, drop, equip, throw |
| `A` `S` `B` `R` | Skills, module, breach charge, line spool |
| `C` `Q` `X` | Operate, descend, swap |
| `Y` / `W` | Choose traversal tool / spend Time Knot rewind |
| `H` `J` `F1` | Help, journal, settings |
| `M` | Open the General Manifest in the Jomon hub (pauses Route Reckoning) |
| `V` | Cycle ASCII and runes |
| `F` / `Shift` + `F` | Toggle autoplay / change autoplay policy |
| `+` `-` `0` or mouse wheel | Change / reset board zoom |
| `Esc` or backtick | Pause or cancel |

## Reference

`Jomon` is thoroughly inspired by [Bob Nystrom](https://x.com/munificentbob?lang=en) *(a.k.a [@munifcent](https://github.com/munificent))*'s [Hauberk](https://github.com/munificent/hauberk).

<div align="center">
    <img src="./asset/logo/hauberk.png" width="75%">
</div>
