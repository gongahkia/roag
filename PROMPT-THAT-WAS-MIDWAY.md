You are implementing the next major expansion of **Dullest Dungeon**. This is an implementation request, not a read-only audit and not merely a design plan. Work autonomously through all milestones below, take as many turns and context compactions as necessary, and do not stop after planning unless a genuine permission, authority, or irreducible product-choice blocker appears.

Use the highest reasoning effort available. Model capacity and token budget are not evidence that generated content is good: prototype broadly, inspect interactions, cull weak designs, test, play, and revise.

## Known context, to verify rather than blindly assume

The last supplied verified state was:

- Implementation Pass 2 ending at `639d678db2f57bc2b4c5df800b5ce3efaa6984c9`.
- 25 crew archetypes.
- 190 techniques plus six curse cards.
- 70 enemies and 109 encounter templates.
- 11 biomes, 11 facilities, and six layouts.
- 18 boons, 18 curses, and 18 stackable items.
- Four biome objectives per run; any two open access to the Overseer Core.
- Save version 26 and content schema 19.
- 183 tests passing at the end of Pass 2.
- Three ordinary Pass 2 runs failed before the Core; Flooded Undertow was particularly severe for Breach Protocol.

A calibration pass or user-authored commits may have landed since then. Inspect the actual repository, current `HEAD`, recent history, documentation, status, content, tests, and verification evidence. Treat the repository as ground truth. Preserve all user and external changes. Do not reset, discard, overwrite, or silently undo work you did not create.

The game remains:

- Publicly titled **Dullest Dungeon**, with user-facing slug `dullest-dungeon`; keeping the internal `dumbest_dungeon` Python package is acceptable.
- An actual-terminal game permanently. Do not add a graphical or browser shell.
- Python 3.11+ standard library only, primarily `curses`.
- Intended for Linux, macOS, WSL, and comparable Unix terminals.
- Fully keyboard operable. Mouse support is optional and must never be overstated.
- Deterministic under a seed, with serializable random state and mid-run save/load.
- A shared five-card hand and three shared energy across the living crew.
- Owner-bound: cards belong to a crew member and must respect that owner’s consciousness and rank.
- A game where a dead crew member’s owned cards are removed, ranks collapse, survivors continue, and only a full wipe ends the expedition.
- A finite base expedition targeting roughly 30–45 minutes, with severe but legible pressure and a relatively forgiving base tier.
- Horizontally progressive only. Do not add permanent health, damage, energy, resource, or reward-quality bonuses between runs.

Do not change those principles unless the current repository contains an explicit later decision from the user.

## Product and design thesis

The next pass should make this the memorable proposition:

> Four owner-bound crew share one position-dependent deck; the world grows more hostile while they detour for power, and a casualty tears that crew member’s cards out of the build without ending the expedition.

The desired Risk of Rain influence is not “inflate enemy HP forever.” It is the tension between rushing while underbuilt and spending expedition actions to assemble a stronger, potentially absurd engine while the world advances. Broken builds are welcome. Software nontermination, unreadable chains, one obvious build every run, and hard counters that invalidate a legal build are not.

Do not claim “infinite fun” or “infinite replayability.” Build and measure high interaction density, run-to-run divergence, multiple viable engines, low perceptual encounter repetition, and optional unbounded post-victory play.

The next pass must be a large **systems-plus-content expansion**, not indiscriminate JSON inflation. Every new content entry must do at least one of the following:

- Enable a previously unavailable build.
- Deepen an existing branch through a new sequencing or positional decision.
- Bridge two underconnected broad mechanics.
- Provide a real pivot or salvage line for a disrupted party.
- Alter route, pressure, resource, or objective evaluation.
- Create a readable new enemy coordination pattern.
- Make an existing underused card, item, boon, curse, status, or formation newly desirable.

Reject renamed duplicates, scalar clones, and mechanics that exist only to raise a count.

## Git and working discipline

1. Read every applicable `AGENTS.md`, the README, all implementation and verification documents, research notes, relevant source, content JSON, validators, and tests before editing.
2. Record the exact starting commit and baseline status.
3. If the worktree is dirty, inspect and preserve those changes. Work around them or stop only if overlap makes safe progress impossible.
4. Make a local git commit after **every small atomic, coherent change**. Do not accumulate an entire milestone or hours of work in one commit. Examples of appropriate boundaries are one schema contract, one migration, one UI behavior, one archetype’s card batch, one biome’s enemy family, one guardian, or one challenge group.
5. Every commit must leave the repository internally coherent and pass the relevant targeted checks. Run the complete suite at milestone boundaries.
6. Use clear commit messages. Never amend, squash, rebase, reset, rewrite history, force, or push.
7. If `origin/main` or local history moves externally while you work, preserve it, record what happened, and continue only when safe.
8. Finish with a clean working tree and report the starting and ending full SHAs, commit list, test evidence, and any external commits observed.

