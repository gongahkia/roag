import { createServer } from 'node:http';
import { mkdirSync } from 'node:fs';
import { createHash, randomBytes, scryptSync, timingSafeEqual } from 'node:crypto';
import { DatabaseSync } from 'node:sqlite';
import { WebSocket, WebSocketServer } from 'ws';
import { COURSE_TRANSITION_DURATION_MS } from '../src/core/campaign';
import { applyCommand, botMove, createGame, defaultConfig, tickTurn } from '../src/core/game';
import { CONTENT_BY_ID } from '../src/core/catalog';
import { normalizeGameState } from '../src/core/game-state';
import { chooseBotShopOffer } from '../src/core/shop';
import { chooseBotDieAction } from '../src/core/bots';
import type { CaddyId, Emote, GameCommand, GameState, PowerUp } from '../src/core/types';
import type { ClientMessage, LobbyConfig, LobbyMember, RoomClock, RoomSnapshot, ServerMessage } from '../src/net/protocol';

const port = Number(process.env.PORT ?? 8787);
const host = process.env.HOST ?? '127.0.0.1';
const allowedOrigins = new Set((process.env.APP_ORIGINS ?? 'http://localhost:5173,http://127.0.0.1:5173').split(',').map((origin) => origin.trim()).filter(Boolean));
const databasePath = process.env.GAME_DATABASE ?? 'data/golf-with-your-enemies.sqlite';
const configuredCourseTileBudget = Number(process.env.MAX_COURSE_TILES ?? 4096);
const maxCourseTiles = Number.isSafeInteger(configuredCourseTileBudget) && configuredCourseTileBudget >= 140 ? configuredCourseTileBudget : 4096;
const roomTtlMs = Math.max(60 * 60 * 1_000, Number(process.env.ROOM_TTL_HOURS ?? 168) * 60 * 60 * 1_000);
const trustProxy = process.env.TRUST_PROXY === 'true';
const databaseDirectory = databasePath.includes('/') ? databasePath.slice(0, databasePath.lastIndexOf('/')) : '';
if (databaseDirectory) mkdirSync(databaseDirectory, { recursive: true });
const database = new DatabaseSync(databasePath);
database.exec('PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON; CREATE TABLE IF NOT EXISTS rooms (code TEXT PRIMARY KEY, snapshot TEXT NOT NULL, updated_at INTEGER NOT NULL);');

interface StoredRoom extends RoomSnapshot {
  reconnectTokens: Record<string, string>;
  passphraseHash?: string;
}

interface Session {
  socket: WebSocket;
  ip: string;
  playerId?: string;
  roomCode?: string;
  arrivals: number[];
}

interface RoomTimers {
  botFor?: string;
  botTimeout?: NodeJS.Timeout;
  transitionTimeout?: NodeJS.Timeout;
  clockAt?: number;
}

const rooms = new Map<string, StoredRoom>();
const sessions = new Set<Session>();
const timers = new Map<string, RoomTimers>();
const upgradeArrivals = new Map<string, number[]>();
const roomArrivals = new Map<string, number[]>();
const json = (value: unknown) => JSON.stringify(value);
const now = () => Date.now();
const clientIp = (request: import('node:http').IncomingMessage, socket: import('node:net').Socket) => {
  const forwarded = trustProxy ? request.headers['x-forwarded-for'] : undefined;
  const firstForwarded = typeof forwarded === 'string' ? forwarded.split(',')[0]?.trim() : undefined;
  return firstForwarded || socket.remoteAddress || 'unknown';
};
const withinRate = (bucket: Map<string, number[]>, key: string, limit: number, windowMs: number) => {
  const timestamp = now();
  const arrivals = (bucket.get(key) ?? []).filter((arrival) => timestamp - arrival < windowMs);
  if (arrivals.length >= limit) return false;
  arrivals.push(timestamp);
  bucket.set(key, arrivals);
  return true;
};
const randomId = (bytes = 18) => randomBytes(bytes).toString('base64url');
const roomCode = () => randomBytes(3).toString('hex').toUpperCase();
const tokenHash = (token: string) => createHash('sha256').update(token).digest('base64url');
const hashPassphrase = (passphrase: string) => {
  const salt = randomBytes(16).toString('base64url');
  return `${salt}:${scryptSync(passphrase, salt, 32).toString('base64url')}`;
};
const passphraseMatches = (passphrase: string, encoded: string | undefined) => {
  if (!encoded) return false;
  const [salt, expected] = encoded.split(':');
  if (!salt || !expected) return false;
  const actual = scryptSync(passphrase, salt, 32).toString('base64url');
  return actual.length === expected.length && timingSafeEqual(Buffer.from(actual), Buffer.from(expected));
};
const powerUps = new Set([...CONTENT_BY_ID.values()].filter((definition) => definition.category !== 'caddy' && definition.category !== 'reality').map((definition) => definition.id));
const emotes = new Set(['cheer', 'taunt', 'panic', 'wow', 'gg']);

