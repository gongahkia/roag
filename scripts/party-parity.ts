import { spawn } from 'node:child_process';
import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { WebSocket } from 'ws';
import type { ClientMessage, RoomSnapshot, ServerMessage } from '../src/net/protocol';

interface Client {
  socket: WebSocket;
  messages: ServerMessage[];
}

const port = Number(process.env.PARTY_PARITY_PORT ?? 8791);
const url = `ws://127.0.0.1:${port}`;
const timeout = <T>(promise: Promise<T>, label: string) => new Promise<T>((resolve, reject) => {
  const timer = setTimeout(() => reject(new Error(`timed out waiting for ${label}`)), 20_000);
  promise.then((value) => { clearTimeout(timer); resolve(value); }, (error) => { clearTimeout(timer); reject(error); });
});
const onceMessage = <T extends ServerMessage>(client: Client, predicate: (message: ServerMessage) => message is T) => {
  const cached = client.messages.find(predicate);
  if (cached) return Promise.resolve(cached);
  return new Promise<T>((resolve) => {
    const listener = (raw: WebSocket.RawData) => {
      const message = JSON.parse(raw.toString()) as ServerMessage;
      client.messages.push(message);
      if (!predicate(message)) return;
      client.socket.off('message', listener);
      resolve(message);
    };
    client.socket.on('message', listener);
  });
};
const connect = async (): Promise<Client> => {
  const socket = new WebSocket(url, { headers: { Origin: 'http://127.0.0.1:5173' } });
  const client = { socket, messages: [] };
  await timeout(new Promise<void>((resolve, reject) => { socket.once('open', () => resolve()); socket.once('error', reject); }), 'socket open');
  socket.on('message', (raw) => {
    const message = JSON.parse(raw.toString()) as ServerMessage;
    client.messages.push(message);
  });
  return client;
};
const send = (client: Client, message: ClientMessage) => client.socket.send(JSON.stringify(message));
const nextRoomState = (client: Client) => new Promise<Extract<ServerMessage, { type: 'room-state' }>>((resolve) => {
  const listener = (raw: WebSocket.RawData) => {
    const message = JSON.parse(raw.toString()) as ServerMessage;
    if (message.type !== 'room-state') return;
    client.socket.off('message', listener);
    resolve(message);
  };
  client.socket.on('message', listener);
});
const joined = (client: Client) => onceMessage(client, (message): message is Extract<ServerMessage, { type: 'joined' }> => message.type === 'joined');

const main = async () => {
  const databaseDirectory = await mkdtemp(path.join(tmpdir(), 'golf-party-parity-'));
  const server = spawn(process.execPath, ['node_modules/tsx/dist/cli.mjs', 'server/index.ts'], {
    env: { ...process.env, PORT: String(port), HOST: '127.0.0.1', APP_ORIGINS: 'http://127.0.0.1:5173', GAME_DATABASE: path.join(databaseDirectory, 'rooms.sqlite') },
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let host: Client | undefined;
  let guest: Client | undefined;
  let reconnected: Client | undefined;
  try {
    await timeout(new Promise<void>((resolve, reject) => {
      server.stdout.on('data', (buffer) => { if (buffer.toString().includes('server_started')) resolve(); });
      server.once('error', reject);
      server.once('exit', (code) => reject(new Error(`server exited before ready (${code ?? 'unknown'})`)));
    }), 'authoritative server');
    host = await connect();
    const createdWait = nextRoomState(host);
    send(host, { type: 'create-room', name: 'host', passphrase: 'party-passphrase', config: { seed: 'party-parity', holeCount: 1, botCount: 0, botSkill: 5, maxHumans: 2, courseWidth: 20, courseHeight: 14, skipDieBets: false, ruleset: 'party' } });
    const hostJoined = await timeout(joined(host), 'host token');
    const created = await timeout(createdWait, 'created room');
    const code = created.room.code;
    guest = await connect();
    const hostLobbyWait = nextRoomState(host);
    const guestLobbyWait = nextRoomState(guest);
    send(guest, { type: 'join-room', code, name: 'guest', passphrase: 'party-passphrase' });
    const guestJoined = await timeout(joined(guest), 'guest token');
    await timeout(hostLobbyWait, 'host lobby sync');
    await timeout(guestLobbyWait, 'guest lobby sync');
    const startWait = nextRoomState(host);
    send(host, { type: 'start-room' });
    const started = await timeout(startWait, 'started room');
    const game = started.room.game!;
    send(host, { type: 'command', command: { type: 'ready-slot-spin', playerId: hostJoined.playerId } });
    send(guest, { type: 'command', command: { type: 'ready-slot-spin', playerId: guestJoined.playerId } });
    const playingHost = await timeout(new Promise<RoomSnapshot>((resolve) => {
      const check = (message: ServerMessage) => { if (message.type === 'room-state' && message.room.game?.status === 'playing') resolve(message.room); };
      host!.socket.on('message', (raw) => check(JSON.parse(raw.toString()) as ServerMessage));
    }), 'resolved Party Rules reveal');
    const playingGuest = await timeout(new Promise<RoomSnapshot>((resolve) => {
      const latest = guest!.messages.filter((message): message is Extract<ServerMessage, { type: 'room-state' }> => message.type === 'room-state').map((message) => message.room).find((room) => room.game?.status === 'playing');
      if (latest) resolve(latest);
      else guest!.socket.on('message', (raw) => { const message = JSON.parse(raw.toString()) as ServerMessage; if (message.type === 'room-state' && message.room.game?.status === 'playing') resolve(message.room); });
    }), 'guest Party Rules reveal');
    const hostHash = playingHost.game!.coursePlan[0]!.recipe.metadata?.courseHash;
    const guestHash = playingGuest.game!.coursePlan[0]!.recipe.metadata?.courseHash;
    if (!hostHash || hostHash !== guestHash || game.config.ruleset !== 'party') throw new Error('clients did not receive the same Party Rules recipe hash');
    guest.socket.close();
    reconnected = await connect();
    const restoredWait = nextRoomState(reconnected);
    send(reconnected, { type: 'join-room', code, name: 'guest', reconnectToken: guestJoined.reconnectToken });
    await timeout(joined(reconnected), 'reconnect token');
    const restored = await timeout(restoredWait, 'reconnected game state');
    if (restored.room.game?.coursePlan[0]?.recipe.metadata?.courseHash !== hostHash) throw new Error('reconnect did not restore the current recipe hash');
    process.stdout.write(`${JSON.stringify({ ok: true, room: code, ruleset: game.config.ruleset, recipeHash: hostHash, reconnect: true, slotReadyPlayers: playingHost.game!.players.length }, null, 2)}\n`);
  } finally {
    host?.socket.close();
    guest?.socket.close();
    reconnected?.socket.close();
    server.kill('SIGTERM');
    await new Promise((resolve) => server.once('exit', resolve));
    await rm(databaseDirectory, { recursive: true, force: true });
  }
};

main().catch((error) => {
  process.stderr.write(`${error instanceof Error ? error.message : String(error)}\n`);
  process.exitCode = 1;
});
