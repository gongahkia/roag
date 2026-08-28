import { EMOTES, type ChronoCard, type CoursePackage, type GadgetKind, type GameConfig, type GameState, type HoleRules, type Point, type PowerUp, type ShotCommand } from '../core/types';
import { definitionFor } from '../core/catalog';
import { bindingFor, type GamePreferences, type ShortcutId } from '../preferences';

export type Overlay = 'help' | 'settings' | undefined;
export type Drawer = 'run' | 'intel' | undefined;

export interface LedgerEntry { message: string; tone: string; }
export interface Callout { message: string; tone: string; }
export interface MultiplayerView { online: boolean; connected: boolean; roomCode?: string; playerId?: string; host: boolean; controllerName?: string; localMultiplayer?: boolean; }

export interface ViewModel {
  state: GameState;
  config: GameConfig;
  preferences: GamePreferences;
  overlay: Overlay;
  drawer: Drawer;
  rebinding?: ShortcutId;
  aim: ShotCommand;
  placement?: { kind: GadgetKind; point?: Point; valid: boolean; confirmed?: boolean };
  shotInFlight: boolean;
  multiplayer: MultiplayerView;
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
  `${rules.hazardPhaseCount}-step hazard pattern`, `${Math.round(rules.scoreMultiplier * 100)}% score`,
].join(' · ');
const themeIcon: Record<CoursePackage['recipe']['terrain']['theme'], string> = { balanced: '⛳', speedway: '⚡', 'hazard-run': '⚠', 'ice-rink': '❄', quarry: '⛏', drift: '↻', bloom: '✽', pulse: '⌁', carnival: '★', marsh: '▤', zephyr: '〰' };
const themeDescriptor: Record<CoursePackage['recipe']['terrain']['theme'], string> = { balanced: 'steady green', speedway: 'speed lanes', 'hazard-run': 'moving traps', 'ice-rink': 'long slides', quarry: 'hard climbs', drift: 'paired sinkholes', bloom: 'thorn knockback', pulse: 'launch fields', carnival: 'spring banks', marsh: 'cushion turf', zephyr: 'gust lanes' };
const powerUpIcon: Record<string, string> = { turbo: '↯', shield: '⬡', bomb: '✹', freeze: '❄', swap: '⇄', 'two putts': '2P', heavy: '●', bouncy: '◌', ghost: '◐', magnet: '🧲', ice: '❄', portal: '◉', glider: '⌒', sticky: '▣', orbit: '◎', quantum: '∞', mirror: '◇', anvil: '⬟', vampire: '☽', boomerang: '↶', 'cup magnet': '⊙', slipstream: '➳', 'rebound rig': '↩', 'phase shift': '⇤', sandbag: '▰', 'rescue drone': '✈', airhorn: '📣', 'club flipper': '↻', 'time dilator': '⌛', mugger: '♜', scramble: '⇆', 'gravity gloves': '☄', 'bunker buster': '⛏', 'portal remote': '◉', 'red tee': '⚑', 'black flag': '⚐', 'cherry bomb': '✹', copycat: '▣', 'wind sock': '〰', 'slope stabilizer': '⛰', 'spring polish': '⌃', 'bumper wax': '◇', 'cushion map': '▤', 'popper pad': '↑', 'snare patch': '⌁', 'blast mine': '✹', 'slick patch': '≋', 'sky spring': '⌃', 'gravity well': '◉', 'mirror plate': '◇', 'toll booth': '$', 'control inverter': '↻', 'portal gun': '◉', 'undo drive': '↶', 'second chance': '↺', 'echo putt': '◌', 'future sight': '⌘', 'time theft': '⌛', 'frozen frame': '❄', 'parallel parking': '⇆', 'grandfather clause': '⌫', 'anchor line': '⚓', 'spring ticket': '⌃', 'cushion call': '▤', 'bumper lease': '◇', 'shared draft': '⇄', 'wind sail': '〰', 'grounds crew': '⛰', 'rescue pact': '✚', 'clubhouse pool': '$', 'sticky forecast': '▤', 'crosswind debt': '〰', 'dead bounce': '◇' };

export const renderStatus = ({ state, shotInFlight }: Pick<ViewModel, 'state' | 'shotInFlight'>) => {
  if (state.paused) return 'match paused';
  if (state.status === 'rolling') return state.die?.roll ? `the ${state.die.faces.length}-sided course die is tumbling` : `hole ${state.hole}/${state.config.holeCount} · die bets close in ${state.die?.secondsLeft.toFixed(0) ?? 0}s`;
  if (state.status === 'shopping') return `clubhouse merchant · ${state.shop?.secondsLeft.toFixed(0) ?? 0}s remaining`;
  if (state.status === 'transitioning') return `expanding the campaign to hole ${state.hole}/${state.config.holeCount}`;
  if (state.status === 'finished') return 'campaign complete — the full route is on display';
  if (shotInFlight) return `${escapeHtml(current(state).name)}'s ball is in flight · moving hazards are live`;
  return `${escapeHtml(current(state).name)} is taking a turn · moving hazards are live`;
};