const safeName = (value: unknown) => typeof value === 'string' && value.trim().length >= 1 && value.trim().length <= 16 ? value.trim().replace(/[^a-zA-Z0-9 _-]/g, '') : undefined;
const safePassphrase = (value: unknown) => typeof value === 'string' && value.trim().length >= 4 && value.trim().length <= 128 ? value : undefined;
const validConfig = (value: unknown): LobbyConfig | undefined => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return undefined;
  const source = value as Record<string, unknown>;
  const seed = typeof source.seed === 'string' && /^[a-zA-Z0-9_-]{1,32}$/.test(source.seed) ? source.seed : defaultConfig().seed;
  const holeCount = Number(source.holeCount);
  const botCount = Number(source.botCount);
  const maxHumans = Number(source.maxHumans);
  const courseWidth = source.courseWidth === undefined ? 20 : Number(source.courseWidth);
  const courseHeight = source.courseHeight === undefined ? 14 : Number(source.courseHeight);
  const botSkill = source.botSkill === 'adaptive' ? 'adaptive' : Number(source.botSkill);
  const skipDieBets = source.skipDieBets === true || source.skipVoting === true;
  if (!Number.isInteger(holeCount) || holeCount < 1 || holeCount > 18 || !Number.isInteger(botCount) || botCount < 0 || botCount > 4 || !Number.isInteger(maxHumans) || maxHumans < 1 || maxHumans > 8 || maxHumans + botCount > 12 || !Number.isSafeInteger(courseWidth) || courseWidth < 14 || !Number.isSafeInteger(courseHeight) || courseHeight < 10 || !Number.isSafeInteger(courseWidth * courseHeight) || courseWidth * courseHeight > maxCourseTiles || (botSkill !== 'adaptive' && (!Number.isInteger(botSkill) || botSkill < 1 || botSkill > 10))) return undefined;
  return { seed, holeCount, botCount, botSkill, maxHumans, courseWidth, courseHeight, skipDieBets };
};
const validCommand = (value: unknown): GameCommand | undefined => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return undefined;
  const source = value as Record<string, unknown>;
  if (source.type === 'shoot' && source.shot && typeof source.shot === 'object') {
    const shot = source.shot as Record<string, unknown>;
    const kind = shot.kind === 'chip' ? 'chip' : shot.kind === undefined || shot.kind === 'putt' ? 'putt' : undefined;
    return typeof shot.angle === 'number' && typeof shot.power === 'number' && Number.isFinite(shot.angle) && Number.isFinite(shot.power) && kind ? { type: 'shoot', shot: { angle: shot.angle, power: shot.power, kind } } : undefined;
  }
  if (source.type === 'add-slot-stop' && typeof source.playerId === 'string' && typeof source.reelId === 'string' && source.playerId.length <= 24 && source.reelId.length <= 40) return { type: 'add-slot-stop', playerId: source.playerId, reelId: source.reelId };
  if (source.type === 'augment-slot-stop' && typeof source.playerId === 'string' && typeof source.reelId === 'string' && typeof source.stopId === 'string' && source.playerId.length <= 24 && source.reelId.length <= 40 && source.stopId.length <= 96) return { type: 'augment-slot-stop', playerId: source.playerId, reelId: source.reelId, stopId: source.stopId };
  if (source.type === 'add-chaos-reel' && typeof source.playerId === 'string' && source.playerId.length <= 24) return { type: 'add-chaos-reel', playerId: source.playerId };
  if (source.type === 'contribute-reroll' && typeof source.playerId === 'string' && source.playerId.length <= 24) return { type: 'contribute-reroll', playerId: source.playerId };
  if (source.type === 'ready-slot-spin' && typeof source.playerId === 'string' && source.playerId.length <= 24) return { type: 'ready-slot-spin', playerId: source.playerId };
  if (source.type === 'set-paused' && typeof source.paused === 'boolean') return { type: 'set-paused', paused: source.paused };
  if (source.type === 'arm-second-wind') return { type: 'arm-second-wind' };
  if (source.type === 'emote' && typeof source.playerId === 'string' && typeof source.emote === 'string' && emotes.has(source.emote)) return { type: 'emote', playerId: source.playerId, emote: source.emote as Emote };
  if (source.type === 'use-power-up' && typeof source.powerUp === 'string' && powerUps.has(source.powerUp as never)) {
    const cardId = typeof source.cardId === 'string' && /^[a-zA-Z0-9:_-]{1,96}$/.test(source.cardId) ? source.cardId : undefined;
    const targetId = typeof source.targetId === 'string' && source.targetId.length <= 24 ? source.targetId : undefined;
    const portalExitId = typeof source.portalExitId === 'string' && source.portalExitId.length <= 80 ? source.portalExitId : undefined;
    const rawPlacement = source.placement;
    const placement = rawPlacement && typeof rawPlacement === 'object' && !Array.isArray(rawPlacement)
      && Number.isInteger((rawPlacement as Record<string, unknown>).x) && Number.isInteger((rawPlacement as Record<string, unknown>).y)
      ? { x: Number((rawPlacement as Record<string, unknown>).x), y: Number((rawPlacement as Record<string, unknown>).y) }
      : undefined;
    return { type: 'use-power-up', powerUp: source.powerUp as PowerUp, cardId, targetId, portalExitId, placement };
  }
  if (source.type === 'shop-vote-reroll' && typeof source.playerId === 'string' && typeof source.approve === 'boolean') return { type: 'shop-vote-reroll', playerId: source.playerId, approve: source.approve };
  if (source.type === 'shop-buy' && typeof source.playerId === 'string' && typeof source.offerId === 'string' && (source.replaceCaddyId === undefined || (typeof source.replaceCaddyId === 'string' && CONTENT_BY_ID.get(source.replaceCaddyId as never)?.category === 'caddy'))) return { type: 'shop-buy', playerId: source.playerId, offerId: source.offerId, replaceCaddyId: source.replaceCaddyId as CaddyId | undefined };
  if (source.type === 'shop-sell-caddy' && typeof source.playerId === 'string' && typeof source.caddyId === 'string' && CONTENT_BY_ID.get(source.caddyId as never)?.category === 'caddy') return { type: 'shop-sell-caddy', playerId: source.playerId, caddyId: source.caddyId as CaddyId };
  if (source.type === 'shop-skip' && typeof source.playerId === 'string') return { type: 'shop-skip', playerId: source.playerId };
  return undefined;
};

