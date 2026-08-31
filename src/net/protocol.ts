import type { GameCommand, GameConfig, GameState } from '../core/types';

export interface LobbyConfig {
  seed: string;
  holeCount: number;
  botCount: number;
  botSkill: GameConfig['botSkill'];
  maxHumans: number;
  courseWidth: number;
  courseHeight: number;
  skipDieBets: boolean;
  ruleset: NonNullable<GameConfig['ruleset']>;
}

export interface LobbyMember {
  id: string;
  name: string;
  slot: number;
  connected: boolean;
  host: boolean;
}

export interface RoomSnapshot {
  code: string;
  hostId: string;
  config: LobbyConfig;
  members: LobbyMember[];
  phase: 'lobby' | 'game';
  game?: GameState;
  updatedAt: number;
}

/** High-frequency authoritative clock data; full course snapshots are sent only after discrete changes. */
export interface RoomClock {
  roomCode: string;
  status: GameState['status'];
  turnSecondsLeft?: number;
  hazardElapsedMs: number;
  die?: {
    phase: NonNullable<GameState['die']>['phase'];
    secondsLeft: number;
    rollSecondsLeft?: number;
    revealedSecondsLeft?: number;
    rerollPotSecondsLeft?: number;
  };
}

export type ClientMessage =
  | { type: 'create-room'; name: string; config: LobbyConfig; passphrase: string }
  | { type: 'join-room'; code: string; name: string; passphrase?: string; reconnectToken?: string }
  | { type: 'start-room' }
  | { type: 'command'; command: GameCommand }
  | { type: 'leave-room' };

export type ServerMessage =
  | { type: 'joined'; playerId: string; reconnectToken: string }
  | { type: 'room-state'; room: RoomSnapshot }
  | { type: 'room-tick'; clock: RoomClock }
  | { type: 'error'; message: string };