export const renderCallouts = (callouts: readonly Callout[]) => callouts.map((callout) => `<p class="callout ${callout.tone}">${escapeHtml(callout.message)}</p>`).join('');
const renderLedger = (ledger: readonly LedgerEntry[]) => `<ul class="feed" aria-live="polite">${ledger.map((entry) => `<li class="${entry.tone}"><span>${escapeHtml(entry.message)}</span></li>`).join('') || '<li class="neutral"><span>waiting for the die</span></li>'}</ul>`;

const renderDieControls = (state: GameState) => {
  return `<div class="turn"><strong>course die</strong><b>${state.hole}/${state.config.holeCount}</b><small class="phase">shared wager window</small></div><p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;
};

const renderTransitionControls = (state: GameState) => `<div class="turn"><strong>next hole</strong><b>${state.hole}/${state.config.holeCount}</b><small class="phase">the route is building outward</small></div><p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;
const renderFinishedControls = (state: GameState) => `<div class="turn"><strong>campaign complete</strong><b>★</b><small class="phase">the full route has zoomed out behind the results</small></div><p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;

const renderMatchHud = (view: ViewModel) => {
  const { state, aim, multiplayer, shotInFlight } = view;
  if (state.status !== 'playing') return '';
  const player = current(state);
  const disabled = state.paused || player.kind !== 'human' || shotInFlight || (multiplayer.online && multiplayer.playerId !== player.id);
  const strength = Math.round(Math.max(0, Math.min(1, (aim.power - 1) / 7)) * 100);
  const leaderboard = [...state.players]
    .sort((left, right) => left.total - right.total || left.ball.strokes - right.ball.strokes || left.name.localeCompare(right.name))
    .map((candidate, index) => `<li class="${candidate.id === player.id ? 'active' : ''}"><span>${index + 1}</span><i style="background:${candidate.color}"></i><strong>${escapeHtml(candidate.name)}</strong><b>${candidate.total}</b></li>`)
    .join('');
  const shotKind = player.forcedChip ? 'chip' : aim.kind ?? 'putt';
  return `<section class="match-hud" aria-label="match heads-up display"><section class="hud-course"><p>COURSE</p><strong>${themeIcon[state.course.theme]} ${escapeHtml(themeDescriptor[state.course.theme])}</strong><small>hole ${state.hole} · par ${state.course.score.estimatedStrokes} · ${state.course.width}×${state.course.height}</small></section><section class="hud-timer"><p>ON THE TEE</p><strong>${escapeHtml(player.name)}</strong><b id="hud-turn-timer">${state.turn.secondsLeft.toFixed(0)}<small>s</small></b></section><section class="hud-leaderboard" aria-label="leaderboard"><header><span>LEADERBOARD</span><small>total</small></header><ol>${leaderboard}</ol></section><section class="reaction-dock" aria-label="reactions"><span>reactions</span><div>${EMOTES.map((emote) => `<button data-emote="${emote.id}" type="button" aria-label="send ${emote.label} reaction" title="${emote.label}" ${disabled ? 'disabled' : ''}>${emote.glyph}</button>`).join('')}</div></section><section class="hud-strength" aria-label="${shotKind} strength; drag with the left button to putt or the right button to chip"><div class="hud-strength-heading"><span id="hud-shot-label">${shotKind === 'chip' ? 'CHIP' : 'PUTT'} STRENGTH</span><b id="hud-strength-value">${aim.power.toFixed(1)}</b></div><div id="hud-strength-meter" class="hud-strength-meter" style="--strength:${strength}%"><i></i></div></section></section>`;
};

export const renderControlsMarkup = (view: ViewModel) => {
  const { state, preferences, shotInFlight, multiplayer, placement } = view;
  if (state.status === 'rolling') return renderDieControls(state);
  if (state.status === 'shopping') return `<div class="turn merchant-turn"><strong>clubhouse merchant</strong><b>$${playerCash(state, current(state).id)}</b><small class="phase">the shared shelf is open</small></div><p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;
  if (state.status === 'transitioning') return renderTransitionControls(state);
  if (state.status === 'finished') return renderFinishedControls(state);
  const player = current(state);
  const disabled = state.status !== 'playing' || state.paused || player.kind !== 'human' || shotInFlight || (multiplayer.online && multiplayer.playerId !== player.id);
  const cardDisabled = disabled || state.turn.cardPlayed;
  const heldCards = player.pockets.length ? player.pockets : [player.inventory, player.spareInventory].filter(Boolean).map((id, index) => ({ id: id!, source: 'pad' as const, instanceId: `legacy-ui-${index}`, duration: undefined }));
  const heldItems = heldCards.map((card) => card.id) as (PowerUp | ChronoCard)[];
  const tileCards = new Set<PowerUp | ChronoCard>(['popper pad', 'snare patch', 'blast mine', 'slick patch', 'sky spring', 'gravity well', 'mirror plate', 'toll booth', 'control inverter', 'portal gun', 'bunker buster']);
  const targets = state.players.filter((candidate) => !candidate.ball.complete);
  const targetSelector = heldItems.some((powerUp) => !tileCards.has(powerUp)) && targets.length
    ? `<label>play on <select id="powerup-target" ${disabled ? 'disabled' : ''}>${targets.map((candidate) => `<option value="${candidate.id}">${escapeHtml(candidate.name)}${candidate.id === player.id ? ' (self)' : ''}</option>`).join('')}</select></label>`
    : '';
  const portalExits = state.course.portals?.filter((pair) => pair.exit).map((pair) => `<option value="${pair.id}:exit">${escapeHtml(pair.id)} exit</option>`).join('') ?? '';
  return `
    ${player.ballForm ? `<span class="active-form">next shot: ${escapeHtml(player.ballForm)} ball</span>` : ''}
    ${player.forcedChip ? '<span class="active-form">airhorn: next shot is a chip</span>' : ''}
    ${heldItems.includes('portal') || heldItems.includes('portal remote') ? `<label>portal exit <select id="portal-exit" ${disabled ? 'disabled' : ''}>${portalExits}</select></label>` : ''}
    ${targetSelector}
    ${heldCards.length ? heldCards.map((card) => {
      const definition = definitionFor(card.id as never);
      const detail = card.duration ? `${card.duration.amount} ${card.duration.unit}${card.duration.amount === 1 ? '' : 's'}` : definition?.timing === 'putt' ? 'next putt' : definition?.timing === 'immediate' ? 'instant' : 'item';
      const role = definition?.polarity && definition.polarity !== 'neutral' ? `${definition.polarity} · ` : '';
      return `<button data-use-powerup="${escapeHtml(card.id)}" data-card-id="${escapeHtml(card.instanceId ?? '')}" ${cardDisabled ? 'disabled' : ''}>use ${escapeHtml(card.id)} <small>${role}${detail}</small>${card.id === player.inventory ? ` <kbd>${keyLabel(bindingFor(preferences, 'usePowerUp'))}</kbd>` : ''}</button>`;
    }).join('') : ''}
    ${placement ? `<p class="placement-status ${placement.valid ? 'valid' : 'invalid'}">${powerUpIcon[placement.kind]} ${escapeHtml(placement.kind)} · ${placement.point ? placement.valid ? placement.confirmed ? 'click again or press Enter to place' : 'click this tile to lock the preview' : 'choose an open playable tile' : 'click a tile to preview'} <button data-cancel-placement>cancel</button></p>` : ''}
    ${player.secondWindAvailable && !player.twoPuttsArmed ? `<button id="second-wind" ${disabled ? 'disabled' : ''}>use second wind: two putts</button>` : ''}
    ${state.turn.cardPlayed ? '<p class="control-status">one card committed this turn</p>' : ''}
    ${state.players.some((candidate) => candidate.attachments?.length) ? `<div class="card-effects"><strong>table cards</strong>${state.players.filter((candidate) => candidate.attachments?.length).map((candidate) => `<p><i style="background:${candidate.color}"></i>${escapeHtml(candidate.name)} · ${candidate.attachments!.map((attachment) => `${escapeHtml(attachment.cardId)} (${attachment.remaining} ${attachment.unit}${attachment.remaining === 1 ? '' : 's'})`).join(', ')}</p>`).join('')}</div>` : ''}`;
};

