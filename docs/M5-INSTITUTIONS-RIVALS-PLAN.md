# M5 institutions, actors, and recurring rival implementation plan

## Scope and canonical basis

M5 adds one compact autonomous social simulation to the existing M1–M4 campaign. It does not replace the Route Board, sealed-package custody, the five destination partitions, or transit. `LORE.md` supplies the exact authored anchor at Nerida:

- the human-led **Nerida Port Continuity Office**, which claims emergency authority over imported machinery;
- the mixed Taal-human **Closure Eight Cooperative**, which seeks independent pump-control custody;
- the mixed-resident **Blue Intake Residents' Board**, whose habitats flood first;
- **Examiner Iren Vos**, a customs official whose enforcement and selective confiscation serve a personal career as well as the Port Office.

This uses humans and Taal as distinct political actors without treating either civilization as a faction. The degrading pump bank and its autonomous repair organisms are an Orphan Work maintenance failure with material inputs, outputs, liability, and ecological effects; no mystical explanation is introduced.

## Versioned durable model

`GalaxyState` moves from v4 to v5 and gains one `institutionWorld` record. Authored definitions remain source-only data keyed by stable institution IDs. The saved record holds only mutable campaign truth:

- ordered institution presence, capacity, current concern, inter-institution relation, and a multi-axis Jomon standing (`trust`, `scrutiny`, `obligation`, `grievance`, and local `influence`);
- a bounded stable-ID operation queue and resolved-operation deduplication set;
- stable named actors with authored name/role grammar, civilization, affiliation, rank, location, status, methods, bounded personal memories, and vessel-level relationship information;
- a bounded causal-event graph, with stable IDs, parent IDs that always point to older events, source/knowledge provenance, and compact structured effects;
- bounded reports that remain separate from world truth, carrying source, subject, mark, freshness basis, confidence, and authored bias;
- one rival record keyed to Iren Vos, including emergence cause, active status, original courier where applicable, and successor state.

The only initial actors are Iren Vos, Closure Eight’s Taal maintenance delegate Sava Tesh, and Blue Intake’s human emergency clerk Mera Lio. Their IDs, names, origins, roles, and first assignments are authored/stable rather than randomly combined. The actor model remains capable of neutral contacts and conditional partners, not only enemies.

All collections have explicit limits: 12 active operations, 24 resolved operation IDs, 320 causal events after reference-preserving compaction, 16 reports, 6 actors, 8 actor memories, and one successor per institution vacancy. The causal limit accommodates every bounded Manifest, operation, relationship, report, and actor-memory reference. Retained state referenced by an unresolved operation, current relationship provenance, actor memory, or General Manifest entry is never removed merely to meet a history bound.

## Deterministic canonical boundary

`advanceGalaxyRouteReckoning` is the sole simulation caller. It first preserves the existing package/deadline and M4 destination advancement behavior, then passes the target mark to an institution scheduler. The scheduler resolves due operations in `(due mark, operation ID)` order after sorting stable IDs. It never acts per render frame, UI opening, report inspection, or reload.

Each operation has a stable ID, institution/actor, target, initiation mark, purpose, completion rule, effects, visibility, report rule, and causal parents. Random variation, where an authored operation needs it, is derived with `rngFor(seed, 'institution', institutionId, operationId)`; no shared random stream or iteration order can perturb another institution. Repeated or reloaded advancement checks resolved IDs before applying effects.

v1–v4 migration initializes the institutional world at the exact saved Route Reckoning mark with future operations only, sets its processed Manifest watermark to the current Manifest sequence, and does not replay historical events or simulate closed/offline time. v5 normalization validates and bounds current data idempotently.

## Initial operations and destination interaction

The scheduler deliberately uses a small authored queue:

1. The Port Office issues an emergency import inspection/requisition after the Nerida pump crisis is active. It creates a hidden authoritative restriction first; observation at Nerida or a later institutional notice can make it known. A known restriction alters only the affected Route Board connections’ warning, risk, and duration.
2. Closure Eight can prepare a pump-bay repair after Jomon assists or after a Port Office restriction creates a custody conflict. The repair lowers the existing Nerida `pump-wear` partition pressure and records a persistent local-operation consequence. It is material cooperation, not permanent alliance.
3. The Blue Intake Board can file a habitat-intake relief operation. It has an authored ecology effect: it diverts closure labour and filtered feedstock to prevent autonomous repair organisms from clearing inhabited intake structures. Its resolution changes the same Nerida partition’s pressure/opportunity and records a report; it does not reduce Taal people to an ecology role.
4. If the Jomon obstructs or refuses the Port Office, the Office can issue an access audit. That operation differs from Iren’s direct inspection and produces a persistent Port/Closure conflict consequence at Nerida.