const publicRoom = (room: StoredRoom): RoomSnapshot => ({
  code: room.code,
  hostId: room.hostId,
  config: room.config,
  members: room.members.map((member) => ({ ...member, connected: [...sessions].some((session) => session.roomCode === room.code && session.playerId === member.id && session.socket.readyState === WebSocket.OPEN) })),
  phase: room.phase,
  game: room.game,
  updatedAt: room.updatedAt,
});

const persist = (room: StoredRoom) => {
  room.updatedAt = now();
  database.prepare('INSERT INTO rooms (code, snapshot, updated_at) VALUES (?, ?, ?) ON CONFLICT(code) DO UPDATE SET snapshot = excluded.snapshot, updated_at = excluded.updated_at').run(room.code, json(room), room.updatedAt);
};

const send = (session: Session, message: ServerMessage) => {
  if (session.socket.readyState === WebSocket.OPEN) session.socket.send(json(message));
};

const broadcast = (room: StoredRoom) => {
  const message: ServerMessage = { type: 'room-state', room: publicRoom(room) };
  sessions.forEach((session) => { if (session.roomCode === room.code) send(session, message); });
};

const clockFor = (room: StoredRoom): RoomClock | undefined => {
  const game = room.game;
  if (!game) return undefined;
  return {
    roomCode: room.code,
    status: game.status,
    turnSecondsLeft: game.status === 'playing' ? game.turn.secondsLeft : undefined,
    hazardElapsedMs: game.hazardElapsedMs,
    die: game.die ? {
      phase: game.die.phase,
      secondsLeft: game.die.secondsLeft,
      rollSecondsLeft: game.die.roll?.secondsLeft,
      revealedSecondsLeft: game.die.revealed?.secondsLeft,
      rerollPotSecondsLeft: game.die.rerollPot?.secondsLeft,
    } : undefined,
  };
};

