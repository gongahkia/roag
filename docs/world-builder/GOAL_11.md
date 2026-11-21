# Goal 11 · A connected region

**Implementation in progress.** This record is not a completion or deployment
claim. Goal 12 has not started. The requested Sudo/Debug panel and 20× controls
are explicitly deferred until Goal 11 is complete.

## Baseline and preservation

Started from clean `7e013b9e5c33db4197443c6cb3b571c86cabc708`, on
`feat/settlement-goal-10`, including the approved right-hand sidebar. Work is on
`feat/settlement-goal-11`; automatic workspace checkpoints and prior release
branches are retained. The latest existing published application workflow is
[44350273041](https://github.com/gongahkia/roag/actions/runs/44350273041) on
`release/settlement-goal-10` at `3564b070`.

The current baseline reran **214 passing Flutter tests**, the app validator
(47 crops, nine bodies × four palettes), and original Lua bindings (599 roles /
67 source PNGs). The catalogue command reran 48 tests with one failure in its
tracked-media ownership allow-list, reproducing its omission of existing derived
settlement artwork and evidence. It is not an entirely green catalogue baseline.

Initial preservation fingerprints were captured before editing for all six
READMEs, 238 Lua files, 755 original asset/catalogue-related files, 86 existing
app PNGs and four original Lua storage files. Private browser profiles are not
used. All new browser journeys must use disposable contexts and explicit fixtures.

## Geography and terrain

`region_geometry.dart` owns regional generation version 1. It reuses the existing
canonical numeric/text/UTF-8 seed semantics and a separate Park–Miller stream with
domain 715225739. Ten sites occupy a jittered, coherent west-to-east network with
16 weighted links and alternate routes. IDs are stable `site-00` through
`site-09`, independent of display names. Site terrain seeds use a separate bounded
formula; geometry draws do not advance any simulation RNG. The capital retains
its exact existing world. Concrete regional geography is serialized and validated,
never regenerated on load. Travel uses actual graph paths restricted to discovered
sites and open connections.

The original six environments keep their version-2/3 generation paths. Two
version-4 environments are being integrated:

- **Pine Uplands:** dense 60% ranked conifer woodland, rocky ridge/pass rules,
  wet threshold 18 and rock threshold 36. The protected opening remains viable.
  More wind and winter snow affect actual travel. Standing forest enables and
  improves forestry production.
- **Salt Coast:** a coherent northern shoreline with a three-cell inland inlet,
  wet coastal approaches and ordinary short bridge crossings. Forest 22%, wet
  threshold 45 and rock threshold 14. Wind/rain have increased weights; prolonged
  rain waterlogs marsh travel using the existing maximum-penalty rule.

The audited spruce (`urizen-onebit-r08-c003`, `[40,105,12,12]`) and obelisk
(`urizen-onebit-r25-c035`, `[456,326,12,12]`) are two new derivative exports from
the unchanged Urizen source. The latter is explicitly cast as a Wayfarer Monument,
not a claim about the original drawing's gameplay purpose. The Lumber Camp uses
a warehouse/oak composition from already audited frames. No source sheet or
catalogue record is edited.

## Founding, identity and clocks

Village recognition requires four residents, at least three coins from actual
work and a staffed connected Market. Initial neighbouring discovery costs two
capital coins. Farther discovery requires two towns and a completed delivery.
Founding costs 60 capital coins: 50 transfer to the new town and ten fund its
expedition. It creates the selected site's concrete world exactly once. Free
Build relaxes these costs and development gates. New towns share the regional
date and inherit incident consent, with their own terrain/weather/event streams
and introductory incident grace.

Separate town treasuries preserve the existing construction budget, refund and
forward Undo/Redo semantics. The region provides a combined summary; trade handles
payments automatically. No second currency or manual transfer interface is used.

The capital clock orders every town through the original 50 ms colony step.
Inactive towns run the same pure authoritative model, without another Flame world,
rendering, camera, speech presentation or per-frame animation. This initial
contract uses exact model work, not inferred successful activities. Switching does
not advance time. Positions, trips, employment, incidents and session-local
construction histories remain owned by each existing Colony instance. Performance
and practical limits still require final measurements.

## Production and shipments

Real qualifying work produces bounded town stock: Provisions from Farms, Crafted
Wares from Workshops, and Materials from a staffed Lumber Camp with standing
forest within three Manhattan cells. Actual weather and condition output are
passed through once. Forest abundance improves forestry; removing its source or
access stops production. Clear itself grants no Materials. Managed standing
woodland is a renewable source while worked. Stock caps at 24; surplus work does
not become an unlimited latent backlog.

A relationship alternates fairly among three goods and both directions. It
reserves at most three surplus units and the buyer's real coin payment. A pack
visitor legally walks from the origin entry to a staffed Market, collects for two
seconds and walks back out. Only actual departure begins regional travel. Distance,
terrain, weather and closures affect that journey. At the destination, a real
visitor must enter, reach a staffed Market and unload for two seconds before goods
and payment are delivered once. Existing resident work income remains separate;
trade payments transfer coins between towns, rather than minting a second work
payment. Imported goods are not automatically re-exported.

Shipment IDs and loading/transit/unloading/delivered/returned/cancelled/lost states
persist. Uncollected cancellation restores stock and escrow once. A failed local
stop can return along an actual regional journey and unload at the original Market.
Prolonged Standard-mode obstruction can lose collected cargo after 180 blocked
seconds, refunding buyer escrow without paying the exporter. Peaceful mode does
not automatically destroy cargo. Local stranded actors retreat only along legal
roads. Disabling a relationship stops new dispatches; collected journeys finish.

Imported Provisions are consumed by a real populated, connected Market town for
a bounded +3 mood benefit. Consumed Wares create opportunities for the existing
visitor pipeline, which still requires actual arrival/service for any ordinary
reward. Materials support optional projects. Regional interruptions derive from
actual snowbound passes and the existing incident director; no unrelated random
threat scheduler is added.

## Current verification and remaining integration

The first 13 regional/lifecycle tests passed, including a 600-region seed sweep,
three-town ownership, exact legacy Colony preservation, frame-rate independence
and equal standalone/background earnings. Six production/trade tests then passed:
real production and source loss/replanting, bidirectional delivery and zero-sum
accounting, exact in-transit continuation, one-time cancellation, a genuine
Peaceful return journey, and Standard obstruction loss/refund.

Still to finish and verify: regional UI and save plumbing, progression/project
integration and rendering, full compatibility/asset gates, larger multi-town
performance fixtures, all release-browser journeys and moving evidence, public
publication through the existing Pages workflow, and the final preservation audit.
