import { describe, expect, it } from 'vitest';
import { elapsedMsForPhase, isGateOpen, motionColorFor, motionPeriodMsFor, sweeperDirection } from '../src/core/hazards';
import { createGame, defaultConfig, tickTurn } from '../src/core/game';
import { defaultTerrainSettings, generateCourse } from '../src/core/generator';
import { normalizeGameState } from '../src/core/game-state';
import { newBall, simulateShot } from '../src/core/physics';
import type { GameState, GateHazard, SweeperHazard } from '../src/core/types';
import { createLane as lane } from './fixtures';

describe('real-time moving hazards', () => {
  const sweeper = (motionPeriodMs: number): SweeperHazard => ({ id: `sweeper-${motionPeriodMs}`, kind: 'sweeper', point: { x: 3, y: 3 }, phaseOffset: 0, radius: .8, motionPeriodMs });
  const gate = (motionPeriodMs: number): GateHazard => ({ id: `gate-${motionPeriodMs}`, kind: 'gate', point: { x: 3, y: 3 }, phaseOffset: 0, motionPeriodMs });

  it('uses deterministic slow, standard, and fast motion periods with matching colors', () => {
    expect(motionPeriodMsFor(sweeper(12_000))).toBe(12_000);
    expect(motionPeriodMsFor(sweeper(8_000))).toBe(8_000);
    expect(motionPeriodMsFor(sweeper(6_000))).toBe(6_000);
    expect(motionPeriodMsFor({ ...sweeper(8_000), motionPeriodMs: undefined })).toBe(8_000);
    expect(motionColorFor(sweeper(12_000))).toBe('#5ea9de');
    expect(motionColorFor(sweeper(8_000))).toBe('#f0a232');
    expect(motionColorFor(sweeper(6_000))).toBe('#dc5a49');
  });

  it('moves sweepers continuously and gates between open and closed windows', () => {
    const fast = sweeper(6_000);
    const slow = sweeper(12_000);
    expect(sweeperDirection(fast, 1_500).x).toBeCloseTo(0, 6);
    expect(sweeperDirection(fast, 1_500).y).toBeCloseTo(1, 6);
    expect(sweeperDirection(slow, 3_000).x).toBeCloseTo(0, 6);
    expect(sweeperDirection(slow, 3_000).y).toBeCloseTo(1, 6);
    expect(isGateOpen(gate(6_000), 0)).toBe(true);
    expect(isGateOpen(gate(6_000), 1_500)).toBe(false);
    expect(isGateOpen(gate(6_000), 3_000)).toBe(true);
  });

  it('advances obstacle timing inside a shot', () => {
    const course = lane('fairway', 28);
    course.hazards = [sweeper(6_000)];
    const start = newBall(course);
    const early = simulateShot(course, start, { angle: 0, power: 4 }, 1, { hazardElapsedMs: 0 });
    const late = simulateShot(course, start, { angle: 0, power: 4 }, 1, { hazardElapsedMs: 1_500 });
    const frozenCourse = lane('fairway', 28);
    frozenCourse.hazards = [sweeper(Number.MAX_SAFE_INTEGER)];
    const frozen = simulateShot(frozenCourse, start, { angle: 0, power: 4 }, 1, { hazardElapsedMs: 0 });
    expect(early.ball).not.toEqual(late.ball);
    expect(early.ball).not.toEqual(frozen.ball);
  });

  it('advances only during unpaused active play and keeps legacy snapshots readable', () => {
    const game = createGame({ ...defaultConfig(), seed: 'live-clock', holeCount: 1, humanCount: 1, botCount: 0 });
    const running = tickTurn(game, .5);
    expect(running.hazardElapsedMs).toBeCloseTo(500);
    expect(running.coursePhase).toBe(0);
    const pausedState = { ...running, paused: true };
    const paused = tickTurn(pausedState, 2);
    expect(paused).toBe(pausedState);
    expect(paused.hazardElapsedMs).toBe(running.hazardElapsedMs);
    const legacy = { ...game, hazardElapsedMs: undefined } as unknown as GameState;
    legacy.coursePhase = 3;
    expect(normalizeGameState(legacy).hazardElapsedMs).toBe(elapsedMsForPhase(3));
  });

  it('assigns reproducible per-obstacle motion periods during generation', () => {
    const settings = { ...defaultTerrainSettings(), sweeperCount: 3, gateCount: 3 };
    const first = generateCourse('motion-periods', settings);
    const second = generateCourse('motion-periods', settings);
    const periods = first.hazards.filter((hazard): hazard is SweeperHazard | GateHazard => hazard.kind === 'sweeper' || hazard.kind === 'gate').map((hazard) => hazard.motionPeriodMs);
    expect(periods).toHaveLength(6);
    expect(periods.every((period) => period === 6_000 || period === 8_000 || period === 12_000)).toBe(true);
    expect(first.hazards).toEqual(second.hazards);
  });
});
