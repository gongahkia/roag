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

export type ClientMessage =
  | { type: 'create-room'; name: string; config: LobbyConfig }
  | { type: 'join-room'; code: string; name: string; reconnectToken?: string }
  | { type: 'start-room' }
  | { type: 'command'; command: GameCommand }
  | { type: 'leave-room' };

export type ServerMessage =
  | { type: 'joined'; playerId: string; reconnectToken: string }
  | { type: 'room-state'; room: RoomSnapshot }
  | { type: 'error'; message: string };
