import { describe, expect, it } from 'vitest';
import { powerAfterWheel, shouldScheduleBotAfterTick } from '../src/ui/app-controller';
import type { GameState } from '../src/core/types';

const snapshot = (status: GameState['status'], playerIndex: number) => ({ status, turn: { playerIndex } });

describe('bot scheduling', () => {
  it('uses the mouse wheel as a bounded half-step strength control', () => {
    expect(powerAfterWheel(4, -1)).toBe(4.5);
    expect(powerAfterWheel(4, 1)).toBe(3.5);
    expect(powerAfterWheel(8, -1)).toBe(8);
    expect(powerAfterWheel(1, 1)).toBe(1);
    expect(powerAfterWheel(4, 0)).toBe(4);
  });

  it('preserves a pending bot timeout while only the active turn countdown changes', () => {
    expect(shouldScheduleBotAfterTick(snapshot('playing', 1), snapshot('playing', 1))).toBe(false);
  });

  it('schedules a bot after a turn or game-phase transition', () => {
    expect(shouldScheduleBotAfterTick(snapshot('playing', 0), snapshot('playing', 1))).toBe(true);
    expect(shouldScheduleBotAfterTick(snapshot('playing', 3), snapshot('voting', 0))).toBe(true);
  });
});
