import { createServer } from 'node:http';
import { mkdirSync } from 'node:fs';
import { createHash, randomBytes } from 'node:crypto';
import { DatabaseSync } from 'node:sqlite';
import { WebSocket, WebSocketServer } from 'ws';
import { COURSE_TRANSITION_DURATION_MS } from '../src/core/campaign';
import { applyCommand, botMove, createGame, defaultConfig, tickTurn } from '../src/core/game';
import { CONTENT_BY_ID } from '../src/core/catalog';
import { normalizeGameState } from '../src/core/game-state';
import { chooseBotShopOffer } from '../src/core/shop';
import { chooseBotDieAction } from '../src/core/bots';
import type { CaddyId, Emote, GameCommand, GameState, PowerUp } from '../src/core/types';
import type { ClientMessage, LobbyConfig, LobbyMember, RoomSnapshot, ServerMessage } from '../src/net/protocol';

const port = Number(process.env.PORT ?? 8787);
const allowedOrigins = new Set((process.env.APP_ORIGINS ?? 'http://localhost:5173,http://127.0.0.1:5173').split(',').map((origin) => origin.trim()).filter(Boolean));
const databasePath = process.env.GAME_DATABASE ?? 'data/golf-with-your-enemies.sqlite';
const configuredCourseTileBudget = Number(process.env.MAX_COURSE_TILES ?? 262_144);
const maxCourseTiles = Number.isSafeInteger(configuredCourseTileBudget) && configuredCourseTileBudget >= 140 ? configuredCourseTileBudget : 262_144;
const databaseDirectory = databasePath.includes('/') ? databasePath.slice(0, databasePath.lastIndexOf('/')) : '';
if (databaseDirectory) mkdirSync(databaseDirectory, { recursive: true });
const database = new DatabaseSync(databasePath);
database.exec('PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON; CREATE TABLE IF NOT EXISTS rooms (code TEXT PRIMARY KEY, snapshot TEXT NOT NULL, updated_at INTEGER NOT NULL);');

interface StoredRoom extends RoomSnapshot {
  reconnectTokens: Record<string, string>;
}