const playerCash = (state: GameState, id: string) => state.players.find((player) => player.id === id)?.cash ?? 0;

const shortcutRows = (preferences: GamePreferences, rebinding: ShortcutId | undefined, interactive = false) => ['shoot', 'powerDown', 'powerUp', 'usePowerUp', 'pause', 'help', 'settings'].map((id) => {
  const shortcutId = id as ShortcutId;
  const label = ({ shoot: 'shoot', powerDown: 'power down', powerUp: 'power up', usePowerUp: 'use chaos item', pause: 'pause match', help: 'shortcut help', settings: 'settings' })[shortcutId];
  const key = keyLabel(bindingFor(preferences, shortcutId));
  return `<dt>${label}</dt><dd>${interactive ? `<button data-bind="${shortcutId}" class="key-button ${rebinding === shortcutId ? 'selected' : ''}">${key}</button>` : `<kbd>${key}</kbd>`}</dd>`;
}).join('');

const renderOverlay = ({ overlay, preferences, rebinding }: ViewModel) => {
  if (!overlay) return '';
  if (overlay === 'help') return `<section class="overlay" role="dialog" aria-modal="true" aria-label="shortcut help"><div class="overlay-card"><button class="close" data-close-overlay aria-label="close help">×</button><p class="eyebrow">MOUSE FIRST · KEYS READY</p><h2>quick commands</h2><dl class="shortcut-list">${shortcutRows(preferences, rebinding)}</dl><p class="hint">Drag on the course with the left mouse button to putt or the right mouse button to chip. Controller Y also toggles the shot type. Before each hole, everyone may spend cash to add die faces or weight a preferred face before the shared roll.</p></div></section>`;
  return `<section class="overlay" role="dialog" aria-modal="true" aria-label="game settings"><div class="overlay-card settings-card"><button class="close" data-close-overlay aria-label="close settings">×</button><p class="eyebrow">LOCAL PREFERENCES</p><h2>settings</h2><label class="setting-toggle"><input data-preference="reducedMotion" type="checkbox" ${preferences.reducedMotion ? 'checked' : ''}> reduced motion and flash</label><label class="setting-toggle"><input data-preference="highContrast" type="checkbox" ${preferences.highContrast ? 'checked' : ''}> high-contrast palette</label><label class="setting-toggle"><input data-preference="controllerVibration" type="checkbox" ${preferences.controllerVibration ? 'checked' : ''}> controller vibration</label><label class="setting-toggle"><input data-preference="showMerchantHoldings" type="checkbox" ${preferences.showMerchantHoldings ? 'checked' : ''}> show table cards and boons in merchant</label><p class="setting-note">mouse shots: left-drag to putt or right-drag to chip, then release to strike</p><label>master volume <input data-preference-range="masterVolume" type="range" min="0" max="1" step="0.05" value="${preferences.masterVolume}"></label><label>effects volume <input data-preference-range="effectsVolume" type="range" min="0" max="1" step="0.05" value="${preferences.effectsVolume}"></label><label>controller deadzone <input data-preference-range="controllerDeadzone" type="range" min="0.05" max="0.5" step="0.01" value="${preferences.controllerDeadzone}"></label><label>controller aim sensitivity <input data-preference-range="controllerAimSensitivity" type="range" min="0.5" max="2" step="0.05" value="${preferences.controllerAimSensitivity}"></label><h3>shortcuts</h3><dl class="shortcut-list">${shortcutRows(preferences, rebinding, true)}</dl><p class="hint">${rebinding ? `press a key for ${escapeHtml(rebinding)} · Esc cancels` : 'select a key to rebind it'}</p></div></section>`;
};

