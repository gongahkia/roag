import { describe, expect, it } from 'vitest';
import { aimFromPull, PULL_MAX_DISTANCE, shouldScheduleBotAfterTick } from '../src/ui/app-controller';
import type { GameState } from '../src/core/types';

const snapshot = (status: GameState['status'], playerIndex: number) => ({ status, turn: { playerIndex } });

describe('bot scheduling', () => {
  it('maps a pull-back vector to the opposite course aim and bounded strength', () => {
    const aim = aimFromPull({ angle: 0, power: 4, kind: 'putt' }, -PULL_MAX_DISTANCE, 0);
    expect(Math.abs(aim.angle)).toBe(0);
    expect(aim.power).toBe(8);
    expect(aimFromPull({ angle: .4, power: 3, kind: 'chip' }, 0, 0)).toEqual({ angle: .4, power: 3, kind: 'chip' });
    expect(aimFromPull({ angle: 0, power: 1, kind: 'putt' }, 0, PULL_MAX_DISTANCE).angle).toBeCloseTo(-Math.PI / 2);
  });

  it('preserves a pending bot timeout while only the active turn countdown changes', () => {
    expect(shouldScheduleBotAfterTick(snapshot('playing', 1), snapshot('playing', 1))).toBe(false);
  });

  it('schedules a bot after a turn or game-phase transition', () => {
    expect(shouldScheduleBotAfterTick(snapshot('playing', 0), snapshot('playing', 1))).toBe(true);
    expect(shouldScheduleBotAfterTick(snapshot('playing', 3), snapshot('rolling', 0))).toBe(true);
  });
});
