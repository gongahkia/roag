# Jomon engine baseline

Jomon now ships an engine and a non-playable `template` content pack. It does not ship a fictional world.

Mechanics decide rules, costs, collision, persistence, and deterministic outcomes. Stable IDs identify entities and actions. A content pack provides authored presentation and concrete system instances. Debug and ASCII Pygame renderers draw the same immutable views and submit the same semantic commands.

`template` validates but sets `playable: false`; the title screen therefore disables **Join Game**. A real game starts by copying `jomon/content_packs/template` outside the installed package, adding stable IDs and a playable `systems.json`, then selecting it with `JOMON_CONTENT_PACK=/path/to/pack`.

Format 16 is the reset baseline. Saves from earlier content baselines are rejected with a clear error rather than partially loading against unrelated content.
