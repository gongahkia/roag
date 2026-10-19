# Audio runtime and published packs

ROAG contains audio playback and validated pack readers. Studio authoring,
import, cue picking and publishers belong to the separate Paint project; this
checkout has no `make studio`, `--studio` editor or audio-publish command.
Do not run historical publisher commands from ROAG.

## Built-in playback

`src/audio_cues.lua` derives stable contextual cue IDs from the current class,
enemy, biome, boon and room registries. `src/studio_audio_runtime.lua` routes
visible canonical events to those cues. Presentation counters choose variants;
they never draw simulation RNG or advance world time.

`src/studio_audio_samples.lua` generates the original small placeholder sounds
and looped motifs. They remain deliberately provisional, not a composed
soundtrack. Music follows title, biome exploration, elite, final boss, Victory
and Defeat. Compatible authored room music/ambience takes precedence while the
player occupies that room. Voice caps, cooldowns, attenuation, fades and
crossfades belong only to presentation.

In **Settings → Sound**, click or drag Master, Music & ambience, and Effects &
UI sliders. Percentages appear on the right. Keyboard left/right adjusts the
selected slider. These settings are stored separately from the active run and
permanent profile.

## Optional published audio

The retained contracts are `src/studio_audio.lua` and
`src/studio_audio_store.lua`. A compatible external publisher supplies checked
`assets/studio/audio-a.pack` / `audio-b.pack` metadata and content-addressed
clips under `assets/studio/audio/`. Separate world/audio/art publications are
not one atomic transaction. Keep editable sources outside derived runtime packs.

A valid partial pack overrides its bound cues; unbound contexts retain built-in
samples. Absent or corrupt checked metadata permits built-in fallback. Missing,
corrupt or undecodable candidate clips reject replacement; refresh keeps the
last good playing resources where possible. A fresh process still starts with
safe built-in audio if optional content cannot load.

Press **F5** in the game to refresh artwork/audio. Concise notices report the
result. It does not modify the active geometry, scheduler, RNG, pending attacks,
profile or run snapshot. Source handles use LÖVE 11.5 `FileData` and
`love.audio.newSource` (`static` effects, `stream` music/long clips), with bounded
cloned voices and retiring transitions.

## Verification and limits

```sh
TMPDIR=/var/tmp luajit tests/run.lua
sh tests/visual_packs_disk_smoke.sh
sh tests/idea_environment_disk_resume.sh
```

The headless suite includes cue registry, format/store validation, placeholder
coverage, transitions and last-good reload behavior through mocked audio
sources. Native GUI refresh/persistence checks and their limits are recorded in
[the current acceptance guide](ROAG_IDEA_ACCEPTANCE.md). No obsolete Studio audio
GUI script is required in this game checkout.

`tests/fixtures/original_test_tone.ogg` is a synthesized 440-Hz, 0.25-second
stereo decoder fixture, not a third-party recording. Its SHA-256 remains
`a44a9284c4d73c550a506f9a58313a74092ad751e0388a36b27fdf7abc4d7358`.
Physical speakers, headphones and human listening remain separate acceptance
work; callback/source-creation tests do not establish how the audio sounds.
