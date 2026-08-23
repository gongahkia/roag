import { EMOTES, type ChronoCard, type GadgetKind, type GameConfig, type GameState, type HoleRules, type Point, type PowerUp, type ShotCommand, type VotingOption } from '../core/types';
import { definitionFor } from '../core/catalog';
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
const themeIcon: Record<VotingOption['recipe']['terrain']['theme'], string> = { balanced: '⛳', speedway: '⚡', 'hazard-run': '⚠', 'ice-rink': '❄', quarry: '⛏', drift: '↻', bloom: '✽', pulse: '⌁' };
const themeDescriptor: Record<VotingOption['recipe']['terrain']['theme'], string> = { balanced: 'steady green', speedway: 'speed lanes', 'hazard-run': 'moving traps', 'ice-rink': 'long slides', quarry: 'hard climbs', drift: 'paired sinkholes', bloom: 'thorn knockback', pulse: 'launch fields' };
const powerUpIcon: Record<string, string> = { turbo: '↯', shield: '⬡', bomb: '✹', freeze: '❄', swap: '⇄', 'two putts': '2P', heavy: '●', bouncy: '◌', ghost: '◐', magnet: '🧲', ice: '❄', portal: '◉', glider: '⌒', sticky: '▣', orbit: '◎', quantum: '∞', mirror: '◇', anvil: '⬟', vampire: '☽', boomerang: '↶', 'cup magnet': '⊙', slipstream: '➳', 'rebound rig': '↩', 'phase shift': '⇤', sandbag: '▰', 'rescue drone': '✈', airhorn: '📣', 'club flipper': '↻', 'time dilator': '⌛', mugger: '♜', scramble: '⇆', 'gravity gloves': '☄', 'bunker buster': '⛏', 'portal remote': '◉', 'red tee': '⚑', 'black flag': '⚐', 'cherry bomb': '✹', copycat: '▣', 'popper pad': '↑', 'snare patch': '⌁', 'blast mine': '✹', 'slick patch': '≋', 'sky spring': '⌃', 'gravity well': '◉', 'mirror plate': '◇', 'toll booth': '$', 'control inverter': '↻', 'portal gun': '◉', 'undo drive': '↶', 'second chance': '↺', 'echo putt': '◌', 'future sight': '⌘', 'time theft': '⌛', 'frozen frame': '❄', 'parallel parking': '⇆', 'grandfather clause': '⌫' };

export const renderStatus = ({ state, shotInFlight }: Pick<ViewModel, 'state' | 'shotInFlight'>) => {
  if (state.paused) return 'match paused';
  if (state.status === 'voting') return `match selection: hole ${state.coursePlan.length + 1}/${state.config.holeCount} · ${Object.keys(state.vote?.ballots ?? {}).length}/${state.players.length} ballots cast`;
  if (state.status === 'shopping') return `clubhouse merchant · ${state.shop?.secondsLeft.toFixed(0) ?? 0}s remaining`;
  if (state.status === 'transitioning') return `expanding the campaign to hole ${state.hole}/${state.config.holeCount}`;
  if (state.status === 'finished') return 'campaign complete — the full route is on display';
  if (shotInFlight) return `${escapeHtml(current(state).name)}'s ball is in flight · moving hazards are live`;
  return `${escapeHtml(current(state).name)} is taking a turn · moving hazards are live`;
};

export const renderCallouts = (callouts: readonly Callout[]) => callouts.map((callout) => `<p class="callout ${callout.tone}">${escapeHtml(callout.message)}</p>`).join('');
const renderLedger = (ledger: readonly LedgerEntry[]) => `<ul class="feed" aria-live="polite">${ledger.map((entry) => `<li class="${entry.tone}"><span>${escapeHtml(entry.message)}</span></li>`).join('') || '<li class="neutral"><span>waiting for the ballot</span></li>'}</ul>`;

