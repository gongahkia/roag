# ConformanceFixtureV1

`kenjaku-conformance-fixture-v1` is a strict, self-contained JSON envelope for deterministic synthetic rule scenarios. It is not replay data and has no source URI field.

Every fixture has a lowercase `id`, a `ruleset` (`tenhou-4p` or `tenhou-3p`), a lowercase snake-case `scenario`, a non-empty deterministic `seed`, unique tags, a non-empty JSON-object `setup`, an ordered `ActionV1` trace, and a non-empty JSON-object `expected` assertion payload. Every trace action must use the fixture ruleset.

`setup` and `expected` are scenario-specific JSON objects. This keeps the versioned envelope stable while rule oracles own their specific inputs and assertions. Unknown envelope fields, non-finite values, empty setup/expected objects, mixed-rule traces, and non-canonical actions are rejected.

Use `ConformanceFixtureV1.from_dict` for untrusted JSON, `to_dict` for a detached JSON payload, and `to_json` for deterministically key-sorted serialization.
