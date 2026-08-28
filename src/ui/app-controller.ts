import { presentFeedback } from '../feedback';
import { chooseBotDieAction } from '../core/bots';
import { COURSE_TRANSITION_DURATION_MS, type CourseExpansion } from '../core/campaign';
import { applyCommand, botMove, createGame, defaultConfig, previewShot, tickTurn } from '../core/game';
import { expansionForTransition } from '../core/game-state';
import { tileAt } from '../core/physics';
import { canPlaceGadget } from '../core/powerups';
import { chooseBotShopOffer } from '../core/shop';
import type { Ball, ChronoCard, Emote, EmoteEvent, GadgetKind, GameCommand, GameConfig, GameState, Point, PowerUp, ShotCommand } from '../core/types';
import { OnlineClient } from '../net/online-client';
import type { ClientMessage, LobbyConfig, RoomSnapshot } from '../net/protocol';
import { isEditableElement, loadPreferences, savePreferences, setShortcut, shortcutForKey, type ShortcutId } from '../preferences';
import { lobbyConfigFromGame, renderHomeMarkup, renderLobbyMarkup, renderMatchLaunchMarkup, type HomeMode, type HomePanel } from './home-markup';
import { type Callout, type Drawer, type LedgerEntry, type Overlay, type ViewModel, renderAppMarkup, renderCallouts, renderControlsMarkup, renderStatus } from './markup';
import type { CourseDieScene } from './course-die';
import { createRenderer } from './render';

interface TimedCallout extends Callout { expiresAt: number; }
interface ShotAnimation { playerId: string; frame: number; }
interface LiveEmote extends EmoteEvent { expiresAt: number; }
interface PlacementState { kind: GadgetKind; cardId?: string; point?: Point; valid: boolean; confirmed: boolean; ownerId: string; }
type Screen = 'home' | 'lobby' | 'launching' | 'game';
type BotScheduleSnapshot = Pick<GameState, 'status'> & { turn: Pick<GameState['turn'], 'playerIndex'> };

export const shouldScheduleBotAfterTick = (previous: BotScheduleSnapshot, next: BotScheduleSnapshot) => previous.status !== next.status || previous.turn.playerIndex !== next.turn.playerIndex;

const MIN_POWER = 1;
const MAX_POWER = 8;
const POWER_STEP = .5;
export const PULL_MAX_DISTANCE = 170;
const QUICK_START_LAUNCH_DURATION_MS = 1_250;
export const aimFromPull = (aim: ShotCommand, pullX: number, pullY: number): ShotCommand => {
  const distance = Math.hypot(pullX, pullY);
  if (!Number.isFinite(distance) || distance < .5) return aim;
  const boundedDistance = Math.min(PULL_MAX_DISTANCE, distance);
  return {
    ...aim,
    angle: Math.atan2(-pullY, -pullX),
    power: Number((MIN_POWER + boundedDistance / PULL_MAX_DISTANCE * (MAX_POWER - MIN_POWER)).toFixed(1)),
  };
};
export const shotKindForPointerButton = (button: number, forcedChip = false): ShotCommand['kind'] => forcedChip || button === 2 ? 'chip' : 'putt';
const clamped = (value: number) => Math.max(0, Math.min(1, value));

const readText = (app: HTMLElement, id: string, fallback: string) => app.querySelector<HTMLInputElement>(`#${id}`)?.value.trim() || fallback;
const readNumber = (app: HTMLElement, id: string, fallback: number) => {
  const value = Number(app.querySelector<HTMLInputElement | HTMLSelectElement>(`#${id}`)?.value);
  return Number.isFinite(value) ? value : fallback;
};
const validCourseDimensions = (width: number, height: number) => Number.isSafeInteger(width) && width >= 14 && Number.isSafeInteger(height) && height >= 10 && Number.isSafeInteger(width * height);
const browserServerUrl = () => {
  const secure = window.location.protocol === 'https:';
  const port = window.location.port === '5173' || !window.location.port ? '8787' : window.location.port;
  return `${secure ? 'wss' : 'ws'}://${window.location.hostname}:${port}`;
};
const validServerUrl = (value: string) => {
  try {
    const parsed = new URL(value);
    return parsed.protocol === 'ws:' || parsed.protocol === 'wss:' ? parsed.toString().replace(/\/$/, '') : undefined;
  } catch { return undefined; }
};
const tokenKey = (roomCode: string) => `golf-with-your-enemies-room-token:${roomCode.toUpperCase()}`;
const getStoredToken = (roomCode: string) => { try { return sessionStorage.getItem(tokenKey(roomCode)); } catch { return null; } };
const storeToken = (roomCode: string, token: string) => { try { sessionStorage.setItem(tokenKey(roomCode), token); } catch { } };

