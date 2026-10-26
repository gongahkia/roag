# Combat presentation authoring

Combat rules stay in Core. The Client presents each resolved action through short anticipation, travel, impact and recovery beats. Its profiles and original synthesized sound cues do not consume world/combat randomness, change damage or costs, or join the gameplay content hash. Save compatibility therefore does not depend on changing an effect's color, sprite, sound or timing.

## Profiles and references

Edit `assets/combat-presentation.json`, then rebuild. It is embedded in Client as `Roag.CombatPresentation.json`; a running game does not hot-reload it. The document has its own SHA-256 presentation hash exposed by `CombatVisualProfiles.Hash`.

| Field | Meaning |
| --- | --- |
| `version` | Schema version, currently `1` |
| `profiles[].id` | Unique stable `combat.name` ID |
| `profiles[].family` | `melee`, `projectile`, `spell`, `heal`, `guard`, `dread`, `cooperative` or `retreat` |
| `profiles[].sprite` | Existing alias from `assets/ithica-expedition-bindings.json` |
| `profiles[].color` | Six hexadecimal color digits, such as `#A6E8B1` |
| `profiles[].anticipation/travel/impact/recovery` | Seconds for each beat; individually `0.04..0.6`, total at most `1.2` |
| `profiles[].sound` | Registered synthesized cue, listed below |
| `abilities` | Stable ability ID → profile ID; never translated display names |
| `effects` | Registered Core effect → profile ID; every registered effect needs a fallback |
| `defaultProfile` | Fallback for unknown effects or unlabelled events |
| `cooperativeProfile` | Fallback for newly added cooperative offensive abilities |

All 31 shipped abilities have an explicit mapping. Thirteen profiles distinguish ordinary attacks, arrows, general magic, fire, lightning, healing, guarding, dread, marks, webs, Twin strike, Shared aid and retreat. A new ability remains playable through its effect fallback even before an explicit mapping is added; the shipped-coverage test flags the omission for authoring review. Unknown cooperative heals retain the healing profile. A profile does not add a second damage resolution: Twin strike's two participants still contribute to one combined hit.

`CombatVisualProfiles.Load(json, content)` validates this document without a graphics device. It rejects unknown JSON fields, duplicate properties/IDs, broken ability/profile/sprite/sound references, missing effect fallbacks, malformed colors, null collections and invalid timing. Use the optional catalog parameter when checking a modified content catalog. `Profiles` and `AbilityIds` enumerate the loaded presentation definitions and references; `Resolve(abilityId, effect, cooperative)` is the runtime lookup.

Existing sprite aliases keep their original source artwork, IDs and provenance. Reusing one icon as an effect does not claim it is a frame in a verified animation sequence. To add a new alias, follow [ASSETS.md](ASSETS.md), inspect the crop and regenerate the runtime/catalog projections before referencing it here.

## Sound cues

`CombatSoundSynthesis` produces original 22,050 Hz, 16-bit little-endian mono PCM for these eight cues:

| Cue | Intended emphasis |
| --- | --- |
| `slash` | Short noisy strike with descending body |
| `arrow` | Higher descending flight/impact |
| `spell` | Rising magical pulse |
| `heal` | Converging rising tones |
| `guard` | Restrained metallic ward/interception |
| `dread` | Falling, close dissonant tones |
| `cooperative` | Combined weight and rising accent |
| `retreat` | Descending release |

The synthesis uses fixed decorative noise and no simulation RNG. `CombatAudio` caches one sound per cue, stops the previous instance when a new cue plays and exposes `Unavailable`/`PlayedCues` for diagnostics. Zero volume does not initialize an audio device; losing or lacking audio hardware disables cues while the visual presentation continues. The game owns and disposes this service. Dialogue's voice volume and combat effects are separate controls.

## Verification while authoring

Run the profile, reference, fallback and audio-buffer checks after an edit:

```sh
./tools/dotnet.sh test tests/Roag.Core.Tests/Roag.Core.Tests.csproj --configuration Release --filter FullyQualifiedName~CombatVisualProfileTests
```

Use an isolated deterministic encounter for visual review; preview scenes ignore player saves:

```sh
./roag.sh --scene world-battle
./roag.sh --scene world-cooperation
./roag.sh --scene world-boss
```

Inspect a physical strike, Arrow, Ember, Mend/Shared aid, Guard, Twin strike and the finishing hit. Compare ordinary and fast playback, reduced motion, muted audio, minimum window size and long names. Check that the source, target and actual result remain recognizable, and that readable health/status information survives the strongest effect. Automated buffer checks establish deterministic, bounded, faded audio data; they do not substitute for listening on a working audio device.

The research and the chosen scope are in [COMBAT_PRESENTATION_REVIEW.md](COMBAT_PRESENTATION_REVIEW.md). Broader task evidence and native validation belong in [VERIFICATION.md](VERIFICATION.md).


## Playback and agent diagnostics

`CombatPlayback.Resolve` captures the battle before invoking the normal resolver exactly once. The Client retains that view through ordered event snapshots and separate subsequent status-damage beats, even after Core clears a finished battle. `Advance` and `Skip` only affect the disposable view. The world remains frozen during a terminal sequence; ordinary enemy progression has a fresh delay after playback. Save persists the already-resolved world; Load discards its old visual sequence. Do not move outcome selection, costs or RNG into drawing, sound or animation callbacks.

The mouse controls are `combat-speed` (1x/2x), `combat-motion`, `combat-volume` (including zero) and `combat-skip`. Reduced motion is shared with conversation portraits; sound volume is separate. The presentation settings file accepts older conversation-only settings with defaults for new fields. Preview runs do not write preferences.

`--ui-report PATH` includes `combatPresentation` with `stageVisible`, `busy`, `endingPending`, sequence, ability ID, family, source/target actor IDs, phase (`anticipation`, `travel`, `impact`, `recovery`, `idle`), total/phase progress, displayed fighter snapshots and stable hit/sprite bounds. It also exposes speed/motion/volume, audio diagnostics and the presentation profile hash. `combatIntents` includes pure individual target forecasts. The live `battle` report can already be on the next turn or be null while the presentation is still busy; agents must wait for `busy == false` before submitting another action.

```sh
./dev verify-native --scenes world-battle world-cooperation world-boss --output artifacts/combat-review/native
./dev agent verify --profile full --task combat-presentation --detach
```

The native harness saves a semantic `.ui.json` next to every PNG. To inspect a short beat reliably, it waits for the reported phase, removes window focus to freeze it, captures, then restores focus. Mouse presses wait for a semantic response before releasing, preventing short clicks from disappearing under software rendering. Native fixtures cover ranged/magic/heal/guard effects, 4v4, long names, multiple conditions, helper eligibility, finish/reward, wipe, retreat, boss completion, focus, mute and save/load. Exact normal/fast/skipped saved states are compared; pure tests additionally establish unchanged RNG and clock. These fixtures use temporary slots and deliberately arranged stats, not player saves or an unassisted balance playthrough.
