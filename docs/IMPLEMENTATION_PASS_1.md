# Implementation Pass 1: crew and deckbuilding

This pass starts from `68c81fd`. It develops the shared-deck party game and
terminal onboarding while leaving objectives, bosses, biome exploration,
progression, and difficulty modes for later passes.

## Verified baseline

- The standard-library test suite passes 81 tests; content validation and
  warning-enabled compilation pass on Python 3.14.
- The catalog contains 25 crew, 155 techniques, 70 enemies, 109 encounters,
  18 boons, 18 curses, and 18 items. These are diagnostics, not target counts.
- Ten biome-affinity crew have five starter-only techniques. Duelist,
  Artillerist, Chaplain, Hacker, and Pilot have only two non-starters.
- No technique authors `tags`; the engine infers every build relationship.
  Only 25 techniques author an upgrade description.
- Broad rank access, repeated damage/block/status shapes, prescriptive reward
  labels, an unscrollable help notice, and synchronous enemy phases reproduce
  the audited comprehension and build-variety problems.

## Archetype identity matrix

Complexity is a player-facing estimate: 1 is direct, 2 asks for sequencing or
formation planning, and 3 asks the player to manage conditional risk.
`Pool` is current techniques to intended minimum; every intended pool contains
at least three non-starter draft options.

| Archetype | Role / preferred ranks / complexity | Positional strength and constraint | Two build directions | Cross-crew bridges | Pool |
| --- | --- | --- | --- | --- | --- |
| Warden | defender / 1–2 / 1 | screens the front; weak when stranded back | guard-riposte; breach-displacement | guarded allies, vulnerable payoff, movers | 8→8 |
| Engineer | engine support / 2–3 / 2 | converts setup from the middle; loses control at rank 1 | mark-energy; plating-gravity | mark users, draw users, forced movement | 8→8 |
| Medic | recovery support / 3–4 / 1 | best rescue reach from back; low pressure | cleanse-triage; adrenaline-risk | Death's Door, stress spenders, wound parties | 8→8 |
| Scout | back striker / 3–4 / 2 | sees every enemy from back; disrupted forward | mark-sniper; reload-disengage | mark setup, discard, allied movement | 8→8 |
| Breacher | front striker / 1–2 / 2 | strongest at point blank; stress and backward recoil | rush-displacement; stress-burst | vulnerable payoff, stress care, formation correction | 7→7 |
| Psion | back controller / 3–4 / 3 | controls the whole line; self-stress limits output | gravity-control; feedback-focus | mark payoff, stress relief, pulled targets | 7→7 |
| Quartermaster | hand support / 2–3 / 2 | smooths the shared hand; modest personal output | draw-energy; team cover-recovery | expensive cards, discard, all party engines | 7→7 |
| Operative | back assassin / 3–4 / 2 | cashes setup anywhere; forced forward movement hurts | mark-execution; vanish-tempo | mark setters, movers, enemy weakening | 7→7 |
| Biologist | attrition support / 2–3 / 3 | wounds and studies targets; slow immediate damage | wound-payoff; serum-rescue | wound setup/payoff, Death's Door, cleanse | 7→7 |
| Synth | flexible defender / 1–2 / 2 | anchors shifting formations; energy cards invite overdraw | guard-hardlight; capacitor-focus | guarded carries, draw, movement crews | 8→8 |
| Duelist | counter striker / 1–2 / 2 | punishes direct attacks; weak against wide pressure | dodge-riposte; lunge-challenge | guard, mark, forced movement | 6→7 |
| Artillerist | back artillery / 3–4 / 2 | pressures full formations; nearly inert at the front | wound-barrage; marked overpressure | mark/wound setup, stress recovery | 6→7 |
| Chaplain | morale support / 2–3 / 1 | stabilises several crew; damage is secondary | cleanse-litany; martyr-fervor | stress spenders, guard, focus consumers | 6→7 |
| Hacker | back controller / 3–4 / 3 | edits enemy and hand state; fragile if displaced | mark-exploit; cycle-debuff | marked payoff, discard, expensive finishers | 6→7 |
| Pilot | formation support / 2–3 / 3 | changes ally and enemy ranks cheaply; low sustain | vector-control; slingshot-tempo | rank specialists, displacement payoff, dodge | 6→7 |
| Cryonaut | control defender / 2–3 / 2 | freezes exposed targets; broad attacks cost tempo | brittle-control; preservation | vulnerable payoff, pull, rescue | 5→8 |
| Horticulturist | growth support / 2–3 / 2 | turns wounds into protection; slow without setup | thorn-attrition; grafted-sustain | wound teams, riposte, healing | 5→8 |
| Foundryman | stress tank / 1–2 / 3 | converts heat and stress at the front; needs relief | tempered-guard; overheat-smash | stress care, vulnerable setup, guard payoff | 5→8 |
| Reactor Saint | risk support / 2–3 / 3 | buys energy and area pressure with stress | critical-energy; decay-support | mark/wound payoff, stress care, expensive cards | 5→8 |
| Mycologist | attrition healer / 3–4 / 2 | sustains wound engines from back; weak burst | spore-wound; symbiotic-cleanse | wound payoff, all-crew defense, cleanse | 5→8 |
| Abyss Diver | line controller / 1–2 / 2 | drags formations into reach; poor back-line damage | pressure-pull; lifeline-defense | short-range attackers, stress care, movement | 5→8 |
| Stormcaller | tempo striker / 3–4 / 3 | chains marks into energy; fragile and rank-bound | marked-surge; ward-riposte | marks, guard, forced movement | 5→8 |
| Archivist | hand controller / 3–4 / 3 | filters draws and catalogs targets; front rank shuts it down | index-mark; revision-cycle | mark payoff, discard, status control | 5→8 |
| Voidwalker | risk controller / 3–4 / 3 | trades position and stress for exceptional control | phase-mobility; vulnerable-absence | displacement, stress care, Death's Door rescue | 5→8 |
| Bonewright | death defender / 1–2 / 2 | fortifies wounded crew at the front; limited reach | marrow-wound; scaffold-guard | Death's Door, wound payoff, guarded carries | 5→8 |

