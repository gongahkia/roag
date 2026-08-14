import './style.css';
import { applyCommand, beginCourse, botMove, createGame, currentUpgradeChoices, defaultConfig, tickTurn } from './core/game';
import { generateCandidates } from './core/generator';
import { hashSeed } from './core/random';
import type { GameState, PowerUp, ShotCommand } from './core/types';
import { createRenderer } from './ui/render';

const app = document.querySelector<HTMLElement>('#app')!;
let config = defaultConfig();
let state = createGame(config);
let candidates = generateCandidates(hashSeed(config.seed, 1));
let selectedCandidate = 0;
let aim: ShotCommand = { angle: 0, power: 4 };
let renderer: ReturnType<typeof createRenderer> | undefined;
let lastTick = performance.now();
let botTimeout: number | undefined;

const escape = (value: string) => value.replace(/[&<>'"]/g, (character) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[character]!);

const current = () => state.players[state.turn.playerIndex]!;

const setState = (next: GameState) => {
  const refreshCandidates = next.status === 'preview' && (next.hole !== state.hole || next.course.seed !== state.course.seed);
  state = next;
  if (refreshCandidates) candidates = generateCandidates(hashSeed(state.config.seed, state.hole));
  render();
  scheduleBot();
};

const setupGame = () => {
  config = {
    ...config,
    seed: (document.querySelector<HTMLInputElement>('#seed')?.value.trim() || config.seed),
    humanCount: Number(document.querySelector<HTMLInputElement>('#humans')?.value || config.humanCount),
    botCount: Number(document.querySelector<HTMLInputElement>('#bots')?.value || config.botCount),
    timerSeconds: Number(document.querySelector<HTMLInputElement>('#timer')?.value || config.timerSeconds),
    botSkill: document.querySelector<HTMLSelectElement>('#skill')?.value === 'adaptive' ? 'adaptive' : Number(document.querySelector<HTMLSelectElement>('#skill')?.value || config.botSkill),
    collisions: document.querySelector<HTMLInputElement>('#collisions')?.checked ?? config.collisions,
    powerUps: document.querySelector<HTMLInputElement>('#powerups')?.checked ?? config.powerUps,
  };
  if (config.humanCount + config.botCount > 12 || config.botCount > 4 || config.humanCount < 1) return;
  state = createGame(config);
  candidates = generateCandidates(hashSeed(config.seed, 1));
  selectedCandidate = 0;
  render();
};

const lockCandidate = () => {
  const candidate = candidates[selectedCandidate];
  if (!candidate) return;
  state = { ...state, course: candidate, players: state.players.map((player) => ({ ...player, ball: { ...player.ball, x: candidate.tee.x + 0.5, y: candidate.tee.y + 0.5, z: candidate.tiles[candidate.tee.y * candidate.width + candidate.tee.x]!.height + 0.18, vx: 0, vy: 0, vz: 0, complete: false, strokes: 0, resetCount: 0 } })), messages: [`locked ${candidate.seed}`, ...state.messages] };
  setState(beginCourse(state));
};

const chooseAim = (event: PointerEvent) => {
  if (!renderer || state.status !== 'playing' || current().kind !== 'human') return;
  const point = renderer.pick(event);
  const canvas = event.currentTarget as HTMLCanvasElement;
  const rect = canvas.getBoundingClientRect();
  const dx = point.x - rect.width / 2;
  const dy = point.y - 120;
  aim = { angle: Math.atan2(dy / 0.5, dx), power: Math.max(1, Math.min(8, Math.hypot(dx, dy * 2) / 50)) };
  renderer.draw(state.course, state.players, aim);
  renderControls();
};

const shoot = () => {
  if (state.status !== 'playing' || current().kind !== 'human') return;
  setState(applyCommand(state, { type: 'shoot', shot: aim }));
};

const usePowerUp = (powerUp: PowerUp) => {
  const target = state.players.find((player) => player.id !== current().id && !player.ball.complete);
  setState(applyCommand(state, { type: 'use-power-up', powerUp, targetId: target?.id }));
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
    }, 450);
    return;
  }
  if (state.status !== 'playing') return;
  botTimeout = window.setTimeout(() => {
    const move = botMove(state);
    if (move) setState(applyCommand(state, { type: 'shoot', shot: move }));
  }, 650);
};