const renderRunDrawer = ({ state, config }: ViewModel) => {
  const scoreRows = state.players.map((player) => `<tr class="${player.id === current(state).id ? 'active' : ''}"><td><i style="background:${player.color}"></i>${escapeHtml(player.name)}</td><td>${player.ball.complete ? '✓' : player.ball.strokes}</td><td>${player.total}</td></tr>`).join('');
  return `<h3>campaign controls</h3><label>run seed <input id="seed" value="${escapeHtml(config.seed)}" maxlength="32"></label><div class="split"><label>humans <input id="humans" type="number" min="1" max="8" value="${config.humanCount}"></label><label>AI <input id="bots" type="number" min="0" max="4" value="${config.botCount}"></label></div><label>AI skill <select id="skill"><option value="adaptive" ${config.botSkill === 'adaptive' ? 'selected' : ''}>adaptive</option>${Array.from({ length: 10 }, (_, index) => `<option value="${index + 1}" ${config.botSkill === index + 1 ? 'selected' : ''}>${index + 1}</option>`).join('')}</select></label><button id="new-run">start new nine-hole run</button><p class="hint">Before every hole, a shared course die lets each player buy new faces or weight a complete package before it lands.</p><h3>scorecard</h3><table><thead><tr><th>player</th><th>hole</th><th>total</th></tr></thead><tbody>${scoreRows}</tbody></table>`;
};