## Design language and data changes

Hero JSON will author `combat_role`, `complexity`, `preferred_ranks`,
`signature`, `strength`, `weakness`, and two `builds`. Card JSON will author
validated tags and upgrade descriptions. Tags describe observable functions
such as setup/payoff, mobility, control, recovery, draw, and guard; they do not
encode a hidden card score.

The effect vocabulary remains shared. New content should use ranks, movement,
guard, block, heal, stress, mark, wound, vulnerable, stun, dodge, focus,
riposte, draw, discard, and energy before adding an operation. Conditional
effects may distinguish healthy, stressed, wounded, or Death's Door states.
Biome affinity remains a situational bonus rather than a hard requirement.

Rewards will expose neutral tradeoffs derived from authored tags: reinforce an
existing engine, cover a missing function, or branch into a low-duplication
line. Skipping remains valid. Transformations will compare source and
destination cost, ranks, effects, and tags instead of presenting an opaque
recommendation. Seeded choices and save/load behavior remain unchanged.

## Atomic implementation sequence

1. Rename the public title and state-directory slug while retaining the Python
   import path; verify launch documentation and historical wording.
2. Replace exact-count guards with semantic schema, ownership, starter-pool,
   tag, status, upgrade-description, and transformation invariants.
3. Add authored hero identity metadata and use it in roster/card interfaces.
4. Author tags and meaningful upgrade text for the existing catalog in small
   archetype families.
5. Add distinct draft cards for Duelist through Pilot, then three non-starters
   for each starter-only affinity archetype, one family per commit.
6. Narrow overly broad rank access where the matrix identifies a real
   positional constraint; add recovery scenario tests.
7. Redesign deterministic rewards and transformation comparison UI around
   dilemmas rather than recommendations.
8. Rework only scalar persistent-effect families that directly enable several
   of the matrix builds.
9. Add beginner squad presets, then advanced roster detail, formation warnings,
   and a combined-deck preview as separate UI changes.
10. Add a deterministic, optional contextual tutorial using real combat and
    exploration rules in independently testable stages.
11. Separately fix scrollable help, reachable target cycling, sequential enemy
    action playback with speed controls, large-terminal use, and mouse claims.
12. Run full automated verification and real-PTY scenarios, including one
    natural run, then record only measured outcomes.

## Acceptance criteria

- Every crew entry communicates identity, role, ranks, complexity, strength,
  weakness, and two build paths at 80×24.
- Every archetype owns at least three non-starter techniques with distinct
  draft purposes; each has legal transformation destinations and two credible
  build branches.
- Every technique has authored valid tags and accurate base and upgrade text;
  structural duplication is materially lower than the verified baseline.
- Rewards are deterministic, owner-valid, dead-owner safe, skippable, and show
  distinguishable neutral tradeoffs. Transform previews show actual deltas.
- Presets and advanced custom selection are fully keyboard-usable. The optional
  deterministic tutorial teaches the shared-deck/rank loop through real rules.
- Help scrolls at 80×24, cycling respects travel reach, and enemy actions can be
  followed or accelerated without changing deterministic state.
- Content validation, focused tests, the full suite, save/load round trips,
  compilation, diff checks, and the required PTY scenarios pass. A natural run
  is reported honestly whether won or lost.
