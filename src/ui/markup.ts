import { EMOTES, type GameConfig, type GameState, type HoleRules, type PowerUp, type ShotCommand, type VotingOption } from '../core/types';
import { bindingFor, type GamePreferences, type ShortcutId } from '../preferences';

export type Overlay = 'help' | 'settings' | undefined;
export type Drawer = 'run' | 'intel' | undefined;

export interface LedgerEntry { message: string; tone: string; }
export interface Callout { message: string; tone: string; }

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
const ruleSummary = (rules: HoleRules) => [
  `${rules.timerSeconds}s`, `cap ${rules.strokeCap}`, rules.collisions ? 'collisions' : 'no collisions',
  rules.powerUps ? `${Math.round(rules.recoveryBias * 100)}% recovery` : 'items off',
  `${Math.round(rules.launchMultiplier * 100)}% launch`, `${Math.round(rules.rollingResistanceMultiplier * 100)}% roll`,
  `${rules.hazardPhaseCount} hazard phases`, `${Math.round(rules.scoreMultiplier * 100)}% score`,
].join(' · ');
const terrainSummary = (option: VotingOption) => {
  const terrain = option.recipe.terrain;
  return `route ${Math.round(terrain.routeLength * 100)}% · bends ${Math.round(terrain.bendiness * 100)}% · lane ${terrain.laneWidth} · ramps ${Math.round(terrain.elevation * 100)}%/${terrain.maxElevation} · walls ${terrain.wallCount} · sweepers ${terrain.sweeperCount} · gates ${terrain.gateCount} · portals ${terrain.portalPairs} · pads ${terrain.recoveryPads}+${terrain.chaosPads}`;
};

export const renderStatus = ({ state, shotInFlight }: Pick<ViewModel, 'state' | 'shotInFlight'>) => {
  if (state.status === 'voting') return `public vote: ${Object.keys(state.vote?.ballots ?? {}).length}/${state.players.length} ballots cast`;
  if (state.status === 'finished') return `winner: ${escapeHtml([...state.players].sort((left, right) => left.total - right.total)[0]!.name)}`;
  if (shotInFlight) return `${escapeHtml(current(state).name)}'s ball is in flight · hazard ${state.coursePhase + 1}/${state.holeRules.hazardPhaseCount} locked`;
  return `${escapeHtml(current(state).name)} is taking a turn · hazard ${state.coursePhase + 1}/${state.holeRules.hazardPhaseCount}`;
};

export const renderCallouts = (callouts: readonly Callout[]) => callouts.map((callout) => `<p class="callout ${callout.tone}">${escapeHtml(callout.message)}</p>`).join('');
const renderLedger = (ledger: readonly LedgerEntry[]) => `<ul class="feed" aria-live="polite">${ledger.map((entry) => `<li class="${entry.tone}"><span>${escapeHtml(entry.message)}</span></li>`).join('') || '<li class="neutral"><span>waiting for the ballot</span></li>'}</ul>`;