const renderDieOverlay = (view: ViewModel) => {
  const { state, multiplayer } = view;
  const die = state.die;
  if (state.status !== 'rolling' || !die) return '';
  const ownPlayer = multiplayer.online
    ? state.players.find((player) => player.id === multiplayer.playerId)
    : state.players.find((player) => player.kind === 'human' && !die.wagers[player.id]?.ready);
  const ownWager = ownPlayer ? die.wagers[ownPlayer.id] : undefined;
  const canBet = Boolean(ownPlayer && !die.roll && !ownWager?.ready);
  const sideCost = ownWager ? 1 + ownWager.addedSides : 1;
  const totalWeight = die.faces.reduce((total, face) => total + face.weight, 0);
  const selectedId = die.roll?.faceId;
  const packageFace = (face: typeof die.faces[number], index: number) => {
    const { terrain, rules } = face.recipe;
    const odds = Math.round(face.weight / totalWeight * 100);
    const augmentationCost = ownWager ? 1 + Math.floor((ownWager.augmentations[face.id] ?? 0) / 2) : 1;
    const augmentedBy = Object.entries(face.augmentations).map(([playerId, amount]) => `${escapeHtml(state.players.find((player) => player.id === playerId)?.name ?? playerId)} +${amount}`).join(' · ');
    return `<article class="die-face theme-${terrain.theme} ${selectedId === face.id ? 'landed' : ''}" aria-label="die face ${index + 1}: ${escapeHtml(face.label)}"><header><span class="die-face-number">${index + 1}</span><div><p class="card-kicker">${themeIcon[terrain.theme]} ${themeDescriptor[terrain.theme]}</p><h2>${escapeHtml(face.label)}</h2></div><strong>${face.weight}<small>weight · ${odds}%</small></strong></header><div class="course-glyphs"><span>▣ ${terrain.width}×${terrain.height} · ${terrain.sizeProfile}</span><span>⌁ ${terrain.archetype}</span><span>⚠ ${terrain.sweeperCount}+${terrain.gateCount}</span><span>⌃ ${terrain.updraftCount} · 〰 ${terrain.gustCount}</span><span>◉ ${terrain.portalPairs}</span><span>✚ ${terrain.recoveryPads} · ✹ ${terrain.chaosPads}</span></div><div class="rule-chip-grid"><span>⏱ ${rules.timerSeconds}s</span><span>🎯 cap ${rules.strokeCap}</span><span>${rules.collisions ? '● collisions' : '○ no collisions'}</span><span>${rules.powerUps ? '🎁 items on' : '⊘ items off'}</span><span>★ ${Math.round(rules.scoreMultiplier * 100)}% score</span></div><footer>${face.addedBy ? `<small>added by ${escapeHtml(state.players.find((player) => player.id === face.addedBy)?.name ?? face.addedBy)}</small>` : '<small>base die face</small>'}${augmentedBy ? `<small>${augmentedBy}</small>` : ''}${canBet ? `<button data-augment-die-face="${face.id}" ${ownPlayer!.cash < augmentationCost ? 'disabled' : ''}>weight +1 · $${augmentationCost}</button>` : ''}</footer></article>`;
  };
  const bettors = state.players.map((player) => {
    const wager = die.wagers[player.id];
    return `<li class="${wager?.ready ? 'ready' : ''}"><i style="background:${player.color}"></i><strong>${escapeHtml(player.name)}</strong><span>$${player.cash}</span><small>${wager?.ready ? 'ready' : `${wager?.addedSides ?? 0} added · ${Object.values(wager?.augmentations ?? {}).reduce((total, amount) => total + amount, 0)} weight`}</small></li>`;
  }).join('');
  const prompt = die.roll ? `rolling… ${die.roll.secondsLeft.toFixed(1)}s` : canBet ? multiplayer.online ? 'place your wager, then ready the die' : `pass the device to ${escapeHtml(ownPlayer!.name)} · place a wager or ready` : 'waiting for the other golfers';
  return `<section class="die-overlay" role="dialog" aria-modal="true" aria-label="course die for hole ${state.hole}"><div class="die-panel"><header class="die-panel-header"><p class="eyebrow">HOLE ${state.hole} / ${state.config.holeCount} · SHARED COURSE DIE</p><h1>${die.roll ? 'the <em>course die</em> tumbles' : 'weight the <em>next hole</em>'}</h1><p id="die-prompt">${prompt}</p></header><section class="die-table"><div class="die-3d ${die.roll ? 'tumbling' : ''}" aria-label="top-down three-dimensional ${die.faces.length}-sided die"><span class="die-top">${selectedId ? die.faces.findIndex((face) => face.id === selectedId) + 1 : '?'}</span><span class="die-edge"></span></div><div class="die-actions">${canBet ? `<button class="primary" data-add-die-side ${ownPlayer!.cash < sideCost ? 'disabled' : ''}>add wild side · $${sideCost}</button><button data-ready-die-roll>ready to roll</button><small>new side costs rise $1, $2, $3… · weighting the same face rises every two boosts</small>` : `<strong>${die.roll ? 'no more bets' : 'wager locked'}</strong>`}</div><ol class="die-bettors">${bettors}</ol></section><section class="die-face-grid">${die.faces.map(packageFace).join('')}</section><footer class="die-panel-footer"><span>${die.faces.length} faces · total weight ${totalWeight}</span><span id="die-timer">${die.roll ? 'the selected face becomes this hole' : `${state.players.filter((player) => die.wagers[player.id]?.ready).length}/${state.players.length} ready · timer ${die.secondsLeft.toFixed(0)}s`}</span></footer></div></section>`;
};

