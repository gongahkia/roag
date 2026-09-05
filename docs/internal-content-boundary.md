# Closed internal content boundary

`src/medieval/internal-content.ts` v3 records the closed ownership policy for medieval content. It is a pure, compiled TypeScript maintainer contract. It is not a mod API, content-pack format, registry, loader, import/export path, plugin hook, or compatibility promise.

## Content families and owners

The catalogue references owner data and invokes owner validation only. It does not copy preset resolution, safety taxonomy, generator behavior, glyph meanings, or delegation logic.

| Family | Authoritative owner | Source category | Required safety and visibility | Change impact / rule |
| --- | --- | --- | --- | --- |
| Commodity definitions | `commodity-catalogue.ts` | Developer-authored, compiled TypeScript data | Exact bounded material records are classified and fail closed; not a cargo/market world fact | Policy validation; persist a later economy only after its owning replay/provenance decision |
| Named settlement profile | `settlement-profile.ts` | Developer-authored, compiled TypeScript data | Exact bounded Hearthford material profile is classified and fail closed; not a generated site/market/contract world fact | Policy validation; establish situated player knowledge and persistent economy facts only under a later owner |
| Content-safety policy | `content-safety.ts` | Developer-authored, compiled TypeScript data | Canonical owner classification; every prohibited class affirmatively excluded | Policy validation; use the policy-version and replay decision |
| World-generation configuration | `generation-config.ts` | Deterministic generator input | Generated records must pass owner safety audit; not a world fact itself | Generator provenance/recreation; make an explicit generator/provenance decision first |
| Initial-world generation | `initial-world.ts` | Deterministic generator input | Every generated/player-facing record is classified and fail-closed | Generator provenance/recreation; make an explicit generator/provenance decision first |
| Frontier generation | `frontier.ts` | Deterministic generator input | Classified commitments and facts; only known facts may surface | Generator provenance/recreation; make an explicit generator/provenance decision first |
| Renderer-semantic glyphs | `ascii-glyphs.ts` | Renderer-semantic data | Classified semantic references only; never a world fact on their own | Renderer contract; make an owning contract-version/replay decision first |
| Delegation task definitions | `delegation.ts` | Runtime task-definition data | Classified task/contract records; fail-closed validation | Persisted task/replay contract; make an owning contract-version/replay decision first |
| Generated world records | `world.ts` / `types.ts` | Generated world record | Validated full records only; `FoundationWorld` remains the sole mutable authority | Manifest/world replay; make an owning manifest/replay decision first |
| Forbidden public extension inputs | Internal boundary | Forbidden user/public extension input | Rejected before any world, knowledge, or execution effect | No runtime effect and no public compatibility surface |

The current owner versions are recorded only as references to their owner contracts. They are not alternate versions, a content schema, or a way to construct data outside those owners.

## Validation and maintenance

`internalContentCatalogue()` v3 returns a fresh copy of the fixed ten-family catalogue. `validateInternalContentCatalogue()` accepts only that closed, canonical descriptor set and fails closed for malformed fields, noncanonical or duplicate identifiers/order, unknown owners/categories, absent or unsafe safety requirements, invalid impact/change rules, and extension-shaped sources. `validateCurrentInternalContentOwners()` calls the existing owner validators/resolvers to ensure those references remain current.

When changing developer-authored content:

- Change the owning domain module and its focused validation/tests; do not add a second resolver, generator, safety policy, glyph catalogue, or delegation table here.
- Before changing generator-consumed authored inputs, make and record an explicit generator/provenance compatibility decision. A changed input must not silently alter recreation of existing manifests.
- Before changing persisted task or presentation semantics, make and record the owning contract-version and replay decision. This boundary adds no migration or compatibility shim.
- Keep static data bounded, deterministic, content-safety classified, and compatible with original low-mysticism medieval content. Never add sexual violence, slavery, torture, or harm/endangerment of children.

## Explicit non-goals

There is no user-authored content input, JSON/runtime loader, dynamic import, plugin discovery, script/callback hook, browser fetch, network source, persistence registry, cache, world/manifest field, or hidden-frontier/player-knowledge channel. The catalogue contains no executable values and never mutates a world.

The commodity catalogue remains compiled content only: the separate cargo-hold owner may reference its closed IDs and material semantics in mutable state v16/replay v8, while `FoundationWorld` v15, manifest v6, generation/RNG, IndexedDB layout v4, valid envelope loading, and explicit-save-only metadata remain unchanged. The named settlement profile is likewise compiled data only: it may reference catalogue IDs but does not instantiate a site, market, person, contract, route, price, stock, or player knowledge. Neither creates a public extension commitment.
