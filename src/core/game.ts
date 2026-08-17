import { chooseBotDecision, type BotDecision } from './bots';
import { activePlayer, castVote, cloneGameState, createGameState, defaultConfig } from './game-state';
import { UPGRADE_DESCRIPTIONS } from './player-effects';
import { armSecondWind, usePowerUp } from './powerups';
import { previewShot, resolveShot, tickTurn } from './turns';
import type { GameCommand, GameConfig, GameState, GameTransport } from './types';

export { UPGRADE_DESCRIPTIONS, defaultConfig, previewShot, tickTurn };

export const createGame = (config: GameConfig): GameState => createGameState(config);

export const applyCommand = (current: GameState, command: GameCommand): GameState => {
  const state = cloneGameState(current);
  if (command.type === 'cast-vote') {
    castVote(state, command.playerId, command.optionId);
    return state;
  }
  if (command.type === 'shoot') {
    resolveShot(state, command.shot);
    return state;
  }
  if (command.type === 'use-power-up' && state.status === 'playing') {
    usePowerUp(state, command.powerUp, command.targetId, command.portalExitId);
    return state;
  }
  if (command.type === 'arm-second-wind' && state.status === 'playing') {
    armSecondWind(state);
    return state;
  }
  if (command.type === 'emote') {
    const player = state.players.find((candidate) => candidate.id === command.playerId);
    if (player) {
      state.emoteSequence += 1;
      state.emotes = [...state.emotes, { id: `${state.course.seed}:${command.playerId}:${state.emoteSequence}`, playerId: command.playerId, emote: command.emote }].slice(-16);
    }
  }
  return state;
};

export const botMove = (state: GameState): BotDecision | undefined => {
  const player = activePlayer(state);
  if (player.kind !== 'bot' || state.status !== 'playing' || state.turn.shotInFlight) return undefined;
  return chooseBotDecision(state.course, player, state.players, state.coursePhase, state.holeRules);
};

export class LocalTransport implements GameTransport {
  private listeners = new Set<(command: GameCommand) => void>();
  send(command: GameCommand) { this.listeners.forEach((listener) => listener(command)); }
  onCommand(listener: (command: GameCommand) => void) { this.listeners.add(listener); return () => this.listeners.delete(listener); }
}