const broadcastTick = (room: StoredRoom) => {
  const clock = clockFor(room);
  if (!clock) return;
  const message: ServerMessage = { type: 'room-tick', clock };
  sessions.forEach((session) => { if (session.roomCode === room.code) send(session, message); });
};

const report = (session: Session, message: string) => send(session, { type: 'error', message });

const roomFor = (session: Session) => session.roomCode ? rooms.get(session.roomCode) : undefined;
const roomTimersFor = (room: StoredRoom) => {
  const existing = timers.get(room.code);
  if (existing) return existing;
  const created: RoomTimers = {};
  timers.set(room.code, created);
  return created;
};

const clearAutomation = (room: StoredRoom) => {
  const current = roomTimersFor(room);
  if (current.botTimeout) clearTimeout(current.botTimeout);
  if (current.transitionTimeout) clearTimeout(current.transitionTimeout);
  current.botTimeout = undefined;
  current.botFor = undefined;
  current.transitionTimeout = undefined;
};

const updateGame = (room: StoredRoom, game: GameState) => {
  room.game = game;
  persist(room);
  broadcast(room);
  scheduleAutomation(room);
};

const discreteStateChange = (previous: GameState, next: GameState) => {
  const dieSignature = (game: GameState) => game.die ? `${game.die.phase}:${game.die.roll?.stopIds.join(',') ?? ''}:${game.die.revealed?.plan.id ?? ''}:${game.die.rerolls}` : '';
  return previous.status !== next.status
    || previous.paused !== next.paused
    || previous.turn.playerIndex !== next.turn.playerIndex
    || previous.turn.shotInFlight !== next.turn.shotInFlight
    || dieSignature(previous) !== dieSignature(next);
};

/** Advance elapsed gameplay time immediately before an authoritative action and on the room heartbeat. */
const advanceRoomClock = (room: StoredRoom, timestamp = now(), publish = true) => {
  if (!room.game) return;
  const timers = roomTimersFor(room);
  const previous = timers.clockAt ?? timestamp;
  timers.clockAt = timestamp;
  const next = tickTurn(room.game, Math.max(0, timestamp - previous) / 1_000);
  if (next === room.game) return;
  if (publish && discreteStateChange(room.game, next)) updateGame(room, next);
  else {
    room.game = next;
    if (publish) broadcastTick(room);
  }
};

