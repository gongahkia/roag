import './style.css';
import { presentFeedback, type FeedbackTone } from './feedback';
import { applyCommand, beginCourse, botMove, createGame, currentUpgradeChoices, defaultConfig, previewShot, tickTurn, UPGRADE_DESCRIPTIONS } from './core/game';
import { generateCandidates } from './core/generator';
import { hashSeed } from './core/random';
import { bindingFor, isEditableElement, loadPreferences, savePreferences, setShortcut, shortcutForKey, type ShortcutId } from './preferences';
import { EMOTES, type Ball, type Emote, type EmoteEvent, type GameState, type PowerUp, type ShotCommand } from './core/types';
import { createRenderer } from './ui/render';

type Overlay = 'help' | 'settings' | undefined;
type Drawer = 'run' | 'intel' | undefined;
interface LedgerEntry { id: number; message: string; tone: FeedbackTone; }
interface Callout { id: number; message: string; tone: FeedbackTone; expiresAt: number; }
interface ShotAnimation { playerId: string; frame: number; }
interface LiveEmote extends EmoteEvent { expiresAt: number; }

const app = document.querySelector<HTMLElement>('#app')!;
const query = new URLSearchParams(window.location.search);
const requestedSeed = query.get('seed')?.trim();
const captureMode = query.get('capture') === '1';
let config = { ...defaultConfig(), ...(requestedSeed ? { seed: requestedSeed } : {}) };
let state = createGame(config);
let candidates = generateCandidates(hashSeed(config.seed, 1));
let selectedCandidate = 0;
let rerollCount = 0;
let aim: ShotCommand = { angle: 0, power: 4 };
let renderer: ReturnType<typeof createRenderer> | undefined;
let lastTick = performance.now();
let botTimeout: number | undefined;
let preferences = loadPreferences();
let overlay: Overlay;
let drawer: Drawer = captureMode ? undefined : 'intel';
let rebinding: ShortcutId | undefined;
let feedbackId = 0;
let lastFeedbackMessage: string | undefined;
let ledger: LedgerEntry[] = [];
let callouts: Callout[] = [];
let shotAnimation: ShotAnimation | undefined;
let liveEmotes: LiveEmote[] = [];
let seenEmoteIds = new Set<string>();

