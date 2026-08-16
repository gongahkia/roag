import { UPGRADE_DESCRIPTIONS } from '../core/game';
import { EMOTES, type BuildTool, type CourseTheme, type GameConfig, type GameState, type PowerUp, type ShotCommand } from '../core/types';
import { bindingFor, type GamePreferences, type ShortcutId } from '../preferences';

export type Overlay = 'help' | 'settings' | undefined;
export type Drawer = 'run' | 'intel' | undefined;

export interface LedgerEntry {
  message: string;
  tone: string;
}

export interface Callout {
  message: string;
  tone: string;
}

export interface ViewModel {
  state: GameState;
  config: GameConfig;
  preferences: GamePreferences;
  overlay: Overlay;
  drawer: Drawer;
  rebinding?: ShortcutId;
  aim: ShotCommand;
  shotInFlight: boolean;
  ledger: readonly LedgerEntry[];
  callouts: readonly Callout[];
}

export const escapeHtml = (value: string) => value.replace(/[&<>'"]/g, (character) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[character]!);

const keyLabel = (key: string) => key === ' ' ? 'Space' : key;
const current = (state: GameState) => state.players[state.turn.playerIndex]!;

export const renderStatus = ({ state, shotInFlight }: Pick<ViewModel, 'state' | 'shotInFlight'>) => {
  if (state.status === 'build') return 'place every tile from scratch, or generate a seeded terrain pass and edit it';
  if (state.status === 'validate') return `${escapeHtml(current(state).name)} must sink this authored course once before it enters competition`;
  if (state.status === 'finished') return `winner: ${escapeHtml([...state.players].sort((left, right) => left.total - right.total)[0]!.name)}`;
  if (shotInFlight) return `${escapeHtml(current(state).name)}'s ball is in flight · hazard ${state.coursePhase + 1}/8 locked`;
  return `${escapeHtml(current(state).name)} is taking a turn · hazard ${state.coursePhase + 1}/8`;
};

export const renderCallouts = (callouts: readonly Callout[]) => callouts.map((callout) => `<p class="callout ${callout.tone}">${escapeHtml(callout.message)}</p>`).join('');

const renderLedger = (ledger: readonly LedgerEntry[]) => `<ul class="feed" aria-live="polite">${ledger.map((entry) => `<li class="${entry.tone}"><span>${escapeHtml(entry.message)}</span></li>`).join('') || '<li class="neutral"><span>waiting for the first stroke</span></li>'}</ul>`;

export const renderControlsMarkup = (view: ViewModel) => {
  const { state, preferences, aim, shotInFlight } = view;
  const player = current(state);
  if (state.status === 'build') {
    const build = state.build!;
    return `
      <div class="turn"><span style="--player:${player.color}"></span><strong>${escapeHtml(player.name)}</strong><b>build</b><small class="phase">author ${build.authorIndex + 1}/${state.players.length}</small></div>
      <p class="control-status">${renderStatus(view)}</p>
      <button id="validate" class="primary" ${player.kind !== 'human' ? 'disabled' : ''}>validate course <kbd>${keyLabel(bindingFor(preferences, 'lock'))}</kbd></button>
    `;
  }
  const disabled = (state.status !== 'playing' && state.status !== 'validate') || player.kind !== 'human' || shotInFlight;
  const heldItems = [player.inventory, player.spareInventory].filter(Boolean) as PowerUp[];
  const portalExits = state.course.portals?.filter((pair) => pair.exit).map((pair) => `<option value="${pair.id}:exit">${escapeHtml(pair.id)} exit</option>`).join('') ?? '';
  return `
    <div class="turn"><span style="--player:${player.color}"></span><strong>${escapeHtml(player.name)}</strong><b>${state.turn.secondsLeft.toFixed(0)}s</b><small class="phase">hazard ${state.coursePhase + 1}/8</small></div>
    <div class="emote-buttons" aria-label="emotes">${EMOTES.map((emote) => `<button data-emote="${emote.id}" title="${emote.label}" ${disabled ? 'disabled' : ''}>${emote.glyph}</button>`).join('')}</div>
    <label>power <input id="power" type="range" min="1" max="8" step="0.1" value="${aim.power}" ${disabled ? 'disabled' : ''}></label>
    <button id="shoot" class="primary" ${disabled ? 'disabled' : ''}>shoot <kbd>${keyLabel(bindingFor(preferences, 'shoot'))}</kbd></button>
    ${player.ballForm ? `<span class="active-form">next shot: ${escapeHtml(player.ballForm)} ball</span>` : ''}
    ${heldItems.includes('portal') ? `<label>portal exit <select id="portal-exit" ${disabled ? 'disabled' : ''}>${portalExits}</select></label>` : ''}
    ${heldItems.length ? heldItems.map((powerUp) => `<button data-use-powerup="${powerUp}" ${disabled ? 'disabled' : ''}>use ${escapeHtml(powerUp)}${powerUp === player.inventory ? ` <kbd>${keyLabel(bindingFor(preferences, 'usePowerUp'))}</kbd>` : ''}</button>`).join('') : '<span class="muted">no chaos item</span>'}
    ${player.secondWindAvailable && !player.twoPuttsArmed ? `<button id="second-wind" ${disabled ? 'disabled' : ''}>use second wind: two putts</button>` : ''}
    <p id="status" class="control-status">${renderStatus(view)}</p>
  `;
};

const shortcutRows = (preferences: GamePreferences, rebinding: ShortcutId | undefined, interactive = false) => ['shoot', 'powerDown', 'powerUp', 'lock', 'reroll', 'usePowerUp', 'help', 'settings'].map((id) => {
  const shortcutId = id as ShortcutId;
  const label = ({ shoot: 'shoot', powerDown: 'power down', powerUp: 'power up', lock: 'validate course', reroll: 'generate terrain', usePowerUp: 'use chaos item', help: 'shortcut help', settings: 'settings' })[shortcutId];
  const key = keyLabel(bindingFor(preferences, shortcutId));
  return `<dt>${label}</dt><dd>${interactive ? `<button data-bind="${shortcutId}" class="key-button ${rebinding === shortcutId ? 'selected' : ''}">${key}</button>` : `<kbd>${key}</kbd>`}</dd>`;
}).join('');

const renderOverlay = ({ overlay, preferences, rebinding }: ViewModel) => {
  if (!overlay) return '';
  if (overlay === 'help') return `<section class="overlay" role="dialog" aria-modal="true" aria-label="shortcut help"><div class="overlay-card"><button class="close" data-close-overlay aria-label="close help">×</button><p class="eyebrow">MOUSE FIRST · KEYS READY</p><h2>quick commands</h2><dl class="shortcut-list">${shortcutRows(preferences, rebinding)}</dl><p class="hint">Shortcuts pause while you edit a field. F1 opens settings to remap commands.</p></div></section>`;
  return `<section class="overlay" role="dialog" aria-modal="true" aria-label="game settings"><div class="overlay-card settings-card"><button class="close" data-close-overlay aria-label="close settings">×</button><p class="eyebrow">LOCAL PREFERENCES</p><h2>terminal settings</h2><label class="setting-toggle"><input data-preference="reducedMotion" type="checkbox" ${preferences.reducedMotion ? 'checked' : ''}> reduced motion and flash</label><label class="setting-toggle"><input data-preference="highContrast" type="checkbox" ${preferences.highContrast ? 'checked' : ''}> high-contrast glyphs</label><h3>shortcuts</h3><dl class="shortcut-list">${shortcutRows(preferences, rebinding, true)}</dl><p class="hint">${rebinding ? `press a key for ${escapeHtml(rebinding)} · Esc cancels` : 'select a key to rebind it'}</p></div></section>`;
};

const renderRunDrawer = ({ state, config }: ViewModel) => {
  const scoreRows = state.players.map((player) => `<tr class="${player.id === current(state).id ? 'active' : ''}"><td><i style="background:${player.color}"></i>${escapeHtml(player.name)}</td><td>${player.ball.complete ? '✓' : player.ball.strokes}</td><td>${player.total}</td></tr>`).join('');
  return `
    <h3>campaign controls</h3>
    <label>run seed <input id="seed" value="${escapeHtml(config.seed)}" maxlength="32"></label>
    <div class="split"><label>humans <input id="humans" type="number" min="1" max="8" value="${config.humanCount}"></label><label>AI <input id="bots" type="number" min="0" max="4" value="${config.botCount}"></label></div>
    <div class="split"><label>timer <input id="timer" type="number" min="8" max="90" value="${config.timerSeconds}"></label><label>skill <select id="skill"><option value="adaptive" ${config.botSkill === 'adaptive' ? 'selected' : ''}>adaptive</option>${Array.from({ length: 10 }, (_, index) => `<option value="${index + 1}" ${config.botSkill === index + 1 ? 'selected' : ''}>${index + 1}</option>`).join('')}</select></label></div>
    <label class="toggle"><input id="collisions" type="checkbox" ${config.collisions ? 'checked' : ''}> physical ball collisions</label>
    <label class="toggle"><input id="powerups" type="checkbox" ${config.powerUps ? 'checked' : ''}> high-chaos power-ups</label>
    <button id="new-run">start new build round</button>
    <p class="hint">up to 12 seats · max 4 AI · every player authors one course, then everyone competes on all validated courses</p>
    <h3>scorecard</h3><table><thead><tr><th>player</th><th>hole</th><th>total</th></tr></thead><tbody>${scoreRows}</tbody></table>`;
};

const renderInspector = (view: ViewModel) => {
  const { state, preferences } = view;
  if (state.status === 'build') {
    const build = state.build!;
    const tools: { id: BuildTool; label: string }[] = [
      { id: 'fairway', label: 'fairway' }, { id: 'rough', label: 'rough' }, { id: 'sand', label: 'sand' }, { id: 'ice', label: 'ice' },
      { id: 'wall', label: 'wall' }, { id: 'booster', label: 'booster' }, { id: 'conveyor', label: 'conveyor' }, { id: 'tee', label: 'tee' },
      { id: 'cup', label: 'cup' }, { id: 'sweeper', label: 'sweeper' }, { id: 'gate', label: 'timed gate' }, { id: 'portal-entrance', label: 'portal entrance' }, { id: 'portal-exit', label: 'portal exit' }, { id: 'recovery-pad', label: 'recovery pad' },
      { id: 'chaos-pad', label: 'chaos pad' }, { id: 'erase', label: 'erase' },
    ];
    const directions = [{ label: '→', x: 1, y: 0 }, { label: '↓', x: 0, y: 1 }, { label: '←', x: -1, y: 0 }, { label: '↑', x: 0, y: -1 }];
    const themes: { id: CourseTheme; label: string }[] = [
      { id: 'balanced', label: 'balanced' }, { id: 'speedway', label: 'speedway' }, { id: 'hazard-run', label: 'hazard run' }, { id: 'ice-rink', label: 'ice rink' }, { id: 'quarry', label: 'quarry' },
    ];
    return `<p class="hint"><strong>${escapeHtml(current(state).name)}</strong> is authoring. Select a tool, then click any isometric grid cell. A course needs one tee, one cup, and eight playable tiles.</p>
      <h3>place</h3><div class="build-tools">${tools.map((tool) => `<button data-build-tool="${tool.id}" class="${build.tool === tool.id ? 'selected' : ''}">${tool.label}</button>`).join('')}</div>
      <h3>tile controls</h3><label class="builder-range">height <output>${build.height}</output><input id="build-height" type="range" min="0" max="3" step="1" value="${build.height}"></label>
      <div class="build-directions" aria-label="surface direction">${directions.map((direction) => `<button data-build-direction="${direction.x},${direction.y}" class="${build.direction.x === direction.x && build.direction.y === direction.y ? 'selected' : ''}" title="direction ${direction.label}">${direction.label}</button>`).join('')}</div>
      <label class="builder-range">portal pair <output>${build.portalPairId}</output><input id="portal-pair" type="number" min="1" step="1" value="${build.portalPairId}"></label><p class="hint">place one entrance and one exit for each numbered, color-coded portal pair. arrows control its exit direction.</p>
      <h3>route composition</h3><p class="hint">these controls shape the playable route before you hand-curate individual tiles.</p>
      <label class="builder-range">route length <output>${Math.round(build.terrain.routeLength * 100)}%</output><input id="route-length" type="range" min="0.35" max="1" step="0.05" value="${build.terrain.routeLength}"></label>
      <label class="builder-range">bendiness <output>${Math.round(build.terrain.bendiness * 100)}%</output><input id="route-bendiness" type="range" min="0" max="1" step="0.05" value="${build.terrain.bendiness}"></label>
      <label class="builder-range">lane width <output>${build.terrain.laneWidth}</output><input id="route-width" type="range" min="1" max="3" step="1" value="${build.terrain.laneWidth}"></label>
      <label class="builder-range">side routes <output>${build.terrain.branches}</output><input id="route-branches" type="range" min="0" max="3" step="1" value="${build.terrain.branches}"></label>
      <h3>course curation</h3><p class="hint">pick a personality, then either tune the generator or take the result apart tile by tile.</p><div class="theme-tools">${themes.map((theme) => `<button data-course-theme="${theme.id}" class="${build.terrain.theme === theme.id ? 'selected' : ''}">${theme.label}</button>`).join('')}</div>
      <h3>seeded terrain generator</h3><p class="hint">deterministic route carving with editable terrain, ramp, wall, and hazard placement. Same seed, controls, and variation produce the same starting course.</p>
      <label class="builder-range">terrain density <output>${Math.round(build.terrain.density * 100)}%</output><input id="terrain-density" type="range" min="0.1" max="1" step="0.05" value="${build.terrain.density}"></label>
      <label class="builder-range">ramp frequency <output>${Math.round(build.terrain.elevation * 100)}%</output><input id="terrain-elevation" type="range" min="0" max="1" step="0.05" value="${build.terrain.elevation}"></label>
      <label class="builder-range">timed hazards <output>${build.terrain.hazards}</output><input id="terrain-hazards" type="range" min="0" max="3" step="1" value="${build.terrain.hazards}"></label>
      <label class="builder-range">surface chaos <output>${Math.round(build.terrain.chaos * 100)}%</output><input id="terrain-chaos" type="range" min="0" max="1" step="0.05" value="${build.terrain.chaos}"></label>
      <h3>competitive perk</h3><p class="hint">choose one upgrade for the play phase. You can change it until your course validates.</p><div class="upgrades build-perks">${Object.entries(UPGRADE_DESCRIPTIONS).map(([upgrade, description]) => `<button data-builder-upgrade="${upgrade}" class="${current(state).upgrades.includes(upgrade as keyof typeof UPGRADE_DESCRIPTIONS) ? 'selected' : ''}"><strong>${upgrade}</strong><small>${description}</small></button>`).join('')}</div>
      <div class="inspector-actions"><button id="randomize-terrain">surprise me</button><button id="auto-terrain">generate terrain <kbd>${keyLabel(bindingFor(preferences, 'reroll'))}</kbd></button><button id="validate" class="primary">validate course <kbd>${keyLabel(bindingFor(preferences, 'lock'))}</kbd></button></div>${renderLedger(view.ledger)}`;
  }
  const author = state.authoredCourses[state.courseIndex] && state.players.find((player) => player.id === state.authoredCourses[state.courseIndex]!.authorId);
  return `${renderLedger(view.ledger)}<h3>course</h3><dl><dt>author</dt><dd>${author ? escapeHtml(author.name) : 'unassigned'}</dd><dt>validated</dt><dd class="good">author completed one sink</dd><dt>hazards</dt><dd>${state.course.hazards.map((hazard) => hazard.kind).join(' + ') || 'none'}</dd><dt>portal pairs</dt><dd>${state.course.portals?.filter((pair) => pair.entrance && pair.exit).length ?? 0}</dd><dt>chaos pads</dt><dd>${state.course.itemPads.length}</dd></dl><h3>course legend</h3><p class="legend">fairway grass · rough · sand bunker · water ice<br>amber arm: sweeper · red/cyan: timed gate · numbered A/B: portal pair<br>cyan +: recovery pad · violet !: chaos pad</p>`;
};

const renderDrawer = (view: ViewModel) => {
  if (!view.drawer) return '';
  const title = view.drawer === 'run' ? 'run controls' : view.state.status === 'build' ? 'course builder' : 'course intel';
  return `<aside class="drawer drawer-${view.drawer} panel" aria-label="${title}"><div class="drawer-heading"><h2>${title}</h2><button data-close-drawer class="close" aria-label="close ${title}">×</button></div><div class="drawer-content">${view.drawer === 'run' ? renderRunDrawer(view) : renderInspector(view)}</div></aside>`;
};

export const renderAppMarkup = (view: ViewModel) => {
  const { state } = view;
  const progress = state.status === 'build'
    ? `BUILD <b>${state.build!.authorIndex + 1}</b> / ${state.players.length}`
    : state.status === 'validate'
      ? `TEST <b>${state.build!.authorIndex + 1}</b> / ${state.players.length}`
      : `COURSE <b>${state.courseIndex + 1}</b> / ${state.authoredCourses.length}`;
  return `
    <section class="topbar"><div class="brand"><p class="eyebrow">SUNNY CLUBHOUSE MINI GOLF</p><h1>GOLF <em>WITH YOUR</em> ENEMIES</h1></div><div class="title-actions"><button data-drawer="run" class="${view.drawer === 'run' ? 'selected' : ''}" aria-expanded="${view.drawer === 'run'}">run</button><button data-drawer="intel" class="${view.drawer === 'intel' ? 'selected' : ''}" aria-expanded="${view.drawer === 'intel'}">${state.status === 'build' ? 'build' : 'intel'}</button><button data-open-overlay="help">? help</button><button data-open-overlay="settings">F1 settings</button><div class="hole">${progress}<br><small>${escapeHtml(state.course.seed)}</small></div></div></section>
    <section class="layout"><section class="board panel"><div class="course-stage"><canvas id="course" aria-label="isometric arcade quarry golf course"></canvas><div id="callouts" class="callouts" aria-live="polite">${renderCallouts(view.callouts)}</div></div><div id="controls" class="controls"></div></section></section>${renderDrawer(view)}${renderOverlay(view)}
  `;
};
