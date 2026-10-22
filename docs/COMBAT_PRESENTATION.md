# Combat presentation authoring

Combat rules stay in Core. The Client presents each resolved action through short anticipation, travel, impact and recovery beats. Its profiles and original synthesized sound cues do not consume world/combat randomness, change damage or costs, or join the gameplay content hash. Save compatibility therefore does not depend on changing an effect's color, sprite, sound or timing.

## Profiles and references

Edit `assets/combat-presentation.json`, then rebuild. It is embedded in Client as `Vagabonds.CombatPresentation.json`; a running game does not hot-reload it. The document has its own SHA-256 presentation hash exposed by `CombatVisualProfiles.Hash`.

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
./tools/dotnet.sh test tests/Vagabonds.Core.Tests/Vagabonds.Core.Tests.csproj --configuration Release --filter FullyQualifiedName~CombatVisualProfileTests
```

Use an isolated deterministic encounter for visual review; preview scenes ignore player saves:

```sh
./vagabonds.sh --scene world-battle
./vagabonds.sh --scene world-cooperation
./vagabonds.sh --scene world-boss
```

Inspect a physical strike, Arrow, Ember, Mend/Shared aid, Guard, Twin strike and the finishing hit. Compare ordinary and fast playback, reduced motion, muted audio, minimum window size and long names. Check that the source, target and actual result remain recognizable, and that readable health/status information survives the strongest effect. Automated buffer checks establish deterministic, bounded, faded audio data; they do not substitute for listening on a working audio device.

The research and the chosen scope are in [COMBAT_PRESENTATION_REVIEW.md](COMBAT_PRESENTATION_REVIEW.md). Broader task evidence and native validation belong in [VERIFICATION.md](VERIFICATION.md).
