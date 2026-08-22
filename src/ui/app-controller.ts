import { presentFeedback } from '../feedback';
import { chooseBotVote } from '../core/bots';
import { applyCommand, botMove, createGame, defaultConfig, previewShot, tickTurn } from '../core/game';
import { courseForPlan } from '../core/game-state';
import { canPlaceGadget } from '../core/powerups';
import { chooseBotShopOffer } from '../core/shop';
import type { Ball, ChronoCard, Emote, EmoteEvent, GadgetKind, GameCommand, GameConfig, GameState, Point, PowerUp, ShotCommand } from '../core/types';
import { OnlineClient } from '../net/online-client';
import type { ClientMessage, LobbyConfig, RoomSnapshot } from '../net/protocol';
import { isEditableElement, loadPreferences, savePreferences, setShortcut, shortcutForKey, type ShortcutId } from '../preferences';
import { lobbyConfigFromGame, renderHomeMarkup, renderLobbyMarkup, type HomeMode, type HomePanel } from './home-markup';
import { type Callout, type Drawer, type LedgerEntry, type Overlay, type ViewModel, renderAppMarkup, renderCallouts, renderControlsMarkup, renderStatus } from './markup';
import { createRenderer } from './render';

interface TimedCallout extends Callout { expiresAt: number; }
interface ShotAnimation { playerId: string; frame: number; }
interface LiveEmote extends EmoteEvent { expiresAt: number; }
interface PlacementState { kind: GadgetKind; cardId?: string; point?: Point; valid: boolean; confirmed: boolean; ownerId: string; }
type Screen = 'home' | 'lobby' | 'game';
type BotScheduleSnapshot = Pick<GameState, 'status'> & { turn: Pick<GameState['turn'], 'playerIndex'> };

export const shouldScheduleBotAfterTick = (previous: BotScheduleSnapshot, next: BotScheduleSnapshot) => previous.status !== next.status || previous.turn.playerIndex !== next.turn.playerIndex;

