# Colony web decisions

Recorded 2026-11-06. User instructions in the current conversation take precedence.

| ID | Decision | Authority/status |
| --- | --- | --- |
| D01 | Colony management and survival with tower defense are the primary game. | Explicit user request. |
| D02 | TypeScript + Phaser + Vite; browser execution with static hosting. | Explicitly accepted by the user. |
| D03 | Preserve the entire assets directory, docs/images, catalog tools and tests; preserve reusable designs/content. | Explicitly accepted by the user. |
| D04 | Defeat when the colony heart is destroyed OR every colonist dies. | Explicit answer to the clarification question in this conversation. |
| D05 | Preserve first; coordinator now implements and reviews P05–P09 internally using isolated agent worktrees, then integrates tested local and remote main. | User explicitly replaced the manual goal/report handoff after P04. Keep milestone reports and acceptance gates. |
| D06 | Existing gameplay code may eventually be replaced; archive it before removal. | User wants a fresh implementation. Removal is sequenced at release cutover, after recovery checks. |
| D07 | Real time with pause and speed controls; autonomous workers and indirect orders. | Recommended in the accepted analysis; working product default. |
| D08 | Vercel is the selected public release target. Keep root and subpath static-build compatibility. | User selected Vercel on 2026-11-07; account/project details requested and pending. |

The following are initial implementation defaults, not separately approved product
requirements. Implementers may tune them with evidence and record changes here:

- Medieval/dark fantasy, overhead square tiles, existing Urizen art and suitable UI icons.
- Desktop browser and mouse/keyboard first; responsive usable layout, touch refinement later.
- One surface map of roughly 128 x 128 tiles; six starting colonists; an initial capacity
  target of 24 colonists. Map size/population limits are measured targets, not promises.
- Fixed 10 Hz simulation, interpolated rendering, pause/1x/2x/4x controls. Simulated
  ticks, not frame rate or wall-clock time, own gameplay.
- Pause when the tab is hidden; no offline simulation or catch-up raids on return.
- Daylight work and announced raids, with bounded recovery intervals; endless survival
  after bosses, rather than a final boss ending the campaign.
- Colonists may be temporarily downed and rescued. Defeat checks actual deaths, not
  temporary unconsciousness. The heart cannot be rebuilt after a terminal defeat.
- New-run progression only for the first release; no legacy save import or permanent
  roguelite unlock tree. A defeat save remains terminal.
- A small coherent equipment/recipe set, no mandatory hero or four-person party cap.
- Local browser saves plus portable export/import. No account/backend/multiplayer.

Ask the user when a discovery would change the defeat rule, direct-control model,
theme, monetization, platform scope, or materially expand the project. Resolve
ordinary library selection, numeric tuning, file organization, and bug fixes with
engineering judgment. Record each changed assumption, its reason, and affected phases.

Before public deployment, identify the repository Pages destination or Vercel project.
This is a missing destination detail, not a reason to delay local implementation or
preparing the production build. Do not create a paid service or change an existing
production site implicitly.
