import { describe, expect, it } from 'vitest';
import { resolveArchitectContracts } from '../src/core/construction';
import { applyCommand, createGame, defaultConfig, tickTurn } from '../src/core/game';
import { validateCourse } from '../src/core/generator';

const coursewrightConfig = (seed: string) => ({ ...defaultConfig(), seed, ruleset: 'coursewright' as const, humanCount: 2, botCount: 0, holeCount: 2 });

const finishBuild = (initial: ReturnType<typeof createGame>) => {
  let state = initial;
  while (state.status === 'building') {
    const builder = state.players[state.turn.playerIndex]!;
    const pieceId = state.construction?.hands[builder.id]?.[0];
    const socketId = state.course.buildSockets?.find((socket) => !socket.pieceId)?.id;
    expect(pieceId).toBeDefined();
    expect(socketId).toBeDefined();
    state = applyCommand(state, { type: 'place-build-piece', pieceId: pieceId!, socketId: socketId! });
  }
  return state;
};

describe('Coursewright construction', () => {
  it('opens a deterministic shell with exactly two snake-order placements per golfer', () => {
    const first = createGame(coursewrightConfig('socket-seed'));
    const second = createGame(coursewrightConfig('socket-seed'));
    expect(first.status).toBe('building');
    expect(first.config.holeCount).toBe(2);
    expect(first.course.buildSockets).toHaveLength(4);
    expect(first.course.buildSockets?.every((socket) => !socket.pieceId)).toBe(true);
    expect(first.construction).toEqual(second.construction);
    expect(first.course.buildSockets).toEqual(second.course.buildSockets);
    expect(first.construction?.placementOrder).toEqual(['human-0', 'human-1', 'human-1', 'human-0']);
  });

  it('installs only legal pieces, completes the build phase, and retains a valid course', () => {
    const opening = createGame(coursewrightConfig('legal-build'));
    const rejected = applyCommand(opening, { type: 'place-build-piece', pieceId: 'bank', socketId: 'missing-socket' });
    expect(rejected).toEqual(opening);
    const ready = finishBuild(opening);
    expect(ready.status).toBe('playing');
    expect(ready.course.buildSockets?.every((socket) => socket.ownerId && socket.pieceId)).toBe(true);
    expect(validateCourse(ready.course)).toEqual({ valid: true, failures: [] });
  });

  it('uses a deterministic fallback when a builder timer expires', () => {
    const opening = createGame(coursewrightConfig('build-timeout'));
    const advanced = tickTurn(opening, 28);
    expect(advanced.status).toBe('building');
    expect(advanced.course.buildSockets?.filter((socket) => socket.pieceId)).toHaveLength(1);
    expect(advanced.construction?.placementIndex).toBe(1);
  });

  it('reveals a completed architect contract only after its module affects play', () => {
    const state = finishBuild(createGame(coursewrightConfig('contract-reveal')));
    const contract = state.construction!.contracts[0]!;
    const socket = state.course.buildSockets!.find((candidate) => candidate.ownerId === contract.ownerId)!;
    socket.pieceId = 'splitter';
    const shooterId = state.players.find((player) => player.id !== contract.ownerId)!.id;
    const completed = resolveArchitectContracts(state, shooterId, {
      frames: [{ ball: { x: socket.point.x + .5, y: socket.point.y + .5, z: .25 } }],
      ricochetCount: 1,
      airtimeSeconds: .3,
    });
    expect(completed.map((candidate) => candidate.id)).toContain(contract.id);
    expect(contract).toMatchObject({ completed: true, revealed: true });
  });
});