const renderControls = () => {
  const control = document.querySelector<HTMLElement>('#controls');
  if (!control) return;
  const player = current();
  const disabled = state.status !== 'playing' || player.kind !== 'human';
  control.innerHTML = `
    <div class="turn"><span style="--player:${player.color}"></span><strong>${escape(player.name)}</strong><b>${state.turn.secondsLeft.toFixed(0)}s</b></div>
    <label>power <input id="power" type="range" min="1" max="8" step="0.1" value="${aim.power}" ${disabled ? 'disabled' : ''}></label>
    <button id="shoot" class="primary" ${disabled ? 'disabled' : ''}>shoot (${aim.power.toFixed(1)})</button>
    ${player.inventory ? `<button id="powerup" ${disabled ? 'disabled' : ''}>use ${player.inventory}</button>` : '<span class="muted">no chaos item</span>'}
  `;
  document.querySelector<HTMLInputElement>('#power')?.addEventListener('input', (event) => {
    aim = { ...aim, power: Number((event.target as HTMLInputElement).value) };
    renderer?.draw(state.course, state.players, aim);
    renderControls();
  });
  document.querySelector<HTMLButtonElement>('#shoot')?.addEventListener('click', shoot);
  document.querySelector<HTMLButtonElement>('#powerup')?.addEventListener('click', () => { if (player.inventory) usePowerUp(player.inventory); });
};

const render = () => {
  const scoreRows = state.players.map((player) => `<tr class="${player.id === current().id ? 'active' : ''}"><td><i style="background:${player.color}"></i>${escape(player.name)}</td><td>${player.ball.complete ? '✓' : player.ball.strokes}</td><td>${player.total}</td></tr>`).join('');
  app.innerHTML = `
    <section class="topbar"><div><p class="eyebrow">ASCII ROGUELIKE MINI GOLF</p><h1>GOLF <em>WITH YOUR</em> ENEMIES</h1></div><div class="hole">HOLE <b>${state.hole}</b> / 9<br><small>${escape(state.course.seed)}</small></div></section>
    <section class="layout">
      <aside class="panel lobby">
        <h2>campaign controls</h2>
        <label>run seed <input id="seed" value="${escape(config.seed)}" maxlength="32"></label>
        <div class="split"><label>humans <input id="humans" type="number" min="1" max="8" value="${config.humanCount}"></label><label>AI <input id="bots" type="number" min="0" max="4" value="${config.botCount}"></label></div>
        <div class="split"><label>timer <input id="timer" type="number" min="8" max="90" value="${config.timerSeconds}"></label><label>skill <select id="skill"><option value="adaptive" ${config.botSkill === 'adaptive' ? 'selected' : ''}>adaptive</option>${Array.from({ length: 10 }, (_, index) => `<option value="${index + 1}" ${config.botSkill === index + 1 ? 'selected' : ''}>${index + 1}</option>`).join('')}</select></label></div>
        <label class="toggle"><input id="collisions" type="checkbox" ${config.collisions ? 'checked' : ''}> ball collision</label>
        <label class="toggle"><input id="powerups" type="checkbox" ${config.powerUps ? 'checked' : ''}> high-chaos power-ups</label>
        <button id="new-run">generate candidates</button>
        <p class="hint">up to 12 seats · max 4 AI · sequential timed turns</p>
        <h2>scorecard</h2><table><thead><tr><th>player</th><th>hole</th><th>total</th></tr></thead><tbody>${scoreRows}</tbody></table>
      </aside>
      <section class="board panel"><div id="status">${renderStatus()}</div><canvas id="course" aria-label="isometric ASCII golf course"></canvas><div id="controls" class="controls"></div></section>
      <aside class="panel inspector">${renderInspector()}</aside>
    </section>
  `;
  document.querySelector<HTMLButtonElement>('#new-run')?.addEventListener('click', setupGame);
  const canvas = document.querySelector<HTMLCanvasElement>('#course')!;
  renderer = createRenderer(canvas);
  renderer.draw(state.course, state.players, aim);
  canvas.addEventListener('pointermove', chooseAim);
  canvas.addEventListener('pointerdown', chooseAim);
  renderControls();
};

