# Implementation Pass 3: pressure, engines, and expeditions

## Starting evidence and preservation

This pass starts on `main` at
`cd534cc86ae77798e00a125d384b3c9f3601353b`. Local `origin/main` has the same
SHA. The only initial status entry is the user-supplied, untracked
`Dullest_Dungeon_Expansion_Research.md`; its SHA-256 is
`363b2ac5e0be10a0feefaca1715abe71e3db842c83aaa504c170dd68d0eb1802`.
Preserve its bytes. Never reset, amend, squash, rebase, force, or push. Check
HEAD and tracked changes before each explicitly staged atomic commit; record
external movement. Existing calibration commits are part of the baseline.

There is no package manifest, Makefile, or shell task runner. The repository is
Python standard library only. Applicable guides are the home and repository
AGENTS.md files. Their Next.js note has no applicable package in this project.

| Evidence | Verified starting result | Acceptance / remaining work |
| --- | --- | --- |
| Calibration history | 17 commits since `639d678db2f57bc2b4c5df800b5ce3efaa6984c9` | Preserve Core guidance, cluster rails, bounded Undertow, and route cycling |
| Versions | Engine 0.1.0; save 26; content schema 20 | Separate profile, telemetry, fingerprint, and RNG architecture versions |
| Crew / techniques | 25 / 190; six additional curse cards | Keep 25; four curated new roles per owner, approximately +100 |
| Owner pools | Seven or eight techniques; three or four non-starters | Check eligible density, sequencing, casualty pivots, source/target ranks |
| Structural duplication | 32 cards in 14 normalized base-effect groups | Reject new scalar clones; distinguish upgrade branches explicitly |
| Enemies / templates | 70 / 109; 67 enemies have two actions, three have three | Measure behavioral density before adding bodies |
| Normal biome pools | Derelict 31; other biomes three or four | Six distinct compatible normals per biome |
| Native elites | Derelict has three elite-only definitions; other biomes zero | Two native elites per biome |
| Bosses | One shared Core encounter; no distinct native guardian roster | One guardian per biome, four finale identities; two bosses per base run |
| World / expedition | Six layouts; eleven biomes, missions, facilities, landmarks | Four selected objectives; any two open access; guardian replaces culmination |
| Persistent content | 18 items, 18 boons, 18 curses | Target +18 / +12 / +12, each earning a mechanical role |
| Tests | 202 discovered; baseline full run started | Record observed pass/fail and duration, never infer from collection |
| Natural completion | Prior records contain no natural Core reach or win | Calibration is a hard gate before expansion |
| Ordinary loss | One historical full wipe; three later attempts were stopped | Reproduce current-rule explainable loss and current-rule victory |
| Duration | No verified winning duration | Demonstrate plausible 30–45 minute path; separate measured and modeled time |

The existing scripted expedition victory test injects one-HP enemies and 99
energy. It is a useful constructed flow check, not natural completion evidence.
The static corridor audit excludes combat and moving patrols. Neither satisfies
the calibration gate.

## Design and implementation boundaries

Four owner-bound crew share five drawn cards and three energy. Consciousness,
rank, casualties, and the loss of owned cards remain central. The terminal is
the permanent interface. No dependency, networking, account, or permanent power
progression is introduced. Simulation actions alone advance global pressure;
light remains its own local resource. Exceptional conditional engines may grow
large; only automatic nontermination receives a chain seal, never a damage cap.

Use existing command boundaries and data conventions where coherent. Register
bounded Python effect operations, validate content once, and expose effectively
immutable typed records. Keep old identities stable; namespace additions and
document explicit aliases/migrations. A card's owner is never inferred from
display text. Preserve Python random state; any substream architecture receives
a separate version and stable digest-based seed derivation.

## Milestones and atomic boundaries

0. **Calibration.** Retain a reproducible content audit and legal-command
   policies, inspect current natural play, isolate any route or combat defect,
   commit each correction with focused regression checks, then replay. Gate:
   natural Core reach, victory, explainable loss, plausible base duration, and
   full suite. Flooded Breach Protocol is a required focused case.
1. **Evidence and contracts.** Commit strict JSON parsing, immutable runtime
   representation, each manifest/opcode/pack contract, synergy diagnostics, run
   history, opt-in local telemetry, and migrations separately. Record offers,
   picks, skips, actual play, ranks, deaths, resource and source attribution.
   Gate: malformed data rejected; graph and reward density reports; historical
   fixtures and checkpoint continuation; no UI RNG consumption.
2. **Interaction queue.** Commit event/phase contract, listener dispatcher,
   limiter graph validation, budget/trace handling, engine integration, and
   resolution inspection as separate coherent steps. Gate: canonical ordering,
   listener snapshots, every-cycle limiter proof, repeat-chain sealing, and
   saveable valid state with exact arithmetic behind concise combat logs.
3. **Stacks and lanes.** Commit integer stack algebra and preview, authored
   policies in small families, conversion contracts, and acquisition filtering.
   Gate: current/next results, caps, trigger limits, loss previews, skip behavior,
   and lane-specific eligible pools are tested.