const renderVotingControls = (state: GameState) => {
  return `<div class="turn"><strong>match ballot</strong><b>${state.coursePlan.length + 1}/${state.config.holeCount}</b><small class="phase">lock every hole before tee-off</small></div><p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;
};

const renderTransitionControls = (state: GameState) => `<div class="turn"><strong>next hole</strong><b>${state.hole}/${state.config.holeCount}</b><small class="phase">the route is building outward</small></div><p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;
const renderFinishedControls = (state: GameState) => `<div class="turn"><strong>campaign complete</strong><b>★</b><small class="phase">the full route has zoomed out behind the results</small></div><p class="control-status">${renderStatus({ state, shotInFlight: false })}</p>`;

export const renderControlsMarkup = (view: ViewModel) => {
  const { state, preferences, aim, shotInFlight, multiplayer, placement } = view;
  if (state.status === 'voting') return renderVotingControls(state);
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
  const powerCells = Array.from({ length: 15 }, (_, index) => {
    const power = Number((1 + index * .5).toFixed(1));
    return `<button class="power-cell ${aim.power >= power ? 'active' : ''}" data-power="${power}" aria-label="set power ${power}" aria-pressed="${aim.power === power}" ${disabled ? 'disabled' : ''}></button>`;
  }).join('');
  const powerHint = preferences.mousePowerMode === 'cursor' ? 'move cursor to set power · click a cell' : 'scroll course · click a cell';
  return `
    <div class="turn"><span style="--player:${player.color}"></span><strong>${escapeHtml(player.name)}</strong><b>${state.turn.secondsLeft.toFixed(0)}s</b><small class="phase">hazards live</small></div>
    <div class="emote-buttons" aria-label="emotes">${EMOTES.map((emote) => `<button data-emote="${emote.id}" title="${emote.label}" ${disabled ? 'disabled' : ''}>${emote.glyph}</button>`).join('')}</div>
    <div class="shot-mode" role="group" aria-label="shot type"><button data-shot-kind="putt" class="${(aim.kind ?? 'putt') === 'putt' ? 'selected' : ''}" aria-pressed="${(aim.kind ?? 'putt') === 'putt'}" ${disabled ? 'disabled' : ''}>◌ putt<small>ground roll</small></button><button data-shot-kind="chip" class="${aim.kind === 'chip' ? 'selected' : ''}" aria-pressed="${aim.kind === 'chip'}" ${disabled ? 'disabled' : ''}>⌒ chip<small>clear walls · C</small></button></div>
    <div class="power-control" role="group" aria-label="${aim.kind === 'chip' ? 'chip' : 'putt'} power"><span>${aim.kind === 'chip' ? 'chip power' : 'putt power'} <b>${aim.power.toFixed(1)}</b></span><div class="power-cells" title="${powerHint}">${powerCells}</div><small>${powerHint}</small></div>
    <button id="shoot" class="primary" ${disabled ? 'disabled' : ''}>${aim.kind === 'chip' ? 'chip' : 'putt'} <kbd>${keyLabel(bindingFor(preferences, 'shoot'))}</kbd></button>
    ${player.ballForm ? `<span class="active-form">next shot: ${escapeHtml(player.ballForm)} ball</span>` : ''}
    ${player.forcedChip ? '<span class="active-form">airhorn: next shot is a chip</span>' : ''}
    ${heldItems.includes('portal') || heldItems.includes('portal remote') ? `<label>portal exit <select id="portal-exit" ${disabled ? 'disabled' : ''}>${portalExits}</select></label>` : ''}
    ${targetSelector}
    ${heldCards.length ? heldCards.map((card) => {
      const definition = definitionFor(card.id as never);
      const detail = card.duration ? `${card.duration.amount} ${card.duration.unit}${card.duration.amount === 1 ? '' : 's'}` : definition?.timing === 'putt' ? 'next putt' : definition?.timing === 'immediate' ? 'instant' : 'item';
      const role = definition?.polarity && definition.polarity !== 'neutral' ? `${definition.polarity} · ` : '';
      return `<button data-use-powerup="${escapeHtml(card.id)}" data-card-id="${escapeHtml(card.instanceId ?? '')}" ${cardDisabled ? 'disabled' : ''}>use ${escapeHtml(card.id)} <small>${role}${detail}</small>${card.id === player.inventory ? ` <kbd>${keyLabel(bindingFor(preferences, 'usePowerUp'))}</kbd>` : ''}</button>`;
    }).join('') : '<span class="muted">no pocket card</span>'}
    ${placement ? `<p class="placement-status ${placement.valid ? 'valid' : 'invalid'}">${powerUpIcon[placement.kind]} ${escapeHtml(placement.kind)} · ${placement.point ? placement.valid ? placement.confirmed ? 'click again or press Enter to place' : 'click this tile to lock the preview' : 'choose an open playable tile' : 'click a tile to preview'} <button data-cancel-placement>cancel</button></p>` : ''}
    ${player.secondWindAvailable && !player.twoPuttsArmed ? `<button id="second-wind" ${disabled ? 'disabled' : ''}>use second wind: two putts</button>` : ''}
    ${state.turn.cardPlayed ? '<p class="control-status">one card committed this turn</p>' : ''}
    ${state.players.some((candidate) => candidate.attachments?.length) ? `<div class="card-effects"><strong>table cards</strong>${state.players.filter((candidate) => candidate.attachments?.length).map((candidate) => `<p><i style="background:${candidate.color}"></i>${escapeHtml(candidate.name)} · ${candidate.attachments!.map((attachment) => `${escapeHtml(attachment.cardId)} (${attachment.remaining} ${attachment.unit}${attachment.remaining === 1 ? '' : 's'})`).join(', ')}</p>`).join('')}</div>` : ''}
    <p id="status" class="control-status">${renderStatus(view)}</p>`;
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
  if (overlay === 'help') return `<section class="overlay" role="dialog" aria-modal="true" aria-label="shortcut help"><div class="overlay-card"><button class="close" data-close-overlay aria-label="close help">×</button><p class="eyebrow">MOUSE FIRST · KEYS READY</p><h2>quick commands</h2><dl class="shortcut-list">${shortcutRows(preferences, rebinding)}</dl><p class="hint">Press C or the controller Y button to switch between a ground putt and a chip arc that can clear walls. Voting is click-only so every local player can vote publicly in any order.</p></div></section>`;
  return `<section class="overlay" role="dialog" aria-modal="true" aria-label="game settings"><div class="overlay-card settings-card"><button class="close" data-close-overlay aria-label="close settings">×</button><p class="eyebrow">LOCAL PREFERENCES</p><h2>terminal settings</h2><label class="setting-toggle"><input data-preference="reducedMotion" type="checkbox" ${preferences.reducedMotion ? 'checked' : ''}> reduced motion and flash</label><label class="setting-toggle"><input data-preference="highContrast" type="checkbox" ${preferences.highContrast ? 'checked' : ''}> high-contrast glyphs</label><label class="setting-toggle"><input data-preference="controllerVibration" type="checkbox" ${preferences.controllerVibration ? 'checked' : ''}> controller vibration</label><label class="setting-toggle"><input data-preference="showMerchantHoldings" type="checkbox" ${preferences.showMerchantHoldings ? 'checked' : ''}> show table cards and boons in merchant</label><label>mouse shot strength <select data-preference-select="mousePowerMode"><option value="scroll" ${preferences.mousePowerMode === 'scroll' ? 'selected' : ''}>mouse scroll</option><option value="cursor" ${preferences.mousePowerMode === 'cursor' ? 'selected' : ''}>cursor distance</option></select></label><label>master volume <input data-preference-range="masterVolume" type="range" min="0" max="1" step="0.05" value="${preferences.masterVolume}"></label><label>effects volume <input data-preference-range="effectsVolume" type="range" min="0" max="1" step="0.05" value="${preferences.effectsVolume}"></label><label>controller deadzone <input data-preference-range="controllerDeadzone" type="range" min="0.05" max="0.5" step="0.01" value="${preferences.controllerDeadzone}"></label><label>controller aim sensitivity <input data-preference-range="controllerAimSensitivity" type="range" min="0.5" max="2" step="0.05" value="${preferences.controllerAimSensitivity}"></label><h3>shortcuts</h3><dl class="shortcut-list">${shortcutRows(preferences, rebinding, true)}</dl><p class="hint">${rebinding ? `press a key for ${escapeHtml(rebinding)} · Esc cancels` : 'select a key to rebind it'}</p></div></section>`;
};

const renderRunDrawer = ({ state, config }: ViewModel) => {
  const scoreRows = state.players.map((player) => `<tr class="${player.id === current(state).id ? 'active' : ''}"><td><i style="background:${player.color}"></i>${escapeHtml(player.name)}</td><td>${player.ball.complete ? '✓' : player.ball.strokes}</td><td>${player.total}</td></tr>`).join('');
  return `<h3>campaign controls</h3><label>run seed <input id="seed" value="${escapeHtml(config.seed)}" maxlength="32"></label><div class="split"><label>humans <input id="humans" type="number" min="1" max="8" value="${config.humanCount}"></label><label>AI <input id="bots" type="number" min="0" max="4" value="${config.botCount}"></label></div><label>AI skill <select id="skill"><option value="adaptive" ${config.botSkill === 'adaptive' ? 'selected' : ''}>adaptive</option>${Array.from({ length: 10 }, (_, index) => `<option value="${index + 1}" ${config.botSkill === index + 1 ? 'selected' : ''}>${index + 1}</option>`).join('')}</select></label><button id="new-run">start new nine-hole run</button><p class="hint">Every hole begins with a public vote between three fully specified course-and-rules packages.</p><h3>scorecard</h3><table><thead><tr><th>player</th><th>hole</th><th>total</th></tr></thead><tbody>${scoreRows}</tbody></table>`;
};

const renderVoteOverlay = (view: ViewModel) => {
  const { state } = view;
  if (state.status !== 'voting' || !state.vote) return '';
  const { vote } = state;
  const selectionHole = state.coursePlan.length + 1;
  const votesFor = (optionId: string) => Object.values(vote.ballots).filter((ballot) => ballot === optionId).length;
  const nextHumanVoter = view.multiplayer.online
    ? state.players.find((player) => player.id === view.multiplayer.playerId && !vote.ballots[player.id])
    : state.players.find((player) => player.kind === 'human' && !vote.ballots[player.id]);
  const packageCard = (option: VotingOption, index: number) => {
    const { terrain, rules } = option.recipe;
    const voters = state.players.filter((player) => vote.ballots[player.id] === option.id);
    const voterPills = voters.length ? voters.map((player) => `<span class="vote-pill" title="${escapeHtml(player.name)} voted for this package"><i style="background:${player.color}"></i>${escapeHtml(player.name)}</span>`).join('') : '<span class="vote-pill empty">no votes</span>';
    return `<article class="vote-card theme-${terrain.theme}" data-vote-option="${option.id}" role="button" tabindex="0" aria-label="vote for ${escapeHtml(option.label)}"><header><span class="package-number">${index + 1}</span><div><p class="card-kicker" title="${escapeHtml(terrain.theme)}">${themeIcon[terrain.theme]} ${themeDescriptor[terrain.theme]}</p><h2>${escapeHtml(option.label)}</h2></div><div class="vote-pills"><strong class="vote-count">${votesFor(option.id)}<small>votes</small></strong>${voterPills}</div></header>
      <div class="course-glyphs" aria-label="course configuration"><span title="course dimensions">▣ ${terrain.width}×${terrain.height}</span><span title="route length">🧭 ${Math.round(terrain.routeLength * 100)}%</span><span title="bendiness">↪ ${Math.round(terrain.bendiness * 100)}%</span><span title="lane width">↔ ${terrain.laneWidth}</span><span title="ramps">⛰ ${Math.round(terrain.elevation * 100)}%/${terrain.maxElevation}</span><span title="walls">🧱 ${terrain.wallCount}</span><span title="sweepers and gates">⚠ ${terrain.sweeperCount}+${terrain.gateCount}</span><span title="air hazards: updrafts and low bars">⌃ ${terrain.updraftCount} · ⊓ ${terrain.lowBarCount}</span><span title="air rings">◯ ${terrain.airRingCount}</span><span title="portal pairs">◉ ${terrain.portalPairs}</span><span title="item pads">✚ ${terrain.recoveryPads} · ✹ ${terrain.chaosPads}</span>${terrain.theme === 'drift' ? `<span title="paired sinkholes">↻ ${terrain.sinkholePairs}</span>` : terrain.theme === 'bloom' ? `<span title="thorn zones">✽ ${terrain.thornCount}</span>` : terrain.theme === 'pulse' ? `<span title="pulse fields">⌁ ${terrain.pulseCount}</span>` : ''}</div>
      <div class="rule-chip-grid" aria-label="hole rules"><span>⏱ ${rules.timerSeconds}s</span><span>🎯 cap ${rules.strokeCap}</span><span>${rules.collisions ? '● collisions' : '○ no collisions'}</span><span>${rules.powerUps ? '🎁 items on' : '⊘ items off'}</span><span>↯ ${Math.round(rules.launchMultiplier * 100)}%</span><span>🧊 ${Math.round(rules.rollingResistanceMultiplier * 100)}%</span><span>↩ ${Math.round(rules.wallRestitutionMultiplier * 100)}%</span><span>◌ cup ${Math.round(rules.cupRadius * 100)}</span><span>◴ ${rules.hazardPhaseCount} phases</span><span>★ ${Math.round(rules.scoreMultiplier * 100)}% score</span></div>
      <div class="boon-row"><span class="boon-chip"><b>♧</b>personal Caddies persist</span><span class="supply-chip"><b>$</b>merchant after every hole</span></div>
      <footer class="card-action">${nextHumanVoter ? `<strong>click to vote${view.multiplayer.online ? '' : ` as ${escapeHtml(nextHumanVoter.name)}`}</strong>` : '<strong>ballot cast · waiting for the table</strong>'}</footer>
    </article>`;
  };
  const ballotPrompt = nextHumanVoter
    ? view.multiplayer.online ? 'click one package to cast your ballot' : `pass the device to ${escapeHtml(nextHumanVoter.name)} · click one package to vote`
    : 'your ballot is locked · waiting for the table';
  return `<section class="vote-overlay" role="region" aria-label="planned hole ${selectionHole} public ballot"><div class="vote-panel"><header class="vote-panel-header"><p class="eyebrow">MATCH PLAN · HOLE ${selectionHole} / ${state.config.holeCount}</p><h1>Choose the next <em>clubhouse condition</em></h1><p>${ballotPrompt} · every hole is selected before the first tee-off.</p></header><div class="vote-card-grid">${vote.options.map(packageCard).join('')}</div><footer class="vote-panel-footer"><span>${view.multiplayer.online ? 'click a card to cast your ballot' : 'click a card to cast the next human vote'}</span><span>bot ballots appear automatically</span></footer></div></section>`;
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
    : `<section class="merchant-action"><p>${buyer ? canBuy ? `your merchant turn · choose any one affordable card${shop.completedBuyerIds.length ? ` · ${shop.completedBuyerIds.length}/${state.players.length} shoppers complete` : ''}` : buyer.kind === 'bot' ? `${escapeHtml(buyer.name)} is choosing a card` : `${escapeHtml(buyer.name)} is shopping` : 'the merchant is closing'}</p>${replacement}${canBuy ? `<button data-shop-skip>skip merchant turn</button>${buyer!.caddies.length ? `<button data-shop-sell="${buyer!.caddies[0]!.id}">sell ${escapeHtml(buyer!.caddies[0]!.id)} · $3</button>` : ''}` : ''}<small>${shop.secondsLeft.toFixed(0)} seconds</small></section>`;
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
  if (state.status === 'voting') return `${renderLedger(view.ledger)}<p class="hint">Lock the whole match plan before tee-off. Courses stay hidden until play starts.</p>`;
  if (state.status === 'shopping') return `${renderLedger(view.ledger)}<p class="hint">The clubhouse merchant sells persistent Caddies, contraband, Reality Cards, and Chrono Cards.</p>`;
  if (state.status === 'transitioning') return `${renderLedger(view.ledger)}<p class="hint">The completed cup becomes the next tee while the arena expands into open terrain. Previous fairways stay playable.</p>`;
  const features = (state.course.features ?? []).map((feature) => feature.kind === 'sinkhole' ? '↻ paired sinkhole' : feature.kind === 'thorn' ? '✽ thorn knockback' : feature.kind === 'pulse' ? '⌁ pulse launch' : '◯ air ring boost').join(' · ') || 'none';
  const player = current(state);
  const caddies = player.caddies.map((caddy) => `${caddy.id}${caddy.stacks > 1 ? ` ×${caddy.stacks}` : ''}`).join(', ') || 'none';
  return `${renderLedger(view.ledger)}<h3>active package</h3><dl><dt>biome</dt><dd>${themeIcon[state.course.theme]} ${themeDescriptor[state.course.theme]}</dd><dt>rules</dt><dd>${escapeHtml(ruleSummary(state.holeRules))}</dd><dt>your cash</dt><dd>$${player.cash}</dd><dt>your Caddies</dt><dd>${escapeHtml(caddies)}</dd><dt>reality</dt><dd>${state.activeReality ?? state.queuedReality ?? 'stable'}</dd><dt>hazards</dt><dd>${state.course.hazards.map((hazard) => hazard.kind).join(' + ') || 'none'}</dd><dt>biome effects</dt><dd>${features}</dd><dt>portal pairs</dt><dd>${state.course.portals?.filter((pair) => pair.entrance && pair.exit).length ?? 0}</dd><dt>item pads</dt><dd>${state.course.itemPads.length}</dd></dl><h3>course legend</h3><p class="legend">fairway grass · rough · sand bunker · water ice<br>blue: slow mover · amber: standard mover · red: fast mover<br>colored arm: sweeper · colored gate: timed gate · cyan gust: airborne updraft · brown bar: chip-height blocker<br>↻ paired sinkhole · ✽ thorn knockback · ⌁ pulse launch · ◯ air-ring boost<br>gold $: cash pad · cyan +: recovery pad · violet !: chaos pad · ↑/⌁/✹/≋/⌃: player gadgets</p>`;
};

const renderDrawer = (view: ViewModel) => {
  if (!view.drawer) return '';
  const title = view.drawer === 'run' ? 'run controls' : view.state.status === 'voting' ? 'public ballot' : 'course intel';
  return `<aside class="drawer drawer-${view.drawer} panel" aria-label="${title}"><div class="drawer-heading"><h2>${title}</h2><button data-close-drawer class="close" aria-label="close ${title}">×</button></div><div class="drawer-content">${view.drawer === 'run' ? renderRunDrawer(view) : renderInspector(view)}</div></aside>`;
};

export const renderAppMarkup = (view: ViewModel) => {
  const { state } = view;
  const progress = state.status === 'voting' ? `PLAN <b>${state.coursePlan.length + 1}</b> / ${state.config.holeCount}` : `HOLE <b>${state.hole}</b> / ${state.config.holeCount}`;
  const shellState = state.status === 'voting' ? 'voting' : state.status === 'shopping' ? 'shopping' : state.status === 'transitioning' ? 'transitioning' : state.status === 'finished' ? 'finished' : '';
  const roomLabel = view.multiplayer.online ? `ONLINE · ${escapeHtml(view.multiplayer.roomCode ?? 'connecting')}` : view.multiplayer.controllerName ? `PAD · ${escapeHtml(view.multiplayer.controllerName)}` : 'LOCAL PARTY';
  return `<main class="app-shell ${shellState}"><section class="topbar"><div class="brand"><p class="eyebrow">${roomLabel}</p><h1>GOLF <em>WITH YOUR</em> ENEMIES</h1></div><div class="title-actions"><button data-toggle-pause ${state.status === 'finished' ? 'disabled' : ''}>pause</button><button data-drawer="run" class="${view.drawer === 'run' ? 'selected' : ''}" aria-expanded="${view.drawer === 'run'}">run</button><button data-drawer="intel" class="${view.drawer === 'intel' ? 'selected' : ''}" aria-expanded="${view.drawer === 'intel'}">${state.status === 'voting' ? 'plan' : 'intel'}</button><button data-open-overlay="help">? help</button><button data-open-overlay="settings">F1 settings</button><div class="hole">${progress}<br><small>${escapeHtml(state.course.seed)}</small></div></div></section><section class="layout"><section class="board panel"><div class="course-stage"><canvas id="course" aria-label="isometric arcade mini golf course"></canvas><div id="callouts" class="callouts" aria-live="polite">${renderCallouts(view.callouts)}</div></div><div id="controls" class="controls"></div></section></section>${renderDrawer(view)}</main>${renderVoteOverlay(view)}${renderShopOverlay(view)}${renderResultsOverlay(view)}${renderPauseOverlay(view)}${renderOverlay(view)}`;
};
