# Regional questlines, tactical encounters, and build content

## Baseline

Work begins from clean `main` commit
`0af193e4b9882d44d5227bea0f0cca112834d19d`, equal to `origin/main`. The
unchanged standard-library build ran 129 tests in 277.165 seconds with no
failures. The older 126-test figure in the living-vessel assessment describes
an earlier point in that milestone; the active tracker correctly reports 129.

The game already has four persistent regions, 26 containers, 14 weapon
definitions, 18 armour pieces, 30 passive definitions, six regional recruit
techniques, five relics, eight drinks, perceptual enemy state, a regional
encounter composer, and a spatial physical inventory. Repository inspection
shows that definition count currently exceeds ordinary-play depth: some
effects are descriptive, the composer is audit-facing rather than used for
normal placement, treasure clues are sparse, objectives share one cargo
shape, and several physical-state reconciliation paths need regression tests.

## Bounded implementation sequence

1. Repair recoverable objectives, physical possession reconciliation,
   transactional cancellation, finite consumable/ammunition authority,
   selected relic use, reconstructed environmental state, and format-5 to
   format-6 migration.
2. Give every retained passive and regional recruit technique an observable
   production hook; expose bounded armour and status trade-offs; make the pike
   and heavy crossbow obtainable.
3. Use the existing regional budget composer for persistent finite production
   groups and implement perceived, readable enemy goals plus explicit ranged
   target selection and constrained negotiation.
4. Author one three-stage material questline per existing region, activate
   secondary contacts and causal recruitment terms, then unlock one four-to-six
   chapter cross-region arc after meaningful progress in two regions.
5. Add one alternative rule-changing elite per region, improve three distinct
   treasure clues per region, and add only bounded weapons and rewards that
   complete twelve verified cross-system builds.
6. Verify serially with focused tests, the full test suite, compilation,
   whitespace checks, encounter/quest seed audits, and real curses PTYs at
   80×24 and 100×32. Record only branches and builds actually exercised.

## Architectural and content limits

`GameState` remains the persistence root. Quest progress is a small set of
authored records and direct regional actions, not a quest engine. Enemy
decisions continue to select from concrete legal actions using perceived
facts, not a planner or omniscient controller. Encounter composition remains
bounded by the four authored regional pools. No region, faction simulator,
economy simulation, real-time advancement, network dependency, generic DSL,
or unlimited magic is added.

The new-world bargemaster remains equipped at Jomon's gangplank and can enter
Hearthford immediately. Deeper systems are discoverable choices, never
mandatory preparation chores.

## Verification record

Implementation, audit, PTY, balance, and candid play findings will be recorded
here as the milestone advances.