const escape = (value: string) => value.replace(/[&<>'"]/g, (character) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[character]!);
const current = () => state.players[state.turn.playerIndex]!;
const keyLabel = (key: string) => key === ' ' ? 'Space' : key;

const applyPreferences = () => {
  document.documentElement.classList.toggle('reduced-motion', preferences.reducedMotion);
  document.documentElement.classList.toggle('high-contrast', preferences.highContrast);
};

const recordFeedback = (message: string) => {
  if (!message || message === lastFeedbackMessage) return;
  lastFeedbackMessage = message;
  const presentation = presentFeedback(message);
  const entry = { id: feedbackId++, message, tone: presentation.tone };
  ledger = [entry, ...ledger].slice(0, 8);
  if (presentation.major) callouts = [{ ...entry, expiresAt: performance.now() + (preferences.reducedMotion ? 900 : 1800) }, ...callouts].slice(0, 2);
};

const recordStateFeedback = (next: GameState) => recordFeedback(next.messages[0] ?? '');

const syncEmotes = (next: GameState) => {
  for (const emote of next.emotes) {
    if (seenEmoteIds.has(emote.id)) continue;
    seenEmoteIds.add(emote.id);
    liveEmotes = [{ ...emote, expiresAt: performance.now() + (preferences.reducedMotion ? 1_100 : 2_700) }, ...liveEmotes].slice(0, 8);
  }
};

const setState = (next: GameState) => {
  const previousStatus = state.status;
  const refreshCandidates = next.status === 'preview' && (next.hole !== state.hole || next.course.seed !== state.course.seed);
  state = next;
  if (!captureMode && state.status === 'preview' && previousStatus !== 'preview') drawer = 'intel';
  if (previousStatus === 'preview' && state.status !== 'preview') drawer = undefined;
  if (refreshCandidates) {
    rerollCount = 0;
    candidates = generateCandidates(hashSeed(state.config.seed, state.hole));
    selectedCandidate = 0;
  }
  recordStateFeedback(state);
  syncEmotes(state);
  render();
  scheduleBot();
};

const setupGame = () => {
  config = {
    ...config,
    seed: document.querySelector<HTMLInputElement>('#seed')?.value.trim() || config.seed,
    humanCount: Number(document.querySelector<HTMLInputElement>('#humans')?.value || config.humanCount),
    botCount: Number(document.querySelector<HTMLInputElement>('#bots')?.value || config.botCount),
    timerSeconds: Number(document.querySelector<HTMLInputElement>('#timer')?.value || config.timerSeconds),
    botSkill: document.querySelector<HTMLSelectElement>('#skill')?.value === 'adaptive' ? 'adaptive' : Number(document.querySelector<HTMLSelectElement>('#skill')?.value || config.botSkill),
    collisions: document.querySelector<HTMLInputElement>('#collisions')?.checked ?? config.collisions,
    powerUps: document.querySelector<HTMLInputElement>('#powerups')?.checked ?? config.powerUps,
  };
  if (config.humanCount + config.botCount > 12 || config.botCount > 4 || config.humanCount < 1) return;
  state = createGame(config);
  drawer = captureMode ? undefined : 'intel';
  candidates = generateCandidates(hashSeed(config.seed, 1));
  selectedCandidate = 0;
  rerollCount = 0;
  liveEmotes = [];
  seenEmoteIds = new Set();
  recordStateFeedback(state);
  render();
};

const rerollCandidates = () => {
  if (state.status !== 'preview') return;
  rerollCount += 1;
  candidates = generateCandidates(`${hashSeed(config.seed, state.hole)}-reroll-${rerollCount}`);
  selectedCandidate = 0;
  state = { ...state, messages: ['candidates rerolled', ...state.messages].slice(0, 5) };
  recordStateFeedback(state);
  render();
};

const lockCandidate = () => {
  const candidate = candidates[selectedCandidate];
  if (!candidate || state.status !== 'preview') return;
  state = {
    ...state,
    course: candidate,
    players: state.players.map((player) => ({
      ...player,
      ball: {
        ...player.ball,
        x: candidate.tee.x + 0.5,
        y: candidate.tee.y + 0.5,
        z: candidate.tiles[candidate.tee.y * candidate.width + candidate.tee.x]!.height + 0.18,
        vx: 0,
        vy: 0,
        vz: 0,
        complete: false,
        strokes: 0,
        resetCount: 0,
      },
    })),
    messages: [`locked ${candidate.seed}`, ...state.messages].slice(0, 5),
  };
  setState(beginCourse(state));
};

const chooseAim = (event: PointerEvent) => {
  if (!renderer || state.status !== 'playing' || current().kind !== 'human') return;
  aim = renderer.aimFromPointer(event, state.course, current().ball);
  renderer.draw(state.course, state.players, aim, liveEmotes);
  renderControls();
};

const adjustPower = (amount: number) => {
  aim = { ...aim, power: Math.max(1, Math.min(8, Number((aim.power + amount).toFixed(1)))) };
  renderer?.draw(state.course, state.players, aim, liveEmotes);
  renderControls();
};

const drawShotFrame = (balls: readonly Ball[]) => {
  const players = state.players.map((player, index) => ({ ...player, ball: balls[index]! }));
  renderer?.draw(state.course, players, undefined, liveEmotes);
};

const playShot = (shot: ShotCommand) => {
  if (state.status !== 'playing' || shotAnimation) return;
  const player = current();
  const frames = previewShot(state, shot);
  if (!frames?.length) {
    setState(applyCommand(state, { type: 'shoot', shot }));
    return;
  }
  const source = state;
  const inFlight = { ...source, turn: { ...source.turn, shotInFlight: true } };
  const duration = Math.min(2_200, Math.max(360, frames.length * 11));
  const startedAt = performance.now();
  shotAnimation = { playerId: player.id, frame: 0 };
  state = inFlight;
  renderControls();
  document.querySelector<HTMLElement>('#status')!.textContent = renderStatus();
  const animate = (now: number) => {
    if (!shotAnimation || state !== inFlight) return;
    const progress = Math.min(1, (now - startedAt) / duration);
    const frame = Math.min(frames.length - 1, Math.floor(progress * (frames.length - 1)));
    shotAnimation.frame = frame;
    drawShotFrame(frames[frame]!);
    if (progress < 1) {
      requestAnimationFrame(animate);
      return;
    }
    shotAnimation = undefined;
    setState(applyCommand(source, { type: 'shoot', shot }));
  };
  requestAnimationFrame(animate);
};

const shoot = () => {
  if (state.status !== 'playing' || current().kind !== 'human') return;
  playShot(aim);
};

const usePowerUp = (powerUp: PowerUp) => {
  if (state.status !== 'playing' || current().kind !== 'human' || shotAnimation) return;
  const target = state.players.find((player) => player.id !== current().id && !player.ball.complete);
  setState(applyCommand(state, { type: 'use-power-up', powerUp, targetId: target?.id }));
};

const sendEmote = (emote: Emote) => {
  if (state.status === 'lobby' || shotAnimation) return;
  const player = current();
  if (player.kind !== 'human') return;
  setState(applyCommand(state, { type: 'emote', playerId: player.id, emote }));
};

const scheduleBot = () => {
  window.clearTimeout(botTimeout);
  if (current().kind !== 'bot') return;
  if (state.status === 'draft') {
    botTimeout = window.setTimeout(() => {
      const choices = currentUpgradeChoices(state);
      const bot = current();
      const skill = typeof bot.skill === 'number' ? bot.skill : 6;
      setState(applyCommand(state, { type: 'draft', upgrade: choices[(skill - 1) % choices.length]! }));
    }, preferences.reducedMotion ? 120 : 450);
    return;
  }
  if (state.status !== 'playing') return;
  botTimeout = window.setTimeout(() => {
    const decision = botMove(state);
    if (!decision) return;
    if (decision.powerUp) setState(applyCommand(state, { type: 'use-power-up', powerUp: decision.powerUp.type, targetId: decision.powerUp.targetId }));
    playShot(decision.shot);
  }, preferences.reducedMotion ? 180 : 650);
};

const renderControls = () => {
  const control = document.querySelector<HTMLElement>('#controls');
  if (!control) return;
  const player = current();
  const disabled = state.status !== 'playing' || player.kind !== 'human' || Boolean(shotAnimation);
  control.innerHTML = `
    <div class="turn"><span style="--player:${player.color}"></span><strong>${escape(player.name)}</strong><b>${state.turn.secondsLeft.toFixed(0)}s</b></div>
    <div class="emote-buttons" aria-label="emotes">${EMOTES.map((emote) => `<button data-emote="${emote.id}" title="${emote.label}" ${disabled ? 'disabled' : ''}>${emote.glyph}</button>`).join('')}</div>
    <label>power <input id="power" type="range" min="1" max="8" step="0.1" value="${aim.power}" ${disabled ? 'disabled' : ''}></label>
    <button id="shoot" class="primary" ${disabled ? 'disabled' : ''}>shoot <kbd>${keyLabel(bindingFor(preferences, 'shoot'))}</kbd></button>
    ${player.inventory ? `<button id="powerup" ${disabled ? 'disabled' : ''}>use ${player.inventory} <kbd>${keyLabel(bindingFor(preferences, 'usePowerUp'))}</kbd></button>` : '<span class="muted">no chaos item</span>'}
    <p id="status" class="control-status">${renderStatus()}</p>
  `;
  document.querySelector<HTMLInputElement>('#power')?.addEventListener('input', (event) => {
    aim = { ...aim, power: Number((event.target as HTMLInputElement).value) };
    renderer?.draw(state.course, state.players, aim, liveEmotes);
    renderControls();
  });
  document.querySelector<HTMLButtonElement>('#shoot')?.addEventListener('click', shoot);
  document.querySelector<HTMLButtonElement>('#powerup')?.addEventListener('click', () => { if (player.inventory) usePowerUp(player.inventory); });
};

const renderOverlay = () => {
  if (!overlay) return '';
  if (overlay === 'help') return `<section class="overlay" role="dialog" aria-modal="true" aria-label="shortcut help"><div class="overlay-card"><button class="close" data-close-overlay aria-label="close help">×</button><p class="eyebrow">MOUSE FIRST · KEYS READY</p><h2>quick commands</h2><dl class="shortcut-list">${shortcutRows()}</dl><p class="hint">Shortcuts pause while you edit a field. F1 opens settings to remap commands.</p></div></section>`;
  return `<section class="overlay" role="dialog" aria-modal="true" aria-label="game settings"><div class="overlay-card settings-card"><button class="close" data-close-overlay aria-label="close settings">×</button><p class="eyebrow">LOCAL PREFERENCES</p><h2>terminal settings</h2><label class="setting-toggle"><input data-preference="reducedMotion" type="checkbox" ${preferences.reducedMotion ? 'checked' : ''}> reduced motion and flash</label><label class="setting-toggle"><input data-preference="highContrast" type="checkbox" ${preferences.highContrast ? 'checked' : ''}> high-contrast glyphs</label><h3>shortcuts</h3><dl class="shortcut-list">${shortcutRows(true)}</dl><p class="hint">${rebinding ? `press a key for ${escape(rebinding)} · Esc cancels` : 'select a key to rebind it'}</p></div></section>`;
};

const shortcutRows = (interactive = false) => ['shoot', 'powerDown', 'powerUp', 'lock', 'reroll', 'usePowerUp', 'help', 'settings'].map((id) => {
  const shortcutId = id as ShortcutId;
  const label = ({ shoot: 'shoot', powerDown: 'power down', powerUp: 'power up', lock: 'lock candidate', reroll: 'reroll candidates', usePowerUp: 'use chaos item', help: 'shortcut help', settings: 'settings' })[shortcutId];
  const key = keyLabel(bindingFor(preferences, shortcutId));
  return `<dt>${label}</dt><dd>${interactive ? `<button data-bind="${shortcutId}" class="key-button ${rebinding === shortcutId ? 'selected' : ''}">${key}</button>` : `<kbd>${key}</kbd>`}</dd>`;
}).join('');

const renderRunDrawer = () => {
  const scoreRows = state.players.map((player) => `<tr class="${player.id === current().id ? 'active' : ''}"><td><i style="background:${player.color}"></i>${escape(player.name)}</td><td>${player.ball.complete ? '✓' : player.ball.strokes}</td><td>${player.total}</td></tr>`).join('');
  return `
    <h3>campaign controls</h3>
    <label>run seed <input id="seed" value="${escape(config.seed)}" maxlength="32"></label>
    <div class="split"><label>humans <input id="humans" type="number" min="1" max="8" value="${config.humanCount}"></label><label>AI <input id="bots" type="number" min="0" max="4" value="${config.botCount}"></label></div>
    <div class="split"><label>timer <input id="timer" type="number" min="8" max="90" value="${config.timerSeconds}"></label><label>skill <select id="skill"><option value="adaptive" ${config.botSkill === 'adaptive' ? 'selected' : ''}>adaptive</option>${Array.from({ length: 10 }, (_, index) => `<option value="${index + 1}" ${config.botSkill === index + 1 ? 'selected' : ''}>${index + 1}</option>`).join('')}</select></label></div>
    <label class="toggle"><input id="collisions" type="checkbox" ${config.collisions ? 'checked' : ''}> physical ball collisions</label>
    <label class="toggle"><input id="powerups" type="checkbox" ${config.powerUps ? 'checked' : ''}> high-chaos power-ups</label>
    <button id="new-run">generate candidates</button>
    <p class="hint">up to 12 seats · max 4 AI · sequential timed turns</p>
    <h3>scorecard</h3><table><thead><tr><th>player</th><th>hole</th><th>total</th></tr></thead><tbody>${scoreRows}</tbody></table>`;
};

const renderDrawer = () => {
  if (!drawer) return '';
  const title = drawer === 'run' ? 'run controls' : state.status === 'preview' ? 'course selection' : 'course intel';
  return `<aside class="drawer drawer-${drawer} panel" aria-label="${title}"><div class="drawer-heading"><h2>${title}</h2><button data-close-drawer class="close" aria-label="close ${title}">×</button></div><div class="drawer-content">${drawer === 'run' ? renderRunDrawer() : renderInspector()}</div></aside>`;
};

const render = () => {
  app.innerHTML = `
    <section class="topbar"><div class="brand"><p class="eyebrow">ARCADE QUARRY MINI GOLF</p><h1>GOLF <em>WITH YOUR</em> ENEMIES</h1></div><div class="title-actions"><button data-drawer="run" class="${drawer === 'run' ? 'selected' : ''}" aria-expanded="${drawer === 'run'}">run</button><button data-drawer="intel" class="${drawer === 'intel' ? 'selected' : ''}" aria-expanded="${drawer === 'intel'}">intel</button><button data-open-overlay="help">? help</button><button data-open-overlay="settings">F1 settings</button><div class="hole">HOLE <b>${state.hole}</b> / 9<br><small>${escape(state.course.seed)}</small></div></div></section>
    <section class="layout">
      <section class="board panel"><div class="course-stage"><canvas id="course" aria-label="isometric arcade quarry golf course"></canvas><div id="callouts" class="callouts" aria-live="polite">${calloutMarkup()}</div></div><div id="controls" class="controls"></div></section>
    </section>${renderDrawer()}${renderOverlay()}
  `;
  document.querySelector<HTMLButtonElement>('#new-run')?.addEventListener('click', setupGame);
  const canvas = document.querySelector<HTMLCanvasElement>('#course')!;
  renderer?.dispose();
  renderer = createRenderer(canvas);
  renderer.draw(state.course, state.players, aim, liveEmotes);
  canvas.addEventListener('pointermove', chooseAim);
  canvas.addEventListener('pointerdown', chooseAim);
  renderControls();
};

const renderStatus = () => {
  if (state.status === 'preview') return 'generator inspection — choose a validated candidate, then lock the hole';
  if (state.status === 'draft') return 'draft phase — each player chooses an upgrade';
  if (state.status === 'finished') return `winner: ${escape([...state.players].sort((a, b) => a.total - b.total)[0]!.name)}`;
  if (shotAnimation) return `${escape(current().name)}'s ball is in flight`;
  return `${escape(current().name)} is taking a turn`;
};

const calloutMarkup = () => callouts.map((callout) => `<p class="callout ${callout.tone}">${escape(callout.message)}</p>`).join('');

const renderInspector = () => {
  if (state.status === 'preview') {
    const candidate = candidates[selectedCandidate] ?? state.course;
    const solver = candidate.score.solverShots[0];
    return `<div class="candidate-tabs">${candidates.map((_, index) => `<button data-candidate="${index}" class="${index === selectedCandidate ? 'selected' : ''}">candidate ${index + 1}</button>`).join('')}</div>
      <dl><dt>seed</dt><dd>${escape(candidate.seed)}</dd><dt>verdict</dt><dd class="good">validated</dd><dt>quality</dt><dd>${candidate.score.total}/100</dd><dt>solver line</dt><dd>${solver ? `${solver.power.toFixed(1)} power @ ${(solver.angle * 180 / Math.PI).toFixed(0)}°` : 'none'}</dd><dt>expected strokes</dt><dd>${candidate.score.estimatedStrokes}</dd><dt>hazards</dt><dd>${candidate.score.hazards}</dd><dt>elevation</dt><dd>${candidate.score.elevation}</dd><dt>route score</dt><dd>${candidate.score.routes} lanes · ${candidate.score.novelty} novelty</dd></dl>
      <div class="inspector-actions"><button id="reroll">reroll <kbd>${keyLabel(bindingFor(preferences, 'reroll'))}</kbd></button><button id="lock" class="primary">lock & tee off <kbd>${keyLabel(bindingFor(preferences, 'lock'))}</kbd></button></div><p class="hint">The generator carves a reachable route first, decorates it with freeform terrain, then accepts only simulated cup lines.</p>${renderLedger()}`;
  }
  if (state.status === 'draft') return `<p>each player keeps one modifier for the rest of the campaign.</p><div class="upgrades">${currentUpgradeChoices(state).map((upgrade) => `<button data-upgrade="${upgrade}"><strong>${upgrade}</strong><small>${UPGRADE_DESCRIPTIONS[upgrade]}</small></button>`).join('')}</div>${renderLedger()}`;
  return `${renderLedger()}<h3>course legend</h3><p class="legend">quarry stone · sand drift · ice sheet<br>red chevrons guide the route</p>`;
};

const renderLedger = () => `<ul class="feed" aria-live="polite">${ledger.map((entry) => `<li class="${entry.tone}"><span>${escape(entry.message)}</span></li>`).join('') || '<li class="neutral"><span>waiting for the first stroke</span></li>'}</ul>`;

const updatePreferences = (partial: Partial<typeof preferences>) => {
  preferences = { ...preferences, ...partial };
  savePreferences(preferences);
  applyPreferences();
  render();
};

app.addEventListener('click', (event) => {
  const element = event.target as HTMLElement;
  const drawerTarget = element.dataset.drawer as Exclude<Drawer, undefined> | undefined;
  if (drawerTarget) { drawer = drawer === drawerTarget ? undefined : drawerTarget; render(); return; }
  if (element.hasAttribute('data-close-drawer')) { drawer = undefined; render(); return; }
  const candidate = element.dataset.candidate;
  if (candidate !== undefined) { selectedCandidate = Number(candidate); render(); return; }
  if (element.id === 'lock') { lockCandidate(); return; }
  if (element.id === 'reroll') { rerollCandidates(); return; }
  const emote = element.dataset.emote as Emote | undefined;
  if (emote) { sendEmote(emote); return; }
  const upgrade = element.dataset.upgrade;
  if (upgrade) { setState(applyCommand(state, { type: 'draft', upgrade: upgrade as keyof typeof UPGRADE_DESCRIPTIONS })); return; }
  const openOverlay = element.dataset.openOverlay as Overlay;
  if (openOverlay) { overlay = openOverlay; rebinding = undefined; render(); return; }
  if (element.hasAttribute('data-close-overlay')) { overlay = undefined; rebinding = undefined; render(); return; }
  const binding = element.dataset.bind as ShortcutId | undefined;
  if (binding) { rebinding = binding; render(); }
});

app.addEventListener('change', (event) => {
  const target = event.target as HTMLInputElement;
  const preference = target.dataset.preference;
  if (preference === 'reducedMotion' || preference === 'highContrast') updatePreferences({ [preference]: target.checked });
});

window.addEventListener('keydown', (event) => {
  if (rebinding) {
    if (event.key === 'Escape') { rebinding = undefined; render(); return; }
    event.preventDefault();
    const next = setShortcut(preferences, rebinding, event.key);
    if (next !== preferences) { preferences = next; savePreferences(preferences); }
    rebinding = undefined;
    render();
    return;
  }
  if (isEditableElement(event.target)) return;
  if (event.key === 'Escape' && overlay) { overlay = undefined; render(); return; }
  const command = shortcutForKey(preferences, event.key);
  if (!command) return;
  event.preventDefault();
  if (command === 'help') { overlay = overlay === 'help' ? undefined : 'help'; render(); return; }
  if (command === 'settings') { overlay = overlay === 'settings' ? undefined : 'settings'; render(); return; }
  if (overlay || shotAnimation) return;
  if (command === 'shoot') shoot();
  if (command === 'powerDown') adjustPower(-0.2);
  if (command === 'powerUp') adjustPower(0.2);
  if (command === 'lock') lockCandidate();
  if (command === 'reroll') rerollCandidates();
  if (command === 'usePowerUp') {
    const player = current();
    if (player.inventory) usePowerUp(player.inventory);
  }
});

const loop = (now: number) => {
  const elapsed = Math.min(1, (now - lastTick) / 1000);
  lastTick = now;
  const previousCalloutCount = callouts.length;
  callouts = callouts.filter((callout) => callout.expiresAt > now);
  if (callouts.length !== previousCalloutCount) document.querySelector<HTMLElement>('#callouts')!.innerHTML = calloutMarkup();
  const previousEmoteCount = liveEmotes.length;
  liveEmotes = liveEmotes.filter((emote) => emote.expiresAt > now);
  if (liveEmotes.length !== previousEmoteCount && !shotAnimation) renderer?.draw(state.course, state.players, aim, liveEmotes);
  if (state.status === 'playing' && !shotAnimation) {
    const next = tickTurn(state, elapsed);
    if (next !== state) {
      state = next;
      recordStateFeedback(state);
      renderControls();
      document.querySelector<HTMLElement>('#status')!.textContent = renderStatus();
      document.querySelector<HTMLElement>('#callouts')!.innerHTML = calloutMarkup();
    }
  }
  requestAnimationFrame(loop);
};

applyPreferences();
document.documentElement.classList.toggle('capture-mode', captureMode);
recordStateFeedback(state);
render();
requestAnimationFrame(loop);