const MIN_POWER = 1;
const MAX_POWER = 8;
const POWER_STEP = .5;
export const powerAfterWheel = (power: number, deltaY: number) => deltaY === 0 ? power : Math.max(MIN_POWER, Math.min(MAX_POWER, Number((power + (deltaY < 0 ? POWER_STEP : -POWER_STEP)).toFixed(1))));

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
  let transitionFrame: number | undefined;
  let transitionProgress = 0;
  let transitionCourse: GameState['course'] | undefined;
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
  let gamepadButtons: boolean[] = [];
  let placement: PlacementState | undefined;
  let audioContext: AudioContext | undefined;

  const online = () => Boolean(onlineClient && room?.phase === 'game');
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
    multiplayer: { online: online(), connected: onlineConnected, roomCode: room?.code, playerId: onlinePlayerId, host: room?.hostId === onlinePlayerId, controllerName },
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

  const drawBoard = (animationBalls?: readonly Ball[], drawAim: ShotCommand | null = aim) => {
    const players = animationBalls ? state.players.map((player, index) => ({ ...player, ball: animationBalls[index] ?? player.ball })) : state.players;
    if (state.status === 'transitioning' && state.transition) {
      if (transitionProgress < .5) renderer?.draw(state.course, [], state.coursePhase, undefined, [], false, state.holeRules.hazardPhaseCount, 1 - transitionProgress * 2, []);
      else renderer?.draw(transitionCourse ?? courseForPlan(state.transition.next), [], 0, undefined, [], false, state.transition.next.recipe.rules.hazardPhaseCount, (transitionProgress - .5) * 2, []);
      return;
    }
    const effectiveAim = drawAim && current().forcedChip ? { ...drawAim, kind: 'chip' as const } : drawAim;
    renderer?.draw(state.course, players, state.coursePhase, placement ? undefined : effectiveAim ?? undefined, liveEmotes, state.holeRules.powerUps, state.holeRules.hazardPhaseCount, undefined, state.gadgets ?? [], placement, players[state.turn.playerIndex]?.ball);
  };
  const startTransition = (completeLocally: boolean) => {
    if (state.status !== 'transitioning') return;
    if (transitionFrame !== undefined) window.cancelAnimationFrame(transitionFrame);
    const duration = preferences.reducedMotion ? 120 : 1_650;
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
  const setState = (next: GameState) => {
    const enteringTransition = state.status !== 'transitioning' && next.status === 'transitioning';
    state = next;
    if (placement && (state.status !== 'playing' || !state.players.find((player) => player.id === placement!.ownerId && (player.inventory === placement!.kind || player.spareInventory === placement!.kind)))) placement = undefined;
    if (enteringTransition) {
      transitionProgress = 0;
      transitionCourse = state.transition ? courseForPlan(state.transition.next) : undefined;
    }
    if (state.status !== 'transitioning' && transitionFrame !== undefined) {
      window.cancelAnimationFrame(transitionFrame);
      transitionFrame = undefined;
      transitionCourse = undefined;
    }
    recordStateFeedback(state);
    syncEmotes(state);
    render();
    if (!online()) {
      scheduleBot();
      if (state.status === 'transitioning') startTransition(true);
    } else if (enteringTransition) startTransition(false);
  };
  const receiveGame = (next: GameState) => setState(next);
  const autoResolveCaptureVote = (source: GameState) => {
    let resolved = source;
    while (resolved.status === 'voting') {
      const optionId = resolved.vote?.options[0]?.id;
      if (!optionId) break;
      resolved = resolved.players.reduce((next, player) => applyCommand(next, { type: 'cast-vote', playerId: player.id, optionId }), resolved);
    }
    return resolved;
  };
  if (captureMode) state = autoResolveCaptureVote(state);

  const readConfig = (prefix: 'local' | 'online'): LobbyConfig => {
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
      skipVoting: false,
    };
  };
  const startLocal = (skipVoting = false) => {
    const selected = { ...readConfig('local'), skipVoting };
    if (selected.maxHumans + selected.botCount > 12 || selected.maxHumans < 1 || selected.botCount > 4) { notice = 'choose between one and twelve total players'; render(); return; }
    if (!validCourseDimensions(selected.courseWidth, selected.courseHeight)) { notice = 'choose whole-number level dimensions of at least 14×10 tiles'; render(); return; }
    onlineClient?.disconnect();
    onlineClient = undefined;
    room = undefined;
    onlinePlayerId = undefined;
    pendingReconnectToken = undefined;
    onlineConnected = false;
    config = { seed: selected.seed, holeCount: selected.holeCount, humanCount: selected.maxHumans, botCount: selected.botCount, botSkill: selected.botSkill, courseWidth: selected.courseWidth, courseHeight: selected.courseHeight, skipVoting: selected.skipVoting };
    drawer = captureMode ? undefined : 'intel';
    overlay = undefined;
    liveEmotes = [];
    seenEmoteIds = new Set();
    screen = 'game';
    notice = undefined;
    setState(captureMode ? autoResolveCaptureVote(createGame(config)) : createGame(config));
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
    setState(captureMode ? autoResolveCaptureVote(createGame(config)) : createGame(config));
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
      onError(messageText) { if (onlineClient === client) { notice = messageText; render(); } },
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
  const createOnlineRoom = (skipVoting = false) => {
    const selected = { ...readConfig('online'), skipVoting };
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
  const chooseAim = (event: PointerEvent) => {
    if (placement) { updatePlacement(event); return; }
    if (!renderer || state.status !== 'playing' || state.paused || current().kind !== 'human' || !canControlCurrent()) return;
    const pointerAim = renderer.aimFromPointer(event, state.course, current().ball);
    aim = { ...aim, angle: pointerAim.angle };
    drawBoard();
    renderControls();
  };
  const adjustPower = (amount: number) => {
    aim = { ...aim, power: Math.max(MIN_POWER, Math.min(MAX_POWER, Number((aim.power + amount).toFixed(1)))) };
    if (state.status !== 'playing' || state.paused) return;
    drawBoard();
    renderControls();
  };
  const setPower = (power: number) => {
    if (!Number.isFinite(power) || state.status !== 'playing' || state.paused || current().kind !== 'human' || !canControlCurrent()) return;
    aim = { ...aim, power: Math.max(MIN_POWER, Math.min(MAX_POWER, power)) };
    drawBoard();
    renderControls();
  };
  const adjustPowerFromWheel = (event: WheelEvent) => {
    if (placement || !renderer || state.status !== 'playing' || state.paused || current().kind !== 'human' || !canControlCurrent()) return;
    event.preventDefault();
    const next = powerAfterWheel(aim.power, event.deltaY);
    if (next === aim.power) return;
    aim = { ...aim, power: next };
    drawBoard();
    renderControls();
  };
  const selectShotKind = (kind: ShotCommand['kind']) => {
    aim = { ...aim, kind: kind ?? 'putt' };
    if (state.status !== 'playing' || state.paused) return;
    drawBoard();
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
    state = inFlight;
    vibrate();
    playEffect(240, .09);
    renderControls();
    const status = app.querySelector<HTMLElement>('#status');
    if (status) status.textContent = renderStatus(view());
    const animate = (now: number) => {
      if (!shotAnimation || state !== inFlight) return;
      const progress = Math.max(0, Math.min(1, (now - startedAt) / duration));
      const frame = Math.max(0, Math.min(frames.length - 1, Math.floor(progress * (frames.length - 1))));
      shotAnimation.frame = frame;
      drawBoard(frames[frame]!, null);
      if (progress < 1) { requestAnimationFrame(animate); return; }
      shotAnimation = undefined;
      setState(applyCommand(source, { type: 'shoot', shot }));
    };
    requestAnimationFrame(animate);
  };
  const shoot = () => { if (state.status === 'playing' && current().kind === 'human' && canControlCurrent()) playShot(aim); };
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
  const castVote = (optionId: string) => {
    const voter = online() ? state.players.find((player) => player.id === onlinePlayerId && !state.vote?.ballots[player.id]) : state.players.find((player) => player.kind === 'human' && !state.vote?.ballots[player.id]);
    if (voter) { playEffect(660, .06); dispatch({ type: 'cast-vote', playerId: voter.id, optionId }); }
  };
  const togglePause = () => {
    if (state.status === 'finished' || (online() && room?.hostId !== onlinePlayerId)) return;
    playEffect(state.paused ? 570 : 330, .1);
    dispatch({ type: 'set-paused', paused: !state.paused });
  };
  const scheduleBot = () => {
    window.clearTimeout(botTimeout);
    if (online() || state.paused) return;
    if (state.status === 'voting' && state.vote) {
      const bot = state.players.find((player) => player.kind === 'bot' && !state.vote!.ballots[player.id]);
      if (!bot) return;
      botTimeout = window.setTimeout(() => {
        if (state.status !== 'voting' || !state.vote || state.vote.ballots[bot.id]) return;
        const optionId = chooseBotVote(state.config.seed, state.hole, bot, state.vote.options);
        setState(applyCommand(state, { type: 'cast-vote', playerId: bot.id, optionId }));
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
    app.querySelectorAll<HTMLButtonElement>('[data-shot-kind]').forEach((button) => button.addEventListener('click', () => selectShotKind(button.dataset.shotKind === 'chip' ? 'chip' : 'putt')));
    app.querySelector<HTMLButtonElement>('#shoot')?.addEventListener('click', shoot);
    app.querySelector<HTMLButtonElement>('#second-wind')?.addEventListener('click', () => dispatch({ type: 'arm-second-wind' }));
  };
  const render = () => {
    renderer?.dispose();
    renderer = undefined;
    if (screen === 'home') {
      app.innerHTML = renderHomeMarkup({ panel: homePanel, mode: homeMode, config: lobbyConfigFromGame(config), preferences, playerName, roomCode: requestedRoomCode, serverUrl, connected: onlineConnected, notice });
      return;
    }
    if (screen === 'lobby') {
      if (room) app.innerHTML = renderLobbyMarkup(room, onlinePlayerId, onlineConnected, notice);
      return;
    }
    app.innerHTML = renderAppMarkup(view());
    app.querySelector<HTMLButtonElement>('#new-run')?.addEventListener('click', setupGame);
    const canvas = app.querySelector<HTMLCanvasElement>('#course');
    if (!canvas) return;
    renderer = createRenderer(canvas);
    drawBoard(undefined, state.status === 'playing' ? aim : null);
    canvas.addEventListener('pointermove', chooseAim);
    canvas.addEventListener('pointerdown', selectPlacement);
    canvas.addEventListener('wheel', adjustPowerFromWheel, { passive: false });
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
      if (Math.abs(nextAim.angle - aim.angle) > .01 || Math.abs(nextAim.power - aim.power) > .05) { aim = nextAim; drawBoard(); renderControls(); }
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
    if (element.hasAttribute('data-start-local')) { startLocal(); return; }
    if (element.hasAttribute('data-quick-start-local')) { startLocal(true); return; }
    if (element.hasAttribute('data-create-room')) { createOnlineRoom(); return; }
    if (element.hasAttribute('data-create-quick-room')) { createOnlineRoom(true); return; }
    if (element.hasAttribute('data-join-room')) { joinOnlineRoom(); return; }
    if (element.hasAttribute('data-start-room')) { onlineClient?.send({ type: 'start-room' }); return; }
    if (element.hasAttribute('data-leave-lobby')) { onlineClient?.send({ type: 'leave-room' }); onlineClient?.disconnect(); onlineClient = undefined; room = undefined; screen = 'home'; notice = undefined; render(); return; }
    if (element.hasAttribute('data-restart-run')) { if (online()) { screen = 'home'; onlineClient?.disconnect(); onlineClient = undefined; room = undefined; render(); } else setupGame(); return; }
    if (element.hasAttribute('data-toggle-pause')) { togglePause(); return; }
    const drawerTarget = element.dataset.drawer as Exclude<Drawer, undefined> | undefined;
    if (drawerTarget) { drawer = drawer === drawerTarget ? undefined : drawerTarget; render(); return; }
    if (element.hasAttribute('data-close-drawer')) { drawer = undefined; render(); return; }
    const voteOption = target.closest<HTMLElement>('[data-vote-option]')?.dataset.voteOption;
    if (voteOption) { castVote(voteOption); return; }
    if (element.dataset.power !== undefined) { setPower(Number(element.dataset.power)); return; }
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
    const target = event.target as HTMLInputElement;
    const preference = target.dataset.preference as keyof typeof preferences | undefined;
    if (preference === 'reducedMotion' || preference === 'highContrast' || preference === 'controllerVibration') updatePreferences({ [preference]: target.checked });
    const range = target.dataset.preferenceRange as keyof typeof preferences | undefined;
    if (range === 'masterVolume' || range === 'effectsVolume' || range === 'controllerDeadzone' || range === 'controllerAimSensitivity') updatePreferences({ [range]: Number(target.value) });
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
    const voteCard = event.target instanceof HTMLElement ? event.target.closest<HTMLElement>('[data-vote-option]') : undefined;
    if (voteCard && (event.key === 'Enter' || event.key === ' ')) {
      const voteOption = voteCard.dataset.voteOption;
      if (voteOption) { event.preventDefault(); castVote(voteOption); return; }
    }
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
    if (!online() && screen === 'game' && state.status === 'playing' && !state.paused && !shotAnimation) {
      const previousStatus = state.status;
      const previousPlayerIndex = state.turn.playerIndex;
      const next = tickTurn(state, elapsed);
      if (next !== state) {
        state = next;
        recordStateFeedback(state);
        drawBoard();
        renderControls();
        const status = app.querySelector<HTMLElement>('#status');
        if (status) status.textContent = renderStatus(view());
        if (shouldScheduleBotAfterTick({ status: previousStatus, turn: { playerIndex: previousPlayerIndex } }, state)) scheduleBot();
      }
    }
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