const renderShopOverlay = (view: ViewModel) => {
  const { state, multiplayer, preferences } = view;
  const shop = state.shop;
  if (state.status !== 'shopping' || !shop) return '';
  const buyer = state.players.find((player) => player.id === shop.buyerOrder[shop.buyerIndex]);
  const localVoter = multiplayer.online
    ? state.players.find((player) => player.id === multiplayer.playerId && shop.rerollVotes[player.id] === undefined)
    : state.players.find((player) => player.kind === 'human' && shop.rerollVotes[player.id] === undefined);
  const canBuy = Boolean(buyer && (!multiplayer.online || buyer.id === multiplayer.playerId) && buyer.kind === 'human' && shop.rerollResolved);
  const buyerCash = buyer?.cash ?? -1;
  const brokerStacks = buyer?.caddies.find((caddy) => caddy.id === 'broker')?.stacks ?? 0;
  const holdingsFor = (player: typeof state.players[number]) => {
    const cards = player.pockets.length ? player.pockets : [player.inventory, player.spareInventory].filter(Boolean).map((id) => ({ id: id!, source: 'pad' as const, duration: undefined }));
    const cardList = cards.map((card) => `${escapeHtml(card.id)}${card.duration ? ` (${card.duration.amount} ${card.duration.unit}${card.duration.amount === 1 ? '' : 's'})` : ''}`).join(' · ') || 'none';
    const caddyList = player.caddies.map((caddy) => `${escapeHtml(caddy.id)} ×${caddy.stacks}`).join(' · ') || 'none';
    const effects = player.attachments?.map((attachment) => `${escapeHtml(attachment.cardId)} (${attachment.remaining} ${attachment.unit}${attachment.remaining === 1 ? '' : 's'})`).join(' · ') || 'none';
    const form = player.ballForm ? ` · form: ${escapeHtml(player.ballForm)}` : '';
    return `<span class="merchant-holdings"><small>boons</small> ${caddyList}</span><span class="merchant-holdings"><small>cards</small> ${cardList}</span>${effects !== 'none' ? `<span class="merchant-holdings"><small>effects</small> ${effects}${form}</span>` : form ? `<span class="merchant-holdings"><small>effects</small>${form}</span>` : ''}`;
  };
  const caddyRows = state.players.map((player) => `<li class="merchant-player ${buyer?.id === player.id ? 'active' : ''}"><i style="background:${player.color}"></i><strong>${escapeHtml(player.name)}</strong><b>$${player.cash}</b>${preferences.showMerchantHoldings ? holdingsFor(player) : '<span class="merchant-holdings hidden">holdings hidden</span>'}</li>`).join('');
  const cards = shop.shelf.map((offer) => {
    const definition = definitionFor(offer.contentId)!;
    const disabled = offer.sold || !canBuy || buyerCash < Math.max(0, offer.price - (definition.category === 'caddy' ? brokerStacks : 0));
    const disabledReason = offer.sold ? 'already claimed' : !shop.rerollResolved ? 'waiting for the reroll vote' : buyer?.kind === 'bot' ? 'waiting for the bot shopper' : !canBuy ? 'waiting for the current shopper' : buyerCash < Math.max(0, offer.price - (definition.category === 'caddy' ? brokerStacks : 0)) ? 'not enough cash' : '';
    const lifetime = offer.duration ? `<small class="card-lifetime">${definition.polarity ?? 'card'} · ${offer.duration.amount} ${offer.duration.unit}${offer.duration.amount === 1 ? '' : 's'}</small>` : definition.timing ? `<small class="card-lifetime">${definition.polarity ?? 'card'} · ${definition.timing === 'putt' ? 'next putt' : definition.timing}</small>` : '';
    return `<article class="merchant-card merchant-${definition.category} ${offer.sold ? 'sold' : ''}"><div class="merchant-card-top"><span>${definition.icon}</span><small>${definition.category}</small><b>$${offer.price}</b></div><h3>${escapeHtml(definition.id)}</h3><p>${escapeHtml(definition.description)}</p>${lifetime}<footer>${offer.sold ? 'claimed' : `<button data-shop-buy="${offer.id}" ${disabled ? `disabled title="${disabledReason}"` : ''}>buy</button>`}</footer></article>`;
  }).join('');
  const replacement = buyer && buyer.caddies.length >= 3 ? `<label class="merchant-replace">replace Caddy <select id="replace-caddy">${buyer.caddies.map((caddy) => `<option value="${escapeHtml(caddy.id)}">${escapeHtml(caddy.id)} ×${caddy.stacks}</option>`).join('')}</select></label>` : '';
  const reroll = !shop.rerollResolved
    ? `<section class="merchant-vote"><p>the dealer offers a free full-shelf reshuffle. every player votes.</p><div>${localVoter ? `<button class="primary" data-shop-reroll="yes">reroll yes · ${escapeHtml(localVoter.name)}</button><button data-shop-reroll="no">keep shelf</button>` : '<span>ballots locked · waiting for the table</span>'}</div><small>${Object.keys(shop.rerollVotes).length}/${state.players.length} votes · strict majority reshuffles</small></section>`
    : `<section class="merchant-action"><p>${buyer ? canBuy ? `your merchant turn · choose any one affordable card${shop.completedBuyerIds.length ? ` · ${shop.completedBuyerIds.length}/${state.players.length} shoppers complete` : ''}` : buyer.kind === 'bot' ? `${escapeHtml(buyer.name)} is choosing a card` : `${escapeHtml(buyer.name)} is shopping` : 'the merchant is closing'}</p>${replacement}${canBuy ? `<button data-shop-skip>skip merchant turn</button>${buyer!.caddies.length ? `<button data-shop-sell="${buyer!.caddies[0]!.id}">sell ${escapeHtml(buyer!.caddies[0]!.id)} · $3</button>` : ''}` : ''}<small id="merchant-timer">${shop.secondsLeft.toFixed(0)} seconds</small></section>`;
  return `<section class="merchant-overlay" role="dialog" aria-modal="true" aria-label="clubhouse merchant"><div class="merchant-table"><header class="merchant-header"><p class="eyebrow">THE NINETEENTH HOLE · SHARED MARKET</p><h1>the <em>clubhouse merchant</em> deals strange advantages</h1><p>one purchase each · the shelf is shared · Caddies stack</p></header>${reroll}<section class="merchant-shelf" aria-label="merchant shelf">${cards}</section><aside class="merchant-ledger"><h2>${preferences.showMerchantHoldings ? 'table holdings' : 'table ledger'}</h2><ol>${caddyRows}</ol><p>next reality: ${state.queuedReality ? escapeHtml(state.queuedReality) : 'none'}</p></aside></div></section>`;
};

const ordinal = (rank: number) => `${rank}${rank % 100 >= 11 && rank % 100 <= 13 ? 'th' : ({ 1: 'st', 2: 'nd', 3: 'rd' })[rank % 10] ?? 'th'}`;

