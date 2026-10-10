# ROEG implementation handoff

This package contains:

- `docs/ROEG_SPEC_v1.0.md` — authoritative architecture/gameplay baseline. Read it before starting each tranche.
- `prompts/TRANCHE_00_FOUNDATION.md` — paste-ready prompt for a **local Codex session with GPT-6 Sol High**.

## Suggested workflow

1. Create a **new** folder/repository named `roeg` (never replace the older `roag` repository).
2. Copy the `docs/` and `prompts/` folders from this package into `roeg/`.
3. Open a Codex session in the **root of `roeg`**, selecting GPT-6 Sol High if available to you.
4. Paste the complete contents of `prompts/TRANCHE_00_FOUNDATION.md` into Codex.
5. After it finishes, run the reported test commands yourself and launch the game with `love .` if LÖVE is installed.
6. Bring the Tranche 00 completion report back for a review, then prepare Tranche 01. Don't automate onward to other tranches before confirming that the foundation works.

There is no source code in this handoff: the first tranche builds it locally.
