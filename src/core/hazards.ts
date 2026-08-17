import type { Course, GateHazard, Point, SweeperHazard } from './types';

export const COURSE_PHASES = 8;

export const phaseAt = (phase: number, offset = 0, phaseCount = COURSE_PHASES) => (phase + offset + phaseCount) % phaseCount;

export const isGateOpen = (hazard: GateHazard, phase: number, phaseCount = COURSE_PHASES) => {
  const localPhase = phaseAt(phase, hazard.phaseOffset, phaseCount);
  return localPhase === 0 || localPhase === 1 || localPhase === Math.floor(phaseCount / 2) || localPhase === Math.floor(phaseCount / 2) + 1;
};

export const closedGateAt = (course: Course, x: number, y: number, phase: number, phaseCount = COURSE_PHASES) => course.hazards.some((hazard) => hazard.kind === 'gate' && !isGateOpen(hazard, phase, phaseCount) && hazard.point.x === Math.floor(x) && hazard.point.y === Math.floor(y));

export const sweeperDirection = (hazard: SweeperHazard, phase: number, phaseCount = COURSE_PHASES): Point => {
  const angle = phaseAt(phase, hazard.phaseOffset, phaseCount) * Math.PI * 2 / phaseCount;
  return { x: Math.cos(angle), y: Math.sin(angle) };
};