const scheduleAutomation = (room: StoredRoom) => {
  const game = room.game;
  const current = roomTimersFor(room);
  if (!game || game.paused || game.status === 'finished') {
    clearAutomation(room);
    return;
  }
  if (game.status === 'transitioning') {
    if (!current.transitionTimeout) {
      const remaining = Math.max(0, COURSE_TRANSITION_DURATION_MS - Math.max(0, now() - room.updatedAt));
      current.transitionTimeout = setTimeout(() => {
        current.transitionTimeout = undefined;
        const latest = rooms.get(room.code);
        if (latest?.game?.status === 'transitioning' && !latest.game.paused) updateGame(latest, applyCommand(latest.game, { type: 'complete-transition' }));
      }, remaining);
    }
    return;
  }
  if (game.status === 'rolling' && game.die && (game.die.phase === 'wagering' || game.die.phase === 'reroll-wagering')) {
    const bot = game.players.find((player) => player.kind === 'bot' && !game.die!.wagers[player.id]?.ready);
    if (!bot || current.botFor === `slot:${bot.id}`) return;
    if (current.botTimeout) clearTimeout(current.botTimeout);
    current.botFor = `slot:${bot.id}`;
    current.botTimeout = setTimeout(() => {
      current.botTimeout = undefined;
      current.botFor = undefined;
      const latest = rooms.get(room.code);
      const die = latest?.game?.die;
      if (!latest?.game || latest.game.paused || latest.game.status !== 'rolling' || !die || (die.phase !== 'wagering' && die.phase !== 'reroll-wagering') || die.wagers[bot.id]?.ready) return;
      const action = chooseBotDieAction(latest.game.config.seed, latest.game.hole, bot, die);
      updateGame(latest, applyCommand(latest.game, action));
    }, 520);
    return;
  }
  if (game.status === 'rolling' && game.die?.phase === 'revealed' && game.die.rerollPot) {
    const bot = game.players.find((player) => player.kind === 'bot' && player.cash > 0 && !game.die!.rerollPot!.contributions[player.id]);
    if (!bot || current.botFor === `reroll:${bot.id}`) return;
    if (current.botTimeout) clearTimeout(current.botTimeout);
    current.botFor = `reroll:${bot.id}`;
    current.botTimeout = setTimeout(() => {
      current.botTimeout = undefined;
      current.botFor = undefined;
      const latest = rooms.get(room.code);
      if (!latest?.game || latest.game.paused || latest.game.status !== 'rolling' || latest.game.die?.phase !== 'revealed' || !latest.game.die.rerollPot || bot.cash < 1) return;
      const wantsReroll = latest.game.holeRules.scoreMultiplier >= 1 || bot.skill === 'adaptive' || bot.skill >= 5;
      if (wantsReroll) updateGame(latest, applyCommand(latest.game, { type: 'contribute-reroll', playerId: bot.id }));
    }, 420);
    return;
  }
  if (game.status === 'shopping' && game.shop) {
    const unresolvedBot = game.players.find((player) => player.kind === 'bot' && game.shop && !game.shop.rerollResolved && game.shop.rerollVotes[player.id] === undefined);
    if (unresolvedBot && current.botFor !== `shop-vote:${unresolvedBot.id}`) {
      if (current.botTimeout) clearTimeout(current.botTimeout);
      current.botFor = `shop-vote:${unresolvedBot.id}`;
      current.botTimeout = setTimeout(() => {
        current.botTimeout = undefined;
        current.botFor = undefined;
        const latest = rooms.get(room.code);
        if (latest?.game?.status === 'shopping') updateGame(latest, applyCommand(latest.game, { type: 'shop-vote-reroll', playerId: unresolvedBot.id, approve: false }));
      }, 350);
      return;
    }
    const shopperId = game.shop.buyerOrder[game.shop.buyerIndex];
    const shopper = game.players.find((player) => player.id === shopperId);
    if (!game.shop.rerollResolved || !shopper || shopper.kind !== 'bot' || current.botFor === `shop-buy:${shopper.id}`) return;
    if (current.botTimeout) clearTimeout(current.botTimeout);
    current.botFor = `shop-buy:${shopper.id}`;
    current.botTimeout = setTimeout(() => {
      current.botTimeout = undefined;
      current.botFor = undefined;
      const latest = rooms.get(room.code);
      const latestShop = latest?.game?.shop;
      if (!latest?.game || latest.game.status !== 'shopping' || !latestShop || latestShop.buyerOrder[latestShop.buyerIndex] !== shopper.id) return;
      const latestShopper = latest.game.players.find((player) => player.id === shopper.id);
      const offer = latestShopper ? chooseBotShopOffer(latest.game, latestShopper) : undefined;
      updateGame(latest, applyCommand(latest.game, offer ? { type: 'shop-buy', playerId: shopper.id, offerId: offer.id } : { type: 'shop-skip', playerId: shopper.id }));
    }, 550);
    return;
  }
  if (game.status !== 'playing') return;
  const bot = game.players[game.turn.playerIndex];
  if (!bot || bot.kind !== 'bot' || current.botFor === `shot:${bot.id}`) return;
  if (current.botTimeout) clearTimeout(current.botTimeout);
  current.botFor = `shot:${bot.id}`;
  current.botTimeout = setTimeout(() => {
    current.botTimeout = undefined;
    current.botFor = undefined;
    const latest = rooms.get(room.code);
    if (!latest?.game || latest.game.paused) return;
    advanceRoomClock(latest, now(), false);
    const decision = botMove(latest.game);
    if (!decision) return;
    let next = latest.game;
    if (decision.secondWind) next = applyCommand(next, { type: 'arm-second-wind' });
    if (decision.powerUp) next = applyCommand(next, { type: 'use-power-up', powerUp: decision.powerUp.type, cardId: decision.powerUp.cardId, targetId: decision.powerUp.targetId, portalExitId: decision.powerUp.portalExitId, placement: decision.powerUp.placement });
    updateGame(latest, applyCommand(next, { type: 'shoot', shot: decision.shot }));
  }, 650);
};

