# Archived migration prototype

This project contains the mechanically translated original rules, dynamic value
support and fixed 65×65 world-combat adapter. It is reference evidence for the
migration, including systems deferred beyond the first playable tranche.

`Roag.Client`, `Roag.Core` and `Roag.Tools` do **not** reference this project.
Production dungeon play uses handwritten native tactics, persistent actors and
actual world chunks. No generated rules or Lua/LÖVE runtime are required by that
client. A small typed optional bridge in Core lets reference tests exercise the
archived adapter; only tests explicitly install `PrototypeReference`.

`Roag.Core.Tests` retains the earlier integration checks alongside native tests.
`Roag.RuleParity` references this project to run archived translated checks.
These checks are historical evidence, not acceptance of native gameplay parity.
The source generator `tools/migration/translate_rules.py` writes here, never into
production Core. Original Lua sources remain preserved while later-tranche
systems still need native integration.
