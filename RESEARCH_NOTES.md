# Research notes for the initial Cosmonauts implementation queue

Checked 19 September 2026. This is a design-research memo, not a claim that the live Cosmonauts repository was inspected or tested. The complete game is not being specified from external sources: owner decisions define the direction, the supplied packet describes the starting source, and the numbered prompts select Cosmonauts-specific implementation contracts.

## Repository evidence supplied by the owner

The attached packet describes a single-threaded, whole-local-map simulation; explicit command-before-simulation tick ordering; stable local worker/job IDs; seven labour duties; body-aware BFS; material/food/water bookkeeping; bounded data-only full saves; and challenge/practice history policies. It also identifies current absence of pressure/temperature, crafting, developed combat, and world extension. These are observations of its named snapshot, not proof of current HEAD.

The packet explicitly says new authoritative state needs validators, defaults, codec/clone coverage, command/replay handling, accounting, map-versus-save decisions, UI, and tests. The prompts apply that existing contract to each feature. They preserve the legacy save identity and map-format identifiers. The permanent finite-map design language is superseded by the owner's later multiworld decision, not erased from the historical source.

See `REFERENCE_REPOSITORY_PACKET.md`, especially its sections 3, 5, 6, 9, and 10. No claim in this pack implies the remote author ran those tests.

## R1 — Simultaneous managed settlements

**Primary source:** Tynan Sylvester/Ludeon Studios, *RimWorld Alpha 16 – Wanderlust released!*, 20 December 2016.

https://ludeon.com/blog/2016/12/rimworld-alpha-16-wanderlust-released/

The developer's release notes describe simultaneous local maps, multiple colonies, location-grouped colonist switching, and actual movement of people/supplies via caravans or transport pods. They also explicitly mention performance/balance limits on the optional multi-colony setting.

**Application here:** use simultaneous controllable settlements and physical transfers as the control model. Cosmonauts's campaign root, three-site test cap, spaceship rules, inventories, and save architecture are our design choices, not claims about RimWorld internals. The source is historical implementation evidence; it is not used to claim current limits across every modern expansion/mod.

## R2 — Related procedural outputs rather than disconnected labels

**Primary source:** Brian Bucklew and Jason Grinblat/Freehold Games, GDC 2019 session page, *Math for Game Developers: End-to-End Procedural Generation in Caves of Qud*.

https://www.gdcvault.com/play/1026313/Math-for-Game-Developers-End

The published session overview describes generating villages with linked histories, cultures, architecture, inhabitants, traditions, and quests, and connecting systems at different granularities. The overview was read; this memo does not claim to have watched an entire video or inspected its unavailable implementation.

**Application here:** the future generator should produce stateful relationships with usable consequences. The first queue establishes stable identities, local material facts, and personal evidence before adding large culture/faction catalogues. This does not imply that a large culture generator has already been designed or implemented by P06.

## R3 — Composition and authored foundations

**Primary testimony:** Brian Bucklew and Jason Grinblat interviewed by John Harris, *Tapping into the potential of procedural generation in Caves of Qud*, Game Developer, 10 March 2022.

https://www.gamedeveloper.com/design/tapping-into-the-potential-of-procedural-generation-in-caves-of-qud

The developers discuss combining authored and procedural material and describe an emergent content possibility from attaching an existing behavior component to furniture. This is direct developer testimony within an interview, rather than a claim inferred from playing the game.

**Application here:** favor reusable behaviors and stable typed content definitions. A new combination can be data; a genuinely new mechanism still requires code/tests. This does not require replacing Cosmonauts's existing modules with a universal ECS. No copyrighted game names, histories, species, or specific component implementation are copied into Cosmonauts content.

## R4 — Deterministic replay is exact, not approximate resemblance

**Primary technical source:** Glenn Fiedler, *Deterministic Lockstep*, 29 November 2014.

https://gafferongames.com/post/deterministic_lockstep/

Fiedler describes exact state agreement from identical initial conditions/inputs, sensitivity to ordering and random state, and the limits of assuming floating-point determinism across builds/platforms.