## Required research grounding

Read the existing `docs/RESEARCH_NOTES.md` and update it with the sources below and the exact implications actually used. Prefer primary/developer sources. Do not cargo-cult mechanics or paste copyrighted prose.

- [Mega Crit on culling cards, powerful combinations, and path choice](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-mega-crit-games-i-slay-the-spire-i-)
- [Mega Crit’s metrics-driven design talk](https://www.gdcvault.com/play/1025731/-Slay-the-Spire-Metrics%EF%BB%BF)
- [LocalThunk’s own Joker-design guidelines](https://www.reddit.com/r/balatro/comments/1czo9g0/guidelines_for_joker_design/)
- [LocalThunk on concise rules, emergent overpowered builds, culling, seeds, and beta feedback](https://gameinformer.com/interview/2024/03/21/balatro-was-almost-called-joker-poker-and-other-details-from-its-creator)
- [Hopoo on Risk of Rain’s rush-versus-loot tension and avoiding content dilution](https://www.gamedeveloper.com/design/risk-of-rain-interview)
- [Official Risk of Rain description of stacking, looping, and simultaneous player/enemy growth](https://riskofrain.2k.com/)
- [Risk of Rain 2 example of a local partial proc-chain resistance rather than universal negation](https://support.2k.com/hc/en-us/articles/47210814399123-Risk-of-Rain-2-Patch-Notes-December-9-2025)
- [Risk of Rain’s explicit burden-for-exceptional-power direction](https://store.steampowered.com/news/app/632360/view/710026912607507071)
- [Cobalt Core’s mixed crew decks, small rules, positional enemy questions, and rejection of isolated character minigames](https://www.gamedeveloper.com/design/how-cobalt-core-makes-movement-as-exciting-as-fighting-in-its-roguelike-deckbuilder-combat)
- [Roguebook on a shared hero deck, emergent engines, improvisation, position, and player-controlled risk](https://www.gamedeveloper.com/design/tackling-deckbuilding-design-in-abrakam-s-roguebook)
- [Monster Train’s Pact Shards: power that also strengthens future enemies](https://news.xbox.com/en-us/2021/04/01/monster-train-the-last-divinity-dlc-available-now/)
- [Monster Train 2’s distinct card layers, challenges, and Endless mode](https://shinyshoe.com/games/monster-train-2/)
- [Hades’s encounter-changing Pact modifiers](https://www.supergiantgames.com/blog/hades-welcome-to-hell-update-patch-notes/)
- [Trials of Fire on telemetry and players pushing card/party systems to breaking point](https://www.unrealengine.com/developer-interviews/how-early-access-helped-shape-tactical-deck-building-adventure-trials-of-fire)
- [Caves of Qud on authored backbones inside procedural worlds](https://www.gamedeveloper.com/design/tapping-into-the-potential-of-procedural-generation-in-caves-of-qud)
- [Cogmind’s commercial ASCII postmortem](https://www.gamedeveloper.com/business/releasing-a-commercial-ascii-roguelike-a-post-mortem)

Use additional research only where a current implementation choice genuinely needs it. Record inference as inference.

## Milestone 0 — Establish the real baseline and finish the calibration gate

Before expanding difficulty or content:

1. Audit the current content and behavior programmatically. Recompute counts, per-archetype pools, structural effect shapes, rank access, build tags, reward pools, enemy actions, biome enemy density, encounter-family density, threat budgets, bosses, objectives, save versions, and test coverage.
2. Read any Implementation Pass 2.5 plan or verification document and inspect its commits. Do not redo verified work.
3. Determine whether an unmodified ordinary expedition has already demonstrated:
   - a natural Core reach,
   - a natural boss victory,
   - a fair, explainable defeat,
   - and a plausible 30–45-minute base-run path.
4. If that evidence does not exist, complete the calibration pass first. Reproduce route, light, supply, combat-attrition, layout, biome, objective, and Core-access pressure. Pay particular attention to Flooded Undertow and Breach Protocol. Fix causes, not symptoms.
5. “Natural” means normal rules, normal enemy HP and energy, normal rewards, no state injection, and ordinary player decisions. Constructed scenarios are useful separately but do not count as natural completion evidence.
6. Commit every calibration fix separately and rerun focused natural play. Do not proceed until the base tier is demonstrably completable and still capable of a fair loss.

Create or update an implementation plan document with an evidence table, risks, milestones, and acceptance tests. Commit it before code changes.

## Milestone 1 — Evidence, typed content contracts, and deterministic foundations

The existing scale is already too large to balance by intuition. Add foundations before expanding it.

### Run history and local telemetry

Add a terminal-readable run history/morgue record. Record at least:

- Run seed; engine, content, save, profile, telemetry, and RNG versions.
- Layout, selected biomes, objectives, approaches, facilities, route and travel ticks.
- Crew, loadouts, doctrine, starting and final formation, deaths, and Death’s Door checks.
- Cards offered, picked, skipped, played, upgraded, mastered, infused, transformed, removed, and lost with an owner.
- Items and stack histories, boons, curses, bargains, conversions, and corruption.
- Encounter IDs/families, enemy compositions, mutations, pressure band, rounds, and results.
- Damage, block, healing, overkill, wasted block, stress, control, and trigger-chain length by source.
- Outcome, cause, duration, final detected build engines, base victory, loop depth, and score.

Run history is a normal local feature. More detailed newline-delimited JSON telemetry must be local-only, opt-in, documented, and contain no networking or account system. Record offers as well as picks. Popularity, conditioned performance, and actual play rate are different signals.

### Content contracts

Fit these ideas to the repository rather than performing a gratuitous rewrite:

- Load JSON once, strictly validate it, and expose immutable or effectively immutable typed runtime content rather than passing arbitrary dictionaries through the simulation.
- Introduce explicit contracts for content manifests/fingerprints, effect opcodes, synergy edges, triggers, stack rules, threat profiles, acquisition lanes, and enabled content packs.
- JSON may compose registered, typed Python opcodes. It must never evaluate code or grow into an unbounded scripting language.
- Reject duplicate JSON keys, unknown fields where appropriate, non-finite numbers, invalid enums, broken references, and invalid ranges.
- Keep stable namespaced IDs. Display-name changes must not change identity. Retired IDs need explicit aliases or migrations and must never silently acquire a new meaning.
- Keep engine/content schema, run-save schema, profile schema, telemetry schema, and RNG architecture version separate.

### Synergy graph

Model mechanical relationships explicitly with concepts equivalent to:

- `produces`
- `exploits`
- `spends`
- `converts`
- `requires`
- `covers`

Derive edges from effect opcodes where possible. Authored UI tags may add language, but validation must reject tags contradicted by effects.

Validation must report:

- Orphan producers and unsupported payoffs.
- Payoffs with insufficient producer density in eligible run pools.
- Supposed build branches with normalized identical effects.
- Owner/rank bottlenecks and excessive rank-invalid hand clogging.
- Cross-owner engines that become permanently inert after one death without a disclosed risk or pivot.
- Reward pools whose expansion dilutes producer/payoff density below a documented threshold.
- Named-card or named-pair dependencies that should use broad grammar instead.

Commit the loader, each contract group, graph analysis, diagnostics, and tests atomically.

## Milestone 2 — A deterministic interaction kernel that can survive broken builds

Automatic stacking, copying, conversion, and retriggers must not resolve through uncontrolled nested callbacks.

Implement an explicit deterministic event queue. Every event should have stable fields equivalent to:

- `event_id`
- `root_action_id`
- `parent_event_id`
- `depth`
- `event_type`
- `source_id`
- `target_ids`
- typed payload

Use a documented stable phase order equivalent to:

1. Replace/prevent.
2. Before.
3. Primary.
4. After.
5. Death.
6. Cleanup.

Listener order must be canonical and independent of set/dict iteration: phase, explicit priority, entity creation ID, then stable effect ID is a reasonable contract. Snapshot listeners before dispatch so mutation during an event does not unpredictably change the remaining listener set.

Each potentially recursive automatic trigger must declare at least one limiter:

- Once per root action.
- Once per card play.
- Once per turn.
- Once per combat.
- Finite charge or consumed status.
- Maximum generated retriggers.
- Proc-family exclusion.

At validation time, build the trigger dependency graph. Reject strongly connected components unless every possible cycle has a provable limiter or consumes a finite resource.

At runtime, add a generous deterministic per-root event budget and ancestry trace as a last-resort guard. If accidental automatic recursion exceeds it, stop only that repeated chain, show a concise `CHAIN SEALED` message, preserve the trace for reproduction, and keep the simulation valid. Do not impose a global damage cap. Do not remove manually initiated high-power sequences merely because they are strong. Prevent nontermination, not delight.

Add a scrollable combat-resolution inspection showing source-attributed arithmetic and trigger order. The normal animation/log must remain concise at 80×24; detailed ancestry belongs behind inspection.

## Milestone 3 — Explicit stacking, conversion, and scoped reward lanes

Give every stackable item, boon, curse, and stackable status an authored policy selected from a small validated vocabulary:

- Linear.
- Multiplicative.
- Independent chance.
- Hyperbolic/diminishing.
- Threshold.
- Duration refresh.
- Unique.
- Conversion/corruption.
- Authored integer table.

Use integers or basis points rather than binary floats where practical. Validate monotonicity when promised, caps, overflow/display bounds, exact current result, and exact next-stack result.

Inspection and reward screens must show:

- Current count and result.
- What the next stack changes.
- Formula/table or concise authored wording.
- Cap or soft cap.
- Trigger limit and whether descendants can retrigger it.
- What will be converted or lost when corruption is accepted.

Use diminishing curves primarily for dodge, repeated stun/control, Death’s Door resistance, cooldown avoidance, and other effects that can remove interaction. Damage, conditional multipliers, conversions, setup engines, and rare retriggers may become spectacular.

Refactor reward categories into distinct jobs instead of letting every list become generic passive bonuses:

- Techniques: immediate actions, positioning, sequencing, and status interaction.
- Stackable items: repeated triggers, thresholds, and explicit scaling.
- Boons: unique or nearly unique run rules.
- Curses: burdens, build-around liabilities, corruption, or dangerous contracts.
- Infusions: one bounded modifier on a specific card copy.
- Doctrines: party-level formation/sequencing identity with a strength and liability.

Keep acquisition lanes scoped:

- Normal combat rewards favor glue, coverage, and modest branch deepeners.
- Elites favor engines, infusions, and scarce conversion.
- Objectives/facilities favor biome affinities, route consequences, and targeted transformation.
- Bargains/curses grant exceptional power with explicit burden.
- Guardians/finales grant signature rules and post-win choices.

Rewards remain skippable. The entire global catalog must never become every run’s undifferentiated pool.

## Milestone 4 — Expedition Pressure: a turn-based Risk of Rain director

Add a monotonic, serialized **Expedition Pressure** system. Choose a concise in-fiction public name if a better one exists, but document the code term.

Pressure advances only from simulated world actions:

- Weighted travel ticks.
- Completed enemy rounds.
- Objective stages and major facility actions.
- Explicitly disclosed event/bargain choices.
- Optional high-power rewards that state their pressure price.

It must never advance from real time, menus, help, card reading, inspection, terminal resizing, animation, input delay, or paused play.

Pressure and light are not synonyms:

- Light remains a depletable local expedition resource affecting visibility, surprise/ambush, stress, and reward opportunity.
- Pressure is irreversible global enemy awareness/escalation.

Create several named, visible pressure bands. Show current band, progress, the causes of recent increases, exact projected route pressure where knowable, the next threshold, and a concise forecast of what it unlocks. Use plain language before lore flavor.

Scale qualitatively before numerically. Pressure bands should progressively enable combinations of:

- More coordinated encounter plans.
- Visible elite mutations.
- Reinforcement tickets or multi-stage enemies.
- Advanced actions/reactions.
- More aggressive patrol doctrines and regional alert behavior.
- Stronger or wider biome hazards.
- Better, stranger, or riskier reward lanes.
- Loop-only enemies and boss modules.
- Modest bounded health/damage changes.

Because combat has four enemy ranks, do not simulate a horde by stuffing illegal extra bodies onto the line. Spend additional director budget on elite conversion, reinforcements, transformations, phase changes, action variants, and formation complications.

Higher pressure must improve selected rewards enough that detouring remains a real choice. Calibrate at least three strategies over identical seeds: objective rusher, ordinary explorer, and exhaustive/greedy detourer. The best policy must not collapse universally to one of them.

Freeze encounter composition, mutations, stats, targets, and intent promises at the appropriate deterministic boundary. Pressure gained during combat affects future encounters, not already displayed intents. Do not secretly scale to current player HP, current build strength, or a recent win streak. The visible pressure system, biome, objectives, selected difficulty, and disclosed modifiers are the allowed amplitude inputs.

## Milestone 5 — Expand the existing 25 crew into many more party types

Do **not** add archetype 26 in this pass. Twenty-five crew already create 12,650 unordered parties and 303,600 ordered starting formations. More roster entries would worsen the recently repaired selection/onboarding problem.

Interpret “more party types” as deeper combinations of the existing roster.

### Techniques

Author and curate a target of **four genuinely new techniques per existing archetype**, approximately +100 relative to the repository at the start of this pass:

1. One deepener for authored build direction A.
2. One deepener for authored build direction B.
3. One cross-owner bridge using broad shared mechanics.
4. One conditional rare rule breaker.

If the current repository has already added cards, calculate the delta and preserve the four-role requirement rather than blindly targeting an old total.

Each card must pass normalized structural comparison and the anti-bloat criteria above. Revise a redundant card instead of padding the count. Use broad grammar such as mark, wound, vulnerability, guard, riposte, movement, stress, focus, dodge, stun, block, discard, draw, rank, owner, Death’s Door, biome state, pressure, and objective state. Do not hardcode 300 pair-specific relationships.

Card text should normally express one effect plus one condition and fit within the existing 80×24 comparison flow. A new opcode must support multiple cards or other content, not one novelty.

Commit each archetype’s small card batch separately with its tests and validator evidence.

### Bounded mastery branches

Do not fork every card’s upgrade tree. Keep the ordinary one-upgrade ceiling. Select approximately two signature or rare cards per archetype for a mutually exclusive A/B mastery:

- One branch intensifies the owner’s engine.
- The other improves positional reliability, coverage, or a cross-owner bridge.

The choice must be irreversible for that card copy during the run, clearly previewed, saved, migrated, and represented in run history. Existing workshop transformation remains meaningful and distinct.

### Card infusions

Add a small, curated infusion set—roughly 16 or more mechanically distinct infusions if the live design supports it. A card copy may have at most one infusion. Infusions should modify existing rules such as retain, exhaust, rank access, conditional energy, once-per-combat echo, status transfer, or owner/position interaction. They must not create inventory micromanagement or a second paragraph of card text.

Handle copy, transform, upgrade/mastery, removal, save/load, reward preview, and owner death explicitly.

### Doctrines, loadouts, and curated squads

Add roughly 10–12 tag-derived squad doctrines and expand curated squads from five to approximately 12–15. Each doctrine has:

- One disclosed strength.
- One real liability or constraint.
- One concise rule that changes formation or sequencing.
- Compatibility requirements expressed through broad tags/roles, not named pairs.

Cover identities such as dance/movement, mark setup-and-cash, wound attrition, guard/riposte, stress conversion, control, discard velocity, Death’s Door gamble, narrow-rank artillery, and recovery after casualty where the actual content supports them.

Add one advanced alternate starter package or loadout per archetype if it can be made from meaningful existing/new cards without bloating the basic selection screen. Keep the current default package available and the first-run path simple. Horizontal unlocks may reveal advanced loadouts; they must not be strict power upgrades.

Update roster inspection, formation warnings, tutorial/help, combined-deck preview, and terminal navigation. Preserve five-card shared hand, three shared energy, and owner-bound card tension.

## Milestone 6 — Expand items, boons, and curses as interaction engines

Relative to the start of this pass, target at least:

- 18 genuinely new stackable items.
- 12 genuinely new boons.
- 12 genuinely new curses.

Audit and revise existing scalar entries that no longer earn their slot. Preserve IDs or migrate explicitly.

The new items should include a balanced mixture of:

- Simple consistency or glue.
- Threshold engines whose function changes at authored stack counts.
- Duration-refresh or reliability stacks.
- Converters and irreversible corruptors.
- Rare conditional multiplicative/retrigger effects.
- Biome/pressure interactions that alter decisions rather than merely coefficients.

Every item needs authored first-stack behavior and later-stack behavior. More stacks should sometimes change targets, duration, trigger capacity, conversion ratio, or reliability rather than always adding `+1`.

Boons should be unique run-rule modifiers, not renamed items. Curses should include genuine penalties, build-around burdens, and explicit contracts that may later be exchanged for exceptional power. A curse is not required to become beneficial, but its gameplay should be more interesting than a hidden scalar tax.

Add scarce, deterministic ways to convert unwanted stacks or trade immediate strength for later targeting, analogous in function—not fiction—to a scrapper. It must create a real present-versus-future decision and must not deterministically print the perfect build every run.

Commit each small family or mechanic separately. Add exact inspect text and tests with the content.

## Milestone 7 — Enemy, encounter, and mutation expansion

Audit live biome density before setting raw totals. Meet semantic floors:

- At least six mechanically distinct normal enemies native or meaningfully compatible with every biome.
- At least two native elites per biome.
- At least one biome guardian/boss per biome.
- Several recognizable encounter families per biome with alternate legal formations.
- At least three meaningful actions/reactions for ordinary enemies unless a deliberately simple enemy has a documented tactical role.

This will probably require approximately 35–55 new enemy definitions including guardians, but add only the amount the audited floors require. Enrich existing predictable two-action enemies where behavior, rather than roster size, causes repetition.

Build encounters around tactical questions, including:

- Protect or expose a marked payoff striker.
- Break a guard wall or pull its support.
- React to repeated low-cost cards without negating them.
- Use offensive movement to reach a screened backliner.
- Manage wide rank damage and stress.
- Race an enemy that scales by round.
- Choose when killing a linked enemy empowers another.
- Answer cleansing, healing, summons, wounds, block, riposte, or rank disruption through multiple lines.

Visible reactions should use concise markers such as `REACT:3RD CARD`, `WARD:MARK`, `SURGE:ALLY DIES`, or similarly clear language. Intent previews must include conditional normal and empowered values and remain frozen as promised.

### Threat vector

Replace or augment maximum-HP-only encounter pricing with a vector covering:

- Durability.
- Sustained damage.
- Burst/opening spike.
- Control and rank denial.
- Sustain, healing, shielding, cleansing, and revival.
- Reach, area coverage, and target priority.
- Tempo, surprise, setup latency, and reactions.

Estimate expected and credible worst values over the first two or three enemy phases. Add composition costs for mark/payoff, movement/rank punishment, guard/protection, debuff/exploit, screened/flanking arrangements, and overlapping control lock.

Normal and elite encounters need both a total budget and dimensional ceilings. A composition must not be legal merely because its HP is low when it can front-load lethal burst or perpetual denial.

Use a deterministic constraint pipeline:

1. Choose an encounter-plan family.
2. Enumerate compatible candidates.
3. Sort by stable ID.
4. Enforce biome, role, rank, vector, spike, repetition, and coordination constraints.
5. Score behavioral novelty separately from raw power.
6. Make one seeded weighted selection.
7. Freeze and serialize the result.

Avoid random retry loops whose random-call count changes whenever content is added.

### Mutations

Add a reusable set of visible general and biome-specific elite mutations. A practical target is at least 16 distinct modules, with compatibility/exclusion metadata. Mutations should change tactics through reactions, protection, targeting, reinforcement, movement, formation, or status relationships. Pure stat-only affixes do not count toward that target.

Base runs should expose mutations gradually through pressure. High difficulty and loops may combine multiple compatible modules. No mutation may make an entire ordinary build tag universally inert.

Apply repetition penalties across enemy ID, encounter family, formation, coordination plan, mutation, and tactical question—not only exact templates.

Commit enemy content in biome-sized or smaller coherent batches, each with validation and encounter tests.

## Milestone 8 — Biome guardians and multiple finales

Implement one strongly differentiated guardian/boss for each of the 11 biomes, plus multiple possible final bosses. Keep the Overseer Core as one possible finale and add at least three genuinely different final-boss identities if current fiction and architecture permit, for a target of four finales total.

Protect the 30–45-minute standard-run target:

- A base run should normally encounter exactly one biome guardian and one final boss, not all guardians.
- A guardian should replace an objective culmination, elite, or final-gate test rather than becoming a new mandatory detour stacked on top of the old route.
- Other guardians appear across different seeds, choices, challenges, and loops.

Objective outcomes, completed biomes, layout, pressure, and selected difficulty may weight the guardian/finale and alter modules. Reveal the likely final pressures after the first completed objective and the exact finale or sufficiently precise profile after the second, early enough for meaningful drafting and transformation.

Every guardian and finale must have:

- A clear tactical thesis.
- At most one major rule break active at a time.
- Multiple telegraphed phases or modules where appropriate.
- Stable intent and reaction ordering.
- Several viable answers rather than a required keyword.
- Partial resistance, timing pressure, or alternate targets rather than blanket immunity.
- Objective- and difficulty-dependent behavioral variants before raw stat inflation.
- Distinct five-line or larger legal ASCII presentation that remains readable at 80×24.

Boss phases must not be skipped accidentally by overflow unless a deliberately earned mechanic says so. If excess damage is bounded or carried between phases, disclose the rule. Do not silently erase an explosive build’s entire payoff.

Add deterministic scenario tests and natural play for every guardian/finale family. Commit each boss and its tests atomically.

## Milestone 9 — Horizontal progression and sustainable replay modes

Add a separate, versioned profile with atomic writes and migrations. It may record history and unlock alternatives, but never permanent combat power.

The game must remain fun and winnable on a fresh profile. Keep the existing 25 crew available unless a later repository decision explicitly says otherwise. Unlock higher-complexity alternatives:

- Advanced techniques and infusions.
- Alternate starter packages.
- Doctrines and curated squads.
- Bosses, mutations, challenges, and optional content packs.
- Lore, compendium entries, terminal titles, alternate ASCII portraits, and records.

Newly unlocked content must not be a strict power replacement for old content. Let players inspect active packs and control them in custom play so global growth does not permanently dilute every pool.

### Twenty-rank ascending ladder

Implement the previously selected long, cumulative, global 20-rank difficulty ladder. Unlock the next rank on a win. Do not require a separate 20-rank climb for every crew or party; track per-crew/preset bests for recognition only.

Each rank must state one concise change. Use major anchor ranks that transform play, with smaller adjustments between them. A sensible distribution is:

- Ranks 1–4: pressure and expedition economy.
- Ranks 5–8: encounter vector budgets, formation, and elite frequency.
- Ranks 9–12: advanced actions, coordination, and mutations.
- Ranks 13–16: objective, hazard, patrol, and route consequences.
- Ranks 17–20: guardian/finale variants and overlapping endgame rules.

Do not make the ladder twenty copies of `+HP/+damage`. Verify each rank’s text, save/profile state, deterministic effect, unlock, and UI.

### Expressive modes

Keep ranked progression separate from expressive challenges. Add:

- Approximately 15–25 authored contracts that teach or invert real mechanics.
- A custom expedition with seed, party, loadout, doctrine, biomes, layout, pressure, content packs, and compatibility-validated modifiers.
- An offline daily expedition derived deterministically from calendar date, ruleset version, and a fixed documented salt.
- Short versioned challenge codes containing seed and modifier configuration, with strict parsing and compatibility checks.

Do not add servers, accounts, networking, remote leaderboards, or online dependencies. Local records are sufficient.

### Run history and compendium

Expose run records, best ladder rank, guardian/finale discoveries, item/boon/curse/card discoveries, objective approaches, challenge completion, and a casualty graveyard through readable terminal screens with search/filter/scroll behavior appropriate to 80×24.

Achievements or contracts should encourage unfamiliar play and reveal system depth, not demand grind. Examples include winning after a crew death, using both approaches to a biome objective, completing a run with a particular broad engine, or answering an encounter through positional play.

## Milestone 10 — Optional post-victory loops for broken builds

After a final boss dies:

1. Record the base victory permanently and atomically.
2. Offer `EXTRACT` and `DESCEND AGAIN`.
3. `EXTRACT` ends normally.
4. `DESCEND AGAIN` preserves the surviving party, deck, masteries, infusions, items, boons, curses, and consequences, then enters a deterministic remixed loop with a sharply higher pressure floor.

A loop may:

- Select new or corrupted biome regions.
- Intensify hazards and patrol doctrines.
- Add compatible mutation slots and coordinated plans.
- Introduce loop-only enemy/action pools.
- Offer paired exceptional power plus explicit burden.
- Use mutated guardians and alternate finales.
- Shorten its access objective so a loop does not simply repeat the entire base run verbatim.

Death after looping does not erase the recorded base clear. Track loop depth, pressure peak, score, party, build engines, boss sequence, and cause of end. No essential content unlock may require a deep loop.

Endless scaling may eventually become unfair; that is acceptable. It must remain deterministic, integer-safe, saveable, inspectable, and capable of ending. Ensure absurd numbers render compactly without losing exact values in inspection.

## Save, RNG, and migration requirements

- All simulation randomness must remain inside the simulation. Rendering, animation, speed/skip, help, inspection, resize, and mouse movement must not consume it.
- Use stable enumeration and named RNG domains where helpful: world, biome selection, encounter composition, patrol, combat intent, combat resolution, reward, event, boss, pressure, loop, and challenge.
- Never derive seeds from Python’s randomized `hash()`. Use a stable canonical byte encoding and a documented `hashlib` digest if creating substreams.
- Preserve current `random.Random` state compatibility within the supported Python policy. Version any RNG architecture change.
- Use explicit pure migration chains for run saves and profiles. A migration accepts exactly one version and produces exactly the next, with validation.
- Never silently regenerate missing durable state from current content.
- Store content fingerprint and enabled pack IDs where needed to prevent ambiguous replay.
- Write saves/profiles through a temp file in the destination directory, flush/fsync where the current design warrants it, and use atomic same-filesystem replacement.
- Keep golden fixtures for supported historical saves.

## Testing and verification

Add or extend automated tests at these layers:

### Content and semantics

- Strict JSON loading, duplicate-key and non-finite rejection.
- Referential integrity, ranks, targets, owners, opcodes, statuses, upgrades/masteries, transforms, infusions, packs, doctrines, bosses, and challenges.
- Honest tags and derived synergy edges.
- Structural duplicate detection for new cards/effects.
- Producer/payoff density and no orphan mechanics.
- Reward-lane eligibility and skip behavior.

### Trigger and stack safety

- Trigger dependency cycles and required limiters.
- Stable phase/priority/entity/effect ordering.
- Listener snapshot behavior.
- Root-event budget drainage and reproducible `CHAIN SEALED` traces.
- Every stack mode, current/next preview, cap/soft cap, monotonicity, and integer safety.
- Copy, mastery, infusion, transform, owner death, and save/load interactions.

### Encounters and pressure

- Threat-vector budgets and dimensional ceilings.
- SET/CASH and reaction previews.
- Formation legality and actor-attached frozen intents.
- Mutation compatibility/exclusion.
- Pressure source accounting and threshold forecasts.
- No pressure from UI-only actions.
- Rusher/ordinary/greedy policies over identical seeds.
- Repetition metrics across family, formation, plan, and mutation.

### Replay and persistence

- Same seed plus same commands produces identical canonical state hashes and event logs.
- Reordering JSON object keys or content enumeration does not change a frozen run.
- Save/load at travel, route cancellation, objective, facility, reward, combat phase, trigger chain, death, guardian phase, final victory, extract, and loop boundaries.
- Uninterrupted continuation equals checkpointed continuation.
- Multiple `PYTHONHASHSEED` values produce identical snapshots.
- Run history/profile writes and migrations are atomic and deterministic.
- Daily seeds and challenge codes are stable and versioned.

### Fuzzing and combinatorial coverage

- Add deterministic random legal-command fuzzing and check invariants after every command. Preserve failing seed and command transcript.
- Use targeted three-way coverage for high-risk interactions such as trigger × stack × owner death, mutation × formation × intent target, and save boundary × pressure threshold × objective state.
- Do not pretend pairwise coverage proves all interactions.

### UI and real terminal

- Warning-enabled compilation and complete content validation.
- `git diff --check`.
- Real PTY checks at 80×24 and at least one large size such as 140×60.
- Roster/loadout/doctrine, reward, stack, mastery, infusion, pressure, intent/reaction, boss, run history, compendium, challenge, and loop screens.
- Keyboard-only completion. Mouse documentation must match actual scope.
- Animation speed/skip and terminal resize must not alter state.

### Generation and ordinary play

- Run broad seed sweeps covering all six layouts, all eleven biomes, all guardians, all finales, pressure bands, encounter families, mutations, objectives, facilities, and loop generation.
- Exercise every new effect opcode and every stack rule in deterministic scenarios.
- Perform multiple unmodified natural base runs across materially different parties and worlds. The final evidence should include at minimum:
  - an objective-rush attempt,
  - an ordinary explorer,
  - a greedy detourer,
  - at least two natural base victories across different parties if feasible,
  - at least two fair losses with understood causes,
  - a crew death followed by continued meaningful play,
  - one naturally assembled or honestly acquired high-power engine,
  - an extract after victory,
  - a descend-again transition and subsequent loop completion or fair loop death.
- Use constructed builds to stress maximum trigger depth, conversions, stack counts, mutation combinations, and boss phases, but label them as constructed.

Initial combat-duration hypotheses remain approximately 3–5 rounds for normal fights, 5–8 for elites, and 8–12 for bosses. Treat them as tuning hypotheses, not invariants. Do not claim a robust win rate, universal party viability, or proven fun from a small sample.

## Balance and success metrics

Report distributions and stratify by party, doctrine, biome, layout, pressure, difficulty, and boss where possible. Track at least:

- Exact encounter-family and tactical-question repetition within a run.
- Card offer, pick, skip, play, upgrade/mastery, transform, infusion, and removal rates.
- Rank-invalid or owner-disabled hand clogging.
- Producer availability and payoff activation.
- Energy spent, cards/actions per turn, trigger-chain length, and overkill.
- Damage, block, healing, wasted block, stress, wounds, and Death’s Door checks by source.
- Encounter rounds, incoming expected versus actual damage, and control-lock streaks.
- Objective approach, route cost, projected/actual light and pressure, and abandonment.
- Base boss reach, base win/loss causes, duration, crew deaths, and loop depth.
- Dominant build-tag combinations and whether one easy line suppresses alternatives.

Provide several deterministic headless policies if architecture permits: objective rusher, reward detourer, greedy offense, conservative defense, tag/synergy maximizer, and position-aware. Compare identical seed cohorts before and after changes. These policies are regression instruments, not proof of fun.

## Explicit prohibitions

Do not:

- Add new crew archetypes in this pass.
- Add real-time pressure to a turn-based terminal game.
- Balance base difficulty around loop builds.
- Make difficulty primarily HP/damage inflation.
- Add universal build-tag immunity or undisclosed hard counters.
- Hide arithmetic, trigger order, stack behavior, pressure prices, or boss-defining rules.
- Let automatic proc/retrigger chains recurse without a deterministic limit.
- Globally cap damage solely because a build is powerful.
- Add named-card Exodia pairs or hundreds of named crew-pair exceptions.
- Add a new keyword for each card.
- Put all content into one reward pool.
- Make unlocked content a strict power upgrade over fresh-profile content.
- Add online accounts, telemetry uploads, leaderboards, dependencies, or services.
- Add food, ammo, crafting, stealth, factions, or unrelated survival systems in this pass.
- Lengthen every base run merely because more bosses/content exist.
- Fake natural play evidence through injected state, one-HP enemies, or extra energy.
- Claim “infinite replayability,” “perfect balance,” or universal viability.
- Push or rewrite git history.

## Documentation and final handoff

Create or update:

- `docs/RESEARCH_NOTES.md`
- A detailed implementation plan for this pass.
- A content/design reference for pressure, stack modes, triggers, acquisition lanes, doctrines, masteries, infusions, mutations, bosses, difficulty ranks, challenges, and loops.
- A save/profile/RNG migration reference.
- A verification report containing commands, exact results, seed cohorts, natural run narratives, limitations, and all content deltas.

The final response must lead with the implemented outcome and include:

- Starting and ending full commit SHAs.
- Final branch/status and whether the tree is clean.
- A concise list of major systems and content added.
- Old → new counts, plus semantic density floors.
- Atomic commit list grouped by milestone.
- Exact automated test totals and durations.
- Seed-sweep, fuzz, cross-process, save/load, PTY, and natural-play evidence.
- Base-run duration and balance evidence, clearly separating natural and constructed runs.
- Known limitations and unproven claims.
- Any external/user commits encountered.
- An explicit statement that nothing was pushed or history-rewritten.

Do not stop at an attractive design document. Implement, test, play, revise, commit atomically, and leave the repository clean.