const commandAllowed = (room: StoredRoom, session: Session, command: GameCommand) => {
  const game = room.game;
  if (!game) return 'the room has not started';
  if (command.type === 'complete-transition') return 'course transition is server controlled';
  if (command.type === 'set-paused') return session.playerId === room.hostId ? undefined : 'only the host can pause the room';
  if (game.paused) return 'the match is paused';
  const active = game.players[game.turn.playerIndex];
  if (command.type === 'add-slot-stop' || command.type === 'augment-slot-stop' || command.type === 'add-chaos-reel' || command.type === 'contribute-reroll' || command.type === 'ready-slot-spin') return command.playerId === session.playerId ? undefined : 'you can only place your own slot wager';
  if (command.type === 'emote') return command.playerId === session.playerId ? undefined : 'you can only send your own emote';
  if (command.type === 'shop-vote-reroll') return command.playerId === session.playerId ? undefined : 'you can only cast your own merchant ballot';
  if (command.type === 'shop-buy' || command.type === 'shop-sell-caddy' || command.type === 'shop-skip') {
    if (command.playerId !== session.playerId) return 'you can only act for your own golfer';
    if (game.status !== 'shopping' || game.shop?.buyerOrder[game.shop.buyerIndex] !== session.playerId) return 'it is not your merchant turn';
    return undefined;
  }
  if (!active || active.id !== session.playerId || active.kind !== 'human') return 'it is not your turn';
  if (command.type === 'shoot' && (!Number.isFinite(command.shot.angle) || !Number.isFinite(command.shot.power) || command.shot.power < 1 || command.shot.power > 8)) return 'invalid shot';
  return undefined;
};

const createRoom = (session: Session, name: string, config: LobbyConfig, passphrase: string) => {
  leaveRoom(session);
  let code = roomCode();
  while (rooms.has(code)) code = roomCode();
  const host: LobbyMember = { id: 'human-0', name, slot: 0, connected: true, host: true };
  const reconnectToken = randomId();
  const room: StoredRoom = { code, hostId: host.id, config, members: [host], phase: 'lobby', reconnectTokens: { [host.id]: tokenHash(reconnectToken) }, passphraseHash: hashPassphrase(passphrase), updatedAt: now() };
  rooms.set(code, room);
  session.roomCode = code;
  session.playerId = host.id;
  persist(room);
  send(session, { type: 'joined', playerId: host.id, reconnectToken });
  broadcast(room);
};

const joinRoom = (session: Session, requestedCode: string, name: string, passphrase?: string, reconnectToken?: string) => {
  const room = rooms.get(requestedCode.toUpperCase());
  if (!room) return report(session, 'room not found');
  const reconnecting = reconnectToken ? room.members.find((member) => room.reconnectTokens[member.id] === tokenHash(reconnectToken)) : undefined;
  if (!reconnecting && !passphraseMatches(passphrase ?? '', room.passphraseHash)) return report(session, 'incorrect room passphrase');
  if (room.phase === 'game' && !reconnecting) return report(session, 'this game has already started');
  if (!reconnecting && room.members.length >= room.config.maxHumans) return report(session, 'room is full');
  const member = reconnecting ?? { id: `human-${room.members.length}`, name, slot: room.members.length, connected: true, host: false };
  const issuedToken = reconnecting ? reconnectToken! : randomId();
  if (!reconnecting) {
    room.members.push(member);
    room.reconnectTokens[member.id] = tokenHash(issuedToken);
  }
  leaveRoom(session);
  session.roomCode = room.code;
  session.playerId = member.id;
  persist(room);
  send(session, { type: 'joined', playerId: member.id, reconnectToken: issuedToken });
  broadcast(room);
};

