import { distanceToCup, simulateShot } from './physics';
import { Random } from './random';
import type { Course, Player, PowerUp, ShotCommand } from './types';

export interface BotDecision {
  shot: ShotCommand;
  powerUp?: { type: PowerUp; targetId?: string };
  confidence: number;
}

const clampedSkill = (player: Player, opponents: Player[]) => {
  if (player.skill !== 'adaptive') return player.skill;
  const average = opponents.reduce((total, opponent) => total + opponent.total, 0) / Math.max(1, opponents.length);
  return Math.max(1, Math.min(10, Math.round(6 + (average - player.total) / 4)));
};

const rankedTarget = (bot: Player, players: Player[]): Player | undefined => players
  .filter((player) => player.id !== bot.id && !player.ball.complete)
  .sort((left, right) => left.total + left.ball.strokes - (right.total + right.ball.strokes))[0];

export const chooseBotDecision = (course: Course, bot: Player, players: Player[], phase = 0): BotDecision => {
  const skill = clampedSkill(bot, players.filter((player) => player.id !== bot.id));
  const random = new Random(`${course.seed}:${bot.id}:${bot.ball.strokes}`);
  const cup = { x: course.cup.x + 0.5, y: course.cup.y + 0.5 };
  const baseAngle = Math.atan2(cup.y - bot.ball.y, cup.x - bot.ball.x);
  const angleStep = 0.24 - skill * 0.018;
  const powerStep = 0.75 - skill * 0.055;
  const sampleCount = 5 + skill * 4;
  const candidates: { shot: ShotCommand; score: number }[] = [];

  for (let index = 0; index < sampleCount; index += 1) {
    const angle = baseAngle + (index - (sampleCount - 1) / 2) * angleStep;
    for (let power = 2; power <= 7.5; power += powerStep) {
      const shot = { angle, power };
      const result = simulateShot(course, bot.ball, shot, 10, { phase });
      const score = (result.holed ? -1000 : distanceToCup(course, result.ball) * 8)
        + result.ball.resetCount * 45
        + Math.max(0, result.ball.z - 1.4) * 3
        + random.next() * (11 - skill) * 3;
      candidates.push({ shot, score });
    }
  }
  const selected = candidates.sort((left, right) => left.score - right.score)[0]!;
  const error = (11 - skill) * 0.006;
  const shot = {
    angle: selected.shot.angle + (random.next() - 0.5) * error,
    power: Math.max(1, selected.shot.power + (random.next() - 0.5) * error * 10),
  };
  const target = rankedTarget(bot, players);
  let powerUp: BotDecision['powerUp'];
  if (bot.inventory && target && skill >= 5 && random.chance(0.18 + skill * 0.025)) {
    powerUp = bot.inventory === 'turbo' || bot.inventory === 'shield' ? { type: bot.inventory } : { type: bot.inventory, targetId: target.id };
  }
  return { shot, powerUp, confidence: Math.max(0, 1 - selected.score / 150) };
};
