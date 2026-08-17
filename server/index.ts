import { createServer } from 'node:http';
import { mkdirSync } from 'node:fs';
import { createHash, randomBytes } from 'node:crypto';
import { DatabaseSync } from 'node:sqlite';
import { WebSocket, WebSocketServer } from 'ws';
import { applyCommand, botMove, createGame, defaultConfig, tickTurn } from '../src/core/game';
import { normalizeGameState } from '../src/core/game-state';
import type { Emote, GameCommand, GameState, PowerUp } from '../src/core/types';
import type { ClientMessage, LobbyConfig, LobbyMember, RoomSnapshot, ServerMessage } from '../src/net/protocol';

const port = Number(process.env.PORT ?? 8787);
const allowedOrigins = new Set((process.env.APP_ORIGINS ?? 'http://localhost:5173,http://127.0.0.1:5173').split(',').map((origin) => origin.trim()).filter(Boolean));
const databasePath = process.env.GAME_DATABASE ?? 'data/golf-with-your-enemies.sqlite';
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
}

const rooms = new Map<string, StoredRoom>();
const sessions = new Set<Session>();
const timers = new Map<string, RoomTimers>();
const json = (value: unknown) => JSON.stringify(value);
const now = () => Date.now();
const randomId = (bytes = 18) => randomBytes(bytes).toString('base64url');
const roomCode = () => randomBytes(3).toString('hex').toUpperCase();
const tokenHash = (token: string) => createHash('sha256').update(token).digest('base64url');
const powerUps = new Set(['turbo', 'shield', 'bomb', 'freeze', 'swap', 'two putts', 'heavy', 'bouncy', 'ghost', 'magnet', 'ice', 'portal', 'glider', 'sticky', 'orbit', 'cup magnet', 'slipstream', 'rebound rig', 'phase shift', 'sandbag', 'rescue drone', 'airhorn', 'popper pad', 'snare patch', 'blast mine', 'slick patch', 'sky spring']);
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
  const skipVoting = source.skipVoting === true;
  if (!Number.isInteger(holeCount) || holeCount < 1 || holeCount > 18 || !Number.isInteger(botCount) || botCount < 0 || botCount > 4 || !Number.isInteger(maxHumans) || maxHumans < 1 || maxHumans > 8 || maxHumans + botCount > 12 || !Number.isInteger(courseWidth) || courseWidth < 14 || courseWidth > 28 || !Number.isInteger(courseHeight) || courseHeight < 10 || courseHeight > 20 || courseWidth * courseHeight > 560 || (botSkill !== 'adaptive' && (!Number.isInteger(botSkill) || botSkill < 1 || botSkill > 10))) return undefined;
  return { seed, holeCount, botCount, botSkill, maxHumans, courseWidth, courseHeight, skipVoting };
};
const validCommand = (value: unknown): GameCommand | undefined => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return undefined;
  const source = value as Record<string, unknown>;
  if (source.type === 'shoot' && source.shot && typeof source.shot === 'object') {
    const shot = source.shot as Record<string, unknown>;
    const kind = shot.kind === 'chip' ? 'chip' : shot.kind === undefined || shot.kind === 'putt' ? 'putt' : undefined;
    return typeof shot.angle === 'number' && typeof shot.power === 'number' && Number.isFinite(shot.angle) && Number.isFinite(shot.power) && kind ? { type: 'shoot', shot: { angle: shot.angle, power: shot.power, kind } } : undefined;
  }
  if (source.type === 'cast-vote' && typeof source.playerId === 'string' && typeof source.optionId === 'string' && source.playerId.length <= 24 && source.optionId.length <= 80) return { type: 'cast-vote', playerId: source.playerId, optionId: source.optionId };
  if (source.type === 'set-paused' && typeof source.paused === 'boolean') return { type: 'set-paused', paused: source.paused };
  if (source.type === 'arm-second-wind') return { type: 'arm-second-wind' };
  if (source.type === 'emote' && typeof source.playerId === 'string' && typeof source.emote === 'string' && emotes.has(source.emote)) return { type: 'emote', playerId: source.playerId, emote: source.emote as Emote };
  if (source.type === 'use-power-up' && typeof source.powerUp === 'string' && powerUps.has(source.powerUp)) {
    const targetId = typeof source.targetId === 'string' && source.targetId.length <= 24 ? source.targetId : undefined;
    const portalExitId = typeof source.portalExitId === 'string' && source.portalExitId.length <= 80 ? source.portalExitId : undefined;
    const rawPlacement = source.placement;
    const placement = rawPlacement && typeof rawPlacement === 'object' && !Array.isArray(rawPlacement)
      && Number.isInteger((rawPlacement as Record<string, unknown>).x) && Number.isInteger((rawPlacement as Record<string, unknown>).y)
      ? { x: Number((rawPlacement as Record<string, unknown>).x), y: Number((rawPlacement as Record<string, unknown>).y) }
      : undefined;
    return { type: 'use-power-up', powerUp: source.powerUp as PowerUp, targetId, portalExitId, placement };
  }
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
      }, 1_650);
    }
    return;
  }
  if (game.status === 'voting' && game.vote) {
    const bot = game.players.find((player) => player.kind === 'bot' && !game.vote!.ballots[player.id]);
    if (!bot || current.botFor === `vote:${bot.id}`) return;
    if (current.botTimeout) clearTimeout(current.botTimeout);
    current.botFor = `vote:${bot.id}`;
    current.botTimeout = setTimeout(() => {
      current.botTimeout = undefined;
      current.botFor = undefined;
      const latest = rooms.get(room.code);
      const vote = latest?.game?.vote;
      if (!latest?.game || latest.game.paused || latest.game.status !== 'voting' || !vote || vote.ballots[bot.id]) return;
      const option = vote.options[(bot.id.length + latest.game.hole) % vote.options.length]!;
      updateGame(latest, applyCommand(latest.game, { type: 'cast-vote', playerId: bot.id, optionId: option.id }));
    }, 520);
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
    const decision = botMove(latest.game);
    if (!decision) return;
    let next = latest.game;
    if (decision.secondWind) next = applyCommand(next, { type: 'arm-second-wind' });
    if (decision.powerUp) next = applyCommand(next, { type: 'use-power-up', powerUp: decision.powerUp.type, targetId: decision.powerUp.targetId, portalExitId: decision.powerUp.portalExitId, placement: decision.powerUp.placement });
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
  if (command.type === 'cast-vote') return command.playerId === session.playerId ? undefined : 'you can only cast your own ballot';
  if (command.type === 'emote') return command.playerId === session.playerId ? undefined : 'you can only send your own emote';
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
  const game = createGame({ seed: room.config.seed, holeCount: room.config.holeCount, botCount: room.config.botCount, botSkill: room.config.botSkill, humanCount: room.members.length, courseWidth: room.config.courseWidth, courseHeight: room.config.courseHeight, skipVoting: room.config.skipVoting === true });
  game.players.filter((player) => player.kind === 'human').forEach((player, index) => { player.name = room.members[index]!.name; });
  room.phase = 'game';
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

const handleMessage = (session: Session, value: unknown) => {
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
        room.config.skipVoting ??= false;
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
    try { handleMessage(session, JSON.parse(raw.toString())); } catch { report(session, 'invalid JSON'); }
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
    if (!room.game || room.game.paused || room.game.status !== 'playing') return;
    const next = tickTurn(room.game, .25);
    if (next !== room.game) updateGame(room, next);
  });
}, 250).unref();

httpServer.listen(port, () => console.log(`Golf server listening on :${port}`));
