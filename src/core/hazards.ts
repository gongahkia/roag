import type { Course, GateHazard, Point, SweeperHazard } from './types';

export const COURSE_PHASES = 8;

export const phaseAt = (phase: number, offset = 0) => (phase + offset + COURSE_PHASES) % COURSE_PHASES;

export const isGateOpen = (hazard: GateHazard, phase: number) => {
  const localPhase = phaseAt(phase, hazard.phaseOffset);
  return localPhase === 0 || localPhase === 1 || localPhase === 4 || localPhase === 5;
};

export const closedGateAt = (course: Course, x: number, y: number, phase: number) => course.hazards.some((hazard) => hazard.kind === 'gate' && !isGateOpen(hazard, phase) && hazard.point.x === Math.floor(x) && hazard.point.y === Math.floor(y));

export const sweeperDirection = (hazard: SweeperHazard, phase: number): Point => {
  const angle = phaseAt(phase, hazard.phaseOffset) * Math.PI / 4;
  return { x: Math.cos(angle), y: Math.sin(angle) };
};
