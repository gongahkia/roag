import { EMOTES, type BuildPieceId, type ChronoCard, type CoursePackage, type GadgetKind, type GameConfig, type GameState, type HoleRules, type Point, type PowerUp, type ShotCommand } from '../core/types';
import { buildPieceDetail } from '../core/construction';
import { definitionFor } from '../core/catalog';
import { partyAwardsFor, partyPacingFor } from '../core/party-insights';
import { rulesetFor, trickCardDetails } from '../core/rulesets';
import { bindingFor, type GamePreferences, type ShortcutId } from '../preferences';

export type Overlay = 'help' | 'settings' | undefined;
export type Drawer = 'run' | 'intel' | undefined;

export interface LedgerEntry { message: string; tone: string; }
export interface Callout { message: string; tone: string; }
export interface MultiplayerView { online: boolean; connected: boolean; roomCode?: string; playerId?: string; host: boolean; controllerName?: string; }

export interface ViewModel {
  state: GameState;
  config: GameConfig;
  preferences: GamePreferences;
  overlay: Overlay;
  drawer: Drawer;
  rebinding?: ShortcutId;
  aim: ShotCommand;
  buildPiece?: BuildPieceId;
  placement?: { kind: GadgetKind; point?: Point; valid: boolean; confirmed?: boolean };
  shotInFlight: boolean;
  camera?: { mode: 'follow' | 'free'; zoom: number };
  multiplayer: MultiplayerView;
  ledger: readonly LedgerEntry[];
  callouts: readonly Callout[];
  briefingHole?: number;
  handoff?: { playerName: string; color: string; hole: number };
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
const routeRoleDetail = {
  safe: { icon: '◆', text: 'wide, steady route' },
  skill: { icon: '▲', text: 'timing, banks, or chips' },
  conflict: { icon: '✦', text: 'shared contested space' },
} as const;
const powerUpIcon: Record<string, string> = { turbo: '↯', shield: '⬡', bomb: '✹', freeze: '❄', swap: '⇄', 'two putts': '2P', heavy: '●', bouncy: '◌', ghost: '◐', magnet: '🧲', ice: '❄', portal: '◉', glider: '⌒', sticky: '▣', orbit: '◎', quantum: '∞', mirror: '◇', anvil: '⬟', vampire: '☽', boomerang: '↶', 'cup magnet': '⊙', slipstream: '➳', 'rebound rig': '↩', 'phase shift': '⇤', sandbag: '▰', 'rescue drone': '✈', airhorn: '📣', 'club flipper': '↻', 'time dilator': '⌛', mugger: '♜', scramble: '⇆', 'gravity gloves': '☄', 'bunker buster': '⛏', 'portal remote': '◉', 'red tee': '⚑', 'black flag': '⚐', 'cherry bomb': '✹', copycat: '▣', 'wind sock': '〰', 'slope stabilizer': '⛰', 'spring polish': '⌃', 'bumper wax': '◇', 'cushion map': '▤', 'popper pad': '↑', 'snare patch': '⌁', 'blast mine': '✹', 'slick patch': '≋', 'sky spring': '⌃', 'gravity well': '◉', 'mirror plate': '◇', 'toll booth': '$', 'control inverter': '↻', 'portal gun': '◉', 'undo drive': '↶', 'second chance': '↺', 'echo putt': '◌', 'future sight': '⌘', 'time theft': '⌛', 'frozen frame': '❄', 'parallel parking': '⇆', 'grandfather clause': '⌫', 'anchor line': '⚓', 'spring ticket': '⌃', 'cushion call': '▤', 'bumper lease': '◇', 'shared draft': '⇄', 'wind sail': '〰', 'grounds crew': '⛰', 'rescue pact': '✚', 'clubhouse pool': '$', 'sticky forecast': '▤', 'crosswind debt': '〰', 'dead bounce': '◇' };

export const renderStatus = ({ state, shotInFlight }: Pick<ViewModel, 'state' | 'shotInFlight'>) => {
  if (state.paused) return 'match paused';
  if (state.status === 'building') return `${escapeHtml(current(state).name)} is placing a course module · ${state.turn.secondsLeft.toFixed(0)}s remaining`;
  if (state.status === 'shopping') return `clubhouse merchant · ${state.shop?.secondsLeft.toFixed(0) ?? 0}s remaining`;
  if (state.status === 'transitioning') return `expanding the campaign to hole ${state.hole}/${state.config.holeCount}`;
  if (state.status === 'finished') return 'campaign complete — the full route is on display';
  if (shotInFlight) return `${escapeHtml(current(state).name)}'s ball is in flight · moving hazards are live`;
  return `${escapeHtml(current(state).name)} is taking a turn · moving hazards are live`;
};

export const renderCallouts = (callouts: readonly Callout[]) => callouts.map((callout) => `<p class="callout ${callout.tone}">${escapeHtml(callout.message)}</p>`).join('');
const renderLedger = (ledger: readonly LedgerEntry[]) => `<ul class="feed" aria-live="polite">${ledger.map((entry) => `<li class="${entry.tone}"><span>${escapeHtml(entry.message)}</span></li>`).join('') || '<li class="neutral"><span>the shuffler is armed</span></li>'}</ul>`;

const renderTransitionControls = (state: GameState) => `<div class="turn"><strong>next hole</strong><b>${state.hole}/${state.config.holeCount}</b><small class="phase">the route is building outward</small></div><p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;
const renderFinishedControls = (state: GameState) => `<div class="turn"><strong>campaign complete</strong><b>★</b><small class="phase">the full route has zoomed out behind the results</small></div><p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;

const renderBuildHud = (view: ViewModel) => {
  const { state, multiplayer, buildPiece } = view;
  const builder = current(state);
  const viewerId = multiplayer.online ? multiplayer.playerId : builder.id;
  const contract = state.construction?.contracts.find((candidate) => candidate.ownerId === viewerId);
  const placed = state.course.buildSockets?.filter((socket) => socket.pieceId).length ?? 0;
  const total = state.course.buildSockets?.length ?? 0;
  const ownContract = contract?.hidden ? undefined : contract;
  return `<section class="match-hud build-hud" aria-label="course construction status"><section class="hud-course"><p>COURSEWRIGHT ROUND</p><strong>${themeIcon[state.course.theme]} build the hole</strong><small>hole ${state.hole}/${state.config.holeCount} · ${placed}/${total} modules installed · the fairway spine stays open</small></section><section class="hud-timer"><p>ON THE WORKBENCH</p><strong>${escapeHtml(builder.name)}</strong><b id="hud-turn-timer">${state.turn.secondsLeft.toFixed(0)}<small>s</small></b><small class="hud-turn-status">choose one of your two modules</small></section><section class="hud-leaderboard" aria-label="scoreboard"><header><span>SCOREBOARD</span><small>total</small></header><ol>${[...state.players].sort((left, right) => left.total - right.total || left.name.localeCompare(right.name)).map((player, index) => `<li class="${player.id === builder.id ? 'active' : ''}"><span>${index + 1}</span><i style="background:${player.color}"></i><strong>${escapeHtml(player.name)}</strong><b>${player.total}</b></li>`).join('')}</ol></section><section class="hud-strength build-contract"><div class="hud-strength-heading"><span>SECRET CONTRACT</span><b>${ownContract?.completed ? '+1' : '—'}</b></div><p>${ownContract ? `${escapeHtml(ownContract.label ?? 'architect contract')} · ${escapeHtml(ownContract.description ?? 'complete it during play for −1 stroke')}` : 'Pass the device when prompted. Your contract is private until it resolves.'}</p><small>${buildPiece ? `selected: ${escapeHtml(buildPieceDetail[buildPiece].label)}` : 'select a module, then click an open socket'}</small></section></section>`;
};

const renderMatchHud = (view: ViewModel) => {
  const { state, aim, multiplayer, shotInFlight } = view;
  const camera = view.camera ?? { mode: 'follow' as const, zoom: 1 };
  if (state.status === 'building') return renderBuildHud(view);
  if (state.status !== 'playing') return '';
  const player = current(state);
  const disabled = state.paused || player.kind !== 'human' || shotInFlight || (multiplayer.online && multiplayer.playerId !== player.id);
  const strength = Math.round(Math.max(0, Math.min(1, (aim.power - 1) / 7)) * 100);
  const leaderboard = [...state.players]
    .sort((left, right) => left.total - right.total || left.ball.strokes - right.ball.strokes || left.name.localeCompare(right.name))
    .map((candidate, index) => `<li class="${candidate.id === player.id ? 'active' : ''}"><span>${index + 1}</span><i style="background:${candidate.color}"></i><strong>${escapeHtml(candidate.name)}</strong><b>${candidate.total}</b></li>`)
    .join('');
  const shotKind = player.forcedChip ? 'chip' : aim.kind ?? 'putt';
  const routeRoles = state.course.routeRoles?.map((assignment) => assignment.label).join(' · ') ?? 'safe line · skill line · conflict line';
  const routeKey = (state.course.routeRoles?.map((assignment) => assignment.role) ?? ['safe', 'skill', 'conflict'] as const)
    .map((role) => `<span class="route-role-chip route-${role}" title="${routeRoleDetail[role].text}"><b aria-hidden="true">${routeRoleDetail[role].icon}</b>${role}</span>`)
    .join('');
  const chaos = state.coursePlan[state.hole - 1]?.recipe.metadata?.resolvedReels.chaos ?? [];
  const cameraStatus = camera.mode === 'free' ? 'FREE ROAM · DRAG TO PAN' : `FOLLOW BALL · ${Math.round(camera.zoom * 100)}%`;
  return `<section class="match-hud" aria-label="match heads-up display"><section class="hud-course"><p>COURSE</p><strong>${themeIcon[state.course.theme]} ${escapeHtml(themeDescriptor[state.course.theme])}</strong><small>hole ${state.hole} · ${escapeHtml(routeRoles)}${chaos.length ? ` · chaos: ${escapeHtml(chaos.join(' + '))}` : ''}</small><div class="route-role-key" aria-label="route role key: safe is wide and steady, skill rewards timing, banks, or chips, conflict is shared contested space">${routeKey}</div></section><section class="hud-timer"><p>ON THE TEE</p><strong>${escapeHtml(player.name)}</strong><b id="hud-turn-timer">${state.turn.secondsLeft.toFixed(0)}<small>s</small></b><small class="hud-turn-status">${shotInFlight ? 'ball in play · hazards live' : `${shotKind} ready · hazards live`}</small></section><section class="hud-leaderboard" aria-label="leaderboard"><header><span>LEADERBOARD</span><small>total</small></header><ol>${leaderboard}</ol></section><section class="reaction-dock" aria-label="reactions"><span>reactions</span><div>${EMOTES.map((emote) => `<button data-emote="${emote.id}" type="button" aria-label="send ${emote.label}" title="${emote.label}" ${disabled ? 'disabled' : ''}>${emote.glyph}</button>`).join('')}</div></section><section class="hud-camera" aria-label="camera controls"><span id="hud-camera-status">${cameraStatus}</span><div><button data-camera-zoom="out" type="button" aria-label="zoom out" title="zoom out">−</button><button data-camera-mode type="button" aria-pressed="${camera.mode === 'free'}">${camera.mode === 'free' ? 'follow ball' : 'free roam'}</button><button data-camera-zoom="in" type="button" aria-label="zoom in" title="zoom in">+</button></div></section><section class="hud-strength" aria-label="${shotKind} strength; drag with the left button to putt or the right button to chip"><div class="hud-strength-heading"><span id="hud-shot-label">${shotKind === 'chip' ? 'CHIP' : 'PUTT'} STRENGTH</span><b id="hud-strength-value">${aim.power.toFixed(1)}</b></div><div id="hud-strength-meter" class="hud-strength-meter" style="--strength:${strength}%"><i></i></div><div class="hud-shot-help"><span><b>left drag</b> putt</span><span><b>right drag</b> chip</span></div></section></section>`;
};

export const renderControlsMarkup = (view: ViewModel) => {
  const { state, preferences, shotInFlight, multiplayer, placement, buildPiece } = view;
  if (state.status === 'shopping') return `<div class="turn merchant-turn"><strong>clubhouse merchant</strong><b>$${playerCash(state, current(state).id)}</b><small class="phase">the shared shelf is open</small></div><p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;
  if (state.status === 'transitioning') return renderTransitionControls(state);
  if (state.status === 'finished') return renderFinishedControls(state);
  const player = current(state);
  if (state.status === 'building') {
    const hand = state.construction?.hands[player.id] ?? [];
    const canBuild = !state.paused && player.kind === 'human' && (!multiplayer.online || multiplayer.playerId === player.id);
    const sockets = state.course.buildSockets?.filter((socket) => !socket.pieceId) ?? [];
    return `<div class="build-controls"><div class="turn"><strong>build turn</strong><b>${escapeHtml(player.name)}</b><small class="phase">place one module in an open cyan socket</small></div><div class="build-piece-list">${hand.map((pieceId) => `<button class="build-piece ${buildPiece === pieceId ? 'selected' : ''}" data-build-piece="${pieceId}" ${canBuild ? '' : 'disabled'}><b>${buildPieceDetail[pieceId].glyph}</b><span>${escapeHtml(buildPieceDetail[pieceId].label)}<small>${escapeHtml(buildPieceDetail[pieceId].description)}</small></span></button>`).join('') || '<p class="control-status">your modules are installed</p>'}</div>${buildPiece && sockets.length ? `<div class="build-socket-list"><small>or choose a socket</small>${sockets.map((socket, index) => `<button data-build-socket="${socket.id}" ${canBuild ? '' : 'disabled'}>${String.fromCharCode(65 + index)}</button>`).join('')}</div>` : ''}<p class="control-status">${renderStatus({ state, shotInFlight: false })}</p></div>`;
  }
  const ruleset = rulesetFor(state.config);
  const disabled = state.status !== 'playing' || state.paused || player.kind !== 'human' || shotInFlight || (multiplayer.online && multiplayer.playerId !== player.id);
  const cardDisabled = disabled || state.turn.cardPlayed;
  const heldCards = player.pockets.length ? player.pockets : [player.inventory, player.spareInventory].filter(Boolean).map((id, index) => ({ id: id!, source: 'pad' as const, instanceId: `legacy-ui-${index}`, duration: undefined }));
  const heldItems = heldCards.map((card) => card.id) as (PowerUp | ChronoCard)[];
  const tileCards = new Set<PowerUp | ChronoCard>(['popper pad', 'snare patch', 'blast mine', 'slick patch', 'sky spring', 'gravity well', 'mirror plate', 'toll booth', 'control inverter', 'portal gun', 'bunker buster']);
  const targets = state.players.filter((candidate) => !candidate.ball.complete);
  const targetCandidates = ruleset.id === 'party' ? targets.filter((candidate) => candidate.id !== player.id) : targets;
  const targetSelector = heldItems.some((powerUp) => !tileCards.has(powerUp) && powerUp !== 'freeze' && (ruleset.id !== 'party' || !['turbo', 'shield', 'heavy', 'rescue drone', 'glider'].includes(powerUp))) && targetCandidates.length
    ? `<label>play on <select id="powerup-target" ${disabled ? 'disabled' : ''}>${targetCandidates.map((candidate) => `<option value="${candidate.id}">${escapeHtml(candidate.name)}${candidate.id === player.id ? ' (self)' : ''}</option>`).join('')}</select></label>`
    : '';
  const freezeSelector = heldItems.includes('freeze') && state.course.hazards.some((hazard) => hazard.kind === 'sweeper' || hazard.kind === 'gate')
    ? `<label>freeze obstacle <select id="hazard-target" ${disabled ? 'disabled' : ''}>${state.course.hazards.filter((hazard) => hazard.kind === 'sweeper' || hazard.kind === 'gate').map((hazard) => `<option value="${hazard.id}">${escapeHtml(hazard.kind)} ${escapeHtml(hazard.id)}</option>`).join('')}</select></label>`
    : '';
  const portalExits = state.course.portals?.filter((pair) => pair.exit).map((pair) => `<option value="${pair.id}:exit">${escapeHtml(pair.id)} exit</option>`).join('') ?? '';
  return `
    ${player.ballForm ? `<span class="active-form">next shot: ${escapeHtml(player.ballForm)} ball</span>` : ''}
    ${player.forcedChip ? '<span class="active-form">airhorn: next shot is a chip</span>' : ''}
    ${heldItems.includes('portal') || heldItems.includes('portal remote') ? `<label>portal exit <select id="portal-exit" ${disabled ? 'disabled' : ''}>${portalExits}</select></label>` : ''}
    ${targetSelector}${freezeSelector}
    ${heldCards.length ? heldCards.map((card) => {
      const definition = definitionFor(card.id as never);
      const partyDetail = ruleset.id === 'party' && card.id in trickCardDetails ? trickCardDetails[card.id as keyof typeof trickCardDetails] : undefined;
      const detail = partyDetail ? `${partyDetail.target} · ${partyDetail.duration}` : card.duration ? `${card.duration.amount} ${card.duration.unit}${card.duration.amount === 1 ? '' : 's'}` : definition?.timing === 'putt' ? 'next putt' : definition?.timing === 'immediate' ? 'instant' : 'item';
      const role = definition?.polarity && definition.polarity !== 'neutral' ? `${definition.polarity} · ` : '';
      const unavailable = (card.id === 'freeze' && !state.course.hazards.some((hazard) => hazard.kind === 'sweeper' || hazard.kind === 'gate'))
        || (ruleset.id === 'party' && card.id === 'airhorn' && !targets.some((candidate) => candidate.id !== player.id));
      return `<button data-use-powerup="${escapeHtml(card.id)}" data-card-id="${escapeHtml(card.instanceId ?? '')}" ${(cardDisabled || unavailable) ? 'disabled' : ''}>use ${escapeHtml(card.id)} <small>${role}${detail}</small>${card.id === player.inventory ? ` <kbd>${keyLabel(bindingFor(preferences, 'usePowerUp'))}</kbd>` : ''}</button>`;
    }).join('') : ''}
    ${placement ? `<p class="placement-status ${placement.valid ? 'valid' : 'invalid'}">${powerUpIcon[placement.kind]} ${escapeHtml(placement.kind)} · ${placement.point ? placement.valid ? placement.confirmed ? 'click again or press Enter to place' : 'click this tile to lock the preview' : 'choose an open playable tile' : 'click a tile to preview'} <button data-cancel-placement>cancel</button></p>` : ''}
    ${player.secondWindAvailable && !player.twoPuttsArmed ? `<button id="second-wind" ${disabled ? 'disabled' : ''}>use second wind: two putts</button>` : ''}
    ${state.turn.cardPlayed ? '<p class="control-status">one card committed this turn</p>' : ''}
    ${state.players.some((candidate) => candidate.attachments?.length) ? `<div class="card-effects"><strong>table cards</strong>${state.players.filter((candidate) => candidate.attachments?.length).map((candidate) => `<p><i style="background:${candidate.color}"></i>${escapeHtml(candidate.name)} · ${candidate.attachments!.map((attachment) => `${escapeHtml(attachment.cardId)} (${attachment.remaining} ${attachment.unit}${attachment.remaining === 1 ? '' : 's'})`).join(', ')}</p>`).join('')}</div>` : ''}
    ${preferences.showPartyDiagnostics && ruleset.id === 'party' ? (() => { const pacing = partyPacingFor(state); return `<p class="party-diagnostics">diagnostics · ${pacing.measuredTurns ? `${pacing.measuredTurns} measured turns · p90 ${pacing.p90TurnSeconds?.toFixed(1)}s` : 'waiting for completed decisions'} · active-hole median ${pacing.medianHoleSeconds?.toFixed(1) ?? '—'}s</p>`; })() : ''}`;
};

const playerCash = (state: GameState, id: string) => state.players.find((player) => player.id === id)?.cash ?? 0;

interface PartyGuidePanel { step: number; eyebrow: string; title: string; body: string; }

/** One contextual instruction at a time; it is local preference state, never match state. */
export const partyGuidePanelFor = (state: GameState, preferences: GamePreferences): PartyGuidePanel | undefined => {
  if (state.config.ruleset !== 'party' || preferences.partyGuideStep >= 8) return undefined;
  const step = preferences.partyGuideStep;
  const panels: Record<number, PartyGuidePanel> = {
    0: { step, eyebrow: 'WELCOME TO PARTY RULES', title: 'Play the problem together.', body: 'The shuffler locks each hole before the round starts. Everyone plays the same strange physical result, then owns the chaos that follows.' },
    1: { step, eyebrow: 'COURSE SHUFFLER', title: 'No votes. No rerolls.', body: 'Every hole is locked from the campaign seed before tee-off. The shuffler throws one or two random chaos modifiers into the next problem.' },
    2: { step, eyebrow: 'COURSE BRIEFING', title: 'Read the three route roles.', body: 'Safe is wide and steady. Skill rewards timing or a clean chip. Conflict passes through contested space where balls and gadgets can matter.' },
    3: { step, eyebrow: 'YOUR SHOT', title: 'Pull, aim, commit.', body: 'Left-drag makes a grounded putt. Right-drag makes a chip. The line and strength meter are a preview—not a random accuracy roll.' },
    4: { step, eyebrow: 'TRICK CARDS', title: 'One visible trick per shot.', body: 'Cards say who or what they affect and when they expire. Self cards improve your line; attack cards must name a target or obstacle.' },
    5: { step, eyebrow: 'THE CLUBHOUSE', title: 'Choose one card or pass.', body: 'The shared shelf is deliberately small. Watch the table’s choices, then keep only the trick you want to carry forward.' },
    6: { step, eyebrow: 'CONNECTED COURSE', title: 'The disaster keeps growing.', body: 'Each cup becomes the next tee. Earlier holes stay visible as an atlas, while only the current hole’s hazards stay active.' },
  };
  const panel = panels[step];
  if (!panel) return undefined;
  if (step === 0 && state.status !== 'finished') return panel;
  if (step === 1 && state.status === 'playing') return panel;
  if (step >= 2 && step <= 4 && state.status === 'playing') return panel;
  if (step === 5 && state.status === 'shopping') return panel;
  if (step === 6 && (state.status === 'transitioning' || state.hole > 1 || state.status === 'finished')) return panel;
  return undefined;
};

const shortcutRows = (preferences: GamePreferences, rebinding: ShortcutId | undefined, interactive = false) => ['shoot', 'powerDown', 'powerUp', 'usePowerUp', 'pause', 'help', 'settings'].map((id) => {
  const shortcutId = id as ShortcutId;
  const label = ({ shoot: 'shoot', powerDown: 'power down', powerUp: 'power up', usePowerUp: 'use chaos item', pause: 'pause match', help: 'shortcut help', settings: 'settings' })[shortcutId];
  const key = keyLabel(bindingFor(preferences, shortcutId));
  return `<dt>${label}</dt><dd>${interactive ? `<button data-bind="${shortcutId}" class="key-button ${rebinding === shortcutId ? 'selected' : ''}">${key}</button>` : `<kbd>${key}</kbd>`}</dd>`;
}).join('');

const renderOverlay = (view: ViewModel) => {
  const { overlay, preferences, rebinding } = view;
  const partyLayers = `${renderCourseBriefing(view)}${renderHandoff(view)}${renderPartyGuide(view)}`;
  if (!overlay) return partyLayers;
  if (overlay === 'help') return `${partyLayers}<section class="overlay" role="dialog" aria-modal="true" aria-label="shortcut help"><div class="overlay-card"><button class="close" data-close-overlay aria-label="close help">×</button><p class="eyebrow">MOUSE FIRST · KEYS READY</p><h2>quick commands</h2><dl class="shortcut-list">${shortcutRows(preferences, rebinding)}</dl><p class="hint">Controller menus use D-pad or left stick to navigate, A to select, and B goes back. On the course, left stick aims, A shoots, B uses the held item, Y toggles putt / chip, View opens game controls, and Menu/Start pauses.</p></div></section>`;
  return `${partyLayers}<section class="overlay" role="dialog" aria-modal="true" aria-label="game settings"><div class="overlay-card settings-card"><button class="close" data-close-overlay aria-label="close settings">×</button><p class="eyebrow">LOCAL PREFERENCES</p><h2>settings</h2><label class="setting-toggle"><input data-preference="reducedMotion" type="checkbox" ${preferences.reducedMotion ? 'checked' : ''}> reduced motion and flash</label><label class="setting-toggle"><input data-preference="highContrast" type="checkbox" ${preferences.highContrast ? 'checked' : ''}> high-contrast palette</label><label class="setting-toggle"><input data-preference="controllerVibration" type="checkbox" ${preferences.controllerVibration ? 'checked' : ''}> controller vibration</label><label class="setting-toggle"><input data-preference="showMerchantHoldings" type="checkbox" ${preferences.showMerchantHoldings ? 'checked' : ''}> show table cards and boons in merchant</label><label class="setting-toggle"><input data-preference="showPartyDiagnostics" type="checkbox" ${preferences.showPartyDiagnostics ? 'checked' : ''}> show local Party Rules diagnostics</label><p class="setting-note">mouse shots: left-drag to putt or right-drag to chip, then release to strike</p><label>master volume <input data-preference-range="masterVolume" type="range" min="0" max="1" step="0.05" value="${preferences.masterVolume}"></label><label>effects volume <input data-preference-range="effectsVolume" type="range" min="0" max="1" step="0.05" value="${preferences.effectsVolume}"></label><label>controller deadzone <input data-preference-range="controllerDeadzone" type="range" min="0.05" max="0.5" step="0.01" value="${preferences.controllerDeadzone}"></label><label>controller aim sensitivity <input data-preference-range="controllerAimSensitivity" type="range" min="0.5" max="2" step="0.05" value="${preferences.controllerAimSensitivity}"></label><section class="developer-controls"><strong>developer</strong><p>Replay the contextual Party Rules guide on this device.</p><button data-reset-party-guide>reset Party Rules guide</button></section><h3>shortcuts</h3><dl class="shortcut-list">${shortcutRows(preferences, rebinding, true)}</dl><p class="hint">${rebinding ? `press a key for ${escapeHtml(rebinding)} · Esc cancels` : 'select a key to rebind it'}</p></div></section>`;
};

const renderRunDrawer = ({ state, config }: ViewModel) => {
  const scoreRows = state.players.map((player) => `<tr class="${player.id === current(state).id ? 'active' : ''}"><td><i style="background:${player.color}"></i>${escapeHtml(player.name)}</td><td>${player.ball.complete ? '✓' : player.ball.strokes}</td><td>${player.total}</td></tr>`).join('');
  return `<h3>campaign controls</h3><label>run seed <input id="seed" value="${escapeHtml(config.seed)}" maxlength="32"></label><div class="split"><label>humans <input id="humans" type="number" min="1" max="8" value="${config.humanCount}"></label><label>AI <input id="bots" type="number" min="0" max="4" value="${config.botCount}"></label></div><label>AI skill <select id="skill"><option value="adaptive" ${config.botSkill === 'adaptive' ? 'selected' : ''}>adaptive</option>${Array.from({ length: 10 }, (_, index) => `<option value="${index + 1}" ${config.botSkill === index + 1 ? 'selected' : ''}>${index + 1}</option>`).join('')}</select></label><button id="new-run">start new nine-hole run</button><p class="hint">The campaign seed pre-shuffles every course with one or more random chaos modifiers. Play the result immediately.</p><h3>scorecard</h3><table><thead><tr><th>player</th><th>hole</th><th>total</th></tr></thead><tbody>${scoreRows}</tbody></table>`;
};

const renderShopOverlay = (view: ViewModel) => {
  const { state, multiplayer, preferences } = view;
  const shop = state.shop;
  if (state.status !== 'shopping' || !shop) return '';
  const ruleset = rulesetFor(state.config);
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
    return `<article class="merchant-card merchant-${definition.category} ${offer.sold ? 'sold' : ''}"><div class="merchant-card-top"><small>${definition.category}</small><b>$${offer.price}</b></div><span class="merchant-card-icon" aria-hidden="true">${definition.icon}</span><h3>${escapeHtml(definition.id)}</h3><p>${escapeHtml(definition.description)}</p>${lifetime}<footer>${offer.sold ? 'claimed' : `<button data-shop-buy="${offer.id}" ${disabled ? `disabled title="${disabledReason}"` : ''}>buy</button>`}</footer></article>`;
  }).join('');
  const replacement = buyer && buyer.caddies.length >= 3 ? `<label class="merchant-replace">replace Caddy <select id="replace-caddy">${buyer.caddies.map((caddy) => `<option value="${escapeHtml(caddy.id)}">${escapeHtml(caddy.id)} ×${caddy.stacks}</option>`).join('')}</select></label>` : '';
  const cardReplacement = buyer && buyer.pockets.length >= ruleset.cards.handLimit && Number.isFinite(ruleset.cards.handLimit)
    ? `<label class="merchant-replace">replace Trick Card <select id="replace-card">${buyer.pockets.map((card) => `<option value="${escapeHtml(card.instanceId ?? '')}">${escapeHtml(card.id)}</option>`).join('')}</select></label>`
    : '';
  const reroll = !shop.rerollResolved
    ? `<section class="merchant-vote"><p>the dealer offers a free full-shelf reshuffle. every player votes.</p><div>${localVoter ? `<button class="primary" data-shop-reroll="yes">reroll yes · ${escapeHtml(localVoter.name)}</button><button data-shop-reroll="no">keep shelf</button>` : '<span>ballots locked · waiting for the table</span>'}</div><small>${Object.keys(shop.rerollVotes).length}/${state.players.length} votes · strict majority reshuffles</small></section>`
    : `<section class="merchant-action"><p>${buyer ? canBuy ? `your merchant turn · choose one offer${shop.completedBuyerIds.length ? ` · ${shop.completedBuyerIds.length}/${state.players.length} shoppers complete` : ''}` : buyer.kind === 'bot' ? `${escapeHtml(buyer.name)} is choosing a card` : `${escapeHtml(buyer.name)} is shopping` : 'the merchant is closing'}</p>${replacement}${cardReplacement}${canBuy ? `<button data-shop-skip>pass this offer</button>${ruleset.id !== 'party' && buyer!.caddies.length ? `<button data-shop-sell="${buyer!.caddies[0]!.id}">sell ${escapeHtml(buyer!.caddies[0]!.id)} · $3</button>` : ''}` : ''}<small id="merchant-timer">${shop.secondsLeft.toFixed(0)} seconds</small></section>`;
  const headerCopy = ruleset.id === 'party' ? 'three Trick Cards · choose one or pass · two-card hand' : 'one purchase each · the shelf is shared · Caddies stack';
  return `<section class="merchant-overlay" role="dialog" aria-modal="true" aria-label="clubhouse merchant"><div class="merchant-table"><header class="merchant-header"><p class="eyebrow">THE NINETEENTH HOLE · SHARED MARKET</p><h1>the <em>clubhouse merchant</em> deals strange advantages</h1><p>${headerCopy}</p></header>${reroll}<section class="merchant-shelf" aria-label="merchant shelf">${cards}</section><aside class="merchant-ledger"><h2>${preferences.showMerchantHoldings ? 'table holdings' : 'table ledger'}</h2><ol>${caddyRows}</ol><p>next reality: ${state.queuedReality ? escapeHtml(state.queuedReality) : 'none'}</p></aside></div></section>`;
};

const ordinal = (rank: number) => `${rank}${rank % 100 >= 11 && rank % 100 <= 13 ? 'th' : ({ 1: 'st', 2: 'nd', 3: 'rd' })[rank % 10] ?? 'th'}`;

const renderResultsOverlay = ({ state, preferences }: ViewModel) => {
  if (state.status !== 'finished') return '';
  const standings = [...state.players].sort((left, right) => left.total - right.total || left.name.localeCompare(right.name));
  const leadingScore = standings[0]!.total;
  const champions = standings.filter((player) => player.total === leadingScore);
  const trailingScore = standings.at(-1)!.total;
  const trailers = standings.filter((player) => player.total === trailingScore);
  const awards = partyAwardsFor(state);
  const pacing = partyPacingFor(state);
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
    const outcome = rank === 1 ? champions.length > 1 ? 'co-champion' : 'winner' : `+${gap}`;
    return `<li class="${rank === 1 ? 'winner' : ''}"><span>${ordinal(rank)}</span><i style="background:${player.color}"></i><strong>${escapeHtml(player.name)}</strong><em>${outcome}</em><b>${player.total}</b></li>`;
  }).join('');
  const championNames = champions.map((player) => escapeHtml(player.name)).join(' + ');
  const trailingNames = trailers.map((player) => escapeHtml(player.name)).join(' + ');
  const headline = champions.length > 1 ? 'Clubhouse co-champions' : 'Clubhouse champion';
  const summary = champions.length > 1
    ? `${championNames} finish level on ${leadingScore} strokes.`
    : `${championNames} claims the trophy with ${leadingScore} strokes.`;
  const finalCallout = trailers.length === standings.length ? 'everyone finishes level.' : `${trailingNames} complete the route on ${trailingScore} strokes.`;
  const awardRows = awards.length ? `<section class="party-awards" aria-label="earned match superlatives"><header><p>THE CLUBHOUSE PRESENTS</p><h2>titles earned in the chaos</h2><span>real match events only · one title per golfer</span></header><ol>${awards.map((award) => {
    const recipient = state.players.find((player) => player.id === award.playerId);
    return `<li class="award-${escapeHtml(award.id)}"><i style="background:${recipient?.color ?? '#f1d058'}">${escapeHtml(award.glyph)}</i><div><strong>${escapeHtml(award.title)}</strong><span>${escapeHtml(award.detail)}</span></div></li>`;
  }).join('')}</ol></section>` : '';
  const pacingNote = pacing.measuredTurns ? `<small class="pacing-note">local pacing: median ${pacing.medianTurnSeconds?.toFixed(1)}s · p90 ${pacing.p90TurnSeconds?.toFixed(1)}s</small>` : '';
  const telemetryExport = state.config.ruleset !== 'custom' && preferences.showPartyDiagnostics ? '<button data-export-party-diagnostics>download Party Rules diagnostics</button>' : '';
  return `<section class="results-overlay" role="dialog" aria-modal="true" aria-label="campaign results"><div class="results-panel"><header class="results-header"><p class="eyebrow">NINE HOLES COMPLETE · FINAL CLUBHOUSE TABLE</p><h1>${headline}</h1><p>${summary} ${finalCallout}</p><small class="campaign-atlas-note">the complete course route remains visible behind the final table</small></header><section class="podium" aria-label="top three podium">${podium.map(podiumCard).join('')}</section><section class="final-standings" aria-label="final standings"><div><h2>full standings</h2><p>lowest aggregate strokes wins</p></div><ol>${standingRows}</ol></section>${awardRows}<footer class="results-actions"><button class="primary" data-restart-run>play again</button><button data-copy-replay>copy replay recipe</button>${telemetryExport}<span>same lineup · replay this seed</span>${pacingNote}</footer></div></section>`;
};

const renderPauseOverlay = ({ state, multiplayer }: ViewModel) => {
  if (!state.paused) return '';
  const canResume = !multiplayer.online || multiplayer.host;
  return `<section class="pause-overlay" role="dialog" aria-modal="true" aria-label="match paused"><div class="pause-card"><p class="eyebrow">${multiplayer.online ? 'ONLINE ROOM PAUSED' : 'LOCAL MATCH PAUSED'}</p><h2>take a breather</h2><p>${canResume ? 'Timers, bot turns, and gameplay input are frozen.' : 'Only the room host can resume this match.'}</p><button class="primary" data-toggle-pause ${canResume ? '' : 'disabled'}>resume match</button><button data-open-overlay="settings">settings</button></div></section>`;
};

const renderCourseBriefing = (view: ViewModel) => {
  const { state, briefingHole, preferences } = view;
  if (briefingHole !== state.hole || partyGuidePanelFor(state, preferences) || view.handoff) return '';
  const metadata = state.coursePlan[state.hole - 1]?.recipe.metadata;
  const chaos = metadata?.resolvedReels.chaos ?? [];
  const roles = state.course.routeRoles?.map((role) => `<li class="route-${role.role}"><strong>${escapeHtml(role.label)}</strong><span>${role.role === 'safe' ? 'wide and steady' : role.role === 'skill' ? 'timing, banks, or air' : 'contested space'}</span></li>`).join('') ?? '';
  return `<section class="party-briefing" role="dialog" aria-modal="true" aria-label="hole ${state.hole} course briefing"><div><p class="eyebrow">HOLE ${state.hole} · COURSE REVEAL</p><h2>${themeIcon[state.course.theme]} ${escapeHtml(themeDescriptor[state.course.theme])}</h2><p>${escapeHtml(state.course.archetype ?? 'ribbon')} layout · ${chaos.length ? `headline: ${escapeHtml(chaos.join(' + '))}` : 'no extra chaos modifier'}</p><ol>${roles}</ol><footer><span>the shuffler built the problem; the group now owns the result</span><button class="primary" data-dismiss-briefing>tee off</button></footer></div></section>`;
};

const renderHandoff = (view: ViewModel) => {
  const handoff = view.handoff;
  if (!handoff || partyGuidePanelFor(view.state, view.preferences)) return '';
  return `<section class="party-handoff" role="dialog" aria-modal="true" aria-label="pass device to ${escapeHtml(handoff.playerName)}"><div><p class="eyebrow">PASS THE DEVICE</p><i style="background:${handoff.color}"></i><h2>${escapeHtml(handoff.playerName)}, you are on the tee.</h2><p>Hole ${handoff.hole}. Your ball, cards, and route preview are now highlighted.</p><button class="primary" data-ready-handoff>ready to shoot</button></div></section>`;
};

const renderPartyGuide = (view: ViewModel) => {
  const panel = partyGuidePanelFor(view.state, view.preferences);
  if (!panel) return '';
  return `<section class="party-guide" role="dialog" aria-modal="true" aria-label="Party Rules guide"><div><p class="eyebrow">${escapeHtml(panel.eyebrow)}</p><h2>${escapeHtml(panel.title)}</h2><p>${escapeHtml(panel.body)}</p><footer><button data-skip-party-guide>skip guide</button><button class="primary" data-next-party-guide>${panel.step === 6 ? 'finish guide' : 'got it'}</button></footer></div></section>`;
};

const renderInspector = (view: ViewModel) => {
  const { state } = view;
  if (state.status === 'shopping') return `${renderLedger(view.ledger)}<p class="hint">${state.config.ruleset === 'party' ? 'Three visible Trick Cards are shared. Each golfer chooses one or passes.' : 'The clubhouse merchant sells persistent Caddies, contraband, Reality Cards, and Chrono Cards.'}</p>`;
  if (state.status === 'transitioning') return `${renderLedger(view.ledger)}<p class="hint">${state.config.ruleset === 'party' ? 'The cup becomes the next tee. Earlier holes remain visible in the campaign atlas while only the newest hole stays active.' : 'The completed cup becomes the next tee while the arena expands into open terrain. Previous fairways stay playable.'}</p>`;
  const features = (state.course.features ?? []).map((feature) => feature.kind === 'sinkhole' ? '↻ paired sinkhole' : feature.kind === 'thorn' ? '✽ thorn knockback' : feature.kind === 'pulse' ? '⌁ pulse launch' : feature.kind === 'gust' ? '〰 gust lane' : '◯ air ring boost').join(' · ') || 'none';
  const player = current(state);
  const ruleset = rulesetFor(state.config);
  const caddies = player.caddies.map((caddy) => `${caddy.id}${caddy.stacks > 1 ? ` ×${caddy.stacks}` : ''}`).join(', ') || 'none';
  const roles = state.course.routeRoles?.map((assignment) => assignment.label).join(' · ') ?? 'safe line · skill line · conflict line';
  const recipe = state.coursePlan[state.hole - 1]?.recipe.metadata;
  return `${renderLedger(view.ledger)}<h3>active package</h3><dl><dt>ruleset</dt><dd>${escapeHtml(ruleset.label)}</dd><dt>biome</dt><dd>${themeIcon[state.course.theme]} ${themeDescriptor[state.course.theme]}</dd><dt>shape</dt><dd>${state.course.archetype ?? 'ribbon'} · ${state.course.sizeProfile ?? 'standard'}</dd><dt>routes</dt><dd>${escapeHtml(roles)}</dd><dt>rules</dt><dd>${escapeHtml(ruleSummary(state.holeRules))}</dd><dt>your cash</dt><dd>$${player.cash}</dd>${ruleset.id === 'party' ? '' : `<dt>your Caddies</dt><dd>${escapeHtml(caddies)}</dd>`}<dt>hazards</dt><dd>${state.course.hazards.map((hazard) => hazard.kind).join(' + ') || 'none'}</dd><dt>biome effects</dt><dd>${features}</dd><dt>recipe</dt><dd>${recipe ? `${recipe.generatorVersion} · ${recipe.courseHash ?? 'unhashed'}` : 'legacy course'}</dd></dl><h3>course legend</h3><p class="legend">safe: wide and stable · skill: timing, banks, or chips · conflict: shared contest space<br>blue: slow mover · amber: standard mover · red: fast mover<br>colored arm: sweeper · colored gate: timed gate · cyan gust: airborne updraft · brown bar: chip-height blocker · dashed cyan: gust lane<br>gold $: cash pad · cyan +: recovery pad · violet !: chaos pad · ↑: player gadget</p>`;
};

const renderDrawer = (view: ViewModel) => {
  if (!view.drawer) return '';
  const title = view.drawer === 'run' ? 'run controls' : 'course intel';
  return `<aside class="drawer drawer-${view.drawer} panel" aria-label="${title}"><div class="drawer-heading"><h2>${title}</h2><button data-close-drawer class="close" aria-label="close ${title}">×</button></div><div class="drawer-content">${view.drawer === 'run' ? renderRunDrawer(view) : renderInspector(view)}</div></aside>`;
};

export const renderAppMarkup = (view: ViewModel) => {
  const { state } = view;
  const progress = `HOLE <b>${state.hole}</b> / ${state.config.holeCount}`;
  const shellState = state.status === 'shopping' ? 'shopping' : state.status === 'transitioning' ? 'transitioning' : state.status === 'finished' ? 'finished' : '';
  const roomLabel = view.multiplayer.online ? `ONLINE · ${escapeHtml(view.multiplayer.roomCode ?? 'connecting')}` : view.multiplayer.controllerName ? `LOCAL PLAY · PAD · ${escapeHtml(view.multiplayer.controllerName)}` : 'LOCAL PLAY';
  return `<main class="app-shell ${shellState}"><section class="topbar"><div class="brand"><p class="eyebrow">${roomLabel}</p><h1>GOLF <em>WITH YOUR</em> ENEMIES</h1></div><div class="title-actions"><button data-toggle-pause ${state.status === 'finished' ? 'disabled' : ''}>pause</button><button data-drawer="run" class="${view.drawer === 'run' ? 'selected' : ''}" aria-expanded="${view.drawer === 'run'}">run</button><button data-drawer="intel" class="${view.drawer === 'intel' ? 'selected' : ''}" aria-expanded="${view.drawer === 'intel'}">intel</button><button data-open-overlay="help">? help</button><button class="icon-button" data-open-overlay="settings" aria-label="open settings" title="settings"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 15.2a3.2 3.2 0 1 0 0-6.4 3.2 3.2 0 0 0 0 6.4Z"/><path d="M19.4 13.5a7.7 7.7 0 0 0 .1-1.5 7.7 7.7 0 0 0-.1-1.5l2-1.5-2-3.4-2.4 1a8 8 0 0 0-2.6-1.5L14.1 2h-4l-.4 3.1a8 8 0 0 0-2.6 1.5l-2.4-1-2 3.4 2 1.5a7.7 7.7 0 0 0-.1 1.5c0 .5 0 1 .1 1.5l-2 1.5 2 3.4 2.4-1a8 8 0 0 0 2.6 1.5l.4 3.1h4l.4-3.1a8 8 0 0 0 2.6-1.5l2.4 1 2-3.4-2.1-1.5Z"/></svg></button><div class="hole">${progress}<br><small>${escapeHtml(state.course.seed)}</small></div></div></section><section class="layout"><section class="board panel"><div class="course-stage"><canvas id="course" aria-label="isometric arcade mini golf course"></canvas><div id="callouts" class="callouts" aria-live="polite">${renderCallouts(view.callouts)}</div></div><section class="match-rail" aria-label="match status and controls">${renderMatchHud(view)}<div id="controls" class="controls"></div></section></section></section>${renderDrawer(view)}</main>${renderShopOverlay(view)}${renderResultsOverlay(view)}${renderPauseOverlay(view)}${renderOverlay(view)}`;
};
