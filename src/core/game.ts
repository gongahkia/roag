import { chooseBotDecision, type BotDecision } from './bots';
import { activePlayer, addDieSide, augmentDieFace, cloneGameState, completeTransition, createGameState, defaultConfig, readyDieRoll, setPaused } from './game-state';
import { UPGRADE_DESCRIPTIONS } from './player-effects';
import { armSecondWind, usePowerUp } from './powerups';
import { buyShopOffer, sellShopCaddy, skipShopBuyer, voteShopReroll } from './shop';
import { previewShot, resolveShot, tickTurn } from './turns';
import type { GameCommand, GameConfig, GameState, GameTransport } from './types';

export { UPGRADE_DESCRIPTIONS, defaultConfig, previewShot, tickTurn };

export const createGame = (config: GameConfig): GameState => createGameState(config);

export const applyCommand = (current: GameState, command: GameCommand): GameState => {
  const state = cloneGameState(current);
  if (command.type === 'add-die-side') {
    addDieSide(state, command.playerId);
    return state;
  }
  if (command.type === 'augment-die-face') {
    augmentDieFace(state, command.playerId, command.faceId);
    return state;
  }
  if (command.type === 'ready-die-roll') {
    readyDieRoll(state, command.playerId);
    return state;
  }
  if (command.type === 'complete-transition') {
    completeTransition(state);
    return state;
  }
  if (command.type === 'set-paused') {
    setPaused(state, command.paused);
    return state;
  }
  if (command.type === 'shoot') {
    resolveShot(state, command.shot);
    return state;
  }
  if (command.type === 'use-power-up' && state.status === 'playing') {
    usePowerUp(state, command.powerUp, command.targetId, command.portalExitId, command.placement, command.cardId);
    return state;
  }
  if (command.type === 'arm-second-wind' && state.status === 'playing') {
    armSecondWind(state);
    return state;
  }
  if (command.type === 'shop-vote-reroll') {
    voteShopReroll(state, command.playerId, command.approve);
    return state;
  }
  if (command.type === 'shop-buy') {
    buyShopOffer(state, command.playerId, command.offerId, command.replaceCaddyId);
    return state;
  }
  if (command.type === 'shop-sell-caddy') {
    sellShopCaddy(state, command.playerId, command.caddyId);
    return state;
  }
  if (command.type === 'shop-skip') {
    skipShopBuyer(state, command.playerId);
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
  if (player.kind !== 'bot' || state.status !== 'playing' || state.paused || state.turn.shotInFlight) return undefined;
  return chooseBotDecision(state.course, player, state.players, state.hazardElapsedMs, state.holeRules, state.gadgets ?? []);
};

export class LocalTransport implements GameTransport {
  private listeners = new Set<(command: GameCommand) => void>();
  send(command: GameCommand) { this.listeners.forEach((listener) => listener(command)); }
  onCommand(listener: (command: GameCommand) => void) { this.listeners.add(listener); return () => this.listeners.delete(listener); }
}