interface Session {
  socket: WebSocket;
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
const json = (value: unknown) => JSON.stringify(value);
const now = () => Date.now();
const randomId = (bytes = 18) => randomBytes(bytes).toString('base64url');
const roomCode = () => randomBytes(3).toString('hex').toUpperCase();
const tokenHash = (token: string) => createHash('sha256').update(token).digest('base64url');
const powerUps = new Set([...CONTENT_BY_ID.values()].filter((definition) => definition.category !== 'caddy' && definition.category !== 'reality').map((definition) => definition.id));
const emotes = new Set(['cheer', 'taunt', 'panic', 'wow', 'gg']);

const safeName = (value: unknown) => typeof value === 'string' && value.trim().length >= 1 && value.trim().length <= 16 ? value.trim().replace(/[^a-zA-Z0-9 _-]/g, '') : undefined;
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
  if (source.type === 'add-die-side' && typeof source.playerId === 'string' && source.playerId.length <= 24) return { type: 'add-die-side', playerId: source.playerId };
  if (source.type === 'augment-die-face' && typeof source.playerId === 'string' && typeof source.faceId === 'string' && source.playerId.length <= 24 && source.faceId.length <= 80) return { type: 'augment-die-face', playerId: source.playerId, faceId: source.faceId };
  if (source.type === 'ready-die-roll' && typeof source.playerId === 'string' && source.playerId.length <= 24) return { type: 'ready-die-roll', playerId: source.playerId };
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

/** Advance elapsed gameplay time immediately before an authoritative action and on the room heartbeat. */
const advanceRoomClock = (room: StoredRoom, timestamp = now(), publish = true) => {
  if (!room.game) return;
  const timers = roomTimersFor(room);
  const previous = timers.clockAt ?? timestamp;
  timers.clockAt = timestamp;
  const next = tickTurn(room.game, Math.max(0, timestamp - previous) / 1_000);
  if (next === room.game) return;
  if (publish) updateGame(room, next);
  else room.game = next;
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
      current.transitionTimeout = setTimeout(() => {
        current.transitionTimeout = undefined;
        const latest = rooms.get(room.code);
        if (latest?.game?.status === 'transitioning' && !latest.game.paused) updateGame(latest, applyCommand(latest.game, { type: 'complete-transition' }));
      }, COURSE_TRANSITION_DURATION_MS);
    }
    return;
  }
  if (game.status === 'rolling' && game.die && !game.die.roll) {
    const bot = game.players.find((player) => player.kind === 'bot' && !game.die!.wagers[player.id]?.ready);
    if (!bot || current.botFor === `die:${bot.id}`) return;
    if (current.botTimeout) clearTimeout(current.botTimeout);
    current.botFor = `die:${bot.id}`;
    current.botTimeout = setTimeout(() => {
      current.botTimeout = undefined;
      current.botFor = undefined;
      const latest = rooms.get(room.code);
      const die = latest?.game?.die;
      if (!latest?.game || latest.game.paused || latest.game.status !== 'rolling' || !die || die.roll || die.wagers[bot.id]?.ready) return;
      const action = chooseBotDieAction(latest.game.config.seed, latest.game.hole, bot, die);
      updateGame(latest, applyCommand(latest.game, action));
    }, 520);
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
  if (command.type === 'add-die-side' || command.type === 'augment-die-face' || command.type === 'ready-die-roll') return command.playerId === session.playerId ? undefined : 'you can only place your own die wager';
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

const createRoom = (session: Session, name: string, config: LobbyConfig) => {
  leaveRoom(session);
  let code = roomCode();
  while (rooms.has(code)) code = roomCode();
  const host: LobbyMember = { id: 'human-0', name, slot: 0, connected: true, host: true };
  const reconnectToken = randomId();
  const room: StoredRoom = { code, hostId: host.id, config, members: [host], phase: 'lobby', reconnectTokens: { [host.id]: tokenHash(reconnectToken) }, updatedAt: now() };
  rooms.set(code, room);
  session.roomCode = code;
  session.playerId = host.id;
  persist(room);
  send(session, { type: 'joined', playerId: host.id, reconnectToken });
  broadcast(room);
};

const joinRoom = (session: Session, requestedCode: string, name: string, reconnectToken?: string) => {
  const room = rooms.get(requestedCode.toUpperCase());
  if (!room) return report(session, 'room not found');
  const reconnecting = reconnectToken ? room.members.find((member) => room.reconnectTokens[member.id] === tokenHash(reconnectToken)) : undefined;
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
  if (message.type === 'create-room') {
    const name = safeName(message.name);
    const config = validConfig(message.config);
    if (!name || !config) return report(session, 'invalid room details');
    return createRoom(session, name, config);
  }
  if (message.type === 'join-room') {
    const name = safeName(message.name);
    if (!name || typeof message.code !== 'string' || !/^[a-fA-F0-9]{6}$/.test(message.code)) return report(session, 'invalid room code');
    return joinRoom(session, message.code, name, typeof message.reconnectToken === 'string' ? message.reconnectToken : undefined);
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
const httpServer = createServer((_, response) => {
  response.writeHead(200, { 'content-type': 'application/json' });
  response.end(json({ service: 'golf-with-your-enemies', rooms: rooms.size }));
});
const socketServer = new WebSocketServer({ noServer: true, maxPayload: 64 * 1024 });

httpServer.on('upgrade', (request, socket, head) => {
  const origin = request.headers.origin;
  if (!origin || !allowedOrigins.has(origin)) {
    socket.write('HTTP/1.1 403 Forbidden\r\nConnection: close\r\n\r\n');
    socket.destroy();
    return;
  }
  socketServer.handleUpgrade(request, socket, head, (webSocket) => socketServer.emit('connection', webSocket));
});

socketServer.on('connection', (socket) => {
  const session: Session = { socket, arrivals: [] };
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

setInterval(() => {
  rooms.forEach((room) => {
    advanceRoomClock(room);
  });
}, 250).unref();

httpServer.listen(port, () => console.log(`Golf server listening on :${port}`));