export const startApp = (app: HTMLElement) => {
  const query = new URLSearchParams(window.location.search);
  const requestedSeed = query.get('seed')?.trim();
  const captureMode = query.get('capture') === '1';
  let config: GameConfig = { ...defaultConfig(), ...(requestedSeed ? { seed: requestedSeed } : {}) };
  let state = createGame(config);
  let aim: ShotCommand = { angle: 0, power: 4, kind: 'putt' };
  let renderer: ReturnType<typeof createRenderer> | undefined;
  let dieScene: CourseDieScene | undefined;
  let dieSceneVersion = 0;
  let lastTick = performance.now();
  let lastOnlineHazardSyncAt = performance.now();
  let botTimeout: number | undefined;
  let preferences = loadPreferences();
  let overlay: Overlay;
  let drawer: Drawer;
  let rebinding: ShortcutId | undefined;
  let lastFeedbackMessage: string | undefined;
  let ledger: LedgerEntry[] = [];
  let callouts: TimedCallout[] = [];
  let shotAnimation: ShotAnimation | undefined;
  let terrainCue: string | undefined;
  let transitionFrame: number | undefined;
  let transitionProgress = 0;
  let transitionExpansion: CourseExpansion | undefined;
  let campaignOverviewFrame: number | undefined;
  let campaignOverviewProgress = 0;
  let liveEmotes: LiveEmote[] = [];
  let seenEmoteIds = new Set<string>();
  let screen: Screen = captureMode ? 'game' : 'home';
  let homePanel: HomePanel = 'play';
  let homeMode: HomeMode = 'modes';
  let playerName = 'golfer-1';
  let requestedRoomCode = '';
  let serverUrl = preferences.onlineServerUrl || browserServerUrl();
  let notice: string | undefined;
  let onlineClient: OnlineClient | undefined;
  let room: RoomSnapshot | undefined;
  let onlinePlayerId: string | undefined;
  let pendingReconnectToken: string | undefined;
  let onlineConnected = false;
  let controllerName: string | undefined;
  let localMultiplayer = false;
  let gamepadButtons: boolean[] = [];
  let placement: PlacementState | undefined;
  let audioContext: AudioContext | undefined;
  let quickStartTimeout: number | undefined;
  let launchFrame: number | undefined;
  let launch = { quickStart: false, title: 'building the opening hole', detail: 'Setting up players, course rules, and the opening tee.' };

  const online = () => Boolean(onlineClient && room?.phase === 'game');
  const hazardElapsedForDraw = () => {
    if (!online() || state.status !== 'playing' || state.paused) return state.hazardElapsedMs;
    return state.hazardElapsedMs + Math.max(0, performance.now() - lastOnlineHazardSyncAt);
  };
  const current = () => state.players[state.turn.playerIndex]!;
  const canControlCurrent = () => !online() || current().id === onlinePlayerId;
  const view = (): ViewModel => ({
    state,
    config,
    preferences,
    overlay,
    drawer,
    rebinding,
    aim,
    placement,
    shotInFlight: Boolean(shotAnimation),
    ledger,
    callouts,
    multiplayer: { online: online(), connected: onlineConnected, roomCode: room?.code, playerId: onlinePlayerId, host: room?.hostId === onlinePlayerId, controllerName, localMultiplayer },
  });
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
    if (presentation.major) callouts = [{ ...entry, expiresAt: performance.now() + (preferences.reducedMotion ? 900 : 1_800) }, ...callouts].slice(0, 2);
  };
  const recordStateFeedback = (next: GameState) => recordFeedback(next.messages[0] ?? '');
  const syncEmotes = (next: GameState) => {
    for (const emote of next.emotes) {
      if (seenEmoteIds.has(emote.id)) continue;
      seenEmoteIds.add(emote.id);
      liveEmotes = [{ ...emote, expiresAt: performance.now() + (preferences.reducedMotion ? 1_100 : 2_700) }, ...liveEmotes].slice(0, 8);
    }
  };
  const vibrate = () => {
    if (!preferences.controllerVibration) return;
    const pad = navigator.getGamepads?.().find((candidate) => candidate?.connected);
    const actuator = (pad as (Gamepad & { vibrationActuator?: { playEffect(type: 'dual-rumble', options: { duration: number; strongMagnitude: number; weakMagnitude: number }): Promise<unknown> } }) | undefined)?.vibrationActuator;
    void actuator?.playEffect('dual-rumble', { duration: 90, strongMagnitude: .25, weakMagnitude: .4 }).catch(() => undefined);
  };
  const playEffect = (frequency: number, duration = .07) => {
    const volume = preferences.masterVolume * preferences.effectsVolume;
    if (!volume) return;
    const AudioContextConstructor = window.AudioContext ?? (window as typeof window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AudioContextConstructor) return;
    audioContext ??= new AudioContextConstructor();
    if (audioContext.state === 'suspended') void audioContext.resume().catch(() => undefined);
    const oscillator = audioContext.createOscillator();
    const gain = audioContext.createGain();
    oscillator.frequency.value = frequency;
    oscillator.type = 'square';
    gain.gain.setValueAtTime(Math.min(.08, volume * .08), audioContext.currentTime);
    gain.gain.exponentialRampToValueAtTime(.001, audioContext.currentTime + duration);
    oscillator.connect(gain).connect(audioContext.destination);
    oscillator.start();
    oscillator.stop(audioContext.currentTime + duration);
  };
  const playTerrainCue = (ball: Ball) => {
    const tile = tileAt(state.course, ball.x, ball.y);
    const gust = (state.course.features ?? []).some((feature) => feature.kind === 'gust'
      && Math.hypot(ball.x - feature.point.x - .5, ball.y - feature.point.y - .5) <= feature.radius);
    const cue = tile?.surface === 'spring' ? 'spring' : tile?.surface === 'cushion' ? 'cushion' : gust ? 'gust' : undefined;
    if (!cue || cue === terrainCue) return;
    terrainCue = cue;
    playEffect(cue === 'spring' ? 780 : cue === 'gust' ? 460 : 180, cue === 'spring' ? .1 : .055);
  };

  const playersInExpansion = (expansion: CourseExpansion) => state.players.map((player) => ({
    ...player,
    ball: { ...player.ball, x: player.ball.x + expansion.offset.x, y: player.ball.y + expansion.offset.y },
  }));
  const drawCourseTransition = () => {
    const expansion = transitionExpansion;
    if (!expansion || !state.transition) { renderer?.draw(state.course, state.players, state.hazardElapsedMs, undefined, [], false, state.holeRules.hazardPhaseCount, undefined, []); return; }
    const players = playersInExpansion(expansion);
    renderer?.drawConstruction({
      previous: expansion.previous,
      course: expansion.course,
      players,
      hazardElapsedMs: state.hazardElapsedMs,
      phaseCount: state.transition.next.recipe.rules.hazardPhaseCount,
      progress: transitionProgress,
      excavated: expansion.excavated,
      added: expansion.added,
      focus: players[0]?.ball,
    });
  };
  const drawCampaignOverview = () => renderer?.drawOverview(state.course, campaignOverviewProgress, state.players[0]?.ball);
  const drawBoard = (animationBalls?: readonly Ball[], drawAim: ShotCommand | null = aim) => {
    const players = animationBalls ? state.players.map((player, index) => ({ ...player, ball: animationBalls[index] ?? player.ball })) : state.players;
    if (state.status === 'transitioning' && state.transition) { drawCourseTransition(); return; }
    if (state.status === 'finished') { drawCampaignOverview(); return; }
    const effectiveAim = drawAim && current().forcedChip ? { ...drawAim, kind: 'chip' as const } : drawAim;
    renderer?.draw(state.course, players, hazardElapsedForDraw(), placement ? undefined : effectiveAim ?? undefined, liveEmotes, state.holeRules.powerUps, state.holeRules.hazardPhaseCount, undefined, state.gadgets ?? [], placement, players[state.turn.playerIndex]?.ball);
  };
  const startTransition = (completeLocally: boolean) => {
    if (state.status !== 'transitioning') return;
    if (transitionFrame !== undefined) window.cancelAnimationFrame(transitionFrame);
    const duration = preferences.reducedMotion ? 120 : COURSE_TRANSITION_DURATION_MS;
    let startedAt = performance.now() - transitionProgress * duration;
    let previousFrameAt = performance.now();
    const animate = (now: number) => {
      if (state.status !== 'transitioning') return;
      if (state.paused) {
        startedAt += now - previousFrameAt;
        previousFrameAt = now;
        transitionFrame = requestAnimationFrame(animate);
        return;
      }
      previousFrameAt = now;
      transitionProgress = Math.max(0, Math.min(1, (now - startedAt) / duration));
      drawBoard(undefined, null);
      if (transitionProgress < 1) { transitionFrame = requestAnimationFrame(animate); return; }
      transitionFrame = undefined;
      if (completeLocally) setState(applyCommand(state, { type: 'complete-transition' }));
    };
    transitionFrame = requestAnimationFrame(animate);
  };
  const startCampaignOverview = () => {
    if (state.status !== 'finished') return;
    if (campaignOverviewFrame !== undefined) window.cancelAnimationFrame(campaignOverviewFrame);
    const duration = preferences.reducedMotion ? 120 : 1_850;
    const startedAt = performance.now() - campaignOverviewProgress * duration;
    const animate = (now: number) => {
      if (state.status !== 'finished') return;
      campaignOverviewProgress = clamped((now - startedAt) / duration);
      drawBoard(undefined, null);
      if (campaignOverviewProgress < 1) { campaignOverviewFrame = requestAnimationFrame(animate); return; }
      campaignOverviewFrame = undefined;
    };
    campaignOverviewFrame = requestAnimationFrame(animate);
  };
  const setState = (next: GameState) => {
    const enteringTransition = state.status !== 'transitioning' && next.status === 'transitioning';
    const enteringFinished = state.status !== 'finished' && next.status === 'finished';
    state = next;
    if (placement && (state.status !== 'playing' || !state.players.find((player) => player.id === placement!.ownerId && (player.inventory === placement!.kind || player.spareInventory === placement!.kind)))) placement = undefined;
    if (enteringTransition) {
      transitionProgress = 0;
      transitionExpansion = expansionForTransition(state);
    }
    if (state.status !== 'transitioning' && transitionFrame !== undefined) {
      window.cancelAnimationFrame(transitionFrame);
      transitionFrame = undefined;
      transitionExpansion = undefined;
    }
    if (enteringFinished) campaignOverviewProgress = 0;
    if (state.status !== 'finished' && campaignOverviewFrame !== undefined) {
      window.cancelAnimationFrame(campaignOverviewFrame);
      campaignOverviewFrame = undefined;
      campaignOverviewProgress = 0;
    }
    recordStateFeedback(state);
    syncEmotes(state);
    render();
    if (!online()) {
      scheduleBot();
      if (state.status === 'transitioning') startTransition(true);
    } else if (enteringTransition) startTransition(false);
    if (enteringFinished) startCampaignOverview();
  };
  const receiveGame = (next: GameState) => {
    lastOnlineHazardSyncAt = performance.now();
    setState(next);
  };
  const autoResolveCaptureDie = (source: GameState) => {
    let resolved = source;
    while (resolved.status === 'rolling') {
      if (!resolved.die?.roll) resolved = resolved.players.reduce((next, player) => applyCommand(next, { type: 'ready-die-roll', playerId: player.id }), resolved);
      resolved = tickTurn(resolved, 2);
    }
    return resolved;
  };
  if (captureMode) state = autoResolveCaptureDie(state);

  const readConfig = (prefix: 'local' | 'local-multiplayer' | 'online'): LobbyConfig => {
    const fallback = lobbyConfigFromGame(config);
    const rawSkill = app.querySelector<HTMLSelectElement>(`#${prefix}-skill`)?.value;
    return {
      seed: readText(app, `${prefix}-seed`, fallback.seed),
      holeCount: readNumber(app, `${prefix}-holes`, fallback.holeCount),
      maxHumans: readNumber(app, `${prefix}-seats`, fallback.maxHumans),
      courseWidth: readNumber(app, `${prefix}-course-width`, fallback.courseWidth),
      courseHeight: readNumber(app, `${prefix}-course-height`, fallback.courseHeight),
      botCount: readNumber(app, `${prefix}-bots`, fallback.botCount),
      botSkill: rawSkill === 'adaptive' ? 'adaptive' : readNumber(app, `${prefix}-skill`, typeof fallback.botSkill === 'number' ? fallback.botSkill : 5),
      skipDieBets: false,
    };
  };
  const launchLocalMatch = (quickStart: boolean) => {
    launch = quickStart
      ? { quickStart: true, title: 'rolling up the course', detail: 'Preparing seeded automatic die results for the round.' }
      : { quickStart: false, title: 'building the opening hole', detail: 'Setting up players, course rules, and the opening tee.' };
    screen = 'launching';
    render();
    window.clearTimeout(quickStartTimeout);
    if (launchFrame !== undefined) window.cancelAnimationFrame(launchFrame);
    const begin = () => {
      quickStartTimeout = undefined;
      if (screen !== 'launching') return;
      screen = 'game';
      setState(captureMode ? autoResolveCaptureDie(createGame(config)) : createGame(config));
    };
    launchFrame = window.requestAnimationFrame(() => {
      launchFrame = window.requestAnimationFrame(() => {
        launchFrame = undefined;
        const launchDelay = quickStart ? QUICK_START_LAUNCH_DURATION_MS : 320;
        quickStartTimeout = window.setTimeout(begin, preferences.reducedMotion ? 80 : launchDelay);
      });
    });
  };
  const startLocal = (prefix: 'local' | 'local-multiplayer', skipDieBets = false) => {
    const selected = { ...readConfig(prefix), skipDieBets };
    const minimumHumans = prefix === 'local-multiplayer' ? 2 : 1;
    if (selected.maxHumans + selected.botCount > 12 || selected.maxHumans < minimumHumans || selected.botCount > 4) { notice = minimumHumans === 2 ? 'choose between two and twelve total players' : 'choose between one and twelve total players'; render(); return; }
    if (!validCourseDimensions(selected.courseWidth, selected.courseHeight)) { notice = 'choose whole-number level dimensions of at least 14×10 tiles'; render(); return; }
    onlineClient?.disconnect();
    onlineClient = undefined;
    room = undefined;
    onlinePlayerId = undefined;
    pendingReconnectToken = undefined;
    onlineConnected = false;
    localMultiplayer = prefix === 'local-multiplayer';
    config = { seed: selected.seed, holeCount: selected.holeCount, humanCount: selected.maxHumans, botCount: selected.botCount, botSkill: selected.botSkill, courseWidth: selected.courseWidth, courseHeight: selected.courseHeight, skipDieBets: selected.skipDieBets };
    drawer = undefined;
    overlay = undefined;
    liveEmotes = [];
    seenEmoteIds = new Set();
    notice = undefined;
    launchLocalMatch(skipDieBets);
  };
  const setupGame = () => {
    const seed = readText(app, 'seed', config.seed);
    const humanCount = readNumber(app, 'humans', config.humanCount);
    const botCount = readNumber(app, 'bots', config.botCount);
    const rawSkill = app.querySelector<HTMLSelectElement>('#skill')?.value;
    config = { ...config, seed, humanCount, botCount, botSkill: rawSkill === 'adaptive' ? 'adaptive' : readNumber(app, 'skill', typeof config.botSkill === 'number' ? config.botSkill : 5) };
    if (config.humanCount + config.botCount > 12 || config.botCount > 4 || config.humanCount < 1) return;
    liveEmotes = [];
    seenEmoteIds = new Set();
    setState(captureMode ? autoResolveCaptureDie(createGame(config)) : createGame(config));
  };
  const dispatch = (command: GameCommand) => {
    if (online()) { onlineClient?.send({ type: 'command', command }); return; }
    setState(applyCommand(state, command));
  };
  const connectOnline = (message: ClientMessage, codeForToken?: string) => {
    const url = validServerUrl(serverUrl);
    if (!url) { notice = 'enter a ws:// or wss:// game server address'; render(); return; }
    preferences = { ...preferences, onlineServerUrl: url };
    savePreferences(preferences);
    notice = 'connecting to game server…';
    onlineClient?.disconnect();
    room = undefined;
    onlinePlayerId = undefined;
    pendingReconnectToken = undefined;
    onlineConnected = false;
    localMultiplayer = false;
    const client = new OnlineClient({
      onRoom(nextRoom) {
        if (onlineClient !== client) return;
        room = nextRoom;
        if (pendingReconnectToken) storeToken(nextRoom.code, pendingReconnectToken);
        if (nextRoom.phase === 'game' && nextRoom.game) {
          config = nextRoom.game.config;
          screen = 'game';
          receiveGame(nextRoom.game);
        } else {
          screen = 'lobby';
          render();
        }
      },
      onJoined(playerId, reconnectToken) {
        if (onlineClient !== client) return;
        onlinePlayerId = playerId;
        pendingReconnectToken = reconnectToken;
        if (codeForToken) storeToken(codeForToken, reconnectToken);
        render();
      },
      onError(messageText) {
        if (onlineClient !== client) return;
        notice = messageText;
        if (screen === 'launching' && room) screen = 'lobby';
        render();
      },
      onConnection(connected) {
        if (onlineClient !== client) return;
        onlineConnected = connected;
        notice = connected ? undefined : 'connection lost — rejoin with your room code to restore your seat';
        render();
      },
    });
    onlineClient = client;
    render();
    client.connect(url, () => client.send(message));
  };
  const startOnlineMatch = () => {
    if (!onlineClient || !room) return;
    launch = { quickStart: room.config.skipDieBets, title: 'starting shared match', detail: 'The host server is assembling the opening course for the table.' };
    screen = 'launching';
    render();
    onlineClient.send({ type: 'start-room' });
  };
  const createOnlineRoom = (skipDieBets = false) => {
    const selected = { ...readConfig('online'), skipDieBets };
    playerName = readText(app, 'player-name', playerName);
    serverUrl = readText(app, 'server-url', serverUrl);
    if (selected.maxHumans + selected.botCount > 12) { notice = 'choose between one and twelve total players'; render(); return; }
    if (!validCourseDimensions(selected.courseWidth, selected.courseHeight)) { notice = 'choose whole-number level dimensions of at least 14×10 tiles'; render(); return; }
    connectOnline({ type: 'create-room', name: playerName, config: selected });
  };
  const joinOnlineRoom = () => {
    playerName = readText(app, 'join-player-name', playerName);
    serverUrl = readText(app, 'join-server-url', serverUrl);
    requestedRoomCode = readText(app, 'room-code', requestedRoomCode).toUpperCase();
    if (!/^[A-F0-9]{6}$/.test(requestedRoomCode)) { notice = 'enter the six-character room code'; render(); return; }
    connectOnline({ type: 'join-room', code: requestedRoomCode, name: playerName, reconnectToken: getStoredToken(requestedRoomCode) ?? undefined }, requestedRoomCode);
  };

  const updatePlacement = (event: PointerEvent) => {
    if (!renderer || !placement || state.status !== 'playing' || state.paused || !canControlCurrent()) return;
    const point = renderer.tileFromPointer(event, state.course);
    placement = { ...placement, point, valid: canPlaceGadget(state, placement.ownerId, point), confirmed: false };
    drawBoard();
    renderControls();
  };
  const updateAimHud = () => {
    const strength = Math.round(Math.max(0, Math.min(1, (aim.power - MIN_POWER) / (MAX_POWER - MIN_POWER))) * 100);
    const shotKind = current().forcedChip ? 'chip' : aim.kind ?? 'putt';
    const meter = app.querySelector<HTMLElement>('#hud-strength-meter');
    if (meter) meter.style.setProperty('--strength', `${strength}%`);
    const value = app.querySelector<HTMLElement>('#hud-strength-value');
    if (value) value.textContent = aim.power.toFixed(1);
    const label = app.querySelector<HTMLElement>('#hud-shot-label');
    if (label) label.textContent = `${shotKind.toUpperCase()} STRENGTH`;
  };
  const updateLiveMatchHud = () => {
    const timer = app.querySelector<HTMLElement>('#hud-turn-timer');
    if (timer) timer.innerHTML = `${state.turn.secondsLeft.toFixed(0)}<small>s</small>`;
  };
  const updatePhaseTimer = () => {
    if (state.status === 'rolling' && state.die) {
      const prompt = app.querySelector<HTMLElement>('#die-prompt');
      if (prompt) prompt.textContent = state.die.roll ? `rolling… ${state.die.roll.secondsLeft.toFixed(1)}s` : 'place a wager or ready the die';
      const timer = app.querySelector<HTMLElement>('#die-timer');
      if (timer) timer.textContent = state.die.roll ? 'the selected face becomes this hole' : `${state.players.filter((player) => state.die!.wagers[player.id]?.ready).length}/${state.players.length} ready · timer ${state.die.secondsLeft.toFixed(0)}s`;
    }
    if (state.status === 'shopping' && state.shop) app.querySelector<HTMLElement>('#merchant-timer')?.replaceChildren(`${state.shop.secondsLeft.toFixed(0)} seconds`);
  };
  const adjustPower = (amount: number) => {
    aim = { ...aim, power: Math.max(MIN_POWER, Math.min(MAX_POWER, Number((aim.power + amount).toFixed(1)))) };
    if (state.status !== 'playing' || state.paused) return;
    drawBoard();
    updateAimHud();
    renderControls();
  };
  const selectShotKind = (kind: ShotCommand['kind']) => {
    aim = { ...aim, kind: kind ?? 'putt' };
    if (state.status !== 'playing' || state.paused) return;
    drawBoard();
    updateAimHud();
    renderControls();
  };
  const playShot = (shot: ShotCommand) => {
    if (state.status !== 'playing' || state.paused || shotAnimation || !canControlCurrent()) return;
    if (online()) { vibrate(); playEffect(240, .09); dispatch({ type: 'shoot', shot }); return; }
    const player = current();
    const frames = previewShot(state, shot);
    if (!frames?.length) { vibrate(); playEffect(240, .09); dispatch({ type: 'shoot', shot }); return; }
    const source = state;
    const inFlight = { ...source, turn: { ...source.turn, shotInFlight: true } };
    const duration = Math.min(2_200, Math.max(360, frames.length * 11));
    const startedAt = performance.now();
    shotAnimation = { playerId: player.id, frame: 0 };
    terrainCue = undefined;
    state = inFlight;
    vibrate();
    playEffect(240, .09);
    renderControls();
    const status = app.querySelector<HTMLElement>('#status');
    if (status) status.textContent = renderStatus(view());
    const animate = (now: number) => {
      if (!shotAnimation || shotAnimation.playerId !== player.id) return;
      const progress = Math.max(0, Math.min(1, (now - startedAt) / duration));
      const frame = Math.max(0, Math.min(frames.length - 1, Math.floor(progress * (frames.length - 1))));
      shotAnimation.frame = frame;
      playTerrainCue(frames[frame]![source.turn.playerIndex]!);
      drawBoard(frames[frame]!, null);
      if (progress < 1) { requestAnimationFrame(animate); return; }
      shotAnimation = undefined;
      const resolved = applyCommand(source, { type: 'shoot', shot });
      resolved.hazardElapsedMs = state.hazardElapsedMs;
      setState(resolved);
    };
    requestAnimationFrame(animate);
  };
  const shoot = () => { if (state.status === 'playing' && current().kind === 'human' && canControlCurrent()) playShot(aim); };
  const bindCourseInput = (canvas: HTMLCanvasElement) => {
    let pull: { pointerId: number; originX: number; originY: number; distance: number } | undefined;
    const resetPull = () => {
      canvas.classList.remove('pulling', 'chip-pull');
    };
    const updatePull = (event: PointerEvent) => {
      if (!pull || pull.pointerId !== event.pointerId) return;
      const rawX = event.clientX - pull.originX;
      const rawY = event.clientY - pull.originY;
      const distance = Math.hypot(rawX, rawY);
      const scale = distance > PULL_MAX_DISTANCE ? PULL_MAX_DISTANCE / distance : 1;
      const pullX = rawX * scale;
      const pullY = rawY * scale;
      pull.distance = Math.hypot(pullX, pullY);
      aim = aimFromPull(aim, pullX, pullY);
      drawBoard();
      updateAimHud();
    };
    const releasePull = (event: PointerEvent) => {
      if (!pull || pull.pointerId !== event.pointerId) return;
      const distance = pull.distance;
      pull = undefined;
      if (canvas.hasPointerCapture(event.pointerId)) canvas.releasePointerCapture(event.pointerId);
      resetPull();
      if (distance >= 12) shoot();
    };
    canvas.addEventListener('contextmenu', (event) => event.preventDefault());
    canvas.addEventListener('pointerdown', (event) => {
      if (placement) { selectPlacement(event); return; }
      if (event.button !== 0 && event.button !== 2) return;
      if (state.status !== 'playing' || state.paused || shotAnimation || current().kind !== 'human' || !canControlCurrent()) return;
      event.preventDefault();
      aim = { ...aim, kind: shotKindForPointerButton(event.button, current().forcedChip) };
      pull = { pointerId: event.pointerId, originX: event.clientX, originY: event.clientY, distance: 0 };
      canvas.setPointerCapture(event.pointerId);
      canvas.classList.add('pulling');
      canvas.classList.toggle('chip-pull', aim.kind === 'chip');
      drawBoard();
      updateAimHud();
    });
    canvas.addEventListener('pointermove', (event) => {
      if (pull) updatePull(event);
      else updatePlacement(event);
    });
    canvas.addEventListener('pointerup', releasePull);
    canvas.addEventListener('pointercancel', (event) => {
      if (!pull || pull.pointerId !== event.pointerId) return;
      pull = undefined;
      resetPull();
    });
  };
  const useHeldPowerUp = (powerUp: PowerUp | ChronoCard, cardId?: string) => {
    if (state.status !== 'playing' || state.paused || current().kind !== 'human' || shotAnimation || !canControlCurrent()) return;
    const gadgets = new Set<PowerUp>(['popper pad', 'snare patch', 'blast mine', 'slick patch', 'sky spring', 'gravity well', 'mirror plate', 'toll booth', 'control inverter', 'portal gun']);
    if (gadgets.has(powerUp as PowerUp)) {
      placement = { kind: powerUp as GadgetKind, cardId, ownerId: current().id, valid: false, confirmed: false };
      playEffect(530, .08);
      drawBoard();
      renderControls();
      return;
    }
    const selectedTarget = app.querySelector<HTMLSelectElement>('#powerup-target')?.value;
    const target = state.players.find((player) => player.id === selectedTarget && !player.ball.complete)
      ?? current();
    const portalExitId = app.querySelector<HTMLSelectElement>('#portal-exit')?.value || undefined;
    playEffect(530, .08);
    dispatch({ type: 'use-power-up', powerUp, cardId, targetId: target.id, portalExitId });
  };
  const confirmPlacement = () => {
    if (!placement || !placement.point || !placement.valid || state.status !== 'playing' || state.paused || current().id !== placement.ownerId || !canControlCurrent()) return;
    const pending = placement;
    placement = undefined;
    playEffect(530, .08);
    dispatch({ type: 'use-power-up', powerUp: pending.kind, cardId: pending.cardId, placement: pending.point });
  };
  const selectPlacement = (event: PointerEvent) => {
    if (!placement) return;
    const point = renderer?.tileFromPointer(event, state.course);
    const valid = canPlaceGadget(state, placement.ownerId, point);
    if (placement.confirmed && placement.point?.x === point?.x && placement.point?.y === point?.y && placement.valid && valid) { confirmPlacement(); return; }
    placement = { ...placement, point, valid, confirmed: true };
    drawBoard();
    renderControls();
  };
  const sendEmote = (emote: Emote) => {
    if (shotAnimation || current().kind !== 'human' || !canControlCurrent()) return;
    dispatch({ type: 'emote', playerId: onlinePlayerId ?? current().id, emote });
  };
  const diePlayer = () => online()
    ? state.players.find((player) => player.id === onlinePlayerId && !state.die?.wagers[player.id]?.ready)
    : state.players.find((player) => player.kind === 'human' && !state.die?.wagers[player.id]?.ready);
  const addDieSide = () => {
    const player = diePlayer();
    if (player) { playEffect(660, .06); dispatch({ type: 'add-die-side', playerId: player.id }); }
  };
  const augmentDieFace = (faceId: string) => {
    const player = diePlayer();
    if (player) { playEffect(720, .06); dispatch({ type: 'augment-die-face', playerId: player.id, faceId }); }
  };
  const readyDieRoll = () => {
    const player = diePlayer();
    if (player) { playEffect(490, .08); dispatch({ type: 'ready-die-roll', playerId: player.id }); }
  };
  const togglePause = () => {
    if (state.status === 'finished' || (online() && room?.hostId !== onlinePlayerId)) return;
    playEffect(state.paused ? 570 : 330, .1);
    dispatch({ type: 'set-paused', paused: !state.paused });
  };
  const scheduleBot = () => {
    window.clearTimeout(botTimeout);
    if (online() || state.paused) return;
    if (state.status === 'rolling' && state.die && !state.die.roll) {
      const bot = state.players.find((player) => player.kind === 'bot' && !state.die!.wagers[player.id]?.ready);
      if (!bot) return;
      botTimeout = window.setTimeout(() => {
        if (state.status !== 'rolling' || !state.die || state.die.roll || state.die.wagers[bot.id]?.ready) return;
        setState(applyCommand(state, chooseBotDieAction(state.config.seed, state.hole, bot, state.die)));
      }, preferences.reducedMotion ? 100 : 520);
      return;
    }
    if (state.status === 'shopping' && state.shop) {
      if (!state.shop.rerollResolved) {
        const bot = state.players.find((player) => player.kind === 'bot' && state.shop!.rerollVotes[player.id] === undefined);
        if (!bot) return;
        botTimeout = window.setTimeout(() => setState(applyCommand(state, { type: 'shop-vote-reroll', playerId: bot.id, approve: false })), preferences.reducedMotion ? 100 : 420);
        return;
      }
      const shopper = state.players.find((player) => player.id === state.shop!.buyerOrder[state.shop!.buyerIndex]);
      if (!shopper || shopper.kind !== 'bot') return;
      botTimeout = window.setTimeout(() => {
        const offer = chooseBotShopOffer(state, shopper);
        setState(applyCommand(state, offer ? { type: 'shop-buy', playerId: shopper.id, offerId: offer.id } : { type: 'shop-skip', playerId: shopper.id }));
      }, preferences.reducedMotion ? 120 : 560);
      return;
    }
    if (state.status !== 'playing' || current().kind !== 'bot') return;
    botTimeout = window.setTimeout(() => {
      const decision = botMove(state);
      if (!decision) return;
      if (decision.secondWind) setState(applyCommand(state, { type: 'arm-second-wind' }));
      if (decision.powerUp) setState(applyCommand(state, { type: 'use-power-up', powerUp: decision.powerUp.type, cardId: decision.powerUp.cardId, targetId: decision.powerUp.targetId, portalExitId: decision.powerUp.portalExitId, placement: decision.powerUp.placement }));
      playShot((decision.secondWind || decision.powerUp ? botMove(state)?.shot : undefined) ?? decision.shot);
    }, preferences.reducedMotion ? 180 : 650);
  };
  const renderControls = () => {
    const control = app.querySelector<HTMLElement>('#controls');
    if (!control) return;
    control.innerHTML = renderControlsMarkup(view());
    app.querySelector<HTMLButtonElement>('#second-wind')?.addEventListener('click', () => dispatch({ type: 'arm-second-wind' }));
  };
  const render = () => {
    const sceneVersion = ++dieSceneVersion;
    renderer?.dispose();
    renderer = undefined;
    dieScene?.dispose();
    dieScene = undefined;
    if (screen === 'home') {
      app.innerHTML = renderHomeMarkup({ panel: homePanel, mode: homeMode, config: lobbyConfigFromGame(config), preferences, playerName, roomCode: requestedRoomCode, serverUrl, connected: onlineConnected, notice });
      return;
    }
    if (screen === 'lobby') {
      if (room) app.innerHTML = renderLobbyMarkup(room, onlinePlayerId, onlineConnected, notice);
      return;
    }
    if (screen === 'launching') {
      app.innerHTML = renderMatchLaunchMarkup(launch);
      return;
    }
    app.innerHTML = renderAppMarkup(view());
    app.querySelector<HTMLButtonElement>('#new-run')?.addEventListener('click', setupGame);
    const dieCanvas = app.querySelector<HTMLCanvasElement>('#course-die');
    const dieState = state.die;
    if (dieCanvas && dieState) {
      void import('./course-die').then(({ createCourseDieScene }) => {
        if (sceneVersion !== dieSceneVersion || !dieCanvas.isConnected) return;
        dieScene = createCourseDieScene(dieCanvas, dieState.faces, Boolean(dieState.roll), dieState.roll?.faceId, dieState.roll?.secondsLeft, preferences.reducedMotion);
      });
    }
    const canvas = app.querySelector<HTMLCanvasElement>('#course');
    if (!canvas) return;
    renderer = createRenderer(canvas);
    drawBoard(undefined, state.status === 'playing' ? aim : null);
    bindCourseInput(canvas);
    renderControls();
  };
  const updatePreferences = (partial: Partial<typeof preferences>) => {
    preferences = { ...preferences, ...partial };
    savePreferences(preferences);
    applyPreferences();
    render();
  };
  const pollController = () => {
    if (screen !== 'game') return;
    const pad = navigator.getGamepads?.().find((candidate) => candidate?.connected);
    if (!pad) { gamepadButtons = []; return; }
    if (!controllerName) { controllerName = pad.id || 'controller'; render(); }
    const pressed = (index: number) => Boolean(pad.buttons[index]?.pressed);
    const edge = (index: number) => pressed(index) && !gamepadButtons[index];
    gamepadButtons = pad.buttons.map((button) => button.pressed);
    if (edge(9)) togglePause();
    if (overlay || state.paused || shotAnimation || state.status !== 'playing' || current().kind !== 'human' || !canControlCurrent()) return;
    if (placement) {
      const origin = placement.point ?? { x: Math.floor(current().ball.x), y: Math.floor(current().ball.y) };
      const left = edge(14);
      const right = edge(15);
      const up = edge(12);
      const down = edge(13);
      if (left || right || up || down) {
        const point = { x: Math.max(0, Math.min(state.course.width - 1, origin.x + (right ? 1 : left ? -1 : 0))), y: Math.max(0, Math.min(state.course.height - 1, origin.y + (down ? 1 : up ? -1 : 0))) };
        placement = { ...placement, point, valid: canPlaceGadget(state, placement.ownerId, point), confirmed: false };
        drawBoard();
        renderControls();
      }
      if (edge(0) && placement.point) {
        if (placement.confirmed) confirmPlacement();
        else { placement = { ...placement, confirmed: true }; drawBoard(); renderControls(); }
      }
      if (edge(1)) { placement = undefined; drawBoard(); renderControls(); }
      return;
    }
    const x = pad.axes[0] ?? 0;
    const y = pad.axes[1] ?? 0;
    const magnitude = Math.hypot(x, y);
    if (magnitude > preferences.controllerDeadzone) {
      const nextAim = { angle: Math.atan2(y, x), power: Math.max(1, Math.min(8, magnitude * 8 * preferences.controllerAimSensitivity)), kind: aim.kind };
      if (Math.abs(nextAim.angle - aim.angle) > .01 || Math.abs(nextAim.power - aim.power) > .05) { aim = nextAim; drawBoard(); updateAimHud(); renderControls(); }
    }
    if (edge(0)) shoot();
    if (edge(3)) selectShotKind(aim.kind === 'chip' ? 'putt' : 'chip');
    const controllerCard = current().pockets.find((card) => card.id === current().inventory) ?? current().pockets[0];
    if (edge(1) && controllerCard) useHeldPowerUp(controllerCard.id, controllerCard.instanceId);
    if (edge(14)) adjustPower(-POWER_STEP);
    if (edge(15)) adjustPower(POWER_STEP);
  };

  app.addEventListener('click', (event) => {
    const target = event.target as HTMLElement;
    const element = target.closest<HTMLElement>('button') ?? target;
    const homeTarget = element.dataset.homePanel as HomePanel | undefined;
    if (homeTarget) { homePanel = homeTarget; if (homeTarget === 'play') homeMode = 'modes'; notice = undefined; render(); return; }
    const homeModeTarget = element.dataset.homeMode as HomeMode | undefined;
    if (homeModeTarget) { homeMode = homeModeTarget; notice = undefined; render(); return; }
    if (element.hasAttribute('data-start-local')) { startLocal('local'); return; }
    if (element.hasAttribute('data-quick-start-local')) { startLocal('local', true); return; }
    if (element.hasAttribute('data-start-local-multiplayer')) { startLocal('local-multiplayer'); return; }
    if (element.hasAttribute('data-quick-start-local-multiplayer')) { startLocal('local-multiplayer', true); return; }
    if (element.hasAttribute('data-create-room')) { createOnlineRoom(); return; }
    if (element.hasAttribute('data-create-quick-room')) { createOnlineRoom(true); return; }
    if (element.hasAttribute('data-join-room')) { joinOnlineRoom(); return; }
    if (element.hasAttribute('data-start-room')) { startOnlineMatch(); return; }
    if (element.hasAttribute('data-leave-lobby')) { onlineClient?.send({ type: 'leave-room' }); onlineClient?.disconnect(); onlineClient = undefined; room = undefined; screen = 'home'; notice = undefined; render(); return; }
    if (element.hasAttribute('data-restart-run')) { if (online()) { screen = 'home'; onlineClient?.disconnect(); onlineClient = undefined; room = undefined; render(); } else setupGame(); return; }
    if (element.hasAttribute('data-toggle-pause')) { togglePause(); return; }
    const drawerTarget = element.dataset.drawer as Exclude<Drawer, undefined> | undefined;
    if (drawerTarget) { drawer = drawer === drawerTarget ? undefined : drawerTarget; render(); return; }
    if (element.hasAttribute('data-close-drawer')) { drawer = undefined; render(); return; }
    if (element.hasAttribute('data-add-die-side')) { addDieSide(); return; }
    const dieFace = element.dataset.augmentDieFace;
    if (dieFace) { augmentDieFace(dieFace); return; }
    if (element.hasAttribute('data-ready-die-roll')) { readyDieRoll(); return; }
    const powerUp = element.dataset.usePowerup as (PowerUp | ChronoCard) | undefined;
    if (powerUp) { useHeldPowerUp(powerUp, element.dataset.cardId || undefined); return; }
    const reroll = element.dataset.shopReroll;
    if (reroll && state.shop && !state.shop.rerollResolved) {
      const voter = online() ? state.players.find((player) => player.id === onlinePlayerId && state.shop!.rerollVotes[player.id] === undefined) : state.players.find((player) => player.kind === 'human' && state.shop!.rerollVotes[player.id] === undefined);
      if (voter) dispatch({ type: 'shop-vote-reroll', playerId: voter.id, approve: reroll === 'yes' });
      return;
    }
    const offerId = element.dataset.shopBuy;
    if (offerId && state.shop) {
      const buyerId = state.shop.buyerOrder[state.shop.buyerIndex];
      const replaceCaddyId = app.querySelector<HTMLSelectElement>('#replace-caddy')?.value;
      if (buyerId) dispatch({ type: 'shop-buy', playerId: buyerId, offerId, replaceCaddyId: replaceCaddyId as never });
      return;
    }
    const sellCaddy = element.dataset.shopSell;
    if (sellCaddy && state.shop) {
      const buyerId = state.shop.buyerOrder[state.shop.buyerIndex];
      if (buyerId) dispatch({ type: 'shop-sell-caddy', playerId: buyerId, caddyId: sellCaddy as never });
      return;
    }
    if (element.hasAttribute('data-shop-skip') && state.shop) {
      const buyerId = state.shop.buyerOrder[state.shop.buyerIndex];
      if (buyerId) dispatch({ type: 'shop-skip', playerId: buyerId });
      return;
    }
    if (element.hasAttribute('data-cancel-placement')) { placement = undefined; drawBoard(); renderControls(); return; }
    const emote = element.dataset.emote as Emote | undefined;
    if (emote) { sendEmote(emote); return; }
    const openOverlay = element.dataset.openOverlay as Overlay;
    if (openOverlay) { overlay = openOverlay; rebinding = undefined; render(); return; }
    if (element.hasAttribute('data-close-overlay')) { overlay = undefined; rebinding = undefined; render(); return; }
    const binding = element.dataset.bind as ShortcutId | undefined;
    if (binding) { rebinding = binding; render(); }
  });
  app.addEventListener('change', (event) => {
    const target = event.target as HTMLInputElement | HTMLSelectElement;
    const preference = target.dataset.preference as keyof typeof preferences | undefined;
    if (preference === 'reducedMotion' || preference === 'highContrast' || preference === 'controllerVibration' || preference === 'showMerchantHoldings') updatePreferences({ [preference]: (target as HTMLInputElement).checked });
    const range = target.dataset.preferenceRange as keyof typeof preferences | undefined;
    if (range === 'masterVolume' || range === 'effectsVolume' || range === 'controllerDeadzone' || range === 'controllerAimSensitivity') updatePreferences({ [range]: Number(target.value) });
    const selection = target.dataset.preferenceSelect as keyof typeof preferences | undefined;
    if (selection === 'mousePowerMode' && (target.value === 'scroll' || target.value === 'cursor')) updatePreferences({ mousePowerMode: target.value });
  });
  window.addEventListener('gamepadconnected', (event) => { controllerName = event.gamepad.id || 'controller'; render(); });
  window.addEventListener('gamepaddisconnected', () => { controllerName = undefined; gamepadButtons = []; render(); });
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
    if (event.key === 'Escape' && placement) { placement = undefined; drawBoard(); renderControls(); return; }
    if (event.key === 'Enter' && placement) { event.preventDefault(); confirmPlacement(); return; }
    if (event.key === 'Escape' && overlay) { overlay = undefined; render(); return; }
    if (event.key.toLowerCase() === 'c' && !overlay && !shotAnimation) { selectShotKind(aim.kind === 'chip' ? 'putt' : 'chip'); return; }
    const command = shortcutForKey(preferences, event.key);
    if (!command) return;
    event.preventDefault();
    if (command === 'help') { overlay = overlay === 'help' ? undefined : 'help'; render(); return; }
    if (command === 'settings') { overlay = overlay === 'settings' ? undefined : 'settings'; render(); return; }
    if (command === 'pause') { togglePause(); return; }
    if (overlay || shotAnimation) return;
    if (command === 'shoot') shoot();
    if (command === 'powerDown') adjustPower(-POWER_STEP);
    if (command === 'powerUp') adjustPower(POWER_STEP);
    const keyboardPowerUp = current().inventory;
    if (command === 'usePowerUp' && keyboardPowerUp) useHeldPowerUp(keyboardPowerUp);
  });
  const loop = (now: number) => {
    const elapsed = Math.min(1, (now - lastTick) / 1000);
    lastTick = now;
    const previousCalloutCount = callouts.length;
    callouts = callouts.filter((callout) => callout.expiresAt > now);
    if (callouts.length !== previousCalloutCount) app.querySelector<HTMLElement>('#callouts')?.replaceChildren();
    const calloutNode = app.querySelector<HTMLElement>('#callouts');
    if (calloutNode && callouts.length !== previousCalloutCount) calloutNode.innerHTML = renderCallouts(callouts);
    const previousEmoteCount = liveEmotes.length;
    liveEmotes = liveEmotes.filter((emote) => emote.expiresAt > now);
    if (liveEmotes.length !== previousEmoteCount && !shotAnimation) drawBoard();
    if (!online() && screen === 'game' && (state.status === 'playing' || state.status === 'rolling' || state.status === 'shopping') && !state.paused) {
      const previousStatus = state.status;
      const previousPlayerIndex = state.turn.playerIndex;
      const next = tickTurn(state, elapsed);
      if (next !== state) {
        const playerOrPhaseChanged = previousStatus !== next.status || previousPlayerIndex !== next.turn.playerIndex;
        if (!playerOrPhaseChanged) {
          state = next;
          if (next.status === 'playing') {
            updateLiveMatchHud();
            if (!shotAnimation) drawBoard();
          } else updatePhaseTimer();
        } else setState(next);
        if (shouldScheduleBotAfterTick({ status: previousStatus, turn: { playerIndex: previousPlayerIndex } }, next)) scheduleBot();
      }
    }
    if (online() && screen === 'game' && state.status === 'playing' && !state.paused) drawBoard();
    pollController();
    requestAnimationFrame(loop);
  };
  applyPreferences();
  document.documentElement.classList.toggle('capture-mode', captureMode);
  recordStateFeedback(state);
  render();
  if (!captureMode) window.clearTimeout(botTimeout); else scheduleBot();
  requestAnimationFrame(loop);
};