Operations work through the M4 partition data: they modify pump pressure and append/deactivate `DestinationConsequence` entries using M4’s bounded condition boundary. They do not create a parallel population or ecology simulator. Existing sixty-mark bypass installation and its verification retain their exact costs, condition transitions, and tests.

## Knowledge, reports, and Manifest

Institutional truth is initially hidden. Arrival, local institution inspection, a known notice, or direct encounter can add a report. Reports give a source, subject, report mark, confidence, and small authored bias label; no operation queue, exact values, hidden actor location, or hidden RNG appears in normal UI.

The General Manifest receives exactly-once entries only for facts learned by Jomon: institutional notice, direct institutional decision, rival emergence, direct rival encounter, known operation, relationship-changing result, and known status/succession. Entries contain stable institution, actor, destination, courier, package, and causal-event references where relevant. The Manifest stays chronological; the dossier is a current-context view.

## Relationship and rival rules

Standing is not a single reputation score. Each institution independently tracks:

- `trust`: expected reliable custody and compliance;
- `scrutiny`: procedural attention and likely inspection;
- `obligation`: unpaid reciprocal work or duty;
- `grievance`: institutional injury or obstruction;
- `influence`: current ability to act at Nerida.

Every adjustment cites one causal-event ID and is deduplicated. Intact delivery, seal breach, refusal, abandonment, and expiry map to distinct Port/Closure effects when they occur after M5 adoption. Existing package mechanics remain authoritative; M5 observes their new Manifest records rather than exposing package contents.

Iren Vos emerges only from a causal trigger. The two supported paths are a confirmed package seal breach and the existing Nerida bypass (interference with the Port Office’s asserted emergency authority). The first qualifying new record creates exactly one `rival:iren-vos` event, names the original courier if present, and stores vessel continuity separately. Later triggers add memories rather than duplicate the rival.

Vos adapts from remembered categories:

- a seal breach produces an evidence-inspection method and a known route inspection after discovery;
- bypass interference or later refusal produces a bypass-custody audit with different local leverage;
- later compliance reduces the audit’s route delay but retains the documented professional objective;
- assisting Closure increases Office grievance while creating Closure obligation, which can cause a repair rather than an automatic friendly outcome.

The two action forms are route inspection and local custody audit/requisition. Both are procedural and non-combat. Memories carry stable deduplication IDs, causal references, first-hand/report provenance, and bounded summaries.

Courier death appends its existing Manifest record. The institution reconciliation records a personal loss memory for Vos if relevant but keeps the institutional conflict with Jomon at vessel level. Selecting an available replacement courier retains the dead courier’s state and changes the dossier’s representation record. A deterministic disgrace/reassignment path can appoint one new Port examiner after a delayed vacancy. The successor receives a briefing memory that names its report source, never a first-hand memory it could not have witnessed.

## Player surface and decision

An unclaimed hub `N` action opens the keyboard-accessible **Institutions & Contacts** dossier. It uses the existing text renderer in both ASCII and detailed modes and shows only public posture, known jurisdiction, qualitative standing, current known concern, known reports, named contacts, known rival role/status, and report freshness. It includes no colour-only state and no new art.

At Nerida, once the Port notice or rival is known, `E` opens a compact confirmation state. `C` complies with the custody request, `R` refuses, and `A` authorizes sixty-mark Closure assistance. The surface states each immediate known term; `Enter` confirms and `C`/`Esc` cancels. The confirmed choice creates one causal event, one once-only relationship adjustment, relevant actor memories, a Manifest entry, and a later scheduled institutional response. Hidden consequences remain hidden.

Known Port or Iren operations extend `routeBoardConnectionForGalaxy` without changing route topology or mutating on inspection/cancellation. A confirmed transit persists its derived duration as before.

## Verification

Focused tests will establish deterministic initialization/IDs, no offline work, batching/order/RNG invariants, bounded exactly-once operations, partition and route effects, report secrecy, all package outcome relationship differences, rival emergence through both paths, memory/adaptation/persistence, courier death/replacement continuity, status/succession, causal acyclicity, non-mutating inspection, decision persistence, M4 Nerida preservation, M1 Kestrel preservation, v4-to-v5 migration, and absence of new legacy terminology.

`scripts/institutions-rivals-soak.ts` will compare whole and chunked campaigns through a package breach, travel, an institutional decision, repeated operations, courier death/replacement, save/reload, and deterministic durable-state fingerprints while asserting all bounds. The autoplay catalogue and one Playwright flow will cover dossier discovery, rival emergence, later known influence, decision confirmation, Manifest history, and normal Route Board/package behavior.
