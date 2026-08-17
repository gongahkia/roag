import { describe, expect, it } from 'vitest';
import { shouldScheduleBotAfterTick } from '../src/ui/app-controller';
import type { GameState } from '../src/core/types';

const snapshot = (status: GameState['status'], playerIndex: number) => ({ status, turn: { playerIndex } });

describe('bot scheduling', () => {
  it('preserves a pending bot timeout while only the active turn countdown changes', () => {
    expect(shouldScheduleBotAfterTick(snapshot('playing', 1), snapshot('playing', 1))).toBe(false);
  });

  it('schedules a bot after a turn or game-phase transition', () => {
    expect(shouldScheduleBotAfterTick(snapshot('playing', 0), snapshot('playing', 1))).toBe(true);
    expect(shouldScheduleBotAfterTick(snapshot('playing', 3), snapshot('voting', 0))).toBe(true);
  });
});