const renderStatus = () => {
  if (state.status === 'preview') return 'generator inspection — choose a validated candidate, then lock the hole';
  if (state.status === 'draft') return 'draft phase — each player chooses an upgrade';
  if (state.status === 'finished') return `winner: ${escape([...state.players].sort((a, b) => a.total - b.total)[0]!.name)}`;
  return `${escape(current().name)} is taking a turn`;
};

const renderInspector = () => {
  if (state.status === 'preview') {
    const candidate = candidates[selectedCandidate] ?? state.course;
    const solver = candidate.score.solverShots[0];
    return `<h2>generator inspector</h2><div class="candidate-tabs">${candidates.map((_, index) => `<button data-candidate="${index}" class="${index === selectedCandidate ? 'selected' : ''}">candidate ${index + 1}</button>`).join('')}</div>
      <dl><dt>seed</dt><dd>${escape(candidate.seed)}</dd><dt>verdict</dt><dd class="good">validated</dd><dt>quality</dt><dd>${candidate.score.total}/100</dd><dt>solver line</dt><dd>${solver ? `${solver.power.toFixed(1)} power @ ${(solver.angle * 180 / Math.PI).toFixed(0)}°` : 'none'}</dd><dt>expected strokes</dt><dd>${candidate.score.estimatedStrokes}</dd><dt>hazards</dt><dd>${candidate.score.hazards}</dd><dt>elevation</dt><dd>${candidate.score.elevation}</dd><dt>route score</dt><dd>${candidate.score.routes} lanes · ${candidate.score.novelty} novelty</dd></dl>
      <p class="hint">The generator carves a reachable route first, decorates it with freeform terrain, then accepts only simulated cup lines.</p><button id="lock" class="primary">lock & tee off</button>`;
  }
  if (state.status === 'draft') return `<h2>upgrade draft</h2><p>each player keeps one modifier for the rest of the campaign.</p><div class="upgrades">${currentUpgradeChoices(state).map((upgrade) => `<button data-upgrade="${upgrade}">${upgrade}</button>`).join('')}</div>`;
  return `<h2>match feed</h2><ul class="feed">${state.messages.map((message) => `<li>${escape(message)}</li>`).join('')}</ul><h3>ASCII legend</h3><p class="legend">T tee · O cup · # wall · : sand · ~ ice<br>&gt; booster · = conveyor</p>`;
};

app.addEventListener('click', (event) => {
  const element = event.target as HTMLElement;
  const candidate = element.dataset.candidate;
  if (candidate !== undefined) { selectedCandidate = Number(candidate); render(); }
  if (element.id === 'lock') lockCandidate();
  const upgrade = element.dataset.upgrade;
  if (upgrade) setState(applyCommand(state, { type: 'draft', upgrade }));
});

const loop = (now: number) => {
  const elapsed = Math.min(1, (now - lastTick) / 1000);
  lastTick = now;
  if (state.status === 'playing') {
    const next = tickTurn(state, elapsed);
    if (next !== state) { state = next; renderControls(); document.querySelector<HTMLElement>('#status')!.textContent = renderStatus(); }
  }
  requestAnimationFrame(loop);
};

render();
requestAnimationFrame(loop);
