# Review the next ROAG ideas

Open **[the interactive idea review](ideas.html)**. It is entirely offline,
contains the original source PNGs, and cannot edit or publish the game.
No gameplay or sprite assignment has been implemented by creating this page.

From the repository root:

```sh
python3 -m http.server 8080
```

Visit **http://localhost:8080/docs/sprite-audit/ideas.html**. You can also open
the HTML file directly. Keep using the same browser/origin for its local draft,
or download/import JSON to move between browsers or origins.

## What is in this review?

The current mapping audit resolves 324/324 required bindings in both choices.
All **3,305 nonempty cells unused by either choice** are accounted for once in
the primary grouping ledger:

- **3,282** cells are in proposed art pools.
- **23** are held in two exclusion groups: earlier art exclusions and obvious
  modern/vehicle motifs outside the current fantasy setting.

There are **89 proposals** across seven areas:

| Area | Ideas | Examples |
| --- | ---: | --- |
| Enemies / encounters | 15 | Grave Cantor, haunted cache, Heavy Rammer, Web Weaver, Hollow Wraith, Gulp Toad, Mirror Hexer, Silt Worm, Oath Twins, Alarm Keeper, Ember Moth, Bone Splitter, Rotating Sentinel, Brood Keeper, Echo Duelist |
| Playable classes | 8 | Duelist, Geomancer, Spiritbinder, Pilgrim, Tinkerer, Stormcaller, Wayfinder, Cantor |
| Items / pickups | 6 | HP font, ration cache, cleanse, escape scroll, return token, charm well |
| Rooms / interactions | 13 | Practice rack, attunement forge, trader, memorial, mirror relay, bell sequence, prism relay, falling fixture, signs, camp, contract, sparring trial, oracle |
| Mechanical terrain | 4 | Clocked heat vents, two-state sluice, push floor, fragile optional crossing |
| Ground / walls / environment kits | 30 | Crypt, sanctuary, timber mine, Observatory mosaic, gardens, drowned sanctum, forge, library carpet, caves, masonry, furniture, banners and counterpart material pools |
| Presentation / existing-art improvements | 13 | Body variants, boon rings, weapon/status icons, HUD frames, bitmap labels, navigation, combat cues, controls and colour skins |

Some are small visual improvements; others are expensive alternatives that
need further design. A class and enemy can offer the same spare body as an
alternative. **Art availability is not permission to implement every idea.**
Several pools intentionally group many related corners, colours or assembly
pieces into one coherent kit. This is why there are not 3,305 feature cards.
Each cell has one accounting owner, but it may also be offered as an
alternative in another proposal. `↔` marks that reuse.

**13 proposals still lack a specific 1-Bit counterpart.** They are labelled
and can be found with the status filter. Their missing artwork cannot be
approved, and the combined approval button is disabled. Other monochrome
counterparts may share geometry/materials; that does not create distinct new
colour palettes or finished connected surfaces.

## Review controls

1. Pick a category or search an idea, motif or cell such as `tiny:122`.
2. Read the idea, current overlap and implementation decisions. Use **See
   sprites and art decisions** to jump to the artwork.
3. Select **Approve**, **Reject** or **Discuss** for the idea. Click the same
   vote again to clear it. Nothing is approved initially.
4. Leave exact comments: change the role, effect, interaction, appearance or
   scope. Each comment supports 4,000 characters.
5. Review **Mixed Kenney** and **Standalone 1-Bit** independently. You can
   approve an idea while rejecting its current artwork, or vice versa.
6. Click thumbnails to select preferred cells, optionally several. This is
   useful for a complete tile family or paired sprites. Selecting/clearing
   preferences clears that look’s earlier approval, requiring a new vote.
7. Expand **Show more cells** for full material families. Expanded pools stay
   open while you select preferences. A pool approval does not automatically
   assign every fragment to every game target.
8. Mark **Next batch** or **Later** if useful. The combined approval button
   explicitly approves that idea and its two displayed pools only.
9. Use Previous/Next, the review-status filter or the queue to continue.
   Arrow keys navigate when focus is outside controls; all controls also
   support ordinary Tab/Enter keyboard navigation.
10. Click **Download feedback**. The filename is
    **`roag-sprite-ideas-feedback.json`**. Partial reviews are valid.

**Import feedback** restores a downloaded review. It rejects incompatible
fingerprints, wrong review schemas, unknown/duplicate IDs, invalid votes,
unavailable sprite choices, duplicate preferences and oversized comments/files
before replacing the current draft. Comments remain literal text.

Browser storage is a convenience, not a backup. Download before changing
browser, clearing data or leaving a browser that reports saving problems.
The original sprite/room/enemy review drafts and accepted files are untouched.

## What happens after you send feedback?

Tell me where the downloaded JSON is, such as your Downloads folder. I can
read its exact votes, priorities, preferred cells and comments. There is also
a read-only helper:

```sh
node tools/sprite_ideas_feedback.js /path/to/roag-sprite-ideas-feedback.json
```

It validates the export against the reviewed proposal/source fingerprint and
prints reviewed records, pending artwork and selected actor-art conflicts.
It cannot implement features, execute comments or publish anything.

Before implementation, comments/conditions, dependencies and shared actor
choices must be resolved. An approved idea with rejected/missing artwork is
not an approved assignment. A rejection/discussion is not consent to silently
substitute a different mechanic. Generic art-pool approval still requires
specific semantic bindings and runtime visual verification.

Prior decisions remain: no hittable spore pods, no volatile bombs, no selected
moving-cart mechanic; the specified flask/brazier exclusions remain excluded.
The earlier five enemy squads and reviewed classes/props already exist and
are not presented as new implementation work.

## Data, generation and verification

- `tools/sprite_idea_proposals.py`: curated game-specific concepts, overlap,
  risks, typed source groups and candidate counterparts.
- `tools/build_sprite_ideas.py`: real native-resolver inventory, independent
  PNG decoding, bounds/occupancy, unique ownership ledger, source checksums,
  held-art exclusions and deterministic generation.
- `ideas.json`: inert proposal, exact cell and source data. `ideas.html`
  embeds the same data plus unchanged source PNG bytes and the review client.
- `tools/sprite_ideas_client.js`: versioned feedback validation, local draft,
  preferences, voting, import/export, filters and selected actor reuse warnings.
- Feedback schema: **`roag-sprite-ideas-feedback/1`**, with stable idea IDs,
  source/proposal fingerprint, current audit mapping fingerprint, one idea
  decision/priority/comment and separate per-look votes/comments/preferences.

```sh
make sprite-ideas        # deliberate rebuild, after the main audit is current
make sprite-ideas-test   # read-only freshness + focused data/state tests

# Optional isolated headless browser; no LÖVE window:
node tests/sprite_ideas_page_test.js --browser /path/to/chrome /var/tmp/roag-idea-review
```

The browser verification covers all 91 proposal/held groups, category/status
filters, literal comments, sprite selection and approval invalidation,
reload persistence, real JSON download/import, failed import preservation,
large galleries, both looks and mobile layout. Source checks cover every
unused cell and exact original PNG bytes. Screenshots are under
`/var/tmp/roag-idea-review/`; fixture exports there are **simulated test data,
not user approvals**. No LÖVE process or physical-input test was used.

Rebuilds change the review fingerprint when proposal meaning or source
evidence changes. Once real feedback arrives, preserve the reviewed page/data
and downloaded file before implementing, as with the accepted enemy review.
Do not silently regenerate that archive and reinterpret old votes.