const renderResultsOverlay = ({ state }: ViewModel) => {
  if (state.status !== 'finished') return '';
  const standings = [...state.players].sort((left, right) => left.total - right.total || left.name.localeCompare(right.name));
  const leadingScore = standings[0]!.total;
  const champions = standings.filter((player) => player.total === leadingScore);
  const trailingScore = standings.at(-1)!.total;
  const trailers = standings.filter((player) => player.total === trailingScore);
  const podium = standings.slice(0, 3);
  const podiumCard = (player: typeof standings[number], position: number) => {
    const rank = standings.findIndex((candidate) => candidate.total === player.total) + 1;
    const gap = player.total - leadingScore;
    const title = rank === 1 ? champions.length > 1 ? 'co-champion' : 'clubhouse champion' : `${ordinal(rank)} place`;
    return `<article class="podium-card podium-place-${position}"><span class="podium-medal">${position === 1 ? '★' : position === 2 ? '◆' : '●'}</span><i style="background:${player.color}"></i><p>${escapeHtml(title)}</p><h3>${escapeHtml(player.name)}</h3><strong>${player.total}<small>strokes</small></strong><footer>${gap ? `+${gap} from lead` : champions.length > 1 ? 'tied for lead' : 'lowest total'}</footer><b class="podium-plinth">${ordinal(rank)}</b></article>`;
  };
  const standingRows = standings.map((player) => {
    const rank = standings.findIndex((candidate) => candidate.total === player.total) + 1;
    const gap = player.total - leadingScore;
    const outcome = rank === 1 ? champions.length > 1 ? 'co-champion' : 'winner' : player.total === trailingScore && trailers.length < standings.length ? 'last place' : `+${gap}`;
    return `<li class="${rank === 1 ? 'winner' : player.total === trailingScore && trailers.length < standings.length ? 'last' : ''}"><span>${ordinal(rank)}</span><i style="background:${player.color}"></i><strong>${escapeHtml(player.name)}</strong><em>${outcome}</em><b>${player.total}</b></li>`;
  }).join('');
  const championNames = champions.map((player) => escapeHtml(player.name)).join(' + ');
  const trailingNames = trailers.map((player) => escapeHtml(player.name)).join(' + ');
  const headline = champions.length > 1 ? 'Clubhouse co-champions' : 'Clubhouse champion';
  const summary = champions.length > 1
    ? `${championNames} finish level on ${leadingScore} strokes.`
    : `${championNames} claims the trophy with ${leadingScore} strokes.`;
  const finalCallout = trailers.length === standings.length ? 'everyone finishes level.' : `${trailingNames} ${trailers.length > 1 ? 'share' : 'takes'} last place at ${trailingScore}.`;
  return `<section class="results-overlay" role="dialog" aria-modal="true" aria-label="campaign results"><div class="results-panel"><header class="results-header"><p class="eyebrow">NINE HOLES COMPLETE · FINAL CLUBHOUSE TABLE</p><h1>${headline}</h1><p>${summary} ${finalCallout}</p><small class="campaign-atlas-note">the complete course route remains visible behind the final table</small></header><section class="podium" aria-label="top three podium">${podium.map(podiumCard).join('')}</section><section class="final-standings" aria-label="final standings"><div><h2>full standings</h2><p>lowest aggregate strokes wins</p></div><ol>${standingRows}</ol></section><footer class="results-actions"><button class="primary" data-restart-run>play again</button><span>same lineup · replay this seed</span></footer></div></section>`;
};

const renderPauseOverlay = ({ state, multiplayer }: ViewModel) => {
  if (!state.paused) return '';
  const canResume = !multiplayer.online || multiplayer.host;
  return `<section class="pause-overlay" role="dialog" aria-modal="true" aria-label="match paused"><div class="pause-card"><p class="eyebrow">${multiplayer.online ? 'ONLINE ROOM PAUSED' : 'LOCAL MATCH PAUSED'}</p><h2>take a breather</h2><p>${canResume ? 'Timers, bot turns, and gameplay input are frozen.' : 'Only the room host can resume this match.'}</p><button class="primary" data-toggle-pause ${canResume ? '' : 'disabled'}>resume match</button><button data-open-overlay="settings">settings</button></div></section>`;
};

