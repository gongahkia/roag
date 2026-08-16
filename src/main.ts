import './style.css';
import { presentFeedback, type FeedbackTone } from './feedback';
import { applyCommand, botMove, createGame, currentUpgradeChoices, defaultConfig, previewShot, tickTurn, UPGRADE_DESCRIPTIONS } from './core/game';
import { bindingFor, isEditableElement, loadPreferences, savePreferences, setShortcut, shortcutForKey, type ShortcutId } from './preferences';
import { EMOTES, type Ball, type BuildTool, type Emote, type EmoteEvent, type GameState, type PowerUp, type ShotCommand } from './core/types';
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
  state = next;
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
  drawer = captureMode ? undefined : 'intel';
  liveEmotes = [];
  seenEmoteIds = new Set();
  setState(createGame(config));
};

const chooseAim = (event: PointerEvent) => {
  if (!renderer || (state.status !== 'playing' && state.status !== 'validate') || current().kind !== 'human') return;
  aim = renderer.aimFromPointer(event, state.course, current().ball);
  renderer.draw(state.course, state.players, state.coursePhase, aim, liveEmotes, state.config.powerUps);
  renderControls();
};

const placeBuild = (event: PointerEvent) => {
  if (!renderer || state.status !== 'build' || current().kind !== 'human') return;
  const point = renderer.tileFromPointer(event, state.course);
  if (point) setState(applyCommand(state, { type: 'build-place', point }));
};

const adjustPower = (amount: number) => {
  aim = { ...aim, power: Math.max(1, Math.min(8, Number((aim.power + amount).toFixed(1)))) };
  if (state.status !== 'playing' && state.status !== 'validate') return;
  renderer?.draw(state.course, state.players, state.coursePhase, aim, liveEmotes, state.config.powerUps);
  renderControls();
};

const drawShotFrame = (balls: readonly Ball[] | undefined) => {
  if (!balls) return;
  const players = state.players.map((player, index) => ({ ...player, ball: balls[index] ?? player.ball }));
  renderer?.draw(state.course, players, state.coursePhase, undefined, liveEmotes, state.config.powerUps);
};

