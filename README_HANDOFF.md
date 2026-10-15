# ROAG foundation handoff (historical)

This file records the original foundation handoff. The current ROAG checkout
already contains the game, Studio, tests, and tools. Use [README.md](README.md)
for current run and development instructions.

The original package contained:

- `docs/ROAG_SPEC_v1.0.md` — authoritative architecture/gameplay baseline. Read it before starting each tranche.
- `prompts/TRANCHE_00_FOUNDATION.md` — paste-ready prompt for a **local Codex session with GPT-6 Sol High**.

## Original suggested workflow

1. Create a separate folder/repository for ROAG without replacing another project.
2. Copy the `docs/` and `prompts/` folders from this package into `roag/`.
3. Open a Codex session in the **root of `roag`**, selecting GPT-6 Sol High if available to you.
4. Paste the complete contents of `prompts/TRANCHE_00_FOUNDATION.md` into Codex.
5. After it finishes, run the reported test commands yourself and launch the game with `love .` if LÖVE is installed.
6. Bring the Tranche 00 completion report back for a review, then prepare Tranche 01. Don't automate onward to other tranches before confirming that the foundation works.

The original handoff contained no source code; this checkout now contains the
implemented game and Studio. Follow [README.md](README.md) for current commands.
