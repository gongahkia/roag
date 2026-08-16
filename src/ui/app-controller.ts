import { presentFeedback } from '../feedback';
import { applyCommand, botMove, createGame, defaultConfig, previewShot, tickTurn, UPGRADE_DESCRIPTIONS } from '../core/game';
import { isEditableElement, loadPreferences, savePreferences, setShortcut, shortcutForKey, type ShortcutId } from '../preferences';
import { type Ball, type BuildTool, type CourseTheme, type Emote, type EmoteEvent, type GameConfig, type GameState, type PowerUp, type ShotCommand } from '../core/types';
import { createRenderer } from './render';
import { type Callout, type Drawer, type LedgerEntry, type Overlay, type ViewModel, renderAppMarkup, renderCallouts, renderControlsMarkup, renderStatus } from './markup';

interface TimedCallout extends Callout {
  expiresAt: number;
}

interface ShotAnimation {
  playerId: string;
  frame: number;
}

interface LiveEmote extends EmoteEvent {
  expiresAt: number;
}

export const startApp = (app: HTMLElement) => {
  const query = new URLSearchParams(window.location.search);
  const requestedSeed = query.get('seed')?.trim();
  const captureMode = query.get('capture') === '1';
  let config: GameConfig = { ...defaultConfig(), ...(requestedSeed ? { seed: requestedSeed } : {}) };
  let state = createGame(config);
  let aim: ShotCommand = { angle: 0, power: 4 };
  let renderer: ReturnType<typeof createRenderer> | undefined;
  let lastTick = performance.now();
  let botTimeout: number | undefined;
  let preferences = loadPreferences();
  let overlay: Overlay;
  let drawer: Drawer = captureMode ? undefined : 'intel';
  let rebinding: ShortcutId | undefined;
  let lastFeedbackMessage: string | undefined;
  let ledger: LedgerEntry[] = [];
  let callouts: TimedCallout[] = [];
  let shotAnimation: ShotAnimation | undefined;
  let liveEmotes: LiveEmote[] = [];
  let seenEmoteIds = new Set<string>();

  const current = () => state.players[state.turn.playerIndex]!;
  const view = (): ViewModel => ({ state, config, preferences, overlay, drawer, rebinding, aim, shotInFlight: Boolean(shotAnimation), ledger, callouts });

  const applyPreferences = () => {
    document.documentElement.classList.toggle('reduced-motion', preferences.reducedMotion);
    document.documentElement.classList.toggle('high-contrast', preferences.highContrast);
  };

  const recordFeedback = (message: string) => {
    if (!message || message === lastFeedbackMessage) return;
    lastFeedbackMessage = message;
    const presentation = presentFeedback(message);
    const entry: LedgerEntry = { message, tone: presentation.tone };
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
      seed: app.querySelector<HTMLInputElement>('#seed')?.value.trim() || config.seed,
      humanCount: Number(app.querySelector<HTMLInputElement>('#humans')?.value || config.humanCount),
      botCount: Number(app.querySelector<HTMLInputElement>('#bots')?.value || config.botCount),
      timerSeconds: Number(app.querySelector<HTMLInputElement>('#timer')?.value || config.timerSeconds),
      botSkill: app.querySelector<HTMLSelectElement>('#skill')?.value === 'adaptive' ? 'adaptive' : Number(app.querySelector<HTMLSelectElement>('#skill')?.value || config.botSkill),
      collisions: app.querySelector<HTMLInputElement>('#collisions')?.checked ?? config.collisions,
      powerUps: app.querySelector<HTMLInputElement>('#powerups')?.checked ?? config.powerUps,
    };
    if (config.humanCount + config.botCount > 12 || config.botCount > 4 || config.humanCount < 1) return;
    drawer = captureMode ? undefined : 'intel';
    liveEmotes = [];
    seenEmoteIds = new Set();
    setState(createGame(config));
  };

  const drawBoard = (animationBalls?: readonly Ball[], drawAim: ShotCommand | null = aim) => {
    const players = animationBalls ? state.players.map((player, index) => ({ ...player, ball: animationBalls[index] ?? player.ball })) : state.players;
    renderer?.draw(state.course, players, state.coursePhase, drawAim ?? undefined, liveEmotes, state.config.powerUps, state.status === 'build');
  };

  const chooseAim = (event: PointerEvent) => {
    if (!renderer || (state.status !== 'playing' && state.status !== 'validate') || current().kind !== 'human') return;
    aim = renderer.aimFromPointer(event, state.course, current().ball);
    drawBoard();
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
    drawBoard();
    renderControls();
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
    const status = app.querySelector<HTMLElement>('#status');
    if (status) status.textContent = renderStatus(view());
    const animate = (now: number) => {
      if (!shotAnimation || state !== inFlight) return;
      const progress = Math.max(0, Math.min(1, (now - startedAt) / duration));
      const frame = Math.max(0, Math.min(frames.length - 1, Math.floor(progress * (frames.length - 1))));
      shotAnimation.frame = frame;
      drawBoard(frames[frame]!, null);
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

  const useHeldPowerUp = (powerUp: PowerUp) => {
    if (state.status !== 'playing' || current().kind !== 'human' || shotAnimation) return;
    const target = state.players.find((player) => player.id !== current().id && !player.ball.complete);
    const portalExitId = app.querySelector<HTMLSelectElement>('#portal-exit')?.value || undefined;
    setState(applyCommand(state, { type: 'use-power-up', powerUp, targetId: target?.id, portalExitId }));
  };

  const sendEmote = (emote: Emote) => {
    if (shotAnimation || current().kind !== 'human') return;
    setState(applyCommand(state, { type: 'emote', playerId: current().id, emote }));
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
    if (state.status !== 'playing' && state.status !== 'validate') return;
    botTimeout = window.setTimeout(() => {
      const decision = botMove(state);
      if (!decision) return;
      if (decision.secondWind) setState(applyCommand(state, { type: 'arm-second-wind' }));
      if (decision.powerUp) setState(applyCommand(state, { type: 'use-power-up', powerUp: decision.powerUp.type, targetId: decision.powerUp.targetId, portalExitId: decision.powerUp.portalExitId }));
      playShot((decision.secondWind || decision.powerUp ? botMove(state)?.shot : undefined) ?? decision.shot);
    }, preferences.reducedMotion ? 180 : 650);
  };

  const renderControls = () => {
    const control = app.querySelector<HTMLElement>('#controls');
    if (!control) return;
    control.innerHTML = renderControlsMarkup(view());
    app.querySelector<HTMLInputElement>('#power')?.addEventListener('input', (event) => {
      aim = { ...aim, power: Number((event.target as HTMLInputElement).value) };
      drawBoard();
      renderControls();
    });
    app.querySelector<HTMLButtonElement>('#shoot')?.addEventListener('click', shoot);
    app.querySelector<HTMLButtonElement>('#second-wind')?.addEventListener('click', () => setState(applyCommand(state, { type: 'arm-second-wind' })));
  };

  const render = () => {
    app.innerHTML = renderAppMarkup(view());
    app.querySelector<HTMLButtonElement>('#new-run')?.addEventListener('click', setupGame);
    const canvas = app.querySelector<HTMLCanvasElement>('#course')!;
    renderer?.dispose();
    renderer = createRenderer(canvas);
    drawBoard(undefined, state.status === 'build' ? null : aim);
    canvas.addEventListener('pointermove', chooseAim);
    canvas.addEventListener('pointerdown', state.status === 'build' ? placeBuild : chooseAim);
    renderControls();
  };

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
    const courseTheme = element.dataset.courseTheme as CourseTheme | undefined;
    if (courseTheme && state.status === 'build') { setState(applyCommand(state, { type: 'build-settings', terrain: { theme: courseTheme } })); return; }
    const direction = element.dataset.buildDirection;
    if (direction && state.status === 'build') {
      const [x, y] = direction.split(',').map(Number);
      setState(applyCommand(state, { type: 'build-settings', direction: { x, y } }));
      return;
    }
    if (element.id === 'auto-terrain' && state.status === 'build') { setState(applyCommand(state, { type: 'build-generate' })); return; }
    if (element.id === 'randomize-terrain' && state.status === 'build') { setState(applyCommand(state, { type: 'build-randomize' })); return; }
    if (element.id === 'validate' && state.status === 'build') { setState(applyCommand(state, { type: 'begin-validation' })); return; }
    const powerUp = element.dataset.usePowerup as PowerUp | undefined;
    if (powerUp) { useHeldPowerUp(powerUp); return; }
    const emote = element.dataset.emote as Emote | undefined;
    if (emote) { sendEmote(emote); return; }
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
    if (target.id === 'portal-pair') {
      setState(applyCommand(state, { type: 'build-settings', portalPairId: Number(target.value) }));
      return;
    }
    const terrain = target.id === 'terrain-density' ? { density: Number(target.value) }
      : target.id === 'terrain-elevation' ? { elevation: Number(target.value) }
        : target.id === 'terrain-hazards' ? { hazards: Number(target.value) }
          : target.id === 'terrain-chaos' ? { chaos: Number(target.value) }
            : target.id === 'route-length' ? { routeLength: Number(target.value) }
              : target.id === 'route-bendiness' ? { bendiness: Number(target.value) }
                : target.id === 'route-width' ? { laneWidth: Number(target.value) }
                  : target.id === 'route-branches' ? { branches: Number(target.value) } : undefined;
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
    if (command === 'usePowerUp' && current().inventory) useHeldPowerUp(current().inventory!);
  });

  const loop = (now: number) => {
    const elapsed = Math.min(1, (now - lastTick) / 1000);
    lastTick = now;
    const previousCalloutCount = callouts.length;
    callouts = callouts.filter((callout) => callout.expiresAt > now);
    if (callouts.length !== previousCalloutCount) {
      const calloutNode = app.querySelector<HTMLElement>('#callouts');
      if (calloutNode) calloutNode.innerHTML = renderCallouts(callouts);
    }
    const previousEmoteCount = liveEmotes.length;
    liveEmotes = liveEmotes.filter((emote) => emote.expiresAt > now);
    if (liveEmotes.length !== previousEmoteCount && !shotAnimation) drawBoard();
    if ((state.status === 'playing' || state.status === 'validate') && !shotAnimation) {
      const next = tickTurn(state, elapsed);
      if (next !== state) {
        state = next;
        recordStateFeedback(state);
        drawBoard();
        renderControls();
        const status = app.querySelector<HTMLElement>('#status');
        if (status) status.textContent = renderStatus(view());
        const calloutNode = app.querySelector<HTMLElement>('#callouts');
        if (calloutNode) calloutNode.innerHTML = renderCallouts(callouts);
        scheduleBot();
      }
    }
    requestAnimationFrame(loop);
  };

  applyPreferences();
  document.documentElement.classList.toggle('capture-mode', captureMode);
  recordStateFeedback(state);
  render();
  scheduleBot();
  requestAnimationFrame(loop);
};