const renderVotingControls = (state: GameState) => {
  const vote = state.vote!;
  const counts = vote.options.map((option) => Object.values(vote.ballots).filter((ballot) => ballot === option.id).length);
  return `<div class="turn"><strong>hole ${state.hole} ballot</strong><b>${Object.keys(vote.ballots).length}/${state.players.length}</b><small class="phase">public concurrent voting</small></div>
    <div class="vote-options" aria-label="course package options">${vote.options.map((option, index) => `<article class="vote-option"><h3>${escapeHtml(option.label)}</h3><p>${escapeHtml(terrainSummary(option))}</p><p>${escapeHtml(ruleSummary(option.recipe.rules))}</p><p class="boon">${option.recipe.rules.sharedBoons.length ? `shared: ${option.recipe.rules.sharedBoons.join(', ')}` : 'shared: no boon'}${option.recipe.rules.startingPowerUp ? ` · supply: ${option.recipe.rules.startingPowerUp}` : ''}</p><b>${counts[index]} vote${counts[index] === 1 ? '' : 's'}</b></article>`).join('')}</div>
    <p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;
};

export const renderControlsMarkup = (view: ViewModel) => {
  const { state, preferences, aim, shotInFlight } = view;
  if (state.status === 'voting') return renderVotingControls(state);
  const player = current(state);
  const disabled = state.status !== 'playing' || player.kind !== 'human' || shotInFlight;
  const heldItems = [player.inventory, player.spareInventory].filter(Boolean) as PowerUp[];
  const portalExits = state.course.portals?.filter((pair) => pair.exit).map((pair) => `<option value="${pair.id}:exit">${escapeHtml(pair.id)} exit</option>`).join('') ?? '';
  return `
    <div class="turn"><span style="--player:${player.color}"></span><strong>${escapeHtml(player.name)}</strong><b>${state.turn.secondsLeft.toFixed(0)}s</b><small class="phase">hazard ${state.coursePhase + 1}/${state.holeRules.hazardPhaseCount}</small></div>
    <div class="emote-buttons" aria-label="emotes">${EMOTES.map((emote) => `<button data-emote="${emote.id}" title="${emote.label}" ${disabled ? 'disabled' : ''}>${emote.glyph}</button>`).join('')}</div>
    <label>power <input id="power" type="range" min="1" max="8" step="0.1" value="${aim.power}" ${disabled ? 'disabled' : ''}></label>
    <button id="shoot" class="primary" ${disabled ? 'disabled' : ''}>shoot <kbd>${keyLabel(bindingFor(preferences, 'shoot'))}</kbd></button>
    ${player.ballForm ? `<span class="active-form">next shot: ${escapeHtml(player.ballForm)} ball</span>` : ''}
    ${heldItems.includes('portal') ? `<label>portal exit <select id="portal-exit" ${disabled ? 'disabled' : ''}>${portalExits}</select></label>` : ''}
    ${heldItems.length ? heldItems.map((powerUp) => `<button data-use-powerup="${powerUp}" ${disabled ? 'disabled' : ''}>use ${escapeHtml(powerUp)}${powerUp === player.inventory ? ` <kbd>${keyLabel(bindingFor(preferences, 'usePowerUp'))}</kbd>` : ''}</button>`).join('') : '<span class="muted">no chaos item</span>'}
    ${player.secondWindAvailable && !player.twoPuttsArmed ? `<button id="second-wind" ${disabled ? 'disabled' : ''}>use second wind: two putts</button>` : ''}
    <p id="status" class="control-status">${renderStatus(view)}</p>`;
};

const shortcutRows = (preferences: GamePreferences, rebinding: ShortcutId | undefined, interactive = false) => ['shoot', 'powerDown', 'powerUp', 'usePowerUp', 'help', 'settings'].map((id) => {
  const shortcutId = id as ShortcutId;
  const label = ({ shoot: 'shoot', powerDown: 'power down', powerUp: 'power up', usePowerUp: 'use chaos item', help: 'shortcut help', settings: 'settings' })[shortcutId];
  const key = keyLabel(bindingFor(preferences, shortcutId));
  return `<dt>${label}</dt><dd>${interactive ? `<button data-bind="${shortcutId}" class="key-button ${rebinding === shortcutId ? 'selected' : ''}">${key}</button>` : `<kbd>${key}</kbd>`}</dd>`;
}).join('');

const renderOverlay = ({ overlay, preferences, rebinding }: ViewModel) => {
  if (!overlay) return '';
  if (overlay === 'help') return `<section class="overlay" role="dialog" aria-modal="true" aria-label="shortcut help"><div class="overlay-card"><button class="close" data-close-overlay aria-label="close help">×</button><p class="eyebrow">MOUSE FIRST · KEYS READY</p><h2>quick commands</h2><dl class="shortcut-list">${shortcutRows(preferences, rebinding)}</dl><p class="hint">Voting is click-only so every local player can vote publicly in any order.</p></div></section>`;
  return `<section class="overlay" role="dialog" aria-modal="true" aria-label="game settings"><div class="overlay-card settings-card"><button class="close" data-close-overlay aria-label="close settings">×</button><p class="eyebrow">LOCAL PREFERENCES</p><h2>terminal settings</h2><label class="setting-toggle"><input data-preference="reducedMotion" type="checkbox" ${preferences.reducedMotion ? 'checked' : ''}> reduced motion and flash</label><label class="setting-toggle"><input data-preference="highContrast" type="checkbox" ${preferences.highContrast ? 'checked' : ''}> high-contrast glyphs</label><h3>shortcuts</h3><dl class="shortcut-list">${shortcutRows(preferences, rebinding, true)}</dl><p class="hint">${rebinding ? `press a key for ${escapeHtml(rebinding)} · Esc cancels` : 'select a key to rebind it'}</p></div></section>`;
};

const renderRunDrawer = ({ state, config }: ViewModel) => {
  const scoreRows = state.players.map((player) => `<tr class="${player.id === current(state).id ? 'active' : ''}"><td><i style="background:${player.color}"></i>${escapeHtml(player.name)}</td><td>${player.ball.complete ? '✓' : player.ball.strokes}</td><td>${player.total}</td></tr>`).join('');
  return `<h3>campaign controls</h3><label>run seed <input id="seed" value="${escapeHtml(config.seed)}" maxlength="32"></label><div class="split"><label>humans <input id="humans" type="number" min="1" max="8" value="${config.humanCount}"></label><label>AI <input id="bots" type="number" min="0" max="4" value="${config.botCount}"></label></div><label>AI skill <select id="skill"><option value="adaptive" ${config.botSkill === 'adaptive' ? 'selected' : ''}>adaptive</option>${Array.from({ length: 10 }, (_, index) => `<option value="${index + 1}" ${config.botSkill === index + 1 ? 'selected' : ''}>${index + 1}</option>`).join('')}</select></label><button id="new-run">start new nine-hole run</button><p class="hint">Every hole begins with a public vote between three fully specified course-and-rules packages.</p><h3>scorecard</h3><table><thead><tr><th>player</th><th>hole</th><th>total</th></tr></thead><tbody>${scoreRows}</tbody></table>`;
};

const renderVoteRows = (state: GameState) => {
  const vote = state.vote!;
  return `<h3>public ballots</h3><div class="ballots">${state.players.map((player) => `<section class="ballot"><strong><i style="background:${player.color}"></i>${escapeHtml(player.name)}</strong><div>${vote.options.map((option, index) => `<button data-vote-player="${player.id}" data-vote-option="${option.id}" class="${vote.ballots[player.id] === option.id ? 'selected' : ''}" ${player.kind === 'bot' ? 'disabled' : ''}>${index + 1}</button>`).join('')}</div><small>${vote.ballots[player.id] ? escapeHtml(vote.options.find((option) => option.id === vote.ballots[player.id])!.label) : player.kind === 'bot' ? 'bot is considering' : 'awaiting vote'}</small></section>`).join('')}</div>`;
};

const renderInspector = (view: ViewModel) => {
  const { state } = view;
  if (state.status === 'voting') return `${renderVoteRows(state)}<h3>packages</h3>${state.vote!.options.map((option, index) => `<section class="package-detail"><h3>${index + 1}. ${escapeHtml(option.label)}</h3><p>${escapeHtml(terrainSummary(option))}</p><p>${escapeHtml(ruleSummary(option.recipe.rules))}</p></section>`).join('')}${renderLedger(view.ledger)}`;
  return `${renderLedger(view.ledger)}<h3>active package</h3><dl><dt>rules</dt><dd>${escapeHtml(ruleSummary(state.holeRules))}</dd><dt>shared boons</dt><dd>${state.holeRules.sharedBoons.join(', ') || 'none'}</dd><dt>starting supply</dt><dd>${state.holeRules.startingPowerUp ?? 'none'}</dd><dt>hazards</dt><dd>${state.course.hazards.map((hazard) => hazard.kind).join(' + ') || 'none'}</dd><dt>portal pairs</dt><dd>${state.course.portals?.filter((pair) => pair.entrance && pair.exit).length ?? 0}</dd><dt>item pads</dt><dd>${state.course.itemPads.length}</dd></dl><h3>course legend</h3><p class="legend">fairway grass · rough · sand bunker · water ice<br>amber arm: sweeper · red/cyan: timed gate · numbered A/B: portal pair<br>cyan +: recovery pad · violet !: chaos pad</p>`;
};

const renderDrawer = (view: ViewModel) => {
  if (!view.drawer) return '';
  const title = view.drawer === 'run' ? 'run controls' : view.state.status === 'voting' ? 'public ballot' : 'course intel';
  return `<aside class="drawer drawer-${view.drawer} panel" aria-label="${title}"><div class="drawer-heading"><h2>${title}</h2><button data-close-drawer class="close" aria-label="close ${title}">×</button></div><div class="drawer-content">${view.drawer === 'run' ? renderRunDrawer(view) : renderInspector(view)}</div></aside>`;
};

export const renderAppMarkup = (view: ViewModel) => {
  const { state } = view;
  const progress = state.status === 'voting' ? `VOTE <b>${state.hole}</b> / ${state.config.holeCount}` : `HOLE <b>${state.hole}</b> / ${state.config.holeCount}`;
  return `<section class="topbar"><div class="brand"><p class="eyebrow">SUNNY CLUBHOUSE MINI GOLF</p><h1>GOLF <em>WITH YOUR</em> ENEMIES</h1></div><div class="title-actions"><button data-drawer="run" class="${view.drawer === 'run' ? 'selected' : ''}" aria-expanded="${view.drawer === 'run'}">run</button><button data-drawer="intel" class="${view.drawer === 'intel' ? 'selected' : ''}" aria-expanded="${view.drawer === 'intel'}">${state.status === 'voting' ? 'vote' : 'intel'}</button><button data-open-overlay="help">? help</button><button data-open-overlay="settings">F1 settings</button><div class="hole">${progress}<br><small>${escapeHtml(state.course.seed)}</small></div></div></section><section class="layout"><section class="board panel"><div class="course-stage"><canvas id="course" aria-label="isometric arcade mini golf course"></canvas><div id="callouts" class="callouts" aria-live="polite">${renderCallouts(view.callouts)}</div></div><div id="controls" class="controls"></div></section></section>${renderDrawer(view)}${renderOverlay(view)}`;
};
