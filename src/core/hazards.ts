import type { Course, GateHazard, Point, SweeperHazard } from './types';

export const COURSE_PHASES = 8;
export const HAZARD_PHASE_DURATION_MS = 1_000;
export const STANDARD_HAZARD_PERIOD_MS = 8_000;

type MovingHazard = SweeperHazard | GateHazard;

export type HazardMotionSpeed = 'slow' | 'standard' | 'fast';

export const motionPeriodMsFor = (hazard: MovingHazard) => hazard.motionPeriodMs ?? STANDARD_HAZARD_PERIOD_MS;

export const motionSpeedFor = (hazard: MovingHazard): HazardMotionSpeed => {
  const period = motionPeriodMsFor(hazard);
  if (period <= 6_000) return 'fast';
  if (period >= 12_000) return 'slow';
  return 'standard';
};

export const motionColorFor = (hazard: MovingHazard) => ({ slow: '#5ea9de', standard: '#f0a232', fast: '#dc5a49' })[motionSpeedFor(hazard)];

export const phaseAt = (phase: number, offset = 0, phaseCount = COURSE_PHASES) => (phase + offset + phaseCount) % phaseCount;

export const elapsedMsForPhase = (phase: number) => phase * HAZARD_PHASE_DURATION_MS;

export const hazardPhaseAt = (hazard: MovingHazard, elapsedMs: number, phaseCount = COURSE_PHASES) => phaseAt(elapsedMs / motionPeriodMsFor(hazard) * phaseCount, hazard.phaseOffset, phaseCount);

export const isGateOpen = (hazard: GateHazard, elapsedMs: number, phaseCount = COURSE_PHASES) => {
  const phase = Math.floor(hazardPhaseAt(hazard, elapsedMs, phaseCount));
  return phase === 0 || phase === 1 || phase === Math.floor(phaseCount / 2) || phase === Math.floor(phaseCount / 2) + 1;
};

export const closedGateAt = (course: Course, x: number, y: number, elapsedMs: number, phaseCount = COURSE_PHASES) => course.hazards.some((hazard) => hazard.kind === 'gate' && !isGateOpen(hazard, elapsedMs, phaseCount) && hazard.point.x === Math.floor(x) && hazard.point.y === Math.floor(y));

export const sweeperDirection = (hazard: SweeperHazard, elapsedMs: number, phaseCount = COURSE_PHASES): Point => {
  const angle = hazardPhaseAt(hazard, elapsedMs, phaseCount) * Math.PI * 2 / phaseCount;
  return { x: Math.cos(angle), y: Math.sin(angle) };
};