const startRoom = (session: Session) => {
  const room = roomFor(session);
  if (!room) return report(session, 'join a room first');
  if (session.playerId !== room.hostId) return report(session, 'only the host can start the room');
  if (room.phase !== 'lobby') return report(session, 'room already started');
  if (room.members.some((member) => ![...sessions].some((candidate) => candidate.roomCode === room.code && candidate.playerId === member.id && candidate.socket.readyState === WebSocket.OPEN))) return report(session, 'wait for every player to reconnect');
  const game = createGame({ seed: room.config.seed, holeCount: room.config.holeCount, botCount: room.config.botCount, botSkill: room.config.botSkill, humanCount: room.members.length, courseWidth: room.config.courseWidth, courseHeight: room.config.courseHeight, skipDieBets: room.config.skipDieBets === true });
  game.players.filter((player) => player.kind === 'human').forEach((player, index) => { player.name = room.members[index]!.name; });
  room.phase = 'game';
  roomTimersFor(room).clockAt = now();
  updateGame(room, game);
};

const leaveRoom = (session: Session) => {
  const room = roomFor(session);
  session.roomCode = undefined;
  session.playerId = undefined;
  if (!room) return;
  persist(room);
  broadcast(room);
};

const handleMessage = (session: Session, value: unknown, timestamp = now()) => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return report(session, 'invalid message');
  const message = value as ClientMessage;
  if ((message.type === 'create-room' || message.type === 'join-room') && !withinRate(roomArrivals, session.ip, 12, 10 * 60_000)) return report(session, 'too many room attempts; try again later');
  if (message.type === 'create-room') {
    const name = safeName(message.name);
    const config = validConfig(message.config);
    const passphrase = safePassphrase(message.passphrase);
    if (!name || !config || !passphrase) return report(session, 'enter a room passphrase of at least four characters');
    return createRoom(session, name, config, passphrase);
  }
  if (message.type === 'join-room') {
    const name = safeName(message.name);
    if (!name || typeof message.code !== 'string' || !/^[a-fA-F0-9]{6}$/.test(message.code)) return report(session, 'invalid room code');
    return joinRoom(session, message.code, name, safePassphrase(message.passphrase), typeof message.reconnectToken === 'string' ? message.reconnectToken : undefined);
  }
  if (message.type === 'leave-room') return leaveRoom(session);
  if (message.type === 'start-room') return startRoom(session);
  if (message.type === 'command') {
    const room = roomFor(session);
    if (!room) return report(session, 'join a room first');
    const command = validCommand(message.command);
    if (!command) return report(session, 'invalid command');
    advanceRoomClock(room, timestamp);
    const denial = commandAllowed(room, session, command);
    if (denial) return report(session, denial);
    return updateGame(room, applyCommand(room.game!, command));
  }
  return report(session, 'unsupported message');
};

const loadRooms = () => {
  const rows = database.prepare('SELECT snapshot FROM rooms').all() as { snapshot: string }[];
  rows.forEach((row) => {
    try {
      const room = JSON.parse(row.snapshot) as StoredRoom;
      if (room.code && room.config && room.members && room.reconnectTokens) {
        room.config.courseWidth ??= 20;
        room.config.courseHeight ??= 14;
        const legacyConfig = room.config as LobbyConfig & { skipVoting?: boolean };
        room.config.skipDieBets ??= legacyConfig.skipVoting === true;
        delete legacyConfig.skipVoting;
        if (room.game) room.game = normalizeGameState(room.game);
        rooms.set(room.code, room);
      }
    } catch { }
  });
};

