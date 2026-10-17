# Movement research and implementation decision

Research checked against available primary sources on 17 October 2026. This is a recommendation for this project, not a universal ranking of engines.

## Direction

The clarified brief calls for a fast precision platformer. The user explicitly selected **Mario Maker: tighter ground control and precise jump arcs** as the leading feel. The implementation follows that choice: sharp starts/stops/reversals, controllable tap/hold jumps, reduced air drift, and a consistent apex. Super Meat Boy informs short, demanding attempts and fast restarts rather than the default amount of movement inertia. Team Meat’s own description emphasizes wall leaps and reflex platforming. [Developer-published Steam page](https://store.steampowered.com/app/40800/Super_Meat_Boy/).

Mario Maker is a family of movement styles, rather than one physics specification: Nintendo presents multiple game styles. Our baseline is Mario-style running and jumping, with modern-style wall kicks available as an advanced move. This is not an assertion that every Mario Maker style supports wall jumps. [Nintendo’s official game-style overview](https://supermariomaker.nintendo.com/es/make/), [Nintendo’s original Mario Maker manual](https://csassets.nintendo.com/noaext/image/private/t_KA_PDF/manual-3DS-Super_Mario_Maker-en?_a=DATC1RAAZAA0).

Everybody Edits remains a further design reference from the brief; I have not obtained authoritative numerical physics constants for it and am not claiming to reproduce them. The prototype has normal downward gravity and no air dash, double jump, or gravity reversal. This is an original controller with tunable values, not an extraction of another game’s code or physics constants.

## Engine options

| Option | Fit for this project | Cost of choosing it |
| --- | --- | --- |
| Godot 4 | Dedicated 2D workflow, editor, input mapping, character collision, UI, and resources that can be tuned in the Inspector. My recommendation for iterating on movement and many original courses. | Migration work; our controller still needs careful design and playtesting. The original solo courses use rectangle data; the shared workshop now provides a separate tile editor. |
| Existing C++ / SDL3 fork | Already runs locally, gives direct control, and could implement all the requested mechanics. | The old campaign’s structures and timing are a poor starting point for a substantially different game; authoring and tooling would require more custom work. This is a project-specific assessment, not an SDL performance limitation. |
| Unity | A viable alternative with established 2D physics and tooling, particularly for an experienced Unity team. | For this small prototype, I found no requirement that justifies switching to its workflow instead of Godot. This is a judgment about scope, not a claim that Unity produces worse movement. |

Godot’s dedicated 2D workflow and MIT licensing are documented on its [features page](https://godotengine.org/features/) and [license page](https://godotengine.org/license/). Unity’s [Rigidbody2D documentation](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rigidbody2D.html) describes its available simulation body types. The cloned VVVVVV desktop source itself establishes that the existing build is C++/SDL3; the mobile ActionScript sources are not involved in our native build.

Godot is the engine choice for the new `speedrunner/` project. Godot 4.7.2 is pinned for the Linux bootstrap, rather than downloading an unspecified future release. Its binary comes directly from the [official release](https://github.com/godotengine/godot/releases/tag/4.7.2-stable).

## Why a controlled character body

Godot’s `CharacterBody2D` detects and responds to collisions while leaving gravity, friction and movement policy to our code. That matches a platformer where jump arcs and wall kicks need to be predictable and adjustable. The controller moves through `move_and_slide()` on physics ticks (driven manually by the room for network prediction), not by teleporting its transform during normal play. [Official character-body documentation](https://docs.godotengine.org/en/stable/tutorials/physics/using_character_body_2d.html).

Maddy Thorson’s account of Celeste and TowerFall describes a different, custom AABB movement system with separate horizontal and vertical movement and subpixel remainders. The useful lesson is to design movement rules explicitly. We are not claiming Godot’s floating-point collision system is identical to that implementation. [Celeste & TowerFall Physics](https://www.mattmakesgames.com/articles/celeste_and_towerfall_physics/index.html).

## First tuning pass

| Mechanic | Starting value | Purpose |
| --- | --- | --- |
| Run / sprint speed | 260 / 420 px/s | A controllable base speed and a faster commitment for gaps and time saves |
| Ground / air acceleration | 6500 / 2500 px/s² | Sharp ground response and responsive air correction |
| Ground reversal acceleration | 10000 px/s² | A fast, deliberate reversal rather than a long skid |
| Ground / air friction | 9500 / 1300 px/s² | A sprint stops within twelve pixels on the ground; air drift is limited |
| Jump takeoff | 600 px/s upward | A full jump that supports the initial gap sizes |
| Released jump cap | 240 px/s upward | Tap and hold produce materially different heights |
| Gravity / falling multiplier | 2000 px/s² / 1.3× | A rising arc with a faster, decisive descent |
| Coyote / jump buffer | 75 / 100 ms | Accept near-edge takeoffs and slightly early landing inputs |
| Wall-slide fall cap | 110 px/s | Give the player time to choose a wall kick |
| Wall-jump launch | 330 sideways / 550 upward px/s | Move clearly away from the wall while gaining height |
| Wall-jump steering lock | 75 ms | Make the kick survive the direction still held into the wall |
| Physics tick | 120 Hz | An 8.33 ms simulation step, independent of rendering |

These values are design choices. They are not measured constants from Super Meat Boy or Mario. They live in `movement_settings.gd` and the exported `movement.tres` resource.

Coyote time and jump buffering are informed by Thorson’s own account of forgiveness techniques. The prototype implements those ideas plus wall-contact grace. Its default apex gravity remains at 1× for a consistent arc, in line with the requested Mario-style precision; the resource exposes an apex scale for later tuning. The article’s corner correction and dash-related features are not implemented. [Celeste & Forgiveness](https://www.mattmakesgames.com/articles/celeste_and_forgiveness/index.html).

Physics interpolation is enabled to smooth rendering between simulation ticks. It improves presentation; it does not make input latency zero or make the physics bit-identical across platforms. Teleports on retry reset interpolation to avoid drawing the player moving across the level. [Godot physics interpolation introduction](https://docs.godotengine.org/en/stable/tutorials/physics/interpolation/physics_interpolation_introduction.html).

## Course and speedrunning rules

Three compact layouts exercise different movement demands:

1. **Running Start:** a short first hop, then full-speed gap jumps.
2. **Wall Flow:** enter a shaft from below, chain wall kicks, then carry speed through gaps and a saw.
3. **Redline:** short hops under ceilings, a late ledge takeoff, landing choices and moving hazards.

The first automated clean routes take approximately 3.85, 4.06 and 4.72 seconds. Those are regression routes, not claims of optimal records or a completed difficulty curve. The next useful iteration is human playtesting: whether direction changes feel deliberate, the jump button feels reliable, and camera movement provides enough look-ahead at sprint speed.

A death immediately starts a fresh attempt. Practice alone enables checkpoint flags. Pausing or losing application focus invalidates a record attempt. Hazard motion restarts with each attempt. PB ghosts replay recorded positions and animation rather than simulating a second colliding character. This avoids depending on cross-platform deterministic replay. Positions are serialized as integer hundredths of a pixel so that saved samples reload exactly.

A public competitive leaderboard would require separate rules, validation and anti-tampering work. The current times are local training records.

## Art and release boundary

Only the unmodified VVVVVV sprite sheet is copied from the free official data pack. This is local placeholder use. The new engine has no Make and Play compile definition, so we cannot assume its distribution exception authorizes bundling the art with a Godot game. Replace the sprites or secure permission before public distribution. Upstream source license and credits remain preserved. [Original license](../LICENSE.md), [distribution exceptions](../License%20exceptions.md), [official assets](https://thelettervsixtim.es/makeandplay/).

Sound effects in the new prototype are original synthesized cues. There is no imported VVVVVV music or audio.

## Verified and still open

Verified in this workspace: headless controller and save/load tests; every course completed through actual collision and hazard logic; all ghost samples preserved across disk reload; native X11 rendering and keyboard interaction. Controller hardware, web/mobile exports, cross-platform behavior and sustained performance on other machines are unverified.

Still open: subjective movement tuning, original art, more expressive animations, richer level authoring, longer courses and a full progression. Shared rooms, a tile editor and expanded tile behaviors are now implemented; see [multiplayer research](MULTIPLAYER_RESEARCH.md). The current code is intentionally a playable movement prototype from which those decisions can be tested.
