import { distanceToCup, simulateShot, type BallPhysicsModifiers } from './physics';
import { Random } from './random';
import type { Course, Player, PowerUp, ShotCommand } from './types';

export interface BotDecision {
  shot: ShotCommand;
  powerUp?: { type: PowerUp; targetId?: string; portalExitId?: string };
  secondWind?: boolean;
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

const modifiersFor = (player: Player): BallPhysicsModifiers => ({
  mass: player.upgrades.includes('heavy ball') && player.ballForm === 'heavy' ? 1.9 : player.ballForm === 'heavy' ? 1.65 : player.upgrades.includes('heavy ball') ? 1.45 : 1,
  iceSkates: player.upgrades.includes('ice skates') || player.ballForm === 'ice',
  bankShot: player.upgrades.includes('bank shot'),
  bouncy: player.ballForm === 'bouncy',
  ghostBall: player.ballForm === 'ghost',
  magnetBall: player.ballForm === 'magnet',
  portalExitId: player.ballForm === 'portal' ? player.portalExitId : undefined,
  portalSpeedMultiplier: player.upgrades.includes('portal savvy') ? 1.18 : 1,
  hazardShield: player.hazardShield,
});

const bestPortalExit = (course: Course) => course.portals?.filter((pair) => pair.exit).map((pair) => ({ id: `${pair.id}:exit`, distance: Math.hypot(course.cup.x - pair.exit!.point.x, course.cup.y - pair.exit!.point.y) })).sort((left, right) => left.distance - right.distance)[0]?.id;

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
      const result = simulateShot(course, bot.ball, shot, undefined, { phase, modifiers: modifiersFor(bot) });
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
  const held = bot.inventory ?? bot.spareInventory;
  if (held && skill >= 5 && random.chance(0.18 + skill * 0.025)) {
    if (held === 'turbo' || held === 'shield' || held === 'two putts' || held === 'heavy' || held === 'bouncy' || held === 'ghost' || held === 'magnet' || held === 'ice') powerUp = { type: held };
    else if (held === 'portal') {
      const portalExitId = bestPortalExit(course);
      if (portalExitId) powerUp = { type: held, portalExitId };
    } else if (target) powerUp = { type: held, targetId: target.id };
  }
  return { shot, powerUp, secondWind: bot.secondWindAvailable && !bot.twoPuttsArmed && skill >= 6 && random.chance(.45), confidence: Math.max(0, 1 - selected.score / 150) };
};
