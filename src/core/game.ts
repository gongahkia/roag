import { chooseBotDecision, type BotDecision } from './bots';
import { applyBuildSettings, beginValidation, generateBuildTerrain, placeBuildTool, randomizeBuildTerrain, selectCompetitivePerk } from './course-builder';
import { activePlayer, cloneGameState, createGameState, defaultConfig } from './game-state';
import { UPGRADE_DESCRIPTIONS } from './player-effects';
import { armSecondWind, usePowerUp } from './powerups';
import { beginCourse, previewShot, resolveShot, tickTurn } from './turns';
import type { GameCommand, GameConfig, GameState, GameTransport } from './types';

export { UPGRADE_DESCRIPTIONS, defaultConfig, beginCourse, previewShot, tickTurn };

export const createGame = (config: GameConfig): GameState => createGameState(config);

export const applyCommand = (current: GameState, command: GameCommand): GameState => {
  const state = cloneGameState(current);
  if (command.type === 'build-settings' && state.status === 'build' && state.build) {
    applyBuildSettings(state.build, command);
    return state;
  }
  if (command.type === 'build-place' && state.status === 'build') {
    placeBuildTool(state, command.point);
    return state;
  }
  if (command.type === 'build-generate' && state.status === 'build') {
    generateBuildTerrain(state);
    return state;
  }
  if (command.type === 'build-randomize' && state.status === 'build') {
    randomizeBuildTerrain(state);
    return state;
  }
  if (command.type === 'select-upgrade' && state.status === 'build') {
    selectCompetitivePerk(state, command.upgrade);
    return state;
  }
  if (command.type === 'begin-validation' && state.status === 'build') {
    beginValidation(state);
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
  if (player.kind !== 'bot' || (state.status !== 'playing' && state.status !== 'validate') || state.turn.shotInFlight) return undefined;
  return chooseBotDecision(state.course, player, state.players, state.coursePhase);
};

export class LocalTransport implements GameTransport {
  private listeners = new Set<(command: GameCommand) => void>();
  send(command: GameCommand) { this.listeners.forEach((listener) => listener(command)); }
  onCommand(listener: (command: GameCommand) => void) { this.listeners.add(listener); return () => this.listeners.delete(listener); }
}