**Application here:** require same-runtime canonical state equality across replay, saves, camera schedules, and transport boundaries. Preserve explicit fixed ticks and ordering. Do not claim cross-runtime identity because one machine passed tests. This is a single-player replay design; the networking discussion does not authorize implementing multiplayer.

## R5 — Lua iteration and sorting need explicit order

**Primary specification:** Lua 5.1 Reference Manual, especially `next`, `pairs`, table length, and `table.sort`.

https://www.lua.org/manual/5.1/manual.html

The manual does not specify `next` enumeration order, defines `pairs` in terms of `next`, warns about sparse-table length ambiguity, and states that `table.sort` is not stable.

**Application here:** use dense arrays/stable IDs for order-sensitive simulation, avoid sparse-array length assumptions, and include complete tie-breakers. An unordered traversal can still be valid for a genuinely order-independent validation/sum; the prompts do not demand rewriting every existing `pairs` call regardless of relevance.

## R6 — Target runtime compatibility

**Primary documentation:** LuaJIT, *Extensions*.

https://luajit.org/extensions.html

LuaJIT documents its Lua 5.1 compatibility and extensions. The owner's packet reports LuaJIT 2.1 and LÖVE 11.5, with no `lua` command in that inspected environment. Current local availability must be checked rather than assumed unchanged.

**Application here:** keep ordinary LuaJIT-compatible syntax, headless core tests, and no mandatory FFI/engine-only state. The prescribed campaign seed arithmetic uses small exact integers and no Lua-5.3-only bitwise syntax. It does not change the existing generator's RNG.

## R7 — Seed and RNG state are different

**Primary API reference:** LÖVE 11.5 API reference maintained in the LÖVE community's API documentation, `RandomGenerator:getState`, `setState`, and `getSeed`.

https://love2d-community.github.io/love-api/

The reference distinguishes the initialization seed from the generator's current state and documents state strings as opaque/implementation-dependent, for restoration in the same major LÖVE version.

**Application here:** persistent random processes need their actual stream state, not only the starting seed. The new campaign helper is plain Lua so headless tests do not require LÖVE userdata; this choice is not a claim that LÖVE cannot produce deterministic random numbers. One-shot terrain generation stores its derived seed/recipe and the actual generated initial world.

## R8 — Fixed simulation, independent presentation

**Primary author text:** Robert Nystrom, *Game Programming Patterns*, Game Loop chapter (author's source repository).

https://raw.githubusercontent.com/munificent/game-programming-patterns/master/book/game-loop.markdown

The chapter explains separating fixed simulation steps from presentation timing and discusses accumulation/catch-up constraints.

**Application here:** retain the existing fixed-step local engine and move campaign orchestration around it. Rendering and site switching cannot advance time or decide which local worlds run. Existing catch-up limitations remain an app scheduling concern, not permission to skip selected worlds' authoritative ticks.

## Calculations and choices that are ours, not source claims

The campaign-only RNG uses modulus 2,147,483,647 and multiplier 48,271, with a 131-based byte namespace fold and rejection sampling for bounded integers. The largest multiplication is below 2^53; this is an arithmetic property of the specified numbers. Its reference vectors are supplied in `RNG_REFERENCE_VECTORS.md`. It is not cryptographic, does not promise collision-free namespace hashes, and does not establish cross-runtime determinism for the rest of the game.

The tick ordering, custody/accounting equations, route durations, checkpoint count, cargo capacity, maintenance-part abstraction, fact/evidence thresholds, school modes, and learning/XP arithmetic are explicit Cosmonauts prototype design choices. Research motivates the invariants, but it does not empirically validate their balance or performance. The local acceptance tests and subsequent play review must do that.

## Research boundary

No claims about Rain World's internal scheduler or current Dwarf Fortress expansion behavior are needed to implement these tranches. Their broader influence remains in the owner's design intent. Do not treat earlier conversational comparisons as permission to import unverified implementation details.

The research required for future gravity/atmosphere models, generative faction diplomacy, rich personal memory, larger automation chains, remote simulation abstraction, and long-range relic progression is deliberately not declared complete. It belongs in the remote design stage before those later prompts, not in an instruction to Terra to invent those systems now.