const renderInspector = (view: ViewModel) => {
  const { state } = view;
  if (state.status === 'rolling') return `${renderLedger(view.ledger)}<p class="hint">Every player may add a complete wild face or pay to weight a visible face before the shared course die lands.</p>`;
  if (state.status === 'shopping') return `${renderLedger(view.ledger)}<p class="hint">The clubhouse merchant sells persistent Caddies, contraband, Reality Cards, and Chrono Cards.</p>`;
  if (state.status === 'transitioning') return `${renderLedger(view.ledger)}<p class="hint">The completed cup becomes the next tee while the arena expands into open terrain. Previous fairways stay playable.</p>`;
  const features = (state.course.features ?? []).map((feature) => feature.kind === 'sinkhole' ? '↻ paired sinkhole' : feature.kind === 'thorn' ? '✽ thorn knockback' : feature.kind === 'pulse' ? '⌁ pulse launch' : feature.kind === 'gust' ? '〰 gust lane' : '◯ air ring boost').join(' · ') || 'none';
  const player = current(state);
  const caddies = player.caddies.map((caddy) => `${caddy.id}${caddy.stacks > 1 ? ` ×${caddy.stacks}` : ''}`).join(', ') || 'none';
  return `${renderLedger(view.ledger)}<h3>active package</h3><dl><dt>biome</dt><dd>${themeIcon[state.course.theme]} ${themeDescriptor[state.course.theme]}</dd><dt>shape</dt><dd>${state.course.archetype ?? 'ribbon'} · ${state.course.sizeProfile ?? 'standard'}</dd><dt>rules</dt><dd>${escapeHtml(ruleSummary(state.holeRules))}</dd><dt>your cash</dt><dd>$${player.cash}</dd><dt>your Caddies</dt><dd>${escapeHtml(caddies)}</dd><dt>reality</dt><dd>${state.activeReality ?? state.queuedReality ?? 'stable'}</dd><dt>hazards</dt><dd>${state.course.hazards.map((hazard) => hazard.kind).join(' + ') || 'none'}</dd><dt>biome effects</dt><dd>${features}</dd><dt>portal pairs</dt><dd>${state.course.portals?.filter((pair) => pair.entrance && pair.exit).length ?? 0}</dd><dt>item pads</dt><dd>${state.course.itemPads.length}</dd></dl><h3>course legend</h3><p class="legend">fairway grass · rough · sand bunker · water ice · cushion turf · spring tile · bumper bank<br>blue: slow mover · amber: standard mover · red: fast mover<br>colored arm: sweeper · colored gate: timed gate · cyan gust: airborne updraft · brown bar: chip-height blocker · dashed cyan: gust lane<br>↻ paired sinkhole · ✽ thorn knockback · ⌁ pulse launch · ◯ air-ring boost<br>gold $: cash pad · cyan +: recovery pad · violet !: chaos pad · ↑/⌁/✹/≋/⌃: player gadgets</p>`;
};

const renderDrawer = (view: ViewModel) => {
  if (!view.drawer) return '';
  const title = view.drawer === 'run' ? 'run controls' : view.state.status === 'rolling' ? 'course die' : 'course intel';
  return `<aside class="drawer drawer-${view.drawer} panel" aria-label="${title}"><div class="drawer-heading"><h2>${title}</h2><button data-close-drawer class="close" aria-label="close ${title}">×</button></div><div class="drawer-content">${view.drawer === 'run' ? renderRunDrawer(view) : renderInspector(view)}</div></aside>`;
};

export const renderAppMarkup = (view: ViewModel) => {
  const { state } = view;
  const progress = `HOLE <b>${state.hole}</b> / ${state.config.holeCount}`;
  const shellState = state.status === 'rolling' ? 'rolling' : state.status === 'shopping' ? 'shopping' : state.status === 'transitioning' ? 'transitioning' : state.status === 'finished' ? 'finished' : '';
  const roomLabel = view.multiplayer.online ? `ONLINE · ${escapeHtml(view.multiplayer.roomCode ?? 'connecting')}` : view.multiplayer.localMultiplayer ? view.multiplayer.controllerName ? `LOCAL MULTIPLAYER · PAD · ${escapeHtml(view.multiplayer.controllerName)}` : 'LOCAL MULTIPLAYER' : view.multiplayer.controllerName ? `PAD · ${escapeHtml(view.multiplayer.controllerName)}` : 'LOCAL PARTY';
  return `<main class="app-shell ${shellState}"><section class="topbar"><div class="brand"><p class="eyebrow">${roomLabel}</p><h1>GOLF <em>WITH YOUR</em> ENEMIES</h1></div><div class="title-actions"><button data-toggle-pause ${state.status === 'finished' ? 'disabled' : ''}>pause</button><button data-drawer="run" class="${view.drawer === 'run' ? 'selected' : ''}" aria-expanded="${view.drawer === 'run'}">run</button><button data-drawer="intel" class="${view.drawer === 'intel' ? 'selected' : ''}" aria-expanded="${view.drawer === 'intel'}">${state.status === 'rolling' ? 'die' : 'intel'}</button><button data-open-overlay="help">? help</button><button class="icon-button" data-open-overlay="settings" aria-label="open settings" title="settings"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 15.2a3.2 3.2 0 1 0 0-6.4 3.2 3.2 0 0 0 0 6.4Z"/><path d="M19.4 13.5a7.7 7.7 0 0 0 .1-1.5 7.7 7.7 0 0 0-.1-1.5l2-1.5-2-3.4-2.4 1a8 8 0 0 0-2.6-1.5L14.1 2h-4l-.4 3.1a8 8 0 0 0-2.6 1.5l-2.4-1-2 3.4 2 1.5a7.7 7.7 0 0 0-.1 1.5c0 .5 0 1 .1 1.5l-2 1.5 2 3.4 2.4-1a8 8 0 0 0 2.6 1.5l.4 3.1h4l.4-3.1a8 8 0 0 0 2.6-1.5l2.4 1 2-3.4-2.1-1.5Z"/></svg></button><div class="hole">${progress}<br><small>${escapeHtml(state.course.seed)}</small></div></div></section><section class="layout"><section class="board panel"><div class="course-stage"><canvas id="course" aria-label="isometric arcade mini golf course"></canvas>${renderMatchHud(view)}<div id="callouts" class="callouts" aria-live="polite">${renderCallouts(view.callouts)}</div></div><div id="controls" class="controls"></div></section></section>${renderDrawer(view)}</main>${renderDieOverlay(view)}${renderShopOverlay(view)}${renderResultsOverlay(view)}${renderPauseOverlay(view)}${renderOverlay(view)}`;
};