4. **Pressure director.** Commit serialized pressure accounting, thresholds and
   forecasts, route/UI projection, then qualitative encounter/reward responses.
   Gate: no real-time/UI advancement, frozen displayed promises, disclosed
   sources, and identical-seed rush/explore/greed comparison.
5. **Existing crew expansion.** Commit reusable opcodes first; prototype/cull
   against normalized effects. Commit each owner's A-deepener, B-deepener,
   broad bridge, and conditional rule-breaker batch separately. Commit bounded
   mastery contracts, approximately 50 A/B choices, approximately 16 infusions,
   10–12 doctrines, alternate loadouts, and 12–15 squads in small related batches.
   Gate: broad synergies, positional identity, casualty behavior, honest text,
   distinct card-copy layers, save/load and 80×24 navigation.
6. **Persistent engines.** Add item/boon/curse families in atomic mechanic-sized
   batches with exact inspection and tests. Add scarce stack conversion. Gate:
   each entry changes a decision; no generic passive pool inflation; burdens
   and irreversible conversions are previewed.
7. **Encounter expansion.** Commit threat-vector estimation and dimensional
   ceilings, stable enumeration and selection, repetition records, then each
   biome's missing enemy families. Add at least 16 tactical mutation modules.
   Gate: normal/elite density floors, three actions or documented simple role,
   coordination costs, four-rank legality, frozen conditional intents, and
   compatibility/exclusion tests.
8. **Guardians and finales.** Integrate one replacement guardian culmination,
   finale forecast/reveal, phase carry rules, and each boss separately with art
   and deterministic scenarios. Gate: eleven distinct guardians, four finales,
   multiple viable answers, natural family coverage, and base route length.
9. **Horizontal replay.** Commit atomic versioned profile/migrations, global
   twenty-rank cumulative ladder in small challenge groups, discoveries,
   approximately twenty contracts, custom/offline daily/code modes, and terminal
   history/compendium screens. Gate: fresh-profile viability, no permanent
   power, versioned strict codes, stable daily derivation, unlock/save/UI checks.
10. **Optional loops.** Commit durable base clear, extract/descend choice,
    deterministic shortened loop world, escalating modules, and records. Gate:
    acquired engine survives, pressure floor rises, loop death retains base win,
    exact huge values remain inspectable, and loop continuation is saveable.

Every atomic commit must pass its relevant checks; run the full suite at
milestone boundaries. Do not accumulate a milestone's changes into one commit.
Update this document's evidence as results arrive, retaining failures and
revision reasons. The design, migration, and verification references accompany
implemented behavior rather than promising absent features.

## Verification and interpretation

- Existing commands: `PYTHONWARNINGS=error python3 -m unittest discover -s tests -v`,
  `PYTHONWARNINGS=error python3 -m compileall -q dumbest_dungeon tests`,
  `python3 -m dumbest_dungeon --validate-content`, `git diff --check`.
- Retain seed cohorts and command transcripts. Headless policies may only use
  normal commands and player-visible information; they are regression evidence,
  not proof of fun or human run duration. Constructed stress scenarios must be
  labeled and must never count as natural clears.
- Test uninterrupted versus checkpointed continuation, malformed durable state,
  pure one-version migrations, reordered JSON enumeration, multiple
  PYTHONHASHSEED values, deterministic fuzz and targeted three-way interactions.
- Real curses PTYs: 80×24 and 140×60, keyboard-only, including every new choice,
  inspection, playback/skip, resize, casualty, boss, history, and loop boundary.
- Stratify rounds, incoming damage, hand clogging, resource use, offer/pick/play
  rates, trigger length, repetition and outcomes by the available party/world/
  difficulty/pressure evidence. Small samples do not establish robust win rates.

## Principal risks

The base calibration gate is incomplete. Thin biome pools and HP-only pricing
hide coordination spikes. Current content dictionaries are mutable; tests
sometimes alter them. Saves have no migration chain or content fingerprint.
Copy identity currently consists only of definition, upgrade flag and curse
owner, so per-copy mastery/infusion requires explicit durable identity. Current
automatic effects are ad hoc and use floating-point capped multipliers; queue
integration must preserve existing timing before introducing new triggers.
Adding rewards before scoped lanes and density diagnostics risks dilution.
Boss additions must replace route work to protect duration. Preserve the
initial user research separately from authored implementation evidence.

## Calibration progress

- Added a normal-command policy runner with complete command hashes and reward
  offers. Five focused tests passed in 8.509 seconds, including seed-42 victory
  replay and checkpointed continuation. Fifteen initial party/seed/policy runs
  completed with fourteen victories and one wipe. These are unmodified headless
  policy runs, not measured human play. Detailed evidence is retained separately.
- Reproduced a casualty persistence defect: moving the rear survivor backward
  after a death produced ranks `[1, 2, 4]`; loading that otherwise legal run then
  failed with `save contains an invalid surviving crew formation`. Movement now
  stops at the surviving line's last occupied rank. The new regression failed
  before the correction. Eight focused casualty, Flooded and policy tests passed
  afterward in 11.481 seconds. Save schema is unchanged because this corrects
  the existing formation invariant rather than introducing durable fields.