loadRooms();
rooms.forEach((room) => {
  roomTimersFor(room).clockAt = now();
  scheduleAutomation(room);
});
const pruneExpiredRooms = () => {
  const cutoff = now() - roomTtlMs;
  rooms.forEach((room) => {
    const connected = [...sessions].some((session) => session.roomCode === room.code && session.socket.readyState === WebSocket.OPEN);
    if (connected || room.updatedAt >= cutoff) return;
    clearAutomation(room);
    timers.delete(room.code);
    rooms.delete(room.code);
    database.prepare('DELETE FROM rooms WHERE code = ?').run(room.code);
    console.info(JSON.stringify({ event: 'room_expired', room: room.code }));
  });
};
const httpServer = createServer((request, response) => {
  const path = new URL(request.url ?? '/', `http://${request.headers.host ?? 'localhost'}`).pathname;
  if (path === '/readyz') {
    try {
      database.prepare('SELECT 1').get();
      response.writeHead(200, { 'content-type': 'application/json' });
      response.end(json({ ready: true }));
    } catch {
      response.writeHead(503, { 'content-type': 'application/json' });
      response.end(json({ ready: false }));
    }
    return;
  }
  if (path === '/healthz') {
    response.writeHead(200, { 'content-type': 'application/json' });
    response.end(json({ healthy: true }));
    return;
  }
  if (path === '/metrics') {
    response.writeHead(200, { 'content-type': 'text/plain; version=0.0.4' });
    response.end(`golf_rooms ${rooms.size}\ngolf_sessions ${sessions.size}\n`);
    return;
  }
  response.writeHead(200, { 'content-type': 'application/json' });
  response.end(json({ service: 'golf-with-your-enemies', rooms: rooms.size }));
});
const socketServer = new WebSocketServer({ noServer: true, maxPayload: 64 * 1024 });

httpServer.on('upgrade', (request, socket, head) => {
  const ip = clientIp(request, socket);
  if (!withinRate(upgradeArrivals, ip, 24, 60_000)) {
    socket.write('HTTP/1.1 429 Too Many Requests\r\nConnection: close\r\n\r\n');
    socket.destroy();
    return;
  }
  const origin = request.headers.origin;
  if (!origin || !allowedOrigins.has(origin)) {
    socket.write('HTTP/1.1 403 Forbidden\r\nConnection: close\r\n\r\n');
    socket.destroy();
    return;
  }
  socketServer.handleUpgrade(request, socket, head, (webSocket) => socketServer.emit('connection', webSocket, ip));
});

socketServer.on('connection', (socket, ip: string) => {
  const session: Session = { socket, ip, arrivals: [] };
  sessions.add(session);
  socket.on('message', (raw) => {
    const timestamp = now();
    session.arrivals = session.arrivals.filter((arrival) => timestamp - arrival < 10_000);
    if (session.arrivals.length >= 30) return report(session, 'slow down');
    session.arrivals.push(timestamp);
    const messageSize = Array.isArray(raw) ? raw.reduce((total, chunk) => total + chunk.byteLength, 0) : raw.byteLength;
    if (messageSize > 64 * 1024) return report(session, 'message too large');
    try { handleMessage(session, JSON.parse(raw.toString()), timestamp); } catch { report(session, 'invalid JSON'); }
  });
  socket.on('close', () => {
    sessions.delete(session);
    const room = roomFor(session);
    if (room) {
      persist(room);
      broadcast(room);
    }
  });
});

const roomHeartbeat = setInterval(() => {
  rooms.forEach((room) => {
    advanceRoomClock(room);
  });
}, 250).unref();

setInterval(pruneExpiredRooms, 60 * 60_000).unref();

let shuttingDown = false;
const shutdown = (signal: string) => {
  if (shuttingDown) return;
  shuttingDown = true;
  clearInterval(roomHeartbeat);
  rooms.forEach((room) => persist(room));
  socketServer.clients.forEach((socket) => socket.close(1012, 'server restart'));
  socketServer.close();
  httpServer.close(() => {
    database.close();
    console.info(JSON.stringify({ event: 'server_stopped', signal }));
    process.exit(0);
  });
  setTimeout(() => process.exit(1), 8_000).unref();
};
process.on('SIGINT', () => shutdown('SIGINT'));
process.on('SIGTERM', () => shutdown('SIGTERM'));
httpServer.listen(port, host, () => console.info(JSON.stringify({ event: 'server_started', host, port })));