const playShot = (shot: ShotCommand) => {
  if ((state.status !== 'playing' && state.status !== 'validate') || shotAnimation) return;
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
    const progress = Math.max(0, Math.min(1, (now - startedAt) / duration));
    const frame = Math.max(0, Math.min(frames.length - 1, Math.floor(progress * (frames.length - 1))));
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
  if ((state.status !== 'playing' && state.status !== 'validate') || current().kind !== 'human') return;
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
  if (state.status === 'build') {
    botTimeout = window.setTimeout(() => {
      if (state.status !== 'build' || current().kind !== 'bot') return;
      const bot = current();
      if (!bot.upgrades.length) {
        const skill = typeof bot.skill === 'number' ? bot.skill : 6;
        const upgrade = Object.keys(UPGRADE_DESCRIPTIONS)[(skill - 1) % Object.keys(UPGRADE_DESCRIPTIONS).length]! as keyof typeof UPGRADE_DESCRIPTIONS;
        setState(applyCommand(state, { type: 'select-upgrade', upgrade }));
        return;
      }
      setState(applyCommand(state, { type: state.build?.generated ? 'begin-validation' : 'build-generate' }));
    }, preferences.reducedMotion ? 90 : 500);
    return;
  }
  if (state.status === 'draft') {
    botTimeout = window.setTimeout(() => {
      const choices = currentUpgradeChoices(state);
      const bot = current();
      const skill = typeof bot.skill === 'number' ? bot.skill : 6;
      setState(applyCommand(state, { type: 'draft', upgrade: choices[(skill - 1) % choices.length]! }));
    }, preferences.reducedMotion ? 120 : 450);
    return;
  }
  if (state.status !== 'playing' && state.status !== 'validate') return;
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
  if (state.status === 'build') {
    const build = state.build!;
    control.innerHTML = `
      <div class="turn"><span style="--player:${player.color}"></span><strong>${escape(player.name)}</strong><b>build</b><small class="phase">author ${build.authorIndex + 1}/${state.players.length}</small></div>
      <p class="control-status">${renderStatus()}</p>
      <button id="validate" class="primary" ${player.kind !== 'human' ? 'disabled' : ''}>validate course <kbd>${keyLabel(bindingFor(preferences, 'lock'))}</kbd></button>
    `;
    document.querySelector<HTMLButtonElement>('#validate')?.addEventListener('click', () => setState(applyCommand(state, { type: 'begin-validation' })));
    return;
  }
  const disabled = (state.status !== 'playing' && state.status !== 'validate') || player.kind !== 'human' || Boolean(shotAnimation);
  control.innerHTML = `
    <div class="turn"><span style="--player:${player.color}"></span><strong>${escape(player.name)}</strong><b>${state.turn.secondsLeft.toFixed(0)}s</b><small class="phase">hazard ${state.coursePhase + 1}/8</small></div>
    <div class="emote-buttons" aria-label="emotes">${EMOTES.map((emote) => `<button data-emote="${emote.id}" title="${emote.label}" ${disabled ? 'disabled' : ''}>${emote.glyph}</button>`).join('')}</div>
    <label>power <input id="power" type="range" min="1" max="8" step="0.1" value="${aim.power}" ${disabled ? 'disabled' : ''}></label>
    <button id="shoot" class="primary" ${disabled ? 'disabled' : ''}>shoot <kbd>${keyLabel(bindingFor(preferences, 'shoot'))}</kbd></button>
    ${player.inventory ? `<button id="powerup" ${disabled ? 'disabled' : ''}>use ${player.inventory} <kbd>${keyLabel(bindingFor(preferences, 'usePowerUp'))}</kbd></button>` : '<span class="muted">no chaos item</span>'}
    <p id="status" class="control-status">${renderStatus()}</p>
  `;
  document.querySelector<HTMLInputElement>('#power')?.addEventListener('input', (event) => {
    aim = { ...aim, power: Number((event.target as HTMLInputElement).value) };
    renderer?.draw(state.course, state.players, state.coursePhase, aim, liveEmotes, state.config.powerUps);
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
  const label = ({ shoot: 'shoot', powerDown: 'power down', powerUp: 'power up', lock: 'validate course', reroll: 'generate terrain', usePowerUp: 'use chaos item', help: 'shortcut help', settings: 'settings' })[shortcutId];
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
    <button id="new-run">start new build round</button>
    <p class="hint">up to 12 seats · max 4 AI · every player authors one course, then everyone competes on all validated courses</p>
    <h3>scorecard</h3><table><thead><tr><th>player</th><th>hole</th><th>total</th></tr></thead><tbody>${scoreRows}</tbody></table>`;
};

const renderDrawer = () => {
  if (!drawer) return '';
  const title = drawer === 'run' ? 'run controls' : state.status === 'build' ? 'course builder' : 'course intel';
  return `<aside class="drawer drawer-${drawer} panel" aria-label="${title}"><div class="drawer-heading"><h2>${title}</h2><button data-close-drawer class="close" aria-label="close ${title}">×</button></div><div class="drawer-content">${drawer === 'run' ? renderRunDrawer() : renderInspector()}</div></aside>`;
};

const render = () => {
  const progress = state.status === 'build'
    ? `BUILD <b>${state.build!.authorIndex + 1}</b> / ${state.players.length}`
    : state.status === 'validate'
      ? `TEST <b>${state.build!.authorIndex + 1}</b> / ${state.players.length}`
      : `COURSE <b>${state.courseIndex + 1}</b> / ${state.authoredCourses.length}`;
  app.innerHTML = `
    <section class="topbar"><div class="brand"><p class="eyebrow">ARCADE QUARRY MINI GOLF</p><h1>GOLF <em>WITH YOUR</em> ENEMIES</h1></div><div class="title-actions"><button data-drawer="run" class="${drawer === 'run' ? 'selected' : ''}" aria-expanded="${drawer === 'run'}">run</button><button data-drawer="intel" class="${drawer === 'intel' ? 'selected' : ''}" aria-expanded="${drawer === 'intel'}">${state.status === 'build' ? 'build' : 'intel'}</button><button data-open-overlay="help">? help</button><button data-open-overlay="settings">F1 settings</button><div class="hole">${progress}<br><small>${escape(state.course.seed)}</small></div></div></section>
    <section class="layout">
      <section class="board panel"><div class="course-stage"><canvas id="course" aria-label="isometric arcade quarry golf course"></canvas><div id="callouts" class="callouts" aria-live="polite">${calloutMarkup()}</div></div><div id="controls" class="controls"></div></section>
    </section>${renderDrawer()}${renderOverlay()}
  `;
  document.querySelector<HTMLButtonElement>('#new-run')?.addEventListener('click', setupGame);
  const canvas = document.querySelector<HTMLCanvasElement>('#course')!;
  renderer?.dispose();
  renderer = createRenderer(canvas);
  renderer.draw(state.course, state.players, state.coursePhase, state.status === 'build' ? undefined : aim, liveEmotes, state.config.powerUps, state.status === 'build');
  canvas.addEventListener('pointermove', chooseAim);
  canvas.addEventListener('pointerdown', state.status === 'build' ? placeBuild : chooseAim);
  renderControls();
};

const renderStatus = () => {
  if (state.status === 'build') return 'place every tile from scratch, or generate a seeded terrain pass and edit it';
  if (state.status === 'validate') return `${escape(current().name)} must sink this authored course once before it enters competition`;
  if (state.status === 'preview') return 'course preview';
  if (state.status === 'draft') return 'draft phase — each player chooses an upgrade';
  if (state.status === 'finished') return `winner: ${escape([...state.players].sort((a, b) => a.total - b.total)[0]!.name)}`;
  if (shotAnimation) return `${escape(current().name)}'s ball is in flight · hazard ${state.coursePhase + 1}/8 locked`;
  return `${escape(current().name)} is taking a turn · hazard ${state.coursePhase + 1}/8`;
};

const calloutMarkup = () => callouts.map((callout) => `<p class="callout ${callout.tone}">${escape(callout.message)}</p>`).join('');

const renderInspector = () => {
  if (state.status === 'build') {
    const build = state.build!;
    const tools: { id: BuildTool; label: string }[] = [
      { id: 'fairway', label: 'fairway' }, { id: 'rough', label: 'rough' }, { id: 'sand', label: 'sand' }, { id: 'ice', label: 'ice' },
      { id: 'wall', label: 'wall' }, { id: 'booster', label: 'booster' }, { id: 'conveyor', label: 'conveyor' }, { id: 'tee', label: 'tee' },
      { id: 'cup', label: 'cup' }, { id: 'sweeper', label: 'sweeper' }, { id: 'gate', label: 'timed gate' }, { id: 'recovery-pad', label: 'recovery pad' },
      { id: 'chaos-pad', label: 'chaos pad' }, { id: 'erase', label: 'erase' },
    ];
    const directions = [{ label: '→', x: 1, y: 0 }, { label: '↓', x: 0, y: 1 }, { label: '←', x: -1, y: 0 }, { label: '↑', x: 0, y: -1 }];
    return `<p class="hint"><strong>${escape(current().name)}</strong> is authoring. Select a tool, then click any isometric grid cell. A course needs one tee, one cup, and eight playable tiles.</p>
      <h3>place</h3><div class="build-tools">${tools.map((tool) => `<button data-build-tool="${tool.id}" class="${build.tool === tool.id ? 'selected' : ''}">${tool.label}</button>`).join('')}</div>
      <h3>tile controls</h3><label class="builder-range">height <output>${build.height}</output><input id="build-height" type="range" min="0" max="3" step="1" value="${build.height}"></label>
      <div class="build-directions" aria-label="surface direction">${directions.map((direction) => `<button data-build-direction="${direction.x},${direction.y}" class="${build.direction.x === direction.x && build.direction.y === direction.y ? 'selected' : ''}" title="direction ${direction.label}">${direction.label}</button>`).join('')}</div>
      <h3>seeded terrain generator</h3><p class="hint">deterministic route carving with editable terrain, ramp, wall, and hazard placement. Same seed and sliders produce the same starting course.</p>
      <label class="builder-range">terrain density <output>${Math.round(build.terrain.density * 100)}%</output><input id="terrain-density" type="range" min="0.1" max="1" step="0.05" value="${build.terrain.density}"></label>
      <label class="builder-range">ramp frequency <output>${Math.round(build.terrain.elevation * 100)}%</output><input id="terrain-elevation" type="range" min="0" max="1" step="0.05" value="${build.terrain.elevation}"></label>
      <label class="builder-range">timed hazards <output>${build.terrain.hazards}</output><input id="terrain-hazards" type="range" min="0" max="3" step="1" value="${build.terrain.hazards}"></label>
      <h3>competitive perk</h3><p class="hint">choose one upgrade for the play phase. You can change it until your course validates.</p><div class="upgrades build-perks">${Object.entries(UPGRADE_DESCRIPTIONS).map(([upgrade, description]) => `<button data-builder-upgrade="${upgrade}" class="${current().upgrades.includes(upgrade as keyof typeof UPGRADE_DESCRIPTIONS) ? 'selected' : ''}"><strong>${upgrade}</strong><small>${description}</small></button>`).join('')}</div>
      <div class="inspector-actions"><button id="auto-terrain">generate terrain <kbd>${keyLabel(bindingFor(preferences, 'reroll'))}</kbd></button><button id="validate" class="primary">validate course <kbd>${keyLabel(bindingFor(preferences, 'lock'))}</kbd></button></div>${renderLedger()}`;
  }
  if (state.status === 'draft') return `<p>each player keeps one modifier for the rest of the campaign.</p><div class="upgrades">${currentUpgradeChoices(state).map((upgrade) => `<button data-upgrade="${upgrade}"><strong>${upgrade}</strong><small>${UPGRADE_DESCRIPTIONS[upgrade]}</small></button>`).join('')}</div>${renderLedger()}`;
  const author = state.authoredCourses[state.courseIndex] && state.players.find((player) => player.id === state.authoredCourses[state.courseIndex]!.authorId);
  return `${renderLedger()}<h3>course</h3><dl><dt>author</dt><dd>${author ? escape(author.name) : 'unassigned'}</dd><dt>validated</dt><dd class="good">author completed one sink</dd><dt>hazards</dt><dd>${state.course.hazards.map((hazard) => hazard.kind).join(' + ') || 'none'}</dd><dt>chaos pads</dt><dd>${state.course.itemPads.length}</dd></dl><h3>course legend</h3><p class="legend">quarry stone · sand drift · ice sheet<br>amber arm: sweeper · red/cyan: timed gate<br>cyan +: recovery pad · violet !: chaos pad</p>`;
};

const renderLedger = () => `<ul class="feed" aria-live="polite">${ledger.map((entry) => `<li class="${entry.tone}"><span>${escape(entry.message)}</span></li>`).join('') || '<li class="neutral"><span>waiting for the first stroke</span></li>'}</ul>`;

const updatePreferences = (partial: Partial<typeof preferences>) => {
  preferences = { ...preferences, ...partial };
  savePreferences(preferences);
  applyPreferences();
  render();
};

app.addEventListener('click', (event) => {
  const target = event.target as HTMLElement;
  const element = target.closest<HTMLElement>('button') ?? target;
  const drawerTarget = element.dataset.drawer as Exclude<Drawer, undefined> | undefined;
  if (drawerTarget) { drawer = drawer === drawerTarget ? undefined : drawerTarget; render(); return; }
  if (element.hasAttribute('data-close-drawer')) { drawer = undefined; render(); return; }
  const buildTool = element.dataset.buildTool as BuildTool | undefined;
  if (buildTool && state.status === 'build') { setState(applyCommand(state, { type: 'build-settings', tool: buildTool })); return; }
  const builderUpgrade = element.dataset.builderUpgrade as keyof typeof UPGRADE_DESCRIPTIONS | undefined;
  if (builderUpgrade && state.status === 'build') { setState(applyCommand(state, { type: 'select-upgrade', upgrade: builderUpgrade })); return; }
  const direction = element.dataset.buildDirection;
  if (direction && state.status === 'build') {
    const [x, y] = direction.split(',').map(Number);
    setState(applyCommand(state, { type: 'build-settings', direction: { x, y } }));
    return;
  }
  if (element.id === 'auto-terrain' && state.status === 'build') { setState(applyCommand(state, { type: 'build-generate' })); return; }
  if (element.id === 'validate' && state.status === 'build') { setState(applyCommand(state, { type: 'begin-validation' })); return; }
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

app.addEventListener('input', (event) => {
  const target = event.target as HTMLInputElement;
  if (state.status !== 'build') return;
  if (target.id === 'build-height') {
    setState(applyCommand(state, { type: 'build-settings', height: Number(target.value) }));
    return;
  }
  const terrain = target.id === 'terrain-density' ? { density: Number(target.value) }
    : target.id === 'terrain-elevation' ? { elevation: Number(target.value) }
      : target.id === 'terrain-hazards' ? { hazards: Number(target.value) } : undefined;
  if (terrain) setState(applyCommand(state, { type: 'build-settings', terrain }));
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
  if (command === 'lock' && state.status === 'build') setState(applyCommand(state, { type: 'begin-validation' }));
  if (command === 'reroll' && state.status === 'build') setState(applyCommand(state, { type: 'build-generate' }));
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
  if (liveEmotes.length !== previousEmoteCount && !shotAnimation) renderer?.draw(state.course, state.players, state.coursePhase, aim, liveEmotes, state.config.powerUps);
  if ((state.status === 'playing' || state.status === 'validate') && !shotAnimation) {
    const next = tickTurn(state, elapsed);
    if (next !== state) {
      state = next;
      recordStateFeedback(state);
      renderer?.draw(state.course, state.players, state.coursePhase, aim, liveEmotes, state.config.powerUps);
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
